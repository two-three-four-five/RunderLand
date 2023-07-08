/******************************************************************************
 * File: FusionFeature.cs
 * Copyright (c) 2021-2023 Qualcomm Technologies, Inc. and/or its subsidiaries. All rights reserved.
 *
 * Confidential and Proprietary - Qualcomm Technologies, Inc.
 *
 ******************************************************************************/

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
#endif

namespace Qualcomm.Snapdragon.Spaces
{
#if UNITY_EDITOR
    [OpenXRFeature(
        UiName = FeatureName,
        BuildTargetGroups = new[] { BuildTargetGroup.Android },
        Company = "Qualcomm",
        Desc = "Enables full simultaneous use of the mobile touchscreen and AR glasses on supported Snapdragon Spaces Development Kits",
        DocumentationLink = "",
        OpenxrExtensionStrings = FeatureExtensions,
        Version = "0.14.0",
        Required = true,
        Category = FeatureCategory.Feature,
        FeatureId = FeatureID)]
#endif
    public partial class FusionFeature : SpacesOpenXRFeature
    {
        public const string FeatureName = "Dual Render Fusion (Experimental)";
        public const string FeatureID = "com.qualcomm.snapdragon.spaces.fusion";
        public const string FeatureExtensions = "XR_UNITY_android_present";

#if UNITY_EDITOR
        [Tooltip("If enabled, the application will prompt to close at runtime if Spaces Services is not installed.\n\nIf disabled, applications will run in mobile-only mode on devices without Spaces Services installed.")]
#endif
        public bool RequireSpacesServices = false;

#if UNITY_EDITOR
        [Tooltip("If enabled, runs validation checks on the open Scene for required components to enable dual-rendering capabilities (recommended for setting up a Scene with dual render capabilities.)\n\nIf disabled, no validation checks will be run on the open Scene (recommended to prevent build errors if the open Scene does not need to be equipped with dual render capabilities.")]
#endif
        public bool ValidateOpenScene = true;
    }
}
