using UnityEngine;

public class Preferences {
    
    private static string KEY_LAST_DOWNLOADED_TIME_MS = "lastDownloadedTimeMs";
    public static int DEFAULT_LAST_DOWNLOADED_TIME_MS = 0;

    public static void SetLastDownloadedTimeMs(int lastDownloadedTimeMs) {
        PlayerPrefs.SetInt(KEY_LAST_DOWNLOADED_TIME_MS, lastDownloadedTimeMs);
    }

    public static int GetLastDownloadedTimeMs() {
        return PlayerPrefs.GetInt(KEY_LAST_DOWNLOADED_TIME_MS, DEFAULT_LAST_DOWNLOADED_TIME_MS);
    }
}