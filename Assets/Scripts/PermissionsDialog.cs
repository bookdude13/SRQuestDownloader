using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PermissionsDialog : MonoBehaviour
{
    [SerializeField] GameObject[] buttons;
    [SerializeField] SRLogHandler logger;

    void OnApplicationFocus(bool hasFocus)
    {
        logger.DebugLog($"OnApplicationFocus {hasFocus}");
        SetButtonsEnabled(hasFocus);
    }

    public void SetButtonsEnabled(bool isEnabled)
    {
        foreach (var buttonGO in buttons)
        {
            var button = buttonGO.GetComponent<Button>();
            if (button != null)
            {
                button.interactable = isEnabled;
            }
        }
    }
}