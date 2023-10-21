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

public class CustomFileManager : MonoBehaviour
{
    public DisplayManager displayManager;
    public SRLogHandler logger;
    public LocalDatabase db;

    private bool isMovingFiles = false;
    public readonly static string synthCustomContentDir = "/sdcard/SynthRidersUC/";

    private readonly string MAP_EXTENSION = ".synth";
    private readonly HashSet<string> STAGE_EXTENSIONS = new HashSet<string>() { ".stagequest", ".spinstagequest" };
    private readonly string PLAYLIST_EXTENSION = ".playlist";


    private void Awake()
    {
        displayManager.DisableActions("Loading Local Maps...");
        db = new LocalDatabase(logger);        
    }

    private async void Start() {
        await RefreshLocalDatabase(synthCustomContentDir);
        displayManager.EnableActions();
        
        // Immediately migrate old playlist files
        MigratePlaylistsForMixedRealityUpdate();
    }

    /// Parses the map at the given path and adds it to the collection
    public async void AddLocalMap(string mapPath, MapItem mapFromZ) {
        MapZMetadata metadata = await ParseLocalMap(mapPath, mapFromZ);
        if (metadata == null) {
            logger.ErrorLog("Failed to parse map at " + mapPath);
            return;
        }

        db.AddMap(metadata, logger);
    }

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

        await RefreshLocalDatabase(synthCustomContentDir);
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
    private string[] GetSynthriderzMapFiles(string rootDirectory) {
        try {
            var directoryExists = Directory.Exists(rootDirectory);
            logger.DebugLog($"Getting map files from {rootDirectory}. Directory exists? {directoryExists}");
            if (directoryExists) {
                return Directory.GetFiles(rootDirectory, $"*{MAP_EXTENSION}");
            }
        } catch (System.Exception e) {
            logger.ErrorLog("Failed to get files: " + e.Message);
        }

        return new string[] {};
    }

    /// Returns list of all stages downloaded from synthriderz.com located in the given directory.
    /// If none found or error occurs, returns empty array
    private string[] GetSynthriderzStageFiles(string rootDirectory) {
        try {
            var directoryExists = Directory.Exists(rootDirectory);
            logger.DebugLog($"Getting stage files from {rootDirectory}. Directory exists? {directoryExists}");
            if (directoryExists) {
                var filePaths = new List<string>();
                foreach (var stageExtension in STAGE_EXTENSIONS) {
                    filePaths.AddRange(Directory.GetFiles(rootDirectory, $"*{stageExtension}"));
                }
                return filePaths.ToArray();
            }
        } catch (System.Exception e) {
            logger.ErrorLog("Failed to get files: " + e.Message);
        }

        return new string[] {};
    }

    /// Returns list of all playlists downloaded from synthriderz.com located in the given directory.
    /// If none found or error occurs, returns empty array
    private string[] GetSynthriderzPlaylistFiles(string rootDirectory) {
        try {
            var directoryExists = Directory.Exists(rootDirectory);
            logger.DebugLog($"Getting playlist files from {rootDirectory}. Directory exists? {directoryExists}");
            if (directoryExists) {
                var filePaths = new List<string>();
                return Directory.GetFiles(rootDirectory, $"*{PLAYLIST_EXTENSION}");
            }
        } catch (System.Exception e) {
            logger.ErrorLog("Failed to get files: " + e.Message);
        }

        return new string[] {};
    }

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
    public string MoveCustomSong(string filePath, DateTime? publishedAtUtc=null) {
        var mapFileName = Path.GetFileName(filePath);
        var destPath = Path.Join(synthCustomContentDir, "CustomSongs", mapFileName);
        FileUtils.MoveFileOverwrite(filePath, destPath, logger);

        if (publishedAtUtc.HasValue) {
            // logger.DebugLog($"Setting {mapFileName} file time to {publishedAtUtc.GetValueOrDefault()}");
            FileUtils.SetDateModifiedUtc(destPath, publishedAtUtc.GetValueOrDefault(), logger);
        }

        return destPath;
    }

    /// Moves a custom stage to the proper synth directory
    /// Returns the final path of the stage
    public string MoveCustomStage(string filePath) {
        var destPath = Path.Join(synthCustomContentDir, "CustomStages", Path.GetFileName(filePath));
        FileUtils.MoveFileOverwrite(filePath, destPath, logger);
        return destPath;
    }

    /// Moves a custom playlist to the proper synth directory
    /// Returns the final path of the playlist
    /// TODO this doesn't check and remove different named playlists with the same identifier!
    public string MoveCustomPlaylist(string filePath) {
        var destPath = Path.Join(synthCustomContentDir, "Playlist", Path.GetFileName(filePath));
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
            logger.DebugLog($"Migrating playlists");
            var oldPlaylistDir = Path.Join(synthCustomContentDir, "Playlist");
            var newPlaylistDir = Path.Join(synthCustomContentDir, "CustomPlaylists");
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
    public List<MapItem> FilterOutExistingMaps(List<MapItem> maps) {
        logger.DebugLog($"{db.GetNumberOfMaps()} local maps found");
        return maps.Where(mapItem => db.GetFromHash(mapItem.hash) == null).ToList();
    }

    /// Refreshes local database metadata. Parses all missing custom map files.
    /// This saves the updated database.
    private async Task RefreshLocalDatabase(string synthCustomContentDir) {
        var localHashes = new HashSet<string>();

        // Make sure local db state is up to date
        await db.Load();

        try {
            var mapsDir = Path.Join(synthCustomContentDir, "CustomSongs");
            if (!Directory.Exists(mapsDir)) {
                logger.ErrorLog("Custom maps directory doesn't exist! Creating...");
                Directory.CreateDirectory(mapsDir);
            }

            var files = Directory.GetFiles(mapsDir, $"*{MAP_EXTENSION}");
            logger.DebugLog($"Updating database with map files ({files.Length} found)...");
            // This will implicitly remove any entries that are only present in the db
            int count = 0;
            int totalFiles = files.Length;
            foreach (var filePath in files) {
                MapZMetadata dbMetadata = db.GetFromPath(filePath);
                if (dbMetadata != null) {
                    // DB has this version already - good to go
                    // logger.DebugLog(Path.GetFileName(filePath) + " already in db");
                    localHashes.Add(dbMetadata.hash);
                }
                else {
                    // DB doesn't have this version; parse and add
                    MapZMetadata metadata = await ParseLocalMap(filePath);
                    if (metadata == null) {
                        logger.ErrorLog("Failed to parse map at " + filePath);
                        continue;
                    }

                    localHashes.Add(metadata.hash);
                    db.AddMap(metadata, logger);
                }

                count++;
                if (count % 100 == 0) {
                    logger.DebugLog($"Processed {count}/{totalFiles}...");
                    
                    // Save partial progress; ignore errors
                    await db.Save();
                }
            }
            logger.DebugLog($"{totalFiles} local files processed");
        }
        catch (System.Exception e) {
            logger.ErrorLog($"Failed to get local maps: {e.Message}");
            return;
        }

        // Successfully loaded maps
        // Remove all db entries that are no longer on the local file system
        logger.DebugLog("Removing database entries that aren't on the local file system...");
        db.RemoveMissingHashes(localHashes);
        
        // Save to db for next run
        if (!await db.Save()) {
            logger.ErrorLog("Failed to save db");
            // ignore for now; we still loaded everything fine
        }
    }

    /// Parses local map file. Returns null if can't parse or no metadata
    private async Task<MapZMetadata> ParseLocalMap(string filePath, MapItem mapFromZ = null) {
        var metadataFileName = "synthriderz.meta.json";
        try {
            using (Stream stream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite))
            using (BufferedStream bufferedStream = new BufferedStream(stream))
            using (ZipArchive archive = new ZipArchive(bufferedStream, ZipArchiveMode.Update))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (entry.FullName == metadataFileName)
                    {
                        using (System.IO.StreamReader sr = new System.IO.StreamReader(entry.Open()))
                        {
                            MapZMetadata metadata = JsonConvert.DeserializeObject<MapZMetadata>(await sr.ReadToEndAsync());
                            metadata.FilePath = filePath;
                            return metadata;
                        }
                    }
                }

                // No return, so missing metadata file.
                var fileName = Path.GetFileName(filePath);
                logger.DebugLog($"Missing {metadataFileName} in map {fileName}");
                if (mapFromZ == null || mapFromZ.hash == null || mapFromZ.id <= 0) {
                    logger.ErrorLog($"Missing {metadataFileName}. Refetch from Z site.");
                } else {
                    // We have information from Z to add this in ourselves
                    logger.ErrorLog($"Creating missing {metadataFileName} for {fileName}");
                    try {
                        JObject zMetadata = new JObject(
                            new JProperty("id", mapFromZ.id),
                            new JProperty("hash", mapFromZ.hash)
                        );

                        var newEntry = archive.CreateEntry(metadataFileName);
                        using System.IO.StreamWriter streamWriter = new System.IO.StreamWriter(newEntry.Open());
                        await streamWriter.WriteAsync(zMetadata.ToString(Formatting.None));

                        return new MapZMetadata(mapFromZ.id, mapFromZ.hash, filePath);
                    } catch (Exception e) {
                        logger.ErrorLog("Failed to create missing metadata entry: " + e.Message);
                    }
                }
            }
        }
        catch (System.Exception e) {
            logger.ErrorLog($"Failed to parse local map {filePath}: {e.Message}");
        }

        return null;
    }
}
