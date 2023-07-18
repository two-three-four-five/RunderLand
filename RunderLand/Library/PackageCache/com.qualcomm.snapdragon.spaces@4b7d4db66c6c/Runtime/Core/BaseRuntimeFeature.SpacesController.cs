/******************************************************************************
 * File: BaseRuntimeFeature.RuntimeCheck.cs
 * Copyright (c) 2022 Qualcomm Technologies, Inc. and/or its subsidiaries. All rights reserved.
 *
 * Confidential and Proprietary - Qualcomm Technologies, Inc.
 *
 ******************************************************************************/

using UnityEngine;

namespace Qualcomm.Snapdragon.Spaces
{
    public partial class BaseRuntimeFeature
    {
        /// <summary>
        ///     Reset the head pose from Spaces Host Controller.
        /// </summary>
        public bool TryResetPose()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (LaunchControllerOnHost)
            {
                var activity = new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity");
                var context = activity.Call<AndroidJavaObject>("getApplicationContext");
                var controller = new AndroidJavaClass("com.qualcomm.snapdragon.spaces.hostcontroller.HostController");
                controller.CallStatic("ResetPose");
                return true;
            }
#endif
            return false;
        }
    }
}
