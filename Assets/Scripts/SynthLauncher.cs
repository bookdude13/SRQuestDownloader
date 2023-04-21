using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SynthLauncher : MonoBehaviour
{
    public SRLogHandler logger;

    public void LaunchSynthRiders()
    {
        logger.DebugLog("Launching Synth Riders...");
        SendLaunchIntent("com.kluge.SynthRiders");
    }

    /// Adapted from https://forum.unity.com/threads/android-ios-launch-from-within-a-unity-app-another-unity-app.222709/?_ga=2.89812563.95253068.1605723327-1322877492.1590351887#post-2308076
    private void SendLaunchIntent(string bundleId) {
        logger.DebugLog("Getting current package manager...");
        AndroidJavaClass up = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject ca = up.GetStatic<AndroidJavaObject>("currentActivity");
        AndroidJavaObject packageManager = ca.Call<AndroidJavaObject>("getPackageManager");
        
        bool didLaunch = false;
        try
        {
            logger.DebugLog("Creating launch intent");
            AndroidJavaObject launchIntent = packageManager.Call<AndroidJavaObject>("getLaunchIntentForPackage", bundleId);

            logger.DebugLog("Trying to start activity...");
            ca.Call("startActivity",launchIntent);

            didLaunch = true;
            launchIntent.Dispose();
        }
        catch (System.Exception e)
        {
            logger.ErrorLog($"Failed to create or send launch intent for bundle {bundleId}: {e.Message}");
        }
        finally {
            logger.DebugLog("Clean up...");
            packageManager.Dispose();
            ca.Dispose();
            up.Dispose();
        }

        if (didLaunch) {
            Application.Quit(0);
        }
    }
}
