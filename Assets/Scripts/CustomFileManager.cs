using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TMPro;
using System.IO.Compression;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Linq;

public class CustomFileManager : MonoBehaviour
{
    public DisplayManager displayManager;

    private bool isMovingFiles = false;
    private string synthCustomContentDir = "/sdcard/SynthRidersUC/";
    private Dictionary<string, MapZMetadata> localMaps = new Dictionary<string, MapZMetadata>();

    private readonly string MAP_EXTENSION = ".synth";
    private readonly HashSet<string> STAGE_EXTENSIONS = new HashSet<string>() { ".stagequest", ".spinstagequest" };
    private readonly string PLAYLIST_EXTENSION = ".playlist";


    private async void Awake()
    {
        await ReloadLocalMaps();
    }

    /// Loads all local maps from disk and updates the dictionary
    private async Task ReloadLocalMaps() {
        // TODO if this takes too long, have a popup with progress bar?
        localMaps = await GetLocalMaps(synthCustomContentDir);
        displayManager.DebugLog($"{localMaps.Count} local maps found");
    }

    /// Parses the map at the given path and adds it to the localMaps dict
    public async void AddLocalMap(string mapPath) {
        MapZMetadata metadata = await ParseLocalMap(mapPath);
        if (metadata == null) {
            displayManager.ErrorLog("Failed to parse map at " + mapPath);
            return;
        }

        if (!localMaps.TryAdd(metadata.hash, metadata)) {
            displayManager.DebugLog("Map already exists in dict. Hash: " + metadata.hash);
        }
    }

    /// Useful for debugging. Clear out all custom songs
    public async void DeleteCustomSongs() {
        try {
            var mapFiles = GetSynthriderzMapFiles(Path.Join(synthCustomContentDir, "CustomSongs"));
            displayManager.DebugLog($"Deleting {mapFiles.Length} files...");
            foreach (var filePath in mapFiles) {
                File.Delete(filePath);
            }
        }
        catch (System.Exception e) {
            displayManager.ErrorLog($"Failed to delete files: {e.Message}");
        }

        await ReloadLocalMaps();
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
            displayManager.DebugLog($"Getting zip files from {rootDirectory}. Directory exists? {directoryExists}");
            if (directoryExists) {
                return Directory.GetFiles(rootDirectory, "*synthriderz-beatmaps.zip");
            }
        } catch (System.Exception e) {
            displayManager.ErrorLog("Failed to get files: " + e.Message);
        }

        return new string[] {};
    }

    /// Returns list of all maps downloaded from synthriderz.com located in the given directory.
    /// If none found or error occurs, returns empty array
    private string[] GetSynthriderzMapFiles(string rootDirectory) {
        try {
            var directoryExists = Directory.Exists(rootDirectory);
            displayManager.DebugLog($"Getting map files from {rootDirectory}. Directory exists? {directoryExists}");
            if (directoryExists) {
                return Directory.GetFiles(rootDirectory, $"*{MAP_EXTENSION}");
            }
        } catch (System.Exception e) {
            displayManager.ErrorLog("Failed to get files: " + e.Message);
        }

        return new string[] {};
    }

    /// Returns list of all stages downloaded from synthriderz.com located in the given directory.
    /// If none found or error occurs, returns empty array
    private string[] GetSynthriderzStageFiles(string rootDirectory) {
        try {
            var directoryExists = Directory.Exists(rootDirectory);
            displayManager.DebugLog($"Getting stage files from {rootDirectory}. Directory exists? {directoryExists}");
            if (directoryExists) {
                var filePaths = new List<string>();
                foreach (var stageExtension in STAGE_EXTENSIONS) {
                    filePaths.AddRange(Directory.GetFiles(rootDirectory, $"*{stageExtension}"));
                }
                return filePaths.ToArray();
            }
        } catch (System.Exception e) {
            displayManager.ErrorLog("Failed to get files: " + e.Message);
        }

        return new string[] {};
    }

    /// Returns list of all playlists downloaded from synthriderz.com located in the given directory.
    /// If none found or error occurs, returns empty array
    private string[] GetSynthriderzPlaylistFiles(string rootDirectory) {
        try {
            var directoryExists = Directory.Exists(rootDirectory);
            displayManager.DebugLog($"Getting playlist files from {rootDirectory}. Directory exists? {directoryExists}");
            if (directoryExists) {
                var filePaths = new List<string>();
                return Directory.GetFiles(rootDirectory, $"*{PLAYLIST_EXTENSION}");
            }
        } catch (System.Exception e) {
            displayManager.ErrorLog("Failed to get files: " + e.Message);
        }

        return new string[] {};
    }

    /// Extracts custom content from a Synthriderz zip file to their respective directories.
    /// zipPath is the path to the zip file
    /// synthDirectory is the path to the root Synth Riders directory for custom content (i.e. SynthRidersUC)
    /// TODO add decals and profiles
    private IEnumerator ExtractSynthriderzZip(string zipPath, string synthDirectory) {
        if (!File.Exists(zipPath)) {
            displayManager.ErrorLog($"File {zipPath} doesn't exist! Not extracting");
            yield break;
        }

        if (!Directory.Exists(synthDirectory)) {
            displayManager.ErrorLog($"Destination {synthDirectory} doesn't exist!");
            yield break;
        }

        // Sanity check
        if (!lzip.validateFile(zipPath)) {
            displayManager.ErrorLog($"Failed to validate {zipPath}");
            yield break;
        }

        // Get general info about the zip archive
        var uncompressedBytes = lzip.getFileInfo(zipPath);
        if (uncompressedBytes == 0) {
            displayManager.ErrorLog("Failed to get info on zip file");
            yield break;
        }
        else {
            displayManager.DebugLog($"Total zip size: {uncompressedBytes}");
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
                destPath = Path.Join(synthDirectory, "Playlist", fileName);
            }

            if (destPath != null) {
                displayManager.DebugLog($"Extracting {filePath} to {destPath}");
                yield return null;
                int result = lzip.extract_entry(zipPath, filePath, destPath);
                if (result != 1) {
                    displayManager.ErrorLog($"Failed to extract {filePath}! Result: {result}. Skipping");
                    continue;
                }
            }
        }
    }

    /// Move synth custom content from the Downloads folder to custom content directories.
    /// Extracts zip files that look like they were downloaded from synthriderz.com
    private IEnumerator MoveDownloadedFiles() {
        displayManager.DebugLog("Trying to move custom content from Download folder...");

        if (isMovingFiles) {
            displayManager.DebugLog("Already moving! Ignoring...");
            yield break;
        }

        isMovingFiles = true;

        var downloadDir = "/sdcard/Download/";
        
        displayManager.DebugLog($"Moving downloaded zips...");
        var zipFilePaths = GetSynthriderzZipFiles(downloadDir);
        displayManager.DebugLog($"{zipFilePaths.Length} zip files found");
        foreach (var filePath in zipFilePaths) {
            displayManager.DebugLog("Zip file: " + filePath);
            yield return ExtractSynthriderzZip(filePath, synthCustomContentDir);
            try {
                File.Delete(filePath);
            } catch (System.Exception e) {
                displayManager.ErrorLog($"Failed to delete zip {filePath}: {e.Message}");
                continue;
            }
        }
        
        displayManager.DebugLog($"Moving downloaded map files...");
        var mapFilePaths = GetSynthriderzMapFiles(downloadDir);
        displayManager.DebugLog($"{mapFilePaths.Length} map files found");
        foreach (var filePath in mapFilePaths) {
            MoveCustomSong(filePath);
            yield return null;
        }

        displayManager.DebugLog($"Moving downloaded stage files...");
        var stageFilePaths = GetSynthriderzStageFiles(downloadDir);
        displayManager.DebugLog($"{stageFilePaths.Length} stage files found");
        foreach (var filePath in stageFilePaths) {
            MoveCustomStage(filePath);
            yield return null;
        }

        displayManager.DebugLog($"Moving downloaded playlist files...");
        var playlistFilePaths = GetSynthriderzPlaylistFiles(downloadDir);
        displayManager.DebugLog($"{playlistFilePaths.Length} playlist files found");
        foreach (var filePath in playlistFilePaths) {
            MoveCustomPlaylist(filePath);
            yield return null;
        }

        isMovingFiles = false;
    }

    /// Moves a custom song to the proper synth directory
    /// Returns the final path of the song
    public string MoveCustomSong(string filePath) {
        var destPath = Path.Join(synthCustomContentDir, "CustomSongs", Path.GetFileName(filePath));
        FileUtils.MoveFileOverwrite(filePath, destPath, displayManager);
        return destPath;
    }

    /// Moves a custom stage to the proper synth directory
    /// Returns the final path of the stage
    public string MoveCustomStage(string filePath) {
        var destPath = Path.Join(synthCustomContentDir, "CustomStages", Path.GetFileName(filePath));
        FileUtils.MoveFileOverwrite(filePath, destPath, displayManager);
        return destPath;
    }

    /// Moves a custom playlist to the proper synth directory
    /// Returns the final path of the playlist
    /// TODO this doesn't check and remove different named playlists with the same identifier!
    public string MoveCustomPlaylist(string filePath) {
        var destPath = Path.Join(synthCustomContentDir, "Playlist", Path.GetFileName(filePath));
        // TODO actually check existing files for matching identifier, since the game can rename them!
        FileUtils.MoveFileOverwrite(filePath, destPath, displayManager);
        return destPath;
    }

    /// Returns a new list with all maps in the source list that aren't contained in the user's custom song directory already
    public List<MapItem> FilterOutExistingMaps(List<MapItem> maps) {
        displayManager.DebugLog($"{localMaps.Count} local maps found");
        return maps.Where(mapItem => !localMaps.ContainsKey(mapItem.hash)).ToList();
    }

    /// Looks in the given directory and parses out all custom map files
    /// Returned dictionary is hash: object
    private async Task<Dictionary<string, MapZMetadata>> GetLocalMaps(string synthCustomContentDir) {
        var maps = new Dictionary<string, MapZMetadata>();

        try {
            var mapsDir = Path.Join(synthCustomContentDir, "CustomSongs");
            if (!Directory.Exists(mapsDir)) {
                displayManager.ErrorLog("Custom maps directory doesn't exist!");
                return new Dictionary<string, MapZMetadata>();
            }

            var files = Directory.EnumerateFiles(mapsDir, $"*{MAP_EXTENSION}");
            foreach (var filePath in files) {
                MapZMetadata metadata = await ParseLocalMap(filePath);
                if (metadata == null) {
                    displayManager.ErrorLog("Failed to parse map at " + filePath);
                    continue;
                }
                maps.Add(metadata.hash, metadata);
            }
        }
        catch (System.Exception e) {
            displayManager.ErrorLog($"Failed to get local maps: {e.Message}");
            return new Dictionary<string, MapZMetadata>();
        }

        return maps;
    }

    /// Parses local map file. Returns null if can't parse or no metadata
    private async Task<MapZMetadata> ParseLocalMap(string filePath) {
        try {
            using (Stream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (BufferedStream bufferedStream = new BufferedStream(stream))
            using (ZipArchive archive = new ZipArchive(bufferedStream))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (entry.FullName == "synthriderz.meta.json")
                    {
                        using (System.IO.StreamReader sr = new System.IO.StreamReader(entry.Open()))
                        {
                            MapZMetadata metadata = JsonConvert.DeserializeObject<MapZMetadata>(await sr.ReadToEndAsync());
                            metadata.FilePath = filePath;
                            return metadata;
                        }
                    }
                }
            }
        }
        catch (System.Exception e) {
            displayManager.ErrorLog($"Failed to parse local map {filePath}: {e.Message}");
        }

        return null;
    }
}