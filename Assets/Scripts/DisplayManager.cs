using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using Oculus.Platform;

public class DisplayManager : MonoBehaviour
{
    public TextMeshProUGUI LastModifiedText;
    public TextMeshProUGUI HashesFoundText;
    public TextMeshProUGUI ResultText;
    public TextMeshProUGUI DebugText;

    private readonly string lastModifiedLabel = "Last Modified: ";
    private readonly string hashesFoundLabel = "Hashes Found: ";
    private readonly string resultLabel = "Result: ";

    private List<string> debugBuffer = new List<string>();


    private void Awake()
    {
        LastModifiedText.SetText(lastModifiedLabel + Preferences.GetLastDownloadedTimeMs());
        LastModifiedText.gameObject.SetActive(true);

        HashesFoundText.SetText(hashesFoundLabel);
        HashesFoundText.gameObject.SetActive(true);

        ResultText.SetText(resultLabel);
        ResultText.gameObject.SetActive(true);

        DebugText.gameObject.SetActive(true);
    }

    private void SetError(string message)
    {
        ResultText.SetText("Error: " + message);
        ResultText.color = Color.red;
        ResultText.gameObject.SetActive(true);

        LastModifiedText.gameObject.SetActive(false);
        HashesFoundText.gameObject.SetActive(false);
    }

    public void DebugLog(string message) {
        debugBuffer.Add(message);
        if (debugBuffer.Count > 10) {
            debugBuffer.RemoveAt(0);
        }
        DebugText.SetText(String.Join("\n", debugBuffer));
    }
}
