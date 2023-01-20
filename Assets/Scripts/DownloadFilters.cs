using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// Handles selecting download filters
public class DownloadFilters : MonoBehaviour {
    public TextMeshProUGUI TimeSelectionText;
    public DisplayManager displayManager;
    
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
        displayManager.DebugLog("Setting download time to " + GetDateCutoffFromCurrentSelection(DateTime.UtcNow));
    }

    public DateTime GetDateCutoffFromCurrentSelection(DateTime nowUtc) {
        DateTime cutoffTimeUtc = nowUtc;
        switch (TimeSelectionText.text) {
            case TIME_LAST_FETCH:
                cutoffTimeUtc = Preferences.GetLastDownloadedTime();
                break;
            case TIME_LAST_WEEK:
                cutoffTimeUtc.AddDays(-7);
                break;
            case TIME_LAST_MONTH:
                cutoffTimeUtc.AddMonths(-1);
                break;
            case TIME_LAST_3_MONTHS:
                cutoffTimeUtc.AddMonths(-3);
                break;
            case TIME_LAST_YEAR:
                cutoffTimeUtc.AddYears(-1);
                break;
            case TIME_ALL_TIME:
            default:
                cutoffTimeUtc = DateTime.UnixEpoch;
                break;
        }
        return cutoffTimeUtc;
    }
}