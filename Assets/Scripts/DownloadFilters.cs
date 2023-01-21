using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Linq;
using System.Collections.Generic;

/// Handles selecting download filters
public class DownloadFilters : MonoBehaviour {
    public TextMeshProUGUI TimeSelectionText;
    public DisplayManager displayManager;
    
    public DownloadFilterDifficulty[] difficultyFilters;
    
    private const string TIME_LAST_FETCH = "Last Fetch";
    private const string TIME_LAST_WEEK = "Last Week";
    private const string TIME_LAST_MONTH = "Last Month";
    private const string TIME_LAST_3_MONTHS = "Last 3 Months";
    private const string TIME_LAST_YEAR = "Last Year";
    private const string TIME_ALL_TIME = "All Time";
    private string[] TimeSelectionTextOptions = new string[] {
        TIME_LAST_FETCH,
        TIME_LAST_WEEK,
        TIME_LAST_MONTH,
        TIME_LAST_3_MONTHS,
        TIME_LAST_YEAR,
        TIME_ALL_TIME,
    };
    private int currentTimeSelectionIdx = 0;

    private void Awake() {
        // Set selection from prefs
        var enabledDifficulties = Preferences.GetDifficultiesEnabled();
        foreach (var difficultyFilter in difficultyFilters) {
            difficultyFilter.SetSelected(enabledDifficulties.Contains(difficultyFilter.SiteFilterName));
        }
    }

    private void Start() {
        // Cycle from the last index to get to the start like any other time
        currentTimeSelectionIdx = TimeSelectionTextOptions.Length - 1;
        CycleDownloadTime();
    }

    /// Cycles to the next download time option and updates UI
    public void CycleDownloadTime() {
        if (TimeSelectionTextOptions.Length == 0) {
            return;
        }

        currentTimeSelectionIdx = (currentTimeSelectionIdx + 1) % TimeSelectionTextOptions.Length;
        TimeSelectionText.SetText(TimeSelectionTextOptions[currentTimeSelectionIdx]);
        displayManager.DebugLog("Setting download time to " + GetDateCutoffFromCurrentSelection(DateTime.UtcNow).ToLocalTime());
    }

    public DateTime GetDateCutoffFromCurrentSelection(DateTime nowUtc) {
        switch (TimeSelectionText.text) {
            case TIME_LAST_FETCH:
                return Preferences.GetLastDownloadedTime();
            case TIME_LAST_WEEK:
                return nowUtc.AddDays(-7);
            case TIME_LAST_MONTH:
                return nowUtc.AddMonths(-1);
            case TIME_LAST_3_MONTHS:
                return nowUtc.AddMonths(-3);
            case TIME_LAST_YEAR:
                return nowUtc.AddYears(-1);
            case TIME_ALL_TIME:
            default:
                return DateTime.UnixEpoch;
        }
    }

    /// Gets difficulty filter names for Z site.
    /// Easy, Normal, Hard, Expert, Master, Custom
    public List<string> GetDifficultiesEnabled() {
        return difficultyFilters
            .Where(filter => filter.IsSelected)
            .Select(filter => filter.SiteFilterName)
            .ToList();
    }

    /// Gets all difficult filter names
    public List<string> GetAllDifficulties() {
        return difficultyFilters.Select(filter => filter.SiteFilterName).ToList();
    }

    public void SaveDifficultyFiltersToPrefs() {
        Preferences.SetDifficultiesEnabled(GetDifficultiesEnabled());
    }
}