using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TMPro;
using System.IO.Compression;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json.Linq;
using System;
using JetBrains.Annotations;
using SRTimestampLib;
using SRTimestampLib.Models;
using Unity.Template.VR;

public class CustomFileManagerBehaviour : MonoBehaviour
{
    public DisplayManager displayManager;
    public SRLogHandler logger;
    private CustomFileManager _customFileManager;

    private bool isMovingFiles = false;
    public readonly static string synthCustomContentDir = "/sdcard/SynthRidersUC/";

    private readonly string MAP_EXTENSION = ".synth";
    private readonly HashSet<string> STAGE_EXTENSIONS = new()
    {
        ".stagequest", // Old quest stages, still used for Q1 and Pico
        ".spinstagequest", // Old quest spin stages, still used for Q1 and Pico
        ".stagedroid" // Q2+ stage files, used for both spin and non-spin stages
    };
    private readonly string PLAYLIST_EXTENSION = ".playlist";


    private void Awake()
    {
        displayManager.DisableActions("Loading Local Maps...");
        _customFileManager = new CustomFileManager(logger);
    }

    private async void Start()
    {
        await _customFileManager.Initialize();
        displayManager.EnableActions();
        
        // Immediately migrate old playlist files
        MigratePlaylistsForMixedRealityUpdate();
    }

    public async Task ApplyLocalTimestampMappings()
    {
        // First, try to fix timestamps with local file
        // Note - getting this via addressables instead of the non-Unity lookup approach
        // var localMappings = _customFileManager.GetLocalTimestampMappings();
        var localMappingsRaw = await AddressableUtil.LoadAndParseText<List<MapItem>>("sr_timestamp_mapping");
        if (localMappingsRaw == null || localMappingsRaw.Count == 0)
        {
            logger.ErrorLog("Failed to get mappings from file!");
            return;
        }

        var localMappings = new LocalMapTimestampMappings();
        foreach (var mapping in localMappingsRaw)
        {
            //logger.DebugLog($"Mapping {mapping.hash} {mapping.modified_time} {mapping.modified_time_formatted}");
            localMappings.Add(mapping);
        }

        logger.DebugLog($"{localMappings.MapTimestamps.Count} mappings found");
        
        // Apply saved timestamp values to all local files
        await _customFileManager.ApplyLocalMappings(localMappings);
    }

    /// Parses the map at the given path and adds it to the collection
    public async Task AddLocalMap(string mapPath, MapItem mapFromZ) => await _customFileManager.AddLocalMap(mapPath, mapFromZ);

    public async Task Save() => await _customFileManager.db.Save();

    public async Task UpdateSynthDBTimestamps() =>
        await _customFileManager.UpdateSynthDBTimestamps(_customFileManager.db.GetLocalMapsCopy());

    [CanBeNull] public MapZMetadata GetFromHash(string hash) => _customFileManager.db.GetFromHash(hash);

    /// Useful for debugging. Clear out all custom songs
    public async void DeleteCustomSongs() {
        try {
            var mapFiles = GetSynthriderzMapFiles(Path.Join(synthCustomContentDir, "CustomSongs"));
            logger.DebugLog($"Deleting {mapFiles.Length} files...");
            foreach (var filePath in mapFiles) {
                File.Delete(filePath);
            }
        }
        catch (System.Exception e) {
            logger.ErrorLog($"Failed to delete files: {e.Message}");
        }

        await RefreshLocalDatabase();
    }

    public void StartMoveDownloadedFiles()
    {        
        StartCoroutine(MoveDownloadedFiles());
    }

    /// Returns list of all zip files downloaded from synthriderz.com located in the given directory.
    /// If none found or error occurs, returns empty array
    private string[] GetSynthriderzZipFiles(string rootDirectory) {
        try {
            var directoryExists = Directory.Exists(rootDirectory);
            logger.DebugLog($"Getting zip files from {rootDirectory}. Directory exists? {directoryExists}");
            if (directoryExists) {
                return Directory.GetFiles(rootDirectory, "*synthriderz-beatmaps.zip");
            }
        } catch (System.Exception e) {
            logger.ErrorLog("Failed to get files: " + e.Message);
        }

        return new string[] {};
    }

    /// Returns list of all maps downloaded from synthriderz.com located in the given directory.
    /// If none found or error occurs, returns empty array
    private string[] GetSynthriderzMapFiles(string rootDirectory) => _customFileManager.GetSynthriderzMapFiles(rootDirectory);

    /// Returns list of all stages downloaded from synthriderz.com located in the given directory.
    /// If none found or error occurs, returns empty array
    private string[] GetSynthriderzStageFiles(string rootDirectory) => _customFileManager.GetSynthriderzStageFiles(rootDirectory);

    /// Returns list of all playlists downloaded from synthriderz.com located in the given directory.
    /// If none found or error occurs, returns empty array
    private string[] GetSynthriderzPlaylistFiles(string rootDirectory) => _customFileManager.GetSynthriderzPlaylistFiles(rootDirectory);

    /// Extracts custom content from a Synthriderz zip file to their respective directories.
    /// zipPath is the path to the zip file
    /// synthDirectory is the path to the root Synth Riders directory for custom content (i.e. SynthRidersUC)
    /// TODO add decals and profiles
    private IEnumerator ExtractSynthriderzZip(string zipPath, string synthDirectory) {
        if (!File.Exists(zipPath)) {
            logger.ErrorLog($"File {zipPath} doesn't exist! Not extracting");
            yield break;
        }

        if (!Directory.Exists(synthDirectory)) {
            logger.ErrorLog($"Destination {synthDirectory} doesn't exist!");
            yield break;
        }

        // Sanity check
        if (!lzip.validateFile(zipPath)) {
            logger.ErrorLog($"Failed to validate {zipPath}");
            yield break;
        }

        // Get general info about the zip archive
        var uncompressedBytes = lzip.getFileInfo(zipPath);
        if (uncompressedBytes == 0) {
            logger.ErrorLog("Failed to get info on zip file");
            yield break;
        }
        else {
            logger.DebugLog($"Total zip size: {uncompressedBytes}");
        }

        // Get zip entries
        var entryNames = lzip.ninfo;

        // Extract custom Synth Riderz content
        foreach (var filePath in entryNames) {
            var fileName = Path.GetFileName(filePath);
            string destPath = null;
            if (Path.GetExtension(filePath) == MAP_EXTENSION) {
                // Ignore position within zip file - if it's the right extension, put it in the custom dir directly
                destPath = Path.Join(synthDirectory, "CustomSongs", fileName);
            }
            else if (STAGE_EXTENSIONS.Contains(Path.GetExtension(filePath))) {
                // Ignore position within zip file - if it's the right extension, put it in the custom dir directly
                destPath = Path.Join(synthDirectory, "CustomStages", fileName);
            }
            else if (Path.GetExtension(filePath) == PLAYLIST_EXTENSION) {
                // Ignore position within zip file - if it's the right extension, put it in the custom dir directly
                destPath = Path.Join(synthDirectory, "CustomPlaylists", fileName);
            }

            if (destPath != null) {
                logger.DebugLog($"Extracting {filePath} to {destPath}");
                yield return null;
                int result = lzip.extract_entry(zipPath, filePath, destPath);
                if (result != 1) {
                    logger.ErrorLog($"Failed to extract {filePath}! Result: {result}. Skipping");
                    continue;
                }
            }
        }
    }

    /// Move synth custom content from the Downloads folder to custom content directories.
    /// Extracts zip files that look like they were downloaded from synthriderz.com
    private IEnumerator MoveDownloadedFiles() {
        logger.DebugLog("Trying to move custom content from Download folder...");

        if (isMovingFiles) {
            logger.DebugLog("Already moving! Ignoring...");
            yield break;
        }

        isMovingFiles = true;

        // Disable anything else while we do this
        displayManager.DisableActions("Moving Downloads...");

        var downloadDir = "/sdcard/Download/";
        
        logger.DebugLog($"Moving downloaded zips...");
        var zipFilePaths = GetSynthriderzZipFiles(downloadDir);
        logger.DebugLog($"{zipFilePaths.Length} zip files found");
        foreach (var filePath in zipFilePaths) {
            logger.DebugLog("Zip file: " + filePath);
            yield return ExtractSynthriderzZip(filePath, synthCustomContentDir);
            try {
                File.Delete(filePath);
            } catch (System.Exception e) {
                logger.ErrorLog($"Failed to delete zip {filePath}: {e.Message}");
                continue;
            }
        }
        
        logger.DebugLog($"Moving downloaded map files...");
        var mapFilePaths = GetSynthriderzMapFiles(downloadDir);
        logger.DebugLog($"{mapFilePaths.Length} map files found");
        foreach (var filePath in mapFilePaths) {
            MoveCustomSong(filePath);
            yield return null;
        }

        logger.DebugLog($"Moving downloaded stage files...");
        var stageFilePaths = GetSynthriderzStageFiles(downloadDir);
        logger.DebugLog($"{stageFilePaths.Length} stage files found");
        foreach (var filePath in stageFilePaths) {
            MoveCustomStage(filePath);
            yield return null;
        }

        logger.DebugLog($"Moving downloaded playlist files...");
        var playlistFilePaths = GetSynthriderzPlaylistFiles(downloadDir);
        logger.DebugLog($"{playlistFilePaths.Length} playlist files found");
        foreach (var filePath in playlistFilePaths) {
            MoveCustomPlaylist(filePath);
            yield return null;
        }

        logger.DebugLog("Files moved. Timestamps aren't updated.");
        logger.DebugLog("Consider fixing timestamps for downloaded content!");

        displayManager.EnableActions();

        isMovingFiles = false;
    }

    /// Moves a custom song to the proper synth directory
    /// Sets dateModified to the published date (if provided), for in-game time ordering.
    /// Returns the final path of the song
    public string MoveCustomSong(string filePath, DateTime? publishedAtUtc = null)
    {
        var mapFileName = Path.GetFileName(filePath);
        var destPath = Path.Join(synthCustomContentDir, "CustomSongs", mapFileName);
        FileUtils.MoveFileOverwrite(filePath, destPath, logger);

        if (publishedAtUtc.HasValue) {
            // logger.DebugLog($"Setting {mapFileName} file time to {publishedAtUtc.GetValueOrDefault()}");
            FileUtils.TrySetDateModifiedUtc(destPath, publishedAtUtc.GetValueOrDefault(), logger);
        }

        return destPath;
    }

    /// Moves a custom stage to the proper synth directory
    /// Returns the final path of the stage
    public string MoveCustomStage(string filePath)
    {
        var destPath = Path.Join(synthCustomContentDir, "CustomStages", Path.GetFileName(filePath));
        FileUtils.MoveFileOverwrite(filePath, destPath, logger);
        return destPath;
    }

    /// Moves a custom playlist to the proper synth directory
    /// Returns the final path of the playlist
    /// TODO this doesn't check and remove different named playlists with the same identifier!
    public string MoveCustomPlaylist(string filePath)
    {
        var destPath = Path.Join(synthCustomContentDir, "CustomPlaylists", Path.GetFileName(filePath));
        // TODO actually check existing files for matching identifier, since the game can rename them!
        FileUtils.MoveFileOverwrite(filePath, destPath, logger);
        return destPath;
    }

    /// <summary>
    /// With the SynthRiders mixed reality update (aka Remastered) from Aug 2023, the playlist directory moved.
    /// This migrates playlist files to the new location.
    /// </summary>
    public void MigratePlaylistsForMixedRealityUpdate()
    {
        try
        {
            var oldPlaylistDir = Path.Join(synthCustomContentDir, "Playlist");
            if (!Directory.Exists(oldPlaylistDir))
            {
                logger.DebugLog("No old playlist directory found; skipping migration");
                return;
            }

            var newPlaylistDir = Path.Join(synthCustomContentDir, "CustomPlaylists");
            if (!Directory.Exists(newPlaylistDir))
            {
                logger.DebugLog("New playlist directory not found; did you run the game and accept permissions for custom content?");
                return;
            }

            logger.DebugLog($"Migrating playlists");
            foreach (var file in Directory.GetFiles(oldPlaylistDir))
            {
                if (Path.GetExtension(file) != PLAYLIST_EXTENSION)
                    continue;

                var destPath = Path.Join(newPlaylistDir, Path.GetFileName(file));
                logger.ErrorLog($"Migrating playlist from {file} to {destPath}");
                FileUtils.MoveFileOverwrite(file, destPath, logger);
            }
        }
        catch (Exception e)
        {
            logger.ErrorLog("Failed to migrate playlist files: " + e.Message);
        }
    }

    /// Returns a new list with all maps in the source list that aren't contained in the user's custom song directory already
    public List<MapItem> FilterOutExistingMaps(List<MapItem> maps) => _customFileManager.FilterOutExistingMaps(maps);

    /// Refreshes local database metadata. Parses all missing custom map files.
    /// This saves the updated database.
    private async Task RefreshLocalDatabase() => await _customFileManager.RefreshLocalDatabase();
}
