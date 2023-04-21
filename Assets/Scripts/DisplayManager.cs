using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.IO;

public class DisplayManager : MonoBehaviour
{
    public TextMeshProUGUI VersionText;
    public TextMeshProUGUI LastFetchText;
    public Button FetchMapsButton;
    public TextMeshProUGUI FetchMapsButtonText;
    public DownloadFilters DownloadFilters;
    public Button FixTimestampsButton;
    public Button MoveDownloadsButton;
    public SRLogHandler logger;

    private void Awake()
    {
        string version = UnityEngine.Application.version;
        VersionText.gameObject.SetActive(true);
        VersionText.SetText($"Version: {version}");

        logger.PersistLog($"Starting up at {DateTime.Now}, version {version}");

        LastFetchText.gameObject.SetActive(true);
        UpdateLastFetchTime();
    }

    public void UpdateLastFetchTime() {
        DateTime lastFetchTime = Preferences.GetLastDownloadedTime().ToLocalTime();
        LastFetchText.SetText($"Last Fetch: {lastFetchTime:dd MMM yy H:mm:ss zzz}");
    }

    public void DisableActions(string fetchMapsText) {
        FixTimestampsButton.interactable = false;
        MoveDownloadsButton.interactable = false;

        FetchMapsButton.interactable = false;
        FetchMapsButtonText.fontStyle = FontStyles.Italic;
        FetchMapsButtonText.SetText(fetchMapsText);
    }

    public void UpdateFilterText() {
        if (FetchMapsButton.interactable) {
            var isFiltered = DownloadFilters.GetDifficultiesEnabled().Count != DownloadFilters.GetAllDifficulties().Count;
            FetchMapsButtonText.SetText("Fetch " + (isFiltered ? "Filtered" : "All"));
        }
        else {
            // Not interactable; must be doing something else. Don't update text yet.
            // Whatever is disabling this will call EnableActions afterwards to call this again.
        }
    }

    public void EnableActions() {
        FixTimestampsButton.interactable = true;
        MoveDownloadsButton.interactable = true;

        FetchMapsButton.interactable = true;
        FetchMapsButtonText.fontStyle = FontStyles.Normal;
        UpdateFilterText();
    }
}
