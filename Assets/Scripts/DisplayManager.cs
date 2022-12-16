// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using TMPro;
// using System;
// using Oculus.Platform;

// public class DisplayManager : MonoBehaviour
// {
//     public GameObject NeedToLinkText;
//     public TextMeshProUGUI DisplayNameText;
//     public TextMeshProUGUI IdText;
//     public TextMeshProUGUI OculusIdText;

//     private readonly string OculusAppId = "3496965523764934";
//     private readonly string displayNameLabel = "Display Name: ";
//     private readonly string idLabel = "ID: ";
//     private readonly string oculusIdLabel = "Oculus ID: ";

//     private void Awake()
//     {
//         NeedToLinkText.SetActive(true);

//         DisplayNameText.SetText(displayNameLabel);
//         DisplayNameText.gameObject.SetActive(true);

//         IdText.SetText(idLabel);
//         IdText.gameObject.SetActive(true);

//         OculusIdText.SetText(oculusIdLabel);
//         OculusIdText.gameObject.SetActive(true);

//         InitOculus();
//     }

//     private void InitOculus()
//     {
//         try
//         {
//             Core.AsyncInitialize(OculusAppId);
//             Entitlements.IsUserEntitledToApplication().OnComplete(OnEntitlementCheckComplete);
//         }
//         catch (UnityException e)
//         {
//             Debug.LogError("Failed entitlement check (exception)");
//             Debug.LogException(e);
//             UnityEngine.Application.Quit(1);
//         }
//     }

//     private void OnEntitlementCheckComplete(Message result)
//     {
//         if (result.IsError)
//         {
//             Debug.LogError("Failed entitlement check: " + result.GetError().Message);
//             SetError("Failed entitlement check: " + result.GetError().Message);
//             UnityEngine.Application.Quit(1);
//         }
//         else
//         {
//             Users.GetLoggedInUser().OnComplete(OnGetLoggedInUser);
//             Request.RunCallbacks();
//         }
//     }

//     private void SetError(string message)
//     {
//         DisplayNameText.SetText("Error: " + message);
//         DisplayNameText.color = Color.red;
//         DisplayNameText.gameObject.SetActive(true);

//         IdText.gameObject.SetActive(false);
//         OculusIdText.gameObject.SetActive(false);
//     }

//     private void OnGetLoggedInUser(Message<Oculus.Platform.Models.User> result)
//     {
//         if (result.IsError)
//         {
//             SetError(result.GetError().Message);
//         }
//         else if (result.Data == null)
//         {
//             SetError("Logged in user is null!");
//         }
//         else
//         {
//             var user = result.Data;
//             DisplayNameText.color = Color.white;

//             Debug.Log("display name: " + user.DisplayName);
//             DisplayNameText.SetText(displayNameLabel + user.DisplayName);
//             DisplayNameText.gameObject.SetActive(true);

//             Debug.Log("id: " + user.ID);
//             IdText.SetText(idLabel + user.ID);
//             IdText.gameObject.SetActive(true);

//             Debug.Log("oculus id: " + user.OculusID);
//             OculusIdText.SetText(oculusIdLabel + user.OculusID);
//             OculusIdText.gameObject.SetActive(true);
//         }
//     }
// }
