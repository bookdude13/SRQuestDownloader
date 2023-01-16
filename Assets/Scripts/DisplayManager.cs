using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using Oculus.Platform;

public class DisplayManager : MonoBehaviour
{
    public TextMeshProUGUI VersionText;
    public TextMeshProUGUI LastFetchText;
    public TextMeshProUGUI DebugText;
    public TextMeshProUGUI ErrorText;
    public Button FetchLatestButton;
    public TextMeshProUGUI FetchLatestButtonText;

    private List<string> debugBuffer = new List<string>();
    private List<string> errorBuffer = new List<string>();


    private void Awake()
    {
        VersionText.gameObject.SetActive(true);
        VersionText.SetText($"Version: {UnityEngine.Application.version}");

        LastFetchText.gameObject.SetActive(true);
        UpdateLastFetchTime();

        DebugText.gameObject.SetActive(true);
        ErrorText.gameObject.SetActive(true);
    }

    public void UpdateLastFetchTime() {
        DateTime lastFetchTime = Preferences.GetLastDownloadedTime().ToLocalTime();
        LastFetchText.SetText($"Last Fetch: {lastFetchTime:dd MMM yy H:mm:ss zzz}");
    }

    public void DisableFetchingLatest() {
        FetchLatestButton.interactable = false;
        FetchLatestButtonText.fontStyle = FontStyles.Italic;
        FetchLatestButtonText.SetText("Loading Local Maps...");
    }

    public void EnableFetchingLatest() {
        FetchLatestButtonText.fontStyle = FontStyles.Normal;
        FetchLatestButtonText.SetText("Fetch Latest Songs");
        FetchLatestButton.interactable = true;
    }

    public void ClearDebugLogs() {
        debugBuffer.Clear();
        DebugText.SetText("");
    }

    public void ClearErrorLogs() {
        errorBuffer.Clear();
        ErrorText.SetText("");
    }

    public void DebugLog(string message) {
        if (debugBuffer.Count >= 20) {
            debugBuffer.RemoveAt(0);
        }
        debugBuffer.Add(message);
        DebugText.SetText(String.Join("\n", debugBuffer));
    }

    public void ErrorLog(string message) {
        if (errorBuffer.Count >= 20) {
            errorBuffer.RemoveAt(0);
        }
        errorBuffer.Add(message);
        ErrorText.SetText(String.Join("\n", errorBuffer));

        // Also set in debug log for clarity
        DebugLog(message);
    }
}
