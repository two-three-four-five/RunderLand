/******************************************************************************
 * File: FusionLogic.cs
 * Copyright (c) 2023 Qualcomm Technologies, Inc. and/or its subsidiaries. All rights reserved.
 *
 * Confidential and Proprietary - Qualcomm Technologies, Inc.
 *
 ******************************************************************************/

using UnityEngine;
using UnityEngine.Android;

namespace Qualcomm.Snapdragon.Spaces
{
    public class FusionLogic : MonoBehaviour
    {
        public bool HandleCameraPermissionsCheck = true;

        void Awake()
        {
            Camera xrCamera = OriginLocationUtility.GetOriginCamera();
            if (xrCamera != null)
            {
    #if UNITY_EDITOR
                xrCamera.targetDisplay = 1;
    #else
                xrCamera.targetDisplay = 0;
    #endif
            }

            if (HandleCameraPermissionsCheck)
            {
                if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
                {
                    Permission.RequestUserPermission(Permission.Camera);
                }
            }
        }
    }
}
