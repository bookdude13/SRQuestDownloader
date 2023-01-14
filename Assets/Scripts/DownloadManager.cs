using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DownloadManager : MonoBehaviour
{
    public DisplayManager displayManager;

    public void GetAllCustomSongs()
    {
        displayManager.ResultText.SetText("Getting custom songs...");

        int lastTimeMs = Preferences.GetLastDownloadedTimeMs();
        displayManager.DebugLog($"Downloading songs after {lastTimeMs}");
    }
}
