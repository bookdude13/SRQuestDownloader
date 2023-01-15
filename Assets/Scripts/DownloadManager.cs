using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TMPro;

public class DownloadManager : MonoBehaviour
{
    public DisplayManager displayManager;
    private bool isMovingFiles = false;
    private readonly string MAP_EXTENSION = ".synth";
    private readonly HashSet<string> STAGE_EXTENSIONS = new HashSet<string>() { ".stagequest", ".spinstagequest" };

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
                // Ignore position within zip file - if it's the right extension, put it in the custom songs dir directly
                destPath = Path.Join(synthDirectory, "CustomSongs", fileName);
            }
            else if (STAGE_EXTENSIONS.Contains(Path.GetExtension(filePath))) {
                // Ignore position within zip file - if it's the right extension, put it in the custom songs dir directly
                destPath = Path.Join(synthDirectory, "CustomStages", fileName);
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
        displayManager.ResultText.SetText("Moving custom songs from Download folder...");

        if (isMovingFiles) {
            displayManager.DebugLog("Already moving! Ignoring...");
            yield break;
        }

        isMovingFiles = true;

        // Fresh start each time
        displayManager.ClearLogs();
        
        var downloadDir = "/sdcard/Download/";
        var synthCustomContentDir = "/sdcard/SynthRidersUC/";
        
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
            var destPath = Path.Join(synthCustomContentDir, "CustomSongs", Path.GetFileName(filePath));
            displayManager.DebugLog($"Moving {filePath} to {destPath}");
            yield return null;
            try {
                // Attempt to "move", overwriting destination if it exists.
                File.Copy(filePath, destPath, true);
                File.Delete(filePath);
            } catch (System.Exception e) {
                displayManager.ErrorLog($"Failed to move {filePath}! {e.Message}");
                continue;
            }
        }

        displayManager.DebugLog($"Moving downloaded stage files...");
        var stageFilePaths = GetSynthriderzStageFiles(downloadDir);
        displayManager.DebugLog($"{stageFilePaths.Length} stage files found");
        foreach (var filePath in stageFilePaths) {
            var destPath = Path.Join(synthCustomContentDir, "CustomStages", Path.GetFileName(filePath));
            displayManager.DebugLog($"Moving {filePath} to {destPath}");
            yield return null;
            try {
                // Attempt to "move", overwriting destination if it exists.
                File.Copy(filePath, destPath, true);
                File.Delete(filePath);
            } catch (System.Exception e) {
                displayManager.ErrorLog($"Failed to move {filePath}! {e.Message}");
                continue;
            }
        }

        isMovingFiles = false;
    }
}
