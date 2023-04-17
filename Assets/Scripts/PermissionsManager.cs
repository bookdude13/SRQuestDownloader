using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.SceneManagement;

public class PermissionsManager : MonoBehaviour
{
    [SerializeField] SRLogHandler logger;
    [SerializeField] GameObject permissionDialog;
    private const string MAIN_SCENE = "MainScene";
    private const string MANAGE_EXTERNAL_STORAGE_PERMISSION = "android.permission.MANAGE_EXTERNAL_STORAGE";

    // private void PermissionDenied(string permissionName)
    // {
    //     logger.ErrorLog($"Permission {permissionName} denied");
    // }

    // private void PermissionGranted(string permissionName)
    // {
    //     logger.DebugLog($"Permission {permissionName} granted");
    // }

    // private PermissionCallbacks GetPermissionCallbacks()
    // {
    //     var callbacks = new PermissionCallbacks();
    //     callbacks.PermissionDenied += PermissionDenied;
    //     callbacks.PermissionGranted += PermissionGranted;
    //     return callbacks;
    // }

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
        if (AndroidVersion.SDK_INT >= 30) // Android 11 / 'R'
        {
            // Android 11+ requires MANAGE_EXTERNAL_STORAGE in order to access shared sdcard directories
            permissions.Add(MANAGE_EXTERNAL_STORAGE_PERMISSION);
        }

        foreach (var permission in permissions)
        {
            if (!EnsurePermission(permission))
            {
                return false;
            }
        }

        return true;
    }

    // private bool EnsurePermission(string permissionName)
    // {
    //     if (Permission.HasUserAuthorizedPermission(permissionName))
    //     {
    //         logger.DebugLog($"Permission {permissionName} authorized");
    //     }
    //     else
    //     {
    //         Permission.RequestUserPermission(permissionName, GetPermissionCallbacks());
    //     }

    //     var checkResult = AndroidRuntimePermissions.CheckPermission(permissionName);
    //     if (checkResult == AndroidRuntimePermissions.Permission.Granted)
    //     {
    //         logger.DebugLog($"Permission {permissionName} authorized");
    //         return true;
    //     }
    //     else if (checkResult == AndroidRuntimePermissions.Permission.ShouldAsk)
    //     {
    //         logger.DebugLog($"Requesting permission {permissionName}");
    //         var requestResult = AndroidRuntimePermissions.RequestPermission(permissionName);
    //         logger.DebugLog($"Permission {permissionName} {requestResult}");
    //         return requestResult == AndroidRuntimePermissions.Permission.Granted;
    //     }
    //     else
    //     {
    //         logger.DebugLog($"Permission {permissionName} denied.");
    //         // this.gameObject.SetActive(true);
    //         return false;
    //     }
    // }

    private bool EnsurePermission(string permissionName)
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

    public void OpenSettings()
    {
        AndroidRuntimePermissions.OpenSettings();
    }
}