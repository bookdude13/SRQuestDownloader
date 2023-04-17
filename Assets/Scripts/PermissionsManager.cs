using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.SceneManagement;

public class PermissionsManager : MonoBehaviour
{
    [SerializeField] SRLogHandler logger;
    [SerializeField] GameObject permissionDialog;
    private const string MAIN_SCENE = "MainScene";


    public void Start()
    {
        // Hide until we know we need it, so it looks like a delayed loading time instead of a quick change
        permissionDialog.SetActive(false);

        CheckPermisisons();
    }

    public void CheckPermisisons()
    {
        if (EnsurePermissions())
        {
            ContinueToMainScene();
        }
        else
        {
            // Show prompt
            permissionDialog.SetActive(true);
        }
    }

    public void ContinueToMainScene()
    {
        SceneManager.LoadScene(MAIN_SCENE, LoadSceneMode.Single);
    }

    private bool EnsurePermissions()
    {
        var permissions = new List<string>() {
            Permission.ExternalStorageRead,
            Permission.ExternalStorageWrite,
        };

        foreach (var permission in permissions)
        {
            if (!EnsureBasicPermission(permission))
            {
                return false;
            }
        }

        if (AndroidVersion.SDK_INT >= 29) // Android 11 / 'R'
        {
            // Android 11+ requires MANAGE_EXTERNAL_STORAGE in order to access shared sdcard directories
            // This permission is handled a bit differently
            return EnsureManageExternalStoragePermission();
        }

        return true;
    }

    private bool EnsureBasicPermission(string permissionName)
    {
        var checkResult = AndroidRuntimePermissions.CheckPermission(permissionName);
        if (checkResult == AndroidRuntimePermissions.Permission.Granted)
        {
            logger.DebugLog($"Permission {permissionName} authorized");
            return true;
        }
        else if (checkResult == AndroidRuntimePermissions.Permission.ShouldAsk)
        {
            logger.DebugLog($"Requesting permission {permissionName}");
            var requestResult = AndroidRuntimePermissions.RequestPermission(permissionName);
            logger.DebugLog($"Permission {permissionName} {requestResult}");
            return requestResult == AndroidRuntimePermissions.Permission.Granted;
        }
        else
        {
            logger.DebugLog($"Permission {permissionName} denied. Needs manual settings change.");
            return false;
        }
    }

    /// Requires API 30+
    private bool EnsureManageExternalStoragePermission()
    {
        using (AndroidJavaClass environment = new AndroidJavaClass("android.os.Environment"))
        {
            try
            {
                bool isExternalStorageManager = environment.CallStatic<bool>("isExternalStorageManager");
                if (isExternalStorageManager)
                {
                    logger.DebugLog("MANAGE_EXTERNAL_STORAGE permission granted");
                    return true;
                }
                else
                {
                    logger.ErrorLog("External storage permission requires user intervention");
                    return false;
                }
            }
            catch (System.Exception e)
            {
                logger.ErrorLog($"Failed to retrieve isExternalStorageManager: {e.Message}");
                return false;
            }
        }
    }

    public void OpenSettings()
    {
        AndroidRuntimePermissions.OpenSettings();
    }
}