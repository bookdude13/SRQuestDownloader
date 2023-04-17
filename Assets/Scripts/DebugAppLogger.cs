using UnityEngine;
using System.IO;

/// Logger class that outputs to the debug log file given to the Application
public class DebugAppLogger : SRLogHandler {
    public override void DebugLog(string message) {
        File.WriteAllText(Application.consoleLogPath, message);
    }

    public override void ErrorLog(string message) {
        File.WriteAllText(Application.consoleLogPath, message);
    }

    public override void PersistLog(string message) {
        File.WriteAllText(Application.consoleLogPath, message);
    }
}