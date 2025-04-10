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

    public bool UseZ = true;
    public bool UseSyn = true;
    public bool UseMagnet = true;

    private bool _isDownloading = false;
    private MapRepo _customMapRepo;
    private DownloadManagerZ _downloadManagerZ;
    private DownloadManagerSyn _downloadManagerSyn;

    private void Awake()
    {
        _customMapRepo = new MapRepo(logger, UseZ, UseSyn, UseMagnet, customFileManager.FileManager);
        _downloadManagerZ = new DownloadManagerZ(logger, customFileManager.FileManager);
        _downloadManagerSyn = new DownloadManagerSyn(logger, customFileManager.FileManager);
    }

    private async void OnEnable()
    {
        displayManager.DisableActions("Initializing maps source...");

        await _customMapRepo.Initialize();
        
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

            bool success = await _customMapRepo.TryDownloadWithFallbacks(cutoffTimeUtc, difficultySelections, Application.exitCancellationToken);
            if (success)
            {
                Preferences.SetLastDownloadedTime(nowUtc);
                customFileManager.SetLastDownloadedTime(nowUtc);
                displayManager.UpdateLastFetchTime();
            }
        }
        catch (Exception e)
        {
            logger.ErrorLog("Failed to download: " + e.Message);
        }

        logger.DebugLog("Finished downloading");

        _isDownloading = false;

        displayManager.EnableActions();
    }

    /// Update local map timestamps to match the Z site published_at,
    /// to allow for correct sorting by timestamp in-game
    [ProPlayButton]
    public async void FixMapTimestamps()
    {
        logger.DebugLog("Fixing map timestamp...");

        displayManager.DisableActions("Fixing Timestamps...");
        
        // First, try to fix timestamps with local file
        logger.DebugLog("  Trying local fixes...");
        await customFileManager.ApplyLocalTimestampMappings();

        // Use Z/Syn for any others
        List<string> difficultySelections = downloadFilters.GetDifficultiesEnabled();
        logger.DebugLog("  Getting online fixes...");
        bool success = UseZ && await _downloadManagerZ.ApplyTimestampFixes(difficultySelections, Application.exitCancellationToken);
        if (!success)
        {
            logger.DebugLog("  Not getting fixes from Z. Trying synplicity...");
            success = UseSyn && await _downloadManagerSyn.ApplyTimestampFixes(difficultySelections, Application.exitCancellationToken);
            if (!success)
            {
                logger.ErrorLog("Failed to download timestamp fixes!");
            }
        }
        
        // Finally, update SynthDB so the next load is correct and doesn't need a slow reload of customs
        if (success)
        {
            logger.DebugLog("  Refreshing SynthDB timestamps...");
            await UpdateSynthDBTimestamps();
        }

        logger.DebugLog("Done");
        displayManager.EnableActions();
    }

    /// <summary>
    /// Updates SynthDB with current file timestamps
    /// </summary>
    [ProPlayButton]
    private async Task UpdateSynthDBTimestamps() => await customFileManager.UpdateSynthDBTimestamps();
}
