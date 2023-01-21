using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Preferences {

    private static string KEY_LAST_DOWNLOADED_TIME_SEC = "lastDownloadedTimeSec";
    public static int DEFAULT_LAST_DOWNLOADED_TIME_SEC = 0;
    private static string KEY_SELECTED_DIFFICULTIES = "selectedDifficulties";

    public static void SetLastDownloadedTime(DateTime lastDownloadedTime) {
        DateTimeOffset dto = lastDownloadedTime;
        PlayerPrefs.SetInt(KEY_LAST_DOWNLOADED_TIME_SEC, (int)dto.ToUnixTimeSeconds());
    }

    public static DateTime GetLastDownloadedTime() {
        int timeSec = PlayerPrefs.GetInt(KEY_LAST_DOWNLOADED_TIME_SEC, DEFAULT_LAST_DOWNLOADED_TIME_SEC);
        return DateTimeOffset.FromUnixTimeSeconds(timeSec).UtcDateTime;
    }

    public static void SetDifficultiesEnabled(List<string> enabledDifficulties) {
        // Save as sorted list csv
        enabledDifficulties.Sort();
        var csv = String.Join(",", enabledDifficulties);
        PlayerPrefs.SetString(KEY_SELECTED_DIFFICULTIES, csv);
    }

    public static HashSet<string> GetDifficultiesEnabled() {
        var csvDifficulties = PlayerPrefs.GetString(KEY_SELECTED_DIFFICULTIES, "");
        return csvDifficulties.Split(",", StringSplitOptions.RemoveEmptyEntries).ToHashSet();
    }
}
