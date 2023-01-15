using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using Oculus.Platform;

public class DisplayManager : MonoBehaviour
{
    public TextMeshProUGUI DebugText;
    public TextMeshProUGUI ErrorText;

    private List<string> debugBuffer = new List<string>();
    private List<string> errorBuffer = new List<string>();


    private void Awake()
    {
        DebugText.gameObject.SetActive(true);
        ErrorText.gameObject.SetActive(true);
    }

    public void ClearLogs() {
        debugBuffer.Clear();
        DebugText.SetText("");

        errorBuffer.Clear();
        ErrorText.SetText("");
    }

    public void DebugLog(string message) {
        if (debugBuffer.Count >= 15) {
            debugBuffer.RemoveAt(0);
        }
        debugBuffer.Add(message);
        DebugText.SetText(String.Join("\n", debugBuffer));
    }

    public void ErrorLog(string message) {
        if (errorBuffer.Count >= 15) {
            errorBuffer.RemoveAt(0);
        }
        errorBuffer.Add(message);
        ErrorText.SetText(String.Join("\n", errorBuffer));
    }
}
