/******************************************************************************
 * File: BaseRuntimeFeature.RuntimeCheck.cs
 * Copyright (c) 2022 Qualcomm Technologies, Inc. and/or its subsidiaries. All rights reserved.
 *
 * Confidential and Proprietary - Qualcomm Technologies, Inc.
 *
 ******************************************************************************/

#if UNITY_ANDROID && !UNITY_EDITOR
using System.Linq;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;

namespace Qualcomm.Snapdragon.Spaces
{
    public partial class BaseRuntimeFeature
    {
        // Run the RuntimeChecker to see if the runtime is installed.
        protected override void OnEnable()
        {
            base.OnEnable();
            var activeLoaders = XRGeneralSettings.Instance?.Manager?.activeLoaders;
            if (activeLoaders?.Any(loader => loader.GetType() == typeof(OpenXRLoader)) != true)
            {
                // No OpenXR Loader enabled. Skip this method.
                return;
            }

            if (activeLoaders?.Count > 1)
            {
                Debug.LogError("Multiple active XR Plug-in Providers detected. Please check the XR Plug-in Management settings!");
            }

            if (CheckInstalledRuntime)
            {
                RuntimeCheck();
            }
        }

        private void RuntimeCheck()
        {
            var activity = new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity");
            var context = activity.Call<AndroidJavaObject>("getApplicationContext");
            var runtimeChecker = new AndroidJavaClass("com.qualcomm.snapdragon.spaces.serviceshelper.RuntimeChecker");

            // The dialogs parameters can be modified individually and it will be displayed if the runtime is not found.
            // Alternatively, "CheckInstalled" can be called on the runtimeChecker to just retrieve a boolean if the services are installed.
            // This won't show any dialog.
            var dialogOptions = new AndroidJavaObject("com.qualcomm.snapdragon.spaces.serviceshelper.DialogOptions");
            dialogOptions.Set("Title", "Services not installed");
            dialogOptions.Set("Message", "Please install the Snapdragon Spaces Services before starting this application!");
            dialogOptions.Set("QuitButtonTitle", "Quit");
            dialogOptions.Set("DownloadButtonTitle", "Download");
            dialogOptions.Set("ShowDownloadButton", true);
            //dialogOptions.Set("AlternativeDownloadLink", "https://alternative.store.link");
            if (!runtimeChecker.CallStatic<bool>("CheckInstalledWithDialog", activity, context, dialogOptions))
            {
                Debug.LogError("Snapdragon Spaces Services is not installed! Application will be closed.");
            }
        }
    }
}
#endif
