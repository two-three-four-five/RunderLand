/******************************************************************************
 * File: PlaneDetectionFeature.cs
 * Copyright (c) 2022-2023 Qualcomm Technologies, Inc. and/or its subsidiaries. All rights reserved.
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

namespace Qualcomm.Snapdragon.Spaces
{
#if UNITY_EDITOR
    [OpenXRFeature(UiName = FeatureName,
        BuildTargetGroups = new[]
        {
            BuildTargetGroup.Android
        },
        Company = "Qualcomm",
        Desc = "Enables Plane Detection feature on Snapdragon Spaces enabled devices",
        DocumentationLink = "",
        OpenxrExtensionStrings = XR_QCOM_FeatureExtensions + " " + XR_MSFT_FeatureExtensions,
        Version = "0.14.0",
        Required = false,
        Category = FeatureCategory.Feature,
        FeatureId = FeatureID)]
#endif
    internal sealed partial class PlaneDetectionFeature : SpacesOpenXRFeature
    {
        public const string FeatureName = "Plane Detection";
        public const string FeatureID = "com.qualcomm.snapdragon.spaces.planedetection";
        public const string XR_QCOM_FeatureExtensions = "XR_QCOM_plane_detection";
        public const string XR_MSFT_FeatureExtensions = "XR_MSFT_scene_understanding";
        public bool UseSceneUnderstandingPlaneDetection;
        public bool ConvexHullEnabled = true;
        private static readonly List<XRPlaneSubsystemDescriptor> _planeSubsystemDescriptors = new List<XRPlaneSubsystemDescriptor>();

        private static readonly Dictionary<ulong, PlaneDataCollection> _planesDataMap =
            new Dictionary<ulong, PlaneDataCollection>();

        private BaseRuntimeFeature _baseRuntimeFeature;
        private ulong _activePlaneDetectionHandle;
        private List<string> _subscribedSubsystems;
        public bool IsRunning => _activePlaneDetectionHandle != 0;
        public ulong ActiveHandle => _activePlaneDetectionHandle;
        protected override bool IsRequiringBaseRuntimeFeature => true;

        public void RegisterProviderWithSceneObserver(string subsystemId)
        {
            if (UseSceneUnderstandingPlaneDetection)
            {
                Internal_RegisterProviderWithSceneObserver(subsystemId,
                    (int)(XrSceneComputeFeatureMSFT.XR_SCENE_COMPUTE_FEATURE_PLANE_MSFT |
                        XrSceneComputeFeatureMSFT.XR_SCENE_COMPUTE_FEATURE_PLANE_MESH_MSFT));
            }
        }

        public void UnregisterProviderWithSceneObserver(string subsystemId)
        {
            if (UseSceneUnderstandingPlaneDetection)
            {
                Internal_UnregisterProviderWithSceneObserver(subsystemId);
            }
        }

        public ulong TryCreatePlaneDetection(string subsystemID, PlaneDetectionMode planeDetectionMode, bool enableConvexHull)
        {
            if (UseSceneUnderstandingPlaneDetection)
            {
                return 0;
            }

            if (_xrCreatePlaneDetectionQCOM == null)
            {
                Debug.LogError("XrCreatePlaneDetectionQCOM method not found!");
                return 0;
            }

            if (planeDetectionMode == PlaneDetectionMode.None)
            {
                Debug.LogWarning("Won't create Plane Detection feature if PlaneDetectionMode is 'None'!");
                return 0;
            }

            if (_subscribedSubsystems.Contains(subsystemID))
            {
                Debug.LogWarning("Won't create Plane Detection for subsystem " + subsystemID + " , because it already created it before.");
                return 0;
            }

            _subscribedSubsystems.Add(subsystemID);
            if (subsystemID != PlaneDetectionSubsystem.ID && subsystemID != RaycastSubsystem.ID)
            {
                Debug.LogWarning("Won't create Plane Detection for unsupported subsystem " + subsystemID);
                return 0;
            }

            if (_activePlaneDetectionHandle != 0 && _subscribedSubsystems.Count != 0)
            {
                if (_subscribedSubsystems[0] == PlaneDetectionSubsystem.ID && subsystemID == RaycastSubsystem.ID)
                {
                    Debug.LogWarning("The Plane Detection subsystem was started before the Ray Casting subsystem. The hit results depend on what was selected for the previous one. To get the best hit results, please ensure that the Plane Detection subsystem was started with convex hull for planes enabled.");
                    return _activePlaneDetectionHandle;
                }

                if (_subscribedSubsystems[0] == RaycastSubsystem.ID && subsystemID == PlaneDetectionSubsystem.ID)
                {
                    Debug.LogWarning("The Ray Casting subsystem was started before the Plane Detection subsystem. The underlying Plane Detection dependency will be restarted for the later one and the hit results depend on what was selected for that subsystem. To get the best hit results, please ensure that the Plane Detection subsystem was started with convex hull for planes enabled.");
                    TryDestroyPlaneDetection();
                }
            }

            if (_xrCreatePlaneDetectionQCOM != null)
            {
                var planeFilter = PlaneDetectionModeToXrPlaneFilter(planeDetectionMode);
                var planeDetectionCreateInfo = new XrPlaneDetectionCreateInfoQCOM(planeFilter, enableConvexHull);
                var callResult = _xrCreatePlaneDetectionQCOM(SessionHandle, ref planeDetectionCreateInfo, ref _activePlaneDetectionHandle);
                if (callResult != XrResult.XR_SUCCESS)
                {
                    Debug.LogError("Creating Plane Detection failed with error: " + Enum.GetName(typeof(XrResult), callResult));
                }
            }

            return _activePlaneDetectionHandle;
        }

        public bool TryDestroyPlaneDetection(string subsystemID)
        {
            _subscribedSubsystems.Remove(subsystemID);
            if (_subscribedSubsystems.Count > 1)
            {
                Debug.LogWarning("Plane Detection is still needed and won't be destroyed!");
                return false;
            }

            return TryDestroyPlaneDetection();
        }

        public bool TryGetPlaneDetectionState()
        {
            if (UseSceneUnderstandingPlaneDetection)
            {
                return true;
            }

            if (_xrGetPlaneDetectionStateQCOM == null)
            {
                Debug.LogError("XrGetPlaneDetectionStateQCOM method not found!");
                return false;
            }

            XrPlaneDetectionState state = XrPlaneDetectionState.XR_PLANE_DETECTION_STATE_NONE_QCOM;
            XrResult result = _xrGetPlaneDetectionStateQCOM(_activePlaneDetectionHandle, ref state);
            if (result != XrResult.XR_SUCCESS)
            {
                Debug.LogError("Failed to get plane detection state : " + result);
                return false;
            }

            if (state != XrPlaneDetectionState.XR_PLANE_DETECTION_STATE_TRACKING_QCOM)
            {
                return false;
            }

            return true;
        }

        public bool TryLocatePlanes(out List<Plane> updatedPlanes)
        {
            if (UseSceneUnderstandingPlaneDetection)
            {
                return TryLocatePlanes_XR_MSFT_Scene_Understanding(out updatedPlanes);
            }

            return TryLocatePlanes_XR_QCOM_Plane_Detection(out updatedPlanes);
        }

        public bool TryGetPlaneConvexHullVertexBuffer(ulong convexHullBufferId, ref List<Vector2> vertexPositions)
        {
            if (UseSceneUnderstandingPlaneDetection)
            {
                return TryGetPlaneConvexHullVertexBuffer_XR_MSFT_Scene_Understanding(convexHullBufferId, ref vertexPositions);
            }

            return TryGetPlaneConvexHullVertexBuffer_XR_QCOM_Plane_Detection(convexHullBufferId, ref vertexPositions);
        }

        protected override string GetXrLayersToLoad()
        {
            if (UseSceneUnderstandingPlaneDetection)
            {
                return "XR_APILAYER_QCOM_scene_understanding";
            }

            return "XR_APILAYER_QCOM_retina_tracking";
        }

        protected override IntPtr HookGetInstanceProcAddr(IntPtr func)
        {
            if (UseSceneUnderstandingPlaneDetection)
            {
                return Internal_GetInterceptedInstanceProcAddr(func);
            }

            return base.HookGetInstanceProcAddr(func);
        }

        protected override bool OnInstanceCreate(ulong instanceHandle)
        {
            base.OnInstanceCreate(instanceHandle);
            _baseRuntimeFeature = OpenXRSettings.Instance.GetFeature<BaseRuntimeFeature>();
            _subscribedSubsystems = new List<string>();

            // When using XR_MSFT_Scene_Understanding, plane detection 'can' use XR_MSFT 3dr scene understanding to generate planes.
            // When not using XR_MSFT_Scene_Understanding, plane detection should always use XR_QCOM retina plane detection.
            if (UseSceneUnderstandingPlaneDetection)
            {
                UseSceneUnderstandingPlaneDetection &= OpenXRRuntime.IsExtensionEnabled(XR_MSFT_FeatureExtensions);

                if (!UseSceneUnderstandingPlaneDetection)
                {
                    Debug.LogWarning("Scene understanding was not enabled, although the feature option to Use Scene Understanding Plane Detection was requested. Falling back to not use scene understanding.");
                }
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            if (!_baseRuntimeFeature.CheckServicesCameraPermissions())
            {
                Debug.LogError("The Plane Detection Feature is missing the camera permissions and can't be created therefore!");
                return false;
            }
#endif
            if (UseSceneUnderstandingPlaneDetection)
            {
                Internal_SetInstanceHandle(instanceHandle);
            }

            IEnumerable<string> missingExtensions;
            if (UseSceneUnderstandingPlaneDetection)
            {
                missingExtensions = GetMissingExtensions(XR_MSFT_FeatureExtensions);
            }
            else
            {
                missingExtensions = GetMissingExtensions(XR_QCOM_FeatureExtensions);
            }

            if (missingExtensions.Any())
            {
                Debug.Log(FeatureName + " is missing following extension in the runtime: " + String.Join(",", missingExtensions));
                return false;
            }

            return true;
        }

        protected override void OnSubsystemCreate()
        {
            CreateSubsystem<XRPlaneSubsystemDescriptor, XRPlaneSubsystem>(_planeSubsystemDescriptors, PlaneDetectionSubsystem.ID);
        }

        protected override void OnSubsystemStop()
        {
            StopSubsystem<XRPlaneSubsystem>();
        }

        protected override void OnSubsystemDestroy()
        {
            DestroySubsystem<XRPlaneSubsystem>();
        }

        protected override void OnSessionCreate(ulong sessionHandle)
        {
            base.OnSessionCreate(sessionHandle);
            if (UseSceneUnderstandingPlaneDetection)
            {
                Internal_SetSessionHandle(sessionHandle);
            }
        }

        protected override void OnAppSpaceChange(ulong spaceHandle)
        {
            base.OnAppSpaceChange(spaceHandle);
            if (UseSceneUnderstandingPlaneDetection)
            {
                Internal_SetSpaceHandle(spaceHandle);
            }
        }

        protected override void OnHookMethods()
        {
            HookMethod("xrCreatePlaneDetectionQCOM", out _xrCreatePlaneDetectionQCOM);
            HookMethod("xrDestroyPlaneDetectionQCOM", out _xrDestroyPlaneDetectionQCOM);
            HookMethod("xrLocatePlanesQCOM", out _xrLocatePlanesQCOM);
            HookMethod("xrGetPlaneDetectionStateQCOM", out _xrGetPlaneDetectionStateQCOM);
            HookMethod("xrGetPlaneConvexHullVertexBufferQCOM", out _xrGetPlaneConvexHullVertexBufferQCOM);
        }

        private XrPlaneFilterQCOM PlaneDetectionModeToXrPlaneFilter(PlaneDetectionMode planeDetectionMode)
        {
            switch (planeDetectionMode)
            {
                case PlaneDetectionMode.Horizontal:
                    return XrPlaneFilterQCOM.XR_PLANE_FILTER_HORIZONTAL_QCOM;
                case PlaneDetectionMode.Vertical:
                    return XrPlaneFilterQCOM.XR_PLANE_FILTER_VERTICAL_QCOM;
                default:
                    return XrPlaneFilterQCOM.XR_PLANE_FILTER_ANY_QCOM;
            }
        }

        private bool TryDestroyPlaneDetection()
        {
            if (UseSceneUnderstandingPlaneDetection)
            {
                return true;
            }

            if (_xrDestroyPlaneDetectionQCOM == null)
            {
                Debug.LogError("XrDestroyPlaneDetectionQCOM method not found!");
                return false;
            }

            XrResult result = _xrDestroyPlaneDetectionQCOM(_activePlaneDetectionHandle);
            if (result != XrResult.XR_SUCCESS)
            {
                Debug.LogError("Failed to destroy Plane Detection");
                return false;
            }

            _activePlaneDetectionHandle = 0;
            return true;
        }

        private bool TryLocatePlanes_XR_QCOM_Plane_Detection(out List<Plane> updatedPlanes)
        {
            updatedPlanes = new List<Plane>();
            if (!TryGetPlaneDetectionState())
            {
                return false;
            }

            if (_xrLocatePlanesQCOM == null)
            {
                Debug.LogError("XrLocatePlanesQCOM method not found!");
                return false;
            }

            XrPlanesLocateInfoQCOM locateInfo =
                new XrPlanesLocateInfoQCOM(SpaceHandle, _baseRuntimeFeature.PredictedDisplayTime);
            IntPtr planeCountOutputPtr = Marshal.AllocHGlobal(Marshal.SizeOf<uint>());
            XrPlaneLocationsQCOM locations = new XrPlaneLocationsQCOM(planeCountOutputPtr);
            XrResult result = _xrLocatePlanesQCOM(_activePlaneDetectionHandle, ref locateInfo, ref locations);
            if (result != XrResult.XR_SUCCESS)
            {
                Debug.LogError("Failed to get plane count output: " + result);
                Marshal.FreeHGlobal(planeCountOutputPtr);
                return false;
            }

            uint planeLocationsCount = Marshal.PtrToStructure<uint>(locations.PlaneCountOutput);
            int planeLocationHandleSize = Marshal.SizeOf(typeof(XrPlaneLocationQCOM));
            IntPtr planeLocationsPtr = Marshal.AllocHGlobal(planeLocationHandleSize * (int)planeLocationsCount);
            locations = new XrPlaneLocationsQCOM(planeLocationsCount, planeCountOutputPtr, planeLocationsPtr);
            result = _xrLocatePlanesQCOM(_activePlaneDetectionHandle, ref locateInfo, ref locations);
            if (result != XrResult.XR_SUCCESS)
            {
                Debug.LogError("Failed to get plane locations: " + result);
                Marshal.FreeHGlobal(planeLocationsPtr);
                Marshal.FreeHGlobal(planeCountOutputPtr);
                return false;
            }

            uint planeCountOutput = Marshal.PtrToStructure<uint>(planeCountOutputPtr);
            Marshal.FreeHGlobal(planeCountOutputPtr);
            for (int i = 0; i < planeCountOutput; i++)
            {
                IntPtr planeLocationPtr = planeLocationsPtr + (Marshal.SizeOf(typeof(XrPlaneLocationQCOM)) * i);
                var planeLocation = Marshal.PtrToStructure<XrPlaneLocationQCOM>(planeLocationPtr);
                updatedPlanes.Add(new Plane(planeLocation.GetBoundedPlane(_activePlaneDetectionHandle),
                    planeLocation.ConvexHullId));
            }

            Marshal.FreeHGlobal(planeLocationsPtr);
            return true;
        }

        private bool TryLocatePlanes_XR_MSFT_Scene_Understanding(out List<Plane> updatedPlanes)
        {
            updatedPlanes = new List<Plane>();
            if (!Internal_UpdateObservedScene(PlaneDetectionSubsystem.ID))
            {
                Debug.LogError("Failed to update observed scene!");
                return false;
            }

            if (!Internal_UpdatePlanes())
            {
                Debug.LogError("Failed to update planes!");
                return false;
            }

            uint scenePlaneCount = 0;
            if (!Internal_CountScenePlanes(ref scenePlaneCount))
            {
                Debug.LogError("Failed to count planes!");
                return false;
            }

            if (scenePlaneCount == 0)
            {
                Debug.LogError("No scene planes found!");
                return false;
            }

            int sizeOfScenePlanesBytes = Marshal.SizeOf<SceneUnderstandingMSFTPlane>() * (int)scenePlaneCount;
            IntPtr scenePlanesPtr = Marshal.AllocHGlobal(sizeOfScenePlanesBytes);
            if (!Internal_FetchScenePlanes(scenePlaneCount, scenePlanesPtr))
            {
                Debug.LogError("Failed to fetch planes from the scene!");
                Marshal.FreeHGlobal(scenePlanesPtr);
                return false;
            }

            Quaternion openXrCorrection = Quaternion.AngleAxis(-90.0f, Vector3.right);
            for (int planeIx = 0; planeIx < (int)scenePlaneCount; ++planeIx)
            {
                IntPtr planePtr = scenePlanesPtr + (Marshal.SizeOf<SceneUnderstandingMSFTPlane>() * planeIx);
                SceneUnderstandingMSFTPlane plane = Marshal.PtrToStructure<SceneUnderstandingMSFTPlane>(planePtr);
                int sizeOfXrVector3fBytes = Marshal.SizeOf<XrVector3f>();
                int sizeOfUintBytes = Marshal.SizeOf<uint>();
                IntPtr verticesPtr = Marshal.AllocHGlobal(sizeOfXrVector3fBytes * (int)plane.VertexCount);
                IntPtr indicesPtr = Marshal.AllocHGlobal(sizeOfUintBytes * (int)plane.IndexCount);
                if (!Internal_FetchPlaneVertices(planePtr, verticesPtr, indicesPtr))
                {
                    Debug.LogError($"Failed to fetch vertices from plane {planeIx}");
                    Marshal.FreeHGlobal(verticesPtr);
                    Marshal.FreeHGlobal(indicesPtr);
                    continue;
                }

                List<Vector3> vertexList = new List<Vector3>();
                for (int vertexIx = 0; vertexIx < plane.VertexCount; ++vertexIx)
                {
                    IntPtr nextVertexPtr = verticesPtr + (sizeOfXrVector3fBytes * vertexIx);
                    Vector3 vertex = Marshal.PtrToStructure<XrVector3f>(nextVertexPtr).ToVector3();
                    vertexList.Add(vertex);
                }

                List<uint> indexList = new List<uint>();
                for (int indexIx = 0; indexIx < plane.IndexCount; ++indexIx)
                {
                    IntPtr nextIndexPtr = indicesPtr + (sizeOfUintBytes * indexIx);
                    uint index = Marshal.PtrToStructure<uint>(nextIndexPtr);
                    indexList.Add(index);
                }

                Pose replacementPose = plane.Pose;
                replacementPose.rotation *= openXrCorrection;
                BoundedPlane boundedPlane = plane.GetBoundedPlane(replacementPose);
                updatedPlanes.Add(new Plane(boundedPlane, boundedPlane.trackableId.subId2));
                PlaneDataCollection planeData = new PlaneDataCollection();
                planeData.vertices = vertexList;
                planeData.indices = indexList;
                planeData.extents = boundedPlane.extents;
                if (!ConvexHullEnabled)
                {
                    if (vertexList.Count >= 3 && indexList.Count >= 3)
                    {
                        // Fetch 3 vertices.
                        var v1 = vertexList[(int)indexList[0]];
                        // Next two indices flipped because of winding order changes
                        var v2 = vertexList[(int)indexList[2]];
                        var v3 = vertexList[(int)indexList[1]];

                        // Calculate the determinant of any 3 vertices in the vertex list
                        // to work out winding order for extents planes.
                        // This is necessary to draw extents planes on ceilings oriented correctly.
                        // Otherwise extents plane can point in opposite direction to convex hull plane
                        planeData.reverseExtentPlaneWindingOrder = (v3.x * v2.y) + (v1.x * v3.y) + (v1.y * v2.x) - ((v1.y * v3.x) + (v3.y * v2.x) + (v1.x * v2.y)) < 0;
                    }
                }

                if (_planesDataMap.ContainsKey(boundedPlane.trackableId.subId2))
                {
                    _planesDataMap[boundedPlane.trackableId.subId2] = planeData;
                }
                else
                {
                    _planesDataMap.Add(boundedPlane.trackableId.subId2, planeData);
                }

                Marshal.FreeHGlobal(verticesPtr);
                Marshal.FreeHGlobal(indicesPtr);
            }

            Marshal.FreeHGlobal(scenePlanesPtr);
            return true;
        }

        private bool TryGetPlaneConvexHullVertexBuffer_XR_QCOM_Plane_Detection(ulong convexHullBufferId, ref List<Vector2> vertexPositions)
        {
            if (_xrGetPlaneConvexHullVertexBufferQCOM == null)
            {
                Debug.LogError("XrGetPlaneConvexHullVertexBufferQCOM method not found!");
                return false;
            }

            XrPlaneConvexHullBufferInfoQCOM convexHullInfo = new XrPlaneConvexHullBufferInfoQCOM(convexHullBufferId);
            IntPtr vertexCapacityOutputPtr = Marshal.AllocHGlobal(Marshal.SizeOf<uint>());
            XrPlaneConvexHullVertexBufferQCOM vertexBuffer = new XrPlaneConvexHullVertexBufferQCOM(vertexCapacityOutputPtr);
            XrResult result =
                _xrGetPlaneConvexHullVertexBufferQCOM(_activePlaneDetectionHandle, ref convexHullInfo, ref vertexBuffer);
            if (result != XrResult.XR_SUCCESS)
            {
                Debug.LogError("Failed to get convex hull buffer count output: " + result);
                Marshal.FreeHGlobal(vertexCapacityOutputPtr);
                return false;
            }

            uint vertexCapacityInput = Marshal.PtrToStructure<uint>(vertexCapacityOutputPtr);
            IntPtr verticesPtr = Marshal.AllocHGlobal(Marshal.SizeOf<XrVector3f>() * (int)vertexCapacityInput);
            vertexBuffer = new XrPlaneConvexHullVertexBufferQCOM(vertexCapacityInput, vertexCapacityOutputPtr, verticesPtr);
            result = _xrGetPlaneConvexHullVertexBufferQCOM(_activePlaneDetectionHandle, ref convexHullInfo, ref vertexBuffer);
            if (result != XrResult.XR_SUCCESS)
            {
                Debug.LogError("Failed to get convex hull vertex buffer: " + result);
                Marshal.FreeHGlobal(vertexCapacityOutputPtr);
                Marshal.FreeHGlobal(verticesPtr);
                return false;
            }

            uint vertexCount = Marshal.PtrToStructure<uint>(vertexCapacityOutputPtr);
            Marshal.FreeHGlobal(vertexCapacityOutputPtr);

            // NOTE(AF): Traverse the OpenXR in inverse order because changing the coordinate system handedness.
            // also changes the winding order. Without this, the plane meshes would face the wrong way.
            for (int i = (int)vertexCount - 1; i >= 0; i--)
            {
                IntPtr vertexPtr = verticesPtr + (Marshal.SizeOf<XrVector3f>() * i);
                Vector3 vertexPosition = Marshal.PtrToStructure<XrVector3f>(vertexPtr).ToVector3();

                // NOTE(AF): Flip the Y position to account for the conversion from OpenXR to Unity coordinate system.
                vertexPositions.Add(new Vector2(vertexPosition.x, -vertexPosition.y));
            }

            Marshal.FreeHGlobal(verticesPtr);
            return true;
        }

        private bool TryGetPlaneConvexHullVertexBuffer_XR_MSFT_Scene_Understanding(ulong convexHullBufferId, ref List<Vector2> vertexPositions)
        {
            if (!_planesDataMap.ContainsKey(convexHullBufferId))
            {
                Debug.LogError($"Could not find a convex hull with id: {convexHullBufferId}!");
                return false;
            }

            PlaneDataCollection planeData = _planesDataMap[convexHullBufferId];
            if (ConvexHullEnabled)
            {
                List<uint> indexBuffer = planeData.indices;
                List<Vector3> vertexBuffer = planeData.vertices;
                // NOTE(LE): Like the XR_QCOM impl, traverse the OpenXR indices in inverse order because changing
                // the coordinate system handedness also changes the winding order.
                // Without this, the plane meshes would face the wrong way.
                // For some reason with MSFT this fails to render anything if just iterating the vertices in vertexBuffer, however.
                // And iterating the indices in reverse winding order does some slightly strange things around adding vertices out of order
                // So - iterate forwards over unique vertexIds. Keep track of vertexIds to add. Then reverse the order we added them.
                HashSet<int> done = new HashSet<int>();
                List<int> order = new List<int>();
                for (int indexIx = 0; indexIx < indexBuffer.Count; ++indexIx)
                {
                    int vertexIx = (int)indexBuffer[indexIx];
                    if (done.Contains(vertexIx))
                    {
                        continue;
                    }

                    if (vertexIx >= vertexBuffer.Count)
                    {
                        Debug.LogWarning($"Cannot add vertex with index {vertexIx} because the vertex buffer only contains {vertexBuffer.Count} vertices");
                        continue;
                    }

                    done.Add(vertexIx);
                    order.Add(vertexIx);
                }

                order.Reverse();
                foreach (var vertexIx in order)
                {
                    Vector3 vertex = vertexBuffer[vertexIx];
                    vertexPositions.Add(new Vector2(vertex.x, vertex.y));
                }

                return true;
            }

            float xOffset = planeData.extents.x;
            float yOffset = planeData.extents.y;
            vertexPositions.Add(new Vector2(-xOffset, planeData.reverseExtentPlaneWindingOrder ? +yOffset : -yOffset));
            vertexPositions.Add(new Vector2(-xOffset, planeData.reverseExtentPlaneWindingOrder ? -yOffset : +yOffset));
            vertexPositions.Add(new Vector2(+xOffset, planeData.reverseExtentPlaneWindingOrder ? -yOffset : +yOffset));
            vertexPositions.Add(new Vector2(+xOffset, planeData.reverseExtentPlaneWindingOrder ? +yOffset : -yOffset));
            return true;
        }
    }
}
