using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.IO;

public class DisplayManager : MonoBehaviour, ILogHandler
{
    public TextMeshProUGUI VersionText;
    public TextMeshProUGUI LastFetchText;
    public TextMeshProUGUI DebugText;
    public TextMeshProUGUI ErrorText;
    public Button FetchMapsButton;
    public TextMeshProUGUI FetchMapsButtonText;
    public Button FixTimestampsButton;
    public Button MoveDownloadsButton;

    private List<string> debugBuffer = new List<string>();
    private List<string> errorBuffer = new List<string>();
    private ConcurrentQueue<string> persistBuffer = new ConcurrentQueue<string>();
    private readonly DateTime startTime = DateTime.Now;
    private readonly DebugAppLogger alternateErrorHandler = new DebugAppLogger();


    private void Awake()
    {
        string version = UnityEngine.Application.version;
        VersionText.gameObject.SetActive(true);
        VersionText.SetText($"Version: {version}");

        PersistLog($"Starting up at {startTime}, version {version}");

        LastFetchText.gameObject.SetActive(true);
        UpdateLastFetchTime();

        DebugText.gameObject.SetActive(true);
        ErrorText.gameObject.SetActive(true);
    }

    private void Start() {
        CleanOldLogs();
        InvokeRepeating("AppendLogFile", 1f, 10f);
    }

    public void UpdateLastFetchTime() {
        DateTime lastFetchTime = Preferences.GetLastDownloadedTime().ToLocalTime();
        LastFetchText.SetText($"Last Fetch: {lastFetchTime:dd MMM yy H:mm:ss zzz}");
    }

    public void DisableActions(string fetchMapsText="Fetch Songs") {
        FixTimestampsButton.interactable = false;
        MoveDownloadsButton.interactable = false;

        FetchMapsButton.interactable = false;
        FetchMapsButtonText.fontStyle = FontStyles.Italic;
        FetchMapsButtonText.SetText(fetchMapsText);
    }

    public void EnableActions() {
        FixTimestampsButton.interactable = true;
        MoveDownloadsButton.interactable = true;

        FetchMapsButton.interactable = true;
        FetchMapsButtonText.SetText("Fetch Songs");
        FetchMapsButton.interactable = true;
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

        // Also send to text file for easier debugging
        PersistLog(message);
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

    /// Cleans up old log files
    private void CleanOldLogs() {
        // Keep logs for 7 days
        var expirationTime = DateTime.UtcNow.AddDays(-7);

        try {
            var logsDir = Directory.GetParent(GetLogFilePath());
            if (logsDir.Exists) {
                var logFiles = logsDir.GetFiles();
                foreach (var logFile in logFiles) {
                    if (logFile.LastWriteTimeUtc < expirationTime) {
                        DebugLog($"Removing old log file {logFile.Name}");
                        logFile.Delete();
                    }
                }
            }
        }
        catch (System.Exception e) {
            ErrorLog("Failed to delete old log files: " + e.Message);
        }
    }

    /// Save log message to persistent buffer
    private void PersistLog(string message) {
        persistBuffer.Enqueue(message);
    }

    /// Appends the current log buffer to the log file
    private async void AppendLogFile() {
        // If no updates, skip persist
        if (persistBuffer.Count < 1) {
            return;
        }

        string logFile = GetLogFilePath();
        if (!File.Exists(logFile)) {
            try {
                Directory.GetParent(logFile).Create();
                File.Create(logFile);
            }
            catch (Exception e) {
                alternateErrorHandler.ErrorLog("Failed to create log file: " + e.Message);
                return;
            }
        }

        string message;
        while (true) {
            if (persistBuffer.TryDequeue(out message)) {
                await FileUtils.AppendToFile(message + "\n", logFile, alternateErrorHandler);
            }
            else {
                break;
            }
        }
    }

    /// Gets path to log file.
    /// Consistent within the same launch, changes betwen launches.
    private string GetLogFilePath() {
        string logFileName = $"SRQuestDownloader-{startTime:yyyy_MMM_dd__H_mm_ss}.log";
        return Path.Join(UnityEngine.Application.persistentDataPath, "logs", logFileName);
    }
}
