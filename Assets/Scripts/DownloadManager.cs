using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TMPro;

public class DownloadManager : MonoBehaviour
{
    public DisplayManager displayManager;

    public void GetAllCustomSongs()
    {
        // displayManager.ResultText.SetText("Getting custom songs...");
        
        // int lastTimeMs = Preferences.GetLastDownloadedTimeMs();
        // displayManager.DebugLog($"Downloading songs after {lastTimeMs}");

        MoveDownloadedCustomSongs();
    }

    /// Move synth maps and zip files from the Downloads folder on Quest to the CustomSongs directory
    public void MoveDownloadedCustomSongs() {
        displayManager.DebugLog($"Moving downloaded customs...");
        try {
            displayManager.DebugLog("Download folder exists? " + Directory.Exists("/sdcard/Download/"));
            var files = Directory.GetFiles("/sdcard/Download/");
            var zipsSuffix = "synthriderz-beatmaps.zip";
            displayManager.DebugLog($"{files.Length} files found");
            var filesEnum = Directory.EnumerateFiles("/sdcard/Download/");
            foreach (var file in filesEnum) {
                displayManager.DebugLog("File: " + file);
            }
        } catch (System.Exception e) {
            displayManager.DebugLog("Error! " + e.ToString());
        }
    }
}
