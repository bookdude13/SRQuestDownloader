using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class DownloadManager : MonoBehaviour
{
    public DisplayManager displayManager;
    public CustomFileManager customFileManager;
    public DownloadFilters downloadFilters;
    private bool isDownloading = false;
    private readonly int GET_PAGE_TIMEOUT_SEC = 30;
    private readonly int GET_MAP_TIMEOUT_SEC = 60;
    private readonly int PARALLEL_DOWNLOAD_LIMIT = 10;

    public async void StartDownloading() {
        if (isDownloading) {
            displayManager.DebugLog("Already downloading!");
            return;
        }

        isDownloading = true;
        displayManager.DisableActions("Downloading...");

        try {
            var nowUtc = DateTime.UtcNow;
            var cutoffTimeUtc = downloadFilters.GetDateCutoffFromCurrentSelection(nowUtc);
            displayManager.DebugLog($"Using cutoff time (local) {cutoffTimeUtc.ToLocalTime()}");
            var difficultySelections = downloadFilters.GetDifficultiesEnabled();
            displayManager.DebugLog("Using difficulties " + String.Join(",", difficultySelections));
            var success = await DownloadSongsSinceTime(cutoffTimeUtc, difficultySelections);
            if (success) {
                Preferences.SetLastDownloadedTime(nowUtc);
                displayManager.UpdateLastFetchTime();
            }
        } catch (Exception e) {
            displayManager.ErrorLog("Failed to download: " + e.Message);
        }

        displayManager.DebugLog("Finished downloading");

        isDownloading = false;
        displayManager.EnableActions();
    }

    /// Update local map timestamps to match the Z site published_at,
    /// to allow for correct sorting by timestamp in-game
    public async void FixMapTimestamps() {
        displayManager.DebugLog("Fixing map timestamp...");

        displayManager.DisableActions();

        try {
            var sinceTime = DateTime.UnixEpoch;
            var selectedDifficulties = downloadFilters.GetAllDifficulties();

            displayManager.DebugLog("Getting all maps from Z");
            List<MapItem> mapsFromZ = await GetMapsSinceTimeForDifficulties(sinceTime, selectedDifficulties);

            displayManager.DebugLog("Fixing local files...");
            var notFoundLocally = 0;
            foreach (var mapFromZ in mapsFromZ) {
                MapZMetadata localMetadata = customFileManager.db.GetFromHash(mapFromZ.hash);
                if (localMetadata == null) {
                    notFoundLocally++;
                    // displayManager.DebugLog($"Map id {mapFromZ.id} not found locally, skipping");
                } else {
                    var fileName = Path.GetFileName(localMetadata.FilePath);
                    var publishedAtUtc = mapFromZ.GetPublishedAtUtc();
                    if (publishedAtUtc == null) {
                        displayManager.DebugLog($"Couldn't parse published_at timestamp {mapFromZ.published_at}");
                    } else {
                        displayManager.DebugLog($"Setting timestamp for {fileName} to {publishedAtUtc}");
                        FileUtils.SetDateModifiedUtc(localMetadata.FilePath, publishedAtUtc.GetValueOrDefault(), displayManager);
                    }
                }
            }

            displayManager.DebugLog($"{notFoundLocally} files from Z not found locally and skipped");
            displayManager.DebugLog("Finished correcting timestamps");            
        } catch (Exception e) {
            displayManager.ErrorLog("Failed to fix timestamps: " + e.Message);
        }

        displayManager.EnableActions();
    }

    private async Task<bool> DownloadSongsSinceTime(DateTimeOffset sinceTime, List<string> selectedDifficulties) {
        displayManager.DebugLog($"Getting maps after time {sinceTime.ToLocalTime()}...");

        var tempDir = Path.Join(Application.temporaryCachePath, "Download");
        try {
            // Delete anything from previous runs
            if (Directory.Exists(tempDir)) {
                displayManager.DebugLog($"Clearing temp directory at {tempDir}");
                Directory.Delete(tempDir, true);
            }

            // Recreate
            displayManager.DebugLog($"(re)Creating temp directory at {tempDir}");
            Directory.CreateDirectory(tempDir);
        }
        catch (System.Exception e) {
            displayManager.ErrorLog("Failed to delete or create temp dir: " + e.Message);
            return false;
        }
        
        try {
            List<MapItem> mapsFromZ = await GetMapsSinceTimeForDifficulties(sinceTime, selectedDifficulties);
            displayManager.DebugLog($"{mapsFromZ.Count} maps in Z found since given time for given difficulties.");

            var mapsToDownload = customFileManager.FilterOutExistingMaps(mapsFromZ);
            displayManager.DebugLog($"{mapsToDownload.Count} new files to download...");

            int count = 1;
            var downloadTasks = new Queue<Task<bool>>();
            foreach (MapItem map in mapsToDownload) {
                displayManager.DebugLog($"{count}/{mapsToDownload.Count}: {map.id} {map.title}");

                downloadTasks.Enqueue(DownloadMap(map, tempDir));
                count++;

                // Limit the number of simultaneous downloads
                while (downloadTasks.Count > PARALLEL_DOWNLOAD_LIMIT) {
                    displayManager.DebugLog($"More than {PARALLEL_DOWNLOAD_LIMIT} downloads queued, waiting...");
                    // This is safe since we aren't adding any more to this while we wait
                    await downloadTasks.Dequeue();
                }

                // Every so often for bulk downloads, save the database
                if (count % 100 == 0) {
                    displayManager.DebugLog("Stopping new queued downloads...");
                    while (downloadTasks.Count > 0) {
                        await downloadTasks.Dequeue();
                    }

                    displayManager.DebugLog("Saving current state to db...");
                    await customFileManager.db.Save();

                    displayManager.DebugLog("Resuming...");
                }
            }

            // Wait for last downloads
            displayManager.DebugLog("Waiting for last downloads...");
            while (downloadTasks.Count > 0) {
                await downloadTasks.Dequeue();
            }
            displayManager.DebugLog("Done waiting");

            displayManager.DebugLog("Trying to save database...");
            await customFileManager.db.Save();
            displayManager.DebugLog("Done");
        }
        catch (System.Exception e) {
            displayManager.ErrorLog($"Failed to download maps: {e.Message}");
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
                    displayManager.ErrorLog("Timed out waiting for map download!");
                    return false;
                }
                else {
                    await Task.Delay(50);
                }
            }
            if (!string.IsNullOrEmpty(asyncOp.webRequest.error)) {
                displayManager.ErrorLog($"Error getting request ({asyncOp.webRequest.responseCode}): {asyncOp.webRequest.error}");
                return false;
            }

            byte[] rawResponse = getRequest.downloadHandler.data;
            if (rawResponse == null) {
                displayManager.ErrorLog("Null response from server!");
                return false;
            }

            displayManager.DebugLog("Saving to file...");
            if (false == await FileUtils.WriteToFile(rawResponse, destPath, displayManager)) {
                return false;
            }

            displayManager.DebugLog("Moving to SynthRiders directory...");
            var finalPath = customFileManager.MoveCustomSong(destPath, map.GetPublishedAtUtc());

            displayManager.DebugLog("Success!");
            customFileManager.AddLocalMap(finalPath, map);

            return true;
        }
        catch (System.Exception e) {
            displayManager.ErrorLog($"Failed to download map {map.id} ({map.title}): {e.Message}");
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
                    displayManager.ErrorLog("Timed out waiting for page!");
                    return null;
                }
                else {
                    await Task.Delay(10);
                }
            }
            if (!string.IsNullOrEmpty(asyncOp.webRequest.error)) {
                displayManager.ErrorLog("Error getting request: " + asyncOp.webRequest.error);
                return null;
            }
            rawPage = getRequest.downloadHandler.text;
        }
        catch (System.Exception e) {
            displayManager.ErrorLog($"Failed to get web page: {e.Message}");
            return null;
        }

        displayManager.DebugLog("Deserializing page...");
        try {
            MapPage page = JsonConvert.DeserializeObject<MapPage>(rawPage);
            return page;
        }
        catch (System.Exception e) {
            displayManager.ErrorLog($"Failed to deserialize map page: {e.Message}");
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
                displayManager.DebugLog("Requesting first page");
            }
            else {
                displayManager.DebugLog($"Requesting page {pageIndex}/{numPages}");
            }

            MapPage page = await GetMapPage(pageSize, pageIndex, sinceTime, selectedDifficulties);
            if (page == null) {
                displayManager.ErrorLog($"Returned null page {pageIndex}! Aborting.");
                break;
            }

            displayManager.DebugLog($"Page {pageIndex} retrieved");
            
            displayManager.DebugLog($"{page.data.Count} elements");
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
