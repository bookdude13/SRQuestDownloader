using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using com.cyborgAssets.inspectorButtonPro;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SQLite;
using SRCustomLib;
using SRTimestampLib;
using SRTimestampLib.Models;

/// <summary>
/// Note - needs to start disabled, to avoid double-initialization
/// </summary>
public class DownloadManager : MonoBehaviour
{
    public DisplayManager displayManager;
    public SRLogHandler logger;
    public CustomFileManagerBehaviour customFileManager;
    public DownloadFilters downloadFilters;
    private bool isDownloading = false;
    private readonly int GET_PAGE_TIMEOUT_SEC = 3; // In case the site is down, fail quick
    private readonly int GET_MAP_TIMEOUT_SEC = 60;
    private readonly int PARALLEL_DOWNLOAD_LIMIT = 10;
    private CustomMapRepoTorrent _customMapRepo;

    private void Awake()
    {
        _customMapRepo = new CustomMapRepoTorrent(logger);
    }

    private async void OnEnable()
    {
        await customFileManager.Initialize();
        
        displayManager.DisableActions("Initializing maps source...");

        logger.DebugLog("Setting up custom map source...");
        await _customMapRepo.Initialize();
        
        // Start with a clean download dir, so everything can be moved over via the torrent itself
        FileUtils.EmptyDirectory(FileUtils.TorrentDownloadDirectory);
        
        displayManager.EnableActions();
    }

    public async void StartDownloading() {
        if (isDownloading) {
            logger.DebugLog("Already downloading!");
            return;
        }

        isDownloading = true;
        displayManager.DisableActions("Downloading...");

        try {
            var nowUtc = DateTime.UtcNow;
            var cutoffTimeUtc = downloadFilters.GetDateCutoffFromCurrentSelection(nowUtc);
            logger.DebugLog($"Using cutoff time (local) {cutoffTimeUtc.ToLocalTime()}");
            
            // TODO get difficulty info somewhere. For now, use all difficulties
            var difficultySelections = downloadFilters.GetDifficultiesEnabled();
            // logger.DebugLog("Using difficulties " + String.Join(",", difficultySelections));
            var diffSet = new HashSet<string>(difficultySelections);
            // var success = await DownloadSongsSinceTime(cutoffTimeUtc, difficultySelections);
            
            var downloadedMaps = await _customMapRepo.DownloadMaps(null, cutoffTimeUtc);
            if (downloadedMaps.Count > 0) {
                Preferences.SetLastDownloadedTime(nowUtc);
                customFileManager.SetLastDownloadedTime(nowUtc);
                displayManager.UpdateLastFetchTime();
            }
        } catch (Exception e) {
            logger.ErrorLog("Failed to download: " + e.Message);
        }

        logger.DebugLog("Finished downloading");

        isDownloading = false;
        displayManager.EnableActions();
    }

    /// Update local map timestamps to match the Z site published_at,
    /// to allow for correct sorting by timestamp in-game
    [ProPlayButton]
    public async void FixMapTimestamps() {
        logger.DebugLog("Fixing map timestamp...");

        displayManager.DisableActions("Fixing Timestamps...");

        // First, try to fix timestamps with local file
        logger.DebugLog("  Trying local fixes...");
        await customFileManager.ApplyLocalTimestampMappings();

        // Use Z for any others
        logger.DebugLog("  Z check disabled for now. Not trying Z fixes.");
        // await FixMapsUsingZ();
        
        // Finally, update SynthDB so the next load is correct and doesn't need a slow reload of customs
        logger.DebugLog("  Refreshing SynthDB timestamps...");
        await UpdateSynthDBTimestamps();

        logger.DebugLog("Done");
        displayManager.EnableActions();
    }

    /// <summary>
    /// Updates SynthDB with current file timestamps
    /// </summary>
    [ProPlayButton]
    private async Task UpdateSynthDBTimestamps() => await customFileManager.UpdateSynthDBTimestamps();

#region Old Z Site
    private async Task FixMapsUsingZ()
    {
        try {
            var sinceTime = DateTime.UnixEpoch;
            var selectedDifficulties = downloadFilters.GetAllDifficulties();

            logger.DebugLog("Getting all maps from Z");
            var mapsFromZ = await GetMapsSinceTimeForDifficulties(sinceTime, selectedDifficulties);

            await Task.Yield();

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
        } catch (Exception e) {
            logger.ErrorLog("Failed to fix timestamps: " + e.Message);
        }
    }

    private async Task<bool> DownloadSongsSinceTime(DateTimeOffset sinceTime, List<string> selectedDifficulties) {
        logger.DebugLog($"Getting maps after time {sinceTime.ToLocalTime()}...");

        var tempDir = Path.Join(Application.temporaryCachePath, "Download");
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
            List<MapItem> mapsFromZ = await GetMapsSinceTimeForDifficulties(sinceTime, selectedDifficulties);
            logger.DebugLog($"{mapsFromZ.Count} maps in Z found since given time for given difficulties.");

            var mapsToDownload = customFileManager.FilterOutExistingMaps(mapsFromZ);
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

    /// Downloads the given MapItem to the destination directory
    /// Returns true if successful, false if not
    private async Task<bool> DownloadMap(MapItem map, string destDir) {
        var fullUrl = "https://synthriderz.com" + map.download_url;
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

            return true;
        }
        catch (System.Exception e) {
            logger.ErrorLog($"Failed to download map {map.id} ({map.title}): {e.Message}");
        }

        return false;
    }

    private async Task<MapPage> GetMapPage(int pageSize, int pageIndex, DateTimeOffset sinceTime, List<string> includedDifficulties) {
        var apiEndpoint = "https://synthriderz.com/api/beatmaps";
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

    private async Task<List<MapItem>> GetMapsSinceTimeForDifficulties(DateTimeOffset sinceTime, List<string> selectedDifficulties) {
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
                break;
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
#endregion
}
