using System;
using UnityEngine;

public class Preferences {

    private static string KEY_LAST_DOWNLOADED_TIME_SEC = "lastDownloadedTimeSec";
    public static int DEFAULT_LAST_DOWNLOADED_TIME_SEC = 0;

    public static void SetLastDownloadedTime(DateTime lastDownloadedTime) {
        DateTimeOffset dto = lastDownloadedTime;
        PlayerPrefs.SetInt(KEY_LAST_DOWNLOADED_TIME_SEC, (int)dto.ToUnixTimeSeconds());
    }

    public static DateTime GetLastDownloadedTime() {
        int timeSec = PlayerPrefs.GetInt(KEY_LAST_DOWNLOADED_TIME_SEC, DEFAULT_LAST_DOWNLOADED_TIME_SEC);
        return DateTimeOffset.FromUnixTimeSeconds(timeSec).UtcDateTime;
    }
}
