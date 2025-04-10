using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SRTimestampLib;
using SRTimestampLib.Models;
using UnityEngine.Networking;

namespace Unity.Template.VR
{
    /// <summary>
    /// Handles map downloads through the Z site
    /// </summary>
    public class DownloadManagerZ
    {
        public SRLogHandler logger;
        public CustomFileManagerBehaviour customFileManager;
        
        private bool isDownloading = false;

        protected readonly int GET_PAGE_TIMEOUT_SEC = 3; // In case the site is down, fail quick
        protected readonly int GET_MAP_TIMEOUT_SEC = 60;
        protected readonly int PARALLEL_DOWNLOAD_LIMIT = 10;

        public DownloadManagerZ(SRLogHandler logger, CustomFileManagerBehaviour customFileManager)
        {
            this.logger = logger;
            this.customFileManager = customFileManager;
        }

        public virtual string GetDownloadUrl(MapItem map)
        {
            return "https://synthriderz.com" + map.download_url;
        }

        public virtual string MapPageUrl => "https://synthriderz.com/api/beatmaps";
        
        /// <summary>
        /// Gets all map metadata from the site and uses that to fix local timestamps
        /// </summary>
        /// <param name="selectedDifficulties"></param>
        /// <returns>False on failure, true on success (even if none were changed)</returns>
        public async Task<bool> ApplyTimestampFixes(List<string> selectedDifficulties)
        {
            try {
                var sinceTime = DateTime.UnixEpoch;

                logger.DebugLog("Getting all map metadata from site");
                var mapsFromZ = await GetMapsSinceTimeForDifficulties(sinceTime, selectedDifficulties);
                await Task.Yield();

                // Couldn't get Z data
                if (mapsFromZ == null)
                    return false;

                logger.DebugLog("Fixing local files...");
                var notFoundLocally = 0;
                var numProcessed = 0;
                foreach (var mapFromZ in mapsFromZ) {
                    var localMetadata = customFileManager.GetFromHash(mapFromZ.hash);
                    if (localMetadata == null || string.IsNullOrEmpty(localMetadata.FilePath)) {
                        notFoundLocally++;
                        // logger.DebugLog($"Map id {mapFromZ.id} not found locally, skipping");
                    } else {
                        var fileName = Path.GetFileName(localMetadata.FilePath);
                        var publishedAtUtc = mapFromZ.GetPublishedAtUtc();
                        if (publishedAtUtc == null) {
                            logger.DebugLog($"Couldn't parse published_at timestamp {mapFromZ.published_at}");
                        } else {
                            logger.DebugLog($"Setting timestamp for {fileName} to {publishedAtUtc}");
                            FileUtils.TrySetDateModifiedUtc(localMetadata.FilePath, publishedAtUtc.GetValueOrDefault(), logger);
                        }

                        numProcessed++;
                        
                        // Don't hog main thread
                        if (numProcessed % 20 == 0)
                        {
                            await Task.Yield();
                        }
                        
                        // Let user know work is happening
                        if (numProcessed % 100 == 0)
                        {
                            logger.DebugLog($"  Processed {numProcessed}/{mapsFromZ.Count}");
                        }
                    }
                }

                logger.DebugLog($"{notFoundLocally} files from Z not found locally and skipped");
                logger.DebugLog("Finished correcting timestamps");

                return true;
            } catch (Exception e) {
                logger.ErrorLog("Failed to fix timestamps: " + e.Message);
                return false;
            }
        }

        /// <summary>
        /// Tries to download all songs from Z with any of the given difficulties and published after the given time.
        /// </summary>
        /// <param name="sinceTime"></param>
        /// <param name="selectedDifficulties"></param>
        /// <returns>True if successfully downloaded (or attempted but none to download). False on failure (i.e. Z is down)</returns>
        public async Task<bool> DownloadSongsSinceTime(DateTimeOffset sinceTime, List<string> selectedDifficulties) {
            logger.DebugLog($"Getting maps after time {sinceTime.ToLocalTime()}...");

            var tempDir = Path.Join(FileUtils.TempPath, "Download");
            try {
                // Delete anything from previous runs
                if (Directory.Exists(tempDir)) {
                    logger.DebugLog($"Clearing temp directory at {tempDir}");
                    Directory.Delete(tempDir, true);
                }

                // Recreate
                logger.DebugLog($"(re)Creating temp directory at {tempDir}");
                Directory.CreateDirectory(tempDir);
            }
            catch (System.Exception e) {
                logger.ErrorLog("Failed to delete or create temp dir: " + e.Message);
                return false;
            }
            
            try {
                List<MapItem> mapsFromSite = await GetMapsSinceTimeForDifficulties(sinceTime, selectedDifficulties);
                if (mapsFromSite == null)
                {
                    return false;
                }
                
                logger.DebugLog($"{mapsFromSite.Count} maps found since given time for given difficulties.");

                var mapsToDownload = customFileManager.FilterOutExistingMaps(mapsFromSite);
                logger.DebugLog($"{mapsToDownload.Count} new files to download...");

                int count = 1;
                var downloadTasks = new Queue<Task<bool>>();
                foreach (MapItem map in mapsToDownload) {
                    logger.DebugLog($"{count}/{mapsToDownload.Count}: {map.id} {map.title}");

                    downloadTasks.Enqueue(DownloadMap(map, tempDir));
                    count++;

                    // Limit the number of simultaneous downloads
                    while (downloadTasks.Count > PARALLEL_DOWNLOAD_LIMIT) {
                        logger.DebugLog($"More than {PARALLEL_DOWNLOAD_LIMIT} downloads queued, waiting...");
                        // This is safe since we aren't adding any more to this while we wait
                        await downloadTasks.Dequeue();
                    }

                    // Every so often for bulk downloads, save the database
                    if (count % 100 == 0) {
                        logger.DebugLog("Stopping new queued downloads...");
                        while (downloadTasks.Count > 0) {
                            await downloadTasks.Dequeue();
                        }

                        logger.DebugLog("Saving current state to db...");
                        await customFileManager.Save();

                        logger.DebugLog("Resuming...");
                    }
                }

                // Wait for last downloads
                logger.DebugLog("Waiting for last downloads...");
                while (downloadTasks.Count > 0) {
                    await downloadTasks.Dequeue();
                }
                logger.DebugLog("Done waiting");

                logger.DebugLog("Trying to save database...");
                await customFileManager.Save();
                logger.DebugLog("Done");
            }
            catch (System.Exception e) {
                logger.ErrorLog($"Failed to download maps: {e.Message}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Downloads the given MapItem to the destination directory
        /// Returns true if successful, false if not
        /// </summary>
        /// <param name="map"></param>
        /// <param name="destDir"></param>
        /// <returns></returns>
        public async Task<bool> DownloadMap(MapItem map, string destDir)
        {
            var fullUrl = GetDownloadUrl(map);
            var destPath = Path.Join(destDir, map.filename);

            try {
                var getRequest = UnityWebRequest.Get(new Uri(fullUrl));
                getRequest.timeout = GET_MAP_TIMEOUT_SEC;
                var asyncOp = getRequest.SendWebRequest();
                var startTime = DateTime.Now;
                var timeoutTime = startTime.AddSeconds(GET_MAP_TIMEOUT_SEC);
                while (!asyncOp.isDone) {
                    if (DateTime.Now > timeoutTime) {
                        logger.ErrorLog("Timed out waiting for map download!");
                        return false;
                    }
                    else {
                        await Task.Delay(50);
                    }
                }
                if (!string.IsNullOrEmpty(asyncOp.webRequest.error)) {
                    logger.ErrorLog($"Error getting request ({asyncOp.webRequest.responseCode}): {asyncOp.webRequest.error}");
                    return false;
                }

                byte[] rawResponse = getRequest.downloadHandler.data;
                if (rawResponse == null) {
                    logger.ErrorLog("Null response from server!");
                    return false;
                }

                logger.DebugLog("Saving to file...");
                if (false == await FileUtils.WriteToFile(rawResponse, destPath, logger)) {
                    return false;
                }

                logger.DebugLog("Moving to SynthRiders directory...");
                var finalPath = customFileManager.MoveCustomSong(destPath, map.GetPublishedAtUtc());

                logger.DebugLog("Success!");
                await customFileManager.AddLocalMap(finalPath, map);
                
                // Wait for all other operations to finish, then ensure the timestamp has been set
                await Task.Yield();
                if (map.GetPublishedAtUtc().HasValue)
                {
                    FileUtils.TrySetDateModifiedUtc(finalPath, map.GetPublishedAtUtc().GetValueOrDefault(), logger);
                }

                return true;
            }
            catch (System.Exception e) {
                logger.ErrorLog($"Failed to download map {map.id} ({map.title}): {e.Message}");
            }

            return false;
        }

        /// <summary>
        /// Gets one page of map metdata, with the given filters
        /// </summary>
        /// <param name="pageSize"></param>
        /// <param name="pageIndex"></param>
        /// <param name="sinceTime"></param>
        /// <param name="includedDifficulties"></param>
        /// <returns></returns>
        public virtual async Task<MapPage> GetMapPage(int pageSize, int pageIndex, DateTimeOffset sinceTime, List<string> includedDifficulties)
        {
            var apiEndpoint = MapPageUrl;
            var sort = "published_at,DESC";

            JArray searchParameters = new JArray();

            var publishedDateRange = new JObject(
                new JProperty("published_at",
                    new JObject(
                        new JProperty("$gt",
                            new JValue(sinceTime.UtcDateTime)
                        )
                    )
                )
            );

            var beatSaberConvert = new JObject(
                new JProperty("beat_saber_convert",
                    new JObject(
                        new JProperty("$ne",
                            new JValue(true)
                        )
                    )
                )
            );

            var difficultyFilter = new JObject(
                new JProperty("difficulties",
                    new JObject(
                        new JProperty("$jsonContainsAny",
                            new JArray(includedDifficulties)
                        )
                    )
                )
            );

            searchParameters.Add(publishedDateRange);
            searchParameters.Add(beatSaberConvert);
            searchParameters.Add(difficultyFilter);

            var searchFilter = new JObject(
                new JProperty("$and", searchParameters)
            );

            string request = $"{apiEndpoint}?sort={sort}&limit={pageSize}&page={pageIndex}&s={searchFilter.ToString(Formatting.None)}";
            var requestUri = new Uri(request);
            string rawPage = null;
            try {
                var getRequest = UnityWebRequest.Get(requestUri);
                getRequest.timeout = GET_PAGE_TIMEOUT_SEC;
                var asyncOp = getRequest.SendWebRequest();
                var startTime = DateTime.Now;
                var timeoutTime = startTime.AddSeconds(GET_PAGE_TIMEOUT_SEC);
                while (!asyncOp.isDone) {
                    if (DateTime.Now > timeoutTime) {
                        logger.ErrorLog("Timed out waiting for page!");
                        return null;
                    }
                    else {
                        await Task.Delay(10);
                    }
                }
                if (!string.IsNullOrEmpty(asyncOp.webRequest.error)) {
                    logger.ErrorLog("Error getting request: " + asyncOp.webRequest.error);
                    return null;
                }
                rawPage = getRequest.downloadHandler.text;
            }
            catch (System.Exception e) {
                logger.ErrorLog($"Failed to get web page: {e.Message}");
                return null;
            }

            logger.DebugLog("Deserializing page...");
            try {
                MapPage page = JsonConvert.DeserializeObject<MapPage>(rawPage);
                return page;
            }
            catch (System.Exception e) {
                logger.ErrorLog($"Failed to deserialize map page: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets all maps since the given time with one of the given difficulties.
        /// Returns an empty list if none found, and null if an error occurred.
        /// </summary>
        /// <param name="sinceTime"></param>
        /// <param name="selectedDifficulties"></param>
        /// <returns></returns>
        [ItemCanBeNull]
        public async Task<List<MapItem>> GetMapsSinceTimeForDifficulties(DateTimeOffset sinceTime, List<string> selectedDifficulties) {
            var maps = new List<MapItem>();

            int numPages = 1;
            int pageSize = 50;
            int pageIndex = 1;

            do
            {
                if (pageIndex == 1) {
                    logger.DebugLog("Requesting first page");
                }
                else {
                    logger.DebugLog($"Requesting page {pageIndex}/{numPages}");
                }

                MapPage page = await GetMapPage(pageSize, pageIndex, sinceTime, selectedDifficulties);
                if (page == null) {
                    logger.ErrorLog($"Returned null page {pageIndex}! Aborting.");
                    return null;
                }

                logger.DebugLog($"Page {pageIndex} retrieved");
                
                logger.DebugLog($"{page.data.Count} elements");
                foreach (MapItem item in page.data)
                {
                    // For now, don't care about existing files, just overwrite them
                    maps.Add(item);
                }

                // Set total page count from responses
                numPages = page.pagecount;

                pageIndex++;
            }
            while (pageIndex <= numPages);

            return maps;
        }
    }
}