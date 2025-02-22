using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SRTimestampLib;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class PermissionsManager : MonoBehaviour
{
    [SerializeField] SRLogHandler logger;
    [SerializeField] PermissionsDialog permissionDialog;
    [SerializeField] private bool debugDenyPermission = false;
    private const string MAIN_SCENE = "MainScene";
    // https://developer.android.com/reference/android/provider/Settings#ACTION_MANAGE_APP_ALL_FILES_ACCESS_PERMISSION
    private const string ACTION_MANAGE_APP_ALL_FILES_ACCESS_PERMISSION = "android.settings.MANAGE_APP_ALL_FILES_ACCESS_PERMISSION";

    [SerializeField] private UnityEvent PermissionsGranted;

    public async void Start()
    {
        // Hide until we know we need it, so it looks like a delayed loading time instead of a quick change
        permissionDialog.gameObject.SetActive(false);

        await CheckPermissions();
    }

    public void CheckPermissionsBlocking()
    {
        var task = CheckPermissions();
        task.Wait();
    }

    public async Task CheckPermissions()
    {
        logger.DebugLog("Checking permissions");
#if UNITY_EDITOR
        var permissionGranted = !debugDenyPermission;
#else
        var permissionGranted = await EnsurePermissions();
#endif
        if (permissionGranted)
        {
            PermissionsGranted?.Invoke();
            //ContinueToMainScene();
        }
        else
        {
            // Show prompt
            permissionDialog.gameObject.SetActive(true);
        }
    }

    public void ContinueToMainScene()
    {
        SceneManager.LoadScene(MAIN_SCENE, LoadSceneMode.Single);
    }

    private async Task<bool> EnsurePermissions()
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
        
        if (debugDenyPermission)
            return false;

        if (AndroidVersion.SDK_INT >= 30) // Android 11 / 'R'
        {
            // Android 11+ requires MANAGE_EXTERNAL_STORAGE in order to access shared sdcard directories
            // This permission is handled a bit differently
            return await EnsureManageExternalStoragePermission();
        }

        return true;
    }

    private bool EnsureBasicPermission(string permissionName)
    {
        var checkResult = AndroidRuntimePermissions.CheckPermission(permissionName);
        if (checkResult)
        {
            logger.DebugLog($"Permission {permissionName} authorized");
            return true;
        }
        else
        {
            logger.DebugLog($"Requesting permission {permissionName}");
            var requestResult = AndroidRuntimePermissions.RequestPermission(permissionName);
            logger.DebugLog($"Permission {permissionName} {requestResult}");
            return requestResult == AndroidRuntimePermissions.Permission.Granted;
        }
    }

    /// Requires API 30+
    private async Task<bool> EnsureManageExternalStoragePermission()
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
                    // Try to list out SR customs directory to check permission
                    var testPath = Path.Combine(CustomFileManagerBehaviour.synthCustomContentDir, "permission_test");
                    if (await FileUtils.WriteToFile(DateTime.Now.ToLongDateString(), testPath, logger)) {
                        logger.DebugLog("Permission not set as expected, but writing to customs directory works");
                        FileUtils.DeleteFile(testPath, logger);
                        return true;
                    }

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

    public void RequestExternalStoragePermission()
    {
        logger.DebugLog("Requesting external storage permissions...");
        AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        
        try
        {
            logger.DebugLog("Creating intent");
            AndroidJavaObject requestIntent = new AndroidJavaObject("android.content.Intent", ACTION_MANAGE_APP_ALL_FILES_ACCESS_PERMISSION);

            logger.DebugLog($"Setting data uri with identifier {Application.identifier}");
            AndroidJavaClass uriClass = new AndroidJavaClass("android.net.Uri");
            AndroidJavaObject uri = uriClass.CallStatic<AndroidJavaObject>("fromParts", "package", Application.identifier, null);
            requestIntent = requestIntent.Call<AndroidJavaObject>("setData", uri);

            logger.DebugLog("Trying to start activity...");
            context.Call("startActivity", requestIntent);
            logger.DebugLog("Sent");

            requestIntent.Dispose();
            uri.Dispose();
            uriClass.Dispose();
        }
        catch (System.Exception e)
        {
            logger.ErrorLog($"Failed to create or send intent: {e.Message}");
        }
        finally {
            logger.DebugLog("Clean up...");
            context.Dispose();
            unityPlayer.Dispose();
        }
    }
}