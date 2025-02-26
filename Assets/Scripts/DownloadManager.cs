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
using Unity.Template.VR;

/// <summary>
/// Note - needs to start disabled, to avoid double-initialization
/// </summary>
public class DownloadManager : MonoBehaviour
{
    public DisplayManager displayManager;
    public SRLogHandler logger;
    public CustomFileManagerBehaviour customFileManager;
    public DownloadFilters downloadFilters;

    private bool _isDownloading = false;
    private CustomMapRepoTorrent _customMapRepo;
    private DownloadManagerZ _downloadManagerZ;

    private void Awake()
    {
        _customMapRepo = new CustomMapRepoTorrent(logger);
        _downloadManagerZ = new DownloadManagerZ(logger, customFileManager);
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
        if (_isDownloading) {
            logger.DebugLog("Already downloading!");
            return;
        }

        _isDownloading = true;
        displayManager.DisableActions("Downloading...");

        try {
            var nowUtc = DateTime.UtcNow;
            var cutoffTimeUtc = downloadFilters.GetDateCutoffFromCurrentSelection(nowUtc);
            logger.DebugLog($"Using cutoff time (local) {cutoffTimeUtc.ToLocalTime()}");
            
            // TODO get difficulty info somewhere. For now, use all difficulties
            var difficultySelections = downloadFilters.GetDifficultiesEnabled();
            logger.DebugLog("Using difficulties " + String.Join(",", difficultySelections));
            
            // First, try Z download
            var success = await TryDownloadZ(cutoffTimeUtc, difficultySelections);
            if (!success)
            {
                // Fallback on torrent
                success = await TryDownloadTorrent(nowUtc, difficultySelections);
            }
            
            if (success)
            {
                Preferences.SetLastDownloadedTime(nowUtc);
                customFileManager.SetLastDownloadedTime(nowUtc);
                displayManager.UpdateLastFetchTime();
            }
        } catch (Exception e) {
            logger.ErrorLog("Failed to download: " + e.Message);
        }

        logger.DebugLog("Finished downloading");

        _isDownloading = false;
        displayManager.EnableActions();
    }

    /// <summary>
    /// Tries to download songs from the Z site after the given time w/ difficulty filter
    /// </summary>
    /// <param name="cutoffTimeUtc"></param>
    /// <param name="difficultySelections"></param>
    /// <returns></returns>
    private async Task<bool> TryDownloadZ(DateTime cutoffTimeUtc, List<string> difficultySelections)
    {
        logger.DebugLog("Attempting to download from Z...");
        return await _downloadManagerZ.DownloadSongsSinceTime(cutoffTimeUtc, difficultySelections);
    }
    
    /// <summary>
    /// Tries to download songs from the torrent after the given time w/ difficulty filter
    /// </summary>
    /// <param name="cutoffTimeUtc"></param>
    /// <param name="difficultySelections"></param>
    /// <returns></returns>
    private async Task<bool> TryDownloadTorrent(DateTime cutoffTimeUtc, List<string> difficultySelections)
    {
        logger.DebugLog("Attempting to download from torrent...");
        // TODO get difficulty info to filter from torrent as well
        var diffSet = new HashSet<string>(difficultySelections);
        var downloadedMaps = await _customMapRepo.DownloadMaps(null, cutoffTimeUtc);
        var success = downloadedMaps != null;
        return success;
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


}
