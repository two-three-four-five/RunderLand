/******************************************************************************
 * File: CameraAccessFeature.cs
 * Copyright (c) 2023 Qualcomm Technologies, Inc. and/or its subsidiaries. All rights reserved.
 *
 * Confidential and Proprietary - Qualcomm Technologies, Inc.
 *
 ******************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.OpenXR;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.XR.OpenXR.Features;
#endif
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

namespace Qualcomm.Snapdragon.Spaces
{
#if UNITY_EDITOR
    [OpenXRFeature(
        UiName = FeatureName,
        BuildTargetGroups = new[] { BuildTargetGroup.Android },
        Company = "Qualcomm",
        Desc = "Enables Camera Access feature on Snapdragon Spaces enabled devices",
        DocumentationLink = "",
        OpenxrExtensionStrings = FeatureExtensions,
        Version = "0.14.0",
        Required = false,
        Category = FeatureCategory.Feature,
        FeatureId = FeatureID)]
#endif
    internal sealed partial class CameraAccessFeature : SpacesOpenXRFeature
    {
        public const string FeatureName = "Camera Access (Experimental)";
        public const string FeatureID = "com.qualcomm.snapdragon.spaces.cameraaccess";
        public const string FeatureExtensions = "XR_QCOMX_camera_frame_access";
        private static List<XRCameraSubsystemDescriptor> _cameraSubsystemDescriptors = new List<XRCameraSubsystemDescriptor>();
        private BaseRuntimeFeature _baseRuntimeFeature;

        private List<XrCameraInfoQCOM> _cameraInfos = new List<XrCameraInfoQCOM>();
        private XrCameraFrameBufferQCOM _defaultFrameBuffer;
        private XrCameraSensorPropertiesQCOM _defaultSensorProperties;

        private XrCameraFrameBufferQCOM[] _frameBuffers;

        private bool _frameReleased = true;

        private XrCameraFrameDataQCOM _lastFrame;

        private XrCameraSensorPropertiesQCOM[] _sensorProperties;
        internal List<XrCameraInfoQCOM> CameraInfos => _cameraInfos;
        internal XrCameraSensorPropertiesQCOM[] SensorProperties => _sensorProperties;
        internal XrCameraFrameDataQCOM LastFrame => _lastFrame;
        internal XrCameraFrameBufferQCOM[] FrameBuffers => _frameBuffers;

        protected override bool IsRequiringBaseRuntimeFeature => true;

        protected override string GetXrLayersToLoad()
        {
            return "XR_APILAYER_QCOM_retina_tracking";
        }

        protected override bool OnInstanceCreate(ulong instanceHandle)
        {
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
            base.OnInstanceCreate(instanceHandle);

            _baseRuntimeFeature = OpenXRSettings.Instance.GetFeature<BaseRuntimeFeature>();

            var missingExtensions = GetMissingExtensions(FeatureExtensions);
            if (missingExtensions.Any())
            {
                Debug.Log(FeatureName + " is missing following extension in the runtime: " + String.Join(",", missingExtensions));
                return false;
            }
#if UNITY_ANDROID
            if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
            {
                Debug.LogError(FeatureName + " Feature is missing the camera permissions and can't be created therefore!");
                return false;
            }
#endif
            return true;
        }

        protected override void OnSubsystemCreate()
        {
            CreateSubsystem<XRCameraSubsystemDescriptor, XRCameraSubsystem>(_cameraSubsystemDescriptors, CameraSubsystem.ID);
        }

        protected override void OnSubsystemStop()
        {
            StopSubsystem<XRCameraSubsystem>();
        }

        protected override void OnSubsystemDestroy()
        {
            DestroySubsystem<XRCameraSubsystem>();
        }

        protected override void OnHookMethods()
        {
            // TODO: Change to QCOM upon release
            HookMethod("xrEnumerateCamerasQCOMX", out _xrEnumerateCamerasQCOM);
            HookMethod("xrGetSupportedFrameConfigurationsQCOMX", out _xrGetSupportedFrameConfigurationsQCOM);
            HookMethod("xrCreateCameraHandleQCOMX", out _xrCreateCameraHandleQCOM);
            HookMethod("xrReleaseCameraHandleQCOMX", out _xrReleaseCameraHandleQCOM);
            HookMethod("xrAccessFrameQCOMX", out _xrAccessFrameQCOM);
            HookMethod("xrReleaseFrameQCOMX", out _xrReleaseFrameQCOM);
        }

        public bool TryEnumerateCameras()
        {
            _cameraInfos = new List<XrCameraInfoQCOM>();

            if (_xrEnumerateCamerasQCOM == null)
            {
                Debug.LogError("xrEnumerateCamerasQCOM method not found!");
                return false;
            }

            uint cameraInfoCountOutput = 0;

            var result = _xrEnumerateCamerasQCOM(SessionHandle, 0, ref cameraInfoCountOutput, IntPtr.Zero);
            if (result != XrResult.XR_SUCCESS)
            {
                Debug.LogError("Enumerate device cameras (1) failed: " + Enum.GetName(typeof(XrResult), result));
                return false;
            }

            int size = Marshal.SizeOf(typeof(XrCameraInfoQCOM)) * (int)cameraInfoCountOutput;
            IntPtr cameraInfosPtr = Marshal.AllocHGlobal(size);
            var defaultCameraInfo = new XrCameraInfoQCOM(String.Empty, 0, 0);
            for (int i = 0; i < cameraInfoCountOutput; i++)
            {
                Marshal.StructureToPtr(defaultCameraInfo, cameraInfosPtr + (Marshal.SizeOf(typeof(XrCameraInfoQCOM)) * i), false);
            }

            result = _xrEnumerateCamerasQCOM(SessionHandle, cameraInfoCountOutput, ref cameraInfoCountOutput, cameraInfosPtr);
            if (result != XrResult.XR_SUCCESS)
            {
                Debug.LogError("Enumerate device cameras (2) failed: " + Enum.GetName(typeof(XrResult), result));
                Marshal.FreeHGlobal(cameraInfosPtr);
                return false;
            }

            for (int i = 0; i < cameraInfoCountOutput; i++)
            {
                XrCameraInfoQCOM cameraInfo = Marshal.PtrToStructure<XrCameraInfoQCOM>(cameraInfosPtr + (Marshal.SizeOf(typeof(XrCameraInfoQCOM)) * i));
                _cameraInfos.Add(cameraInfo);
            }

            // Initialise default frame access structures for convenience
            //
            // XR_MAX_CAMERA_RADIAL_DISTORSION_PARAMS_LENGTH_QCOMX == 6
            // XR_MAX_CAMERA_TANGENTIAL_DISTORSION_PARAMS_LENGTH_QCOMX == 2
            //
            // Marshal.SizeOf(XrCameraFramePlaneQCOMX) == 32
            // XR_CAMERA_FRAME_PLANES_SIZE_QCOMX == 4

            var defaultSensorIntrinsics = new XrCameraSensorIntrinsicsQCOM(
                new XrVector2f(Vector2.zero),
                new XrVector2f(Vector2.zero),
                new float[6],
                new float[2],
                0);
            _defaultSensorProperties = new XrCameraSensorPropertiesQCOM(
                defaultSensorIntrinsics,
                new XrPosef(new XrQuaternionf(Quaternion.identity), new XrVector3f(Vector3.zero)),
                new XrOffset2Di(Vector2Int.zero),
                new XrExtent2Di(Vector2Int.zero),
                0,
                0);
            _defaultFrameBuffer = new XrCameraFrameBufferQCOM(
                0,
                IntPtr.Zero,
                0,
                new byte[32 * 4]);

            Marshal.FreeHGlobal(cameraInfosPtr);
            return true;
        }

        public List<XrCameraFrameConfigurationQCOM> TryGetSupportedFrameConfigurations(string cameraSet)
        {
            var defaultReturn = new List<XrCameraFrameConfigurationQCOM>();

            if (_xrGetSupportedFrameConfigurationsQCOM == null)
            {
                Debug.LogError("xrGetSupportedFrameConfigurationsQCOM method not found!");
                return defaultReturn;
            }

            uint frameConfigurationCountOutput = 0;

            var result = _xrGetSupportedFrameConfigurationsQCOM(SessionHandle, cameraSet, 0, ref frameConfigurationCountOutput, IntPtr.Zero);
            if (result != XrResult.XR_SUCCESS)
            {
                Debug.LogError("Failed to get Supported Frame Configurations (1): " + Enum.GetName(typeof(XrResult), result));
                return defaultReturn;
            }

            int size = Marshal.SizeOf(typeof(XrCameraFrameConfigurationQCOM)) * (int)frameConfigurationCountOutput;
            IntPtr frameConfigurationsPtr = Marshal.AllocHGlobal(size);
            var defaultFrameConfig = new XrCameraFrameConfigurationQCOM(0, String.Empty, new XrExtent2Di(0, 0), 0, 0, 0, 0);
            for (int i = 0; i < frameConfigurationCountOutput; i++)
            {
                Marshal.StructureToPtr(defaultFrameConfig, frameConfigurationsPtr + (Marshal.SizeOf(typeof(XrCameraFrameConfigurationQCOM)) * i), false);
            }

            result = _xrGetSupportedFrameConfigurationsQCOM(SessionHandle, cameraSet, frameConfigurationCountOutput, ref frameConfigurationCountOutput, frameConfigurationsPtr);
            if (result != XrResult.XR_SUCCESS)
            {
                Debug.LogError("Failed to get Supported Frame Configurations (2): " + Enum.GetName(typeof(XrResult), result));
                Marshal.FreeHGlobal(frameConfigurationsPtr);
                return defaultReturn;
            }

            var frameConfigurations = new List<XrCameraFrameConfigurationQCOM>();
            for (int i = 0; i < frameConfigurationCountOutput; i++)
            {
                XrCameraFrameConfigurationQCOM frameConfiguration = Marshal.PtrToStructure<XrCameraFrameConfigurationQCOM>(frameConfigurationsPtr + (Marshal.SizeOf(typeof(XrCameraFrameConfigurationQCOM)) * i));
                frameConfigurations.Add(frameConfiguration);
            }

            Marshal.FreeHGlobal(frameConfigurationsPtr);
            return frameConfigurations;
        }

        public bool TryCreateCameraHandle(out ulong cameraHandle, string cameraSet)
        {
            cameraHandle = 0;

            if (_xrCreateCameraHandleQCOM == null)
            {
                Debug.LogError("xrCreateCameraHandleQCOM method not found!");
                return false;
            }

            XrCameraActivationInfoQCOM activationInfo = new XrCameraActivationInfoQCOM(cameraSet);

            var result = _xrCreateCameraHandleQCOM(SessionHandle, ref activationInfo, ref cameraHandle);
            if (result != XrResult.XR_SUCCESS)
            {
                Debug.LogError("Failed to create camera handle: " + Enum.GetName(typeof(XrResult), result));
                return false;
            }

            return true;
        }

        public bool TryReleaseCameraHandle(ulong cameraHandle)
        {
            if (_xrReleaseCameraHandleQCOM == null)
            {
                Debug.LogError("xrReleaseCameraHandleQCOM method not found!");
                return false;
            }

            var result = _xrReleaseCameraHandleQCOM(cameraHandle);
            if (result != XrResult.XR_SUCCESS)
            {
                Debug.LogError("Failed to release camera handle: " + Enum.GetName(typeof(XrResult), result));
                return false;
            }

            return true;
        }

        public bool TryAccessFrame(ulong cameraHandle, XrCameraFrameConfigurationQCOM frameConfig, uint sensorCount)
        {
            if (!_frameReleased && !TryReleaseFrame())
            {
                Debug.LogError("Failed to clear frame buffer before requesting frame.");
                return false;
            }

            if (_xrAccessFrameQCOM == null)
            {
                Debug.LogError("xrAccessFrameQCOM method not found!");
                return false;
            }

            // Create XrCameraSensorInfosQCOM structure
            IntPtr sensorPropertiesPtr = IntPtr.Zero;
            IntPtr sensorInfosPtr = IntPtr.Zero;
            GCHandle pinnedSensorInfos = new GCHandle();

            int sensorPropertiesSize = Marshal.SizeOf(typeof(XrCameraSensorPropertiesQCOM)) * (int)sensorCount;
            sensorPropertiesPtr = Marshal.AllocHGlobal(sensorPropertiesSize);
            for (int i = 0; i < sensorCount; i++)
            {
                Marshal.StructureToPtr(_defaultSensorProperties, sensorPropertiesPtr + (Marshal.SizeOf(typeof(XrCameraSensorPropertiesQCOM)) * i), false);
            }

            XrCameraSensorInfosQCOM sensorInfos = new XrCameraSensorInfosQCOM(sensorCount, sensorPropertiesPtr);
            pinnedSensorInfos = GCHandle.Alloc(sensorInfos, GCHandleType.Pinned);
            sensorInfosPtr = pinnedSensorInfos.AddrOfPinnedObject();

            // Create XrCameraFrameBuffersQCOM structure
            int frameBufferArraySize = Marshal.SizeOf(typeof(XrCameraFrameBufferQCOM)) * (int)frameConfig.FrameBufferCount;
            IntPtr fbArrayPtr = Marshal.AllocHGlobal(frameBufferArraySize);
            for (int i = 0; i < (int)frameConfig.FrameBufferCount; i++)
            {
                Marshal.StructureToPtr(_defaultFrameBuffer, fbArrayPtr + (Marshal.SizeOf(typeof(XrCameraFrameBufferQCOM)) * i), false);
            }

            XrCameraFrameBuffersQCOM frameBuffersStruct = new XrCameraFrameBuffersQCOM(
                sensorInfosPtr,
                frameConfig.FrameBufferCount,
                fbArrayPtr
            );
            GCHandle pinnedFrameBuffersStruct = GCHandle.Alloc(frameBuffersStruct, GCHandleType.Pinned);
            IntPtr fbStructPtr = pinnedFrameBuffersStruct.AddrOfPinnedObject();

            // Request data from runtime
            XrCameraFrameDataQCOM frameData = new XrCameraFrameDataQCOM(fbStructPtr);
            var result = _xrAccessFrameQCOM(cameraHandle, ref frameData);
            if (result != XrResult.XR_SUCCESS)
            {
                Debug.LogError("Failed to access frame: " + Enum.GetName(typeof(XrResult), result));
                Marshal.FreeHGlobal(sensorPropertiesPtr);
                pinnedSensorInfos.Free();
                Marshal.FreeHGlobal(fbArrayPtr);
                pinnedFrameBuffersStruct.Free();
                return false;
            }

            _frameReleased = false;

            // Skip received frame if it is the same as the last one
            if (_lastFrame.Handle == frameData.Handle)
            {
                if (!TryReleaseFrame())
                {
                    Debug.LogWarning("Could not release frame after requesting it.");
                }

                return true;
            }

            _lastFrame = frameData;

            // Extract sensor data
            _sensorProperties = new XrCameraSensorPropertiesQCOM[sensorCount];
            for (int i = 0; i < sensorCount; i++)
            {
                _sensorProperties[i] = Marshal.PtrToStructure<XrCameraSensorPropertiesQCOM>(sensorPropertiesPtr +
                    (Marshal.SizeOf(typeof(XrCameraSensorPropertiesQCOM)) * i));
            }

            Marshal.FreeHGlobal(sensorPropertiesPtr);
            pinnedSensorInfos.Free();

            // Extract frame data
            _frameBuffers = new XrCameraFrameBufferQCOM[frameConfig.FrameBufferCount];
            for (int i = 0; i < frameConfig.FrameBufferCount; i++)
            {
                _frameBuffers[i] = Marshal.PtrToStructure<XrCameraFrameBufferQCOM>(fbArrayPtr + (Marshal.SizeOf(typeof(XrCameraFrameBufferQCOM)) * i));
            }

            Marshal.FreeHGlobal(fbArrayPtr);
            pinnedFrameBuffersStruct.Free();

            if (!TryReleaseFrame())
            {
                Debug.LogWarning("Could not release frame after requesting it.");
            }

            return true;
        }

        public bool TryReleaseFrame()
        {
            if (_frameReleased)
            {
                Debug.LogWarning("Skipped releasing last frame: already released.");
                return true;
            }

            if (_xrReleaseFrameQCOM == null)
            {
                Debug.LogError("xrReleaseFrameQCOM method not found!");
                return false;
            }

            var result = _xrReleaseFrameQCOM(_lastFrame.Handle);
            if (result != XrResult.XR_SUCCESS)
            {
                Debug.LogError("Failed to release frame: " + Enum.GetName(typeof(XrResult), result));
                return false;
            }

            _frameReleased = true;
            return true;
        }
    }
}
