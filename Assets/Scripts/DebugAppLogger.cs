using UnityEngine;
using System.IO;

/// Logger class that outputs to the debug log file given to the Application
public class DebugAppLogger : ILogHandler {
    public void DebugLog(string message) {
        File.WriteAllText(Application.consoleLogPath, message);
    }

    public void ErrorLog(string message) {
        File.WriteAllText(Application.consoleLogPath, message);
    }
}