/******************************************************************************
 * File: OriginLocationUtility.cs
 * Copyright (c) 2023 Qualcomm Technologies, Inc. and/or its subsidiaries. All rights reserved.
 *
 * Confidential and Proprietary - Qualcomm Technologies, Inc.
 ******************************************************************************/

using UnityEngine;
using UnityEngine.XR.ARFoundation;
#if AR_FOUNDATION_5_0_OR_NEWER
using Unity.XR.CoreUtils;
#endif

namespace Qualcomm.Snapdragon.Spaces
{
    public static class OriginLocationUtility
    {
#if AR_FOUNDATION_5_0_OR_NEWER
        public static XROrigin FindXROrigin()
        {
            return GameObject.FindObjectOfType<XROrigin>();
        }
#endif

        // disable deprecated warning for accessing ARSessionOrigin for backwards compatibility
#pragma warning disable CS0618
        public static ARSessionOrigin FindARSessionOrigin()
        {
            return Object.FindObjectOfType<ARSessionOrigin>();
        }
#pragma warning restore CS0618

        public static Camera GetOriginCamera()
        {
#if AR_FOUNDATION_5_0_OR_NEWER
            return FindXROrigin()?.Camera;
#endif
            // disable deprecated warning for accessing .camera for backwards compatibility and unreachable code warning
#pragma warning disable CS0618, CS0162
            return FindARSessionOrigin()?.camera;
#pragma warning restore CS0618,  CS0162
        }

        public static Transform GetOriginTransform()
        {
#if AR_FOUNDATION_5_0_OR_NEWER
            return FindXROrigin()?.transform;
#endif
            // disable warning for unreachable code
#pragma warning disable CS0162
            return FindARSessionOrigin()?.transform;
#pragma warning restore CS0162
        }
    }
}
