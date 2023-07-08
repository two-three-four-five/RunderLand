/******************************************************************************
 * File: FusionFeature.FeatureValidation.cs
 * Copyright (c) 2023 Qualcomm Technologies, Inc. and/or its subsidiaries. All rights reserved.
 *
 * Confidential and Proprietary - Qualcomm Technologies, Inc.
 *
 ******************************************************************************/

#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.OpenXR;

namespace Qualcomm.Snapdragon.Spaces
{
    public partial class FusionFeature
    {
        const string MainCameraTag = "MainCamera";
        const string UntaggedTag = "Untagged";

        Camera FindXRCamera()
        {
            Camera xrCamera = null;
            ARSessionOrigin aso = FindObjectOfType<ARSessionOrigin>(true);
            if (aso != null && aso.camera != null)
            {
                xrCamera = aso.camera;
            }
            if (aso == null)
            {
                XROrigin xrOrigin = FindObjectOfType<XROrigin>(true);
                if (xrOrigin != null)
                {
                    xrCamera = xrOrigin.Camera;
                }
            }
            return xrCamera;
        }

        Camera FindActiveMobileCamera(Camera xrCamera)
        {
            Camera[] cameras = FindObjectsOfType<Camera>();
            foreach (Camera camera in cameras)
            {
                if (xrCamera != camera && camera.targetTexture == null)
                {
                    return camera;
                }
            }
            return null;
        }

        protected override void GetValidationChecks(List<ValidationRule> rules, BuildTargetGroup targetGroup)
        {
            if (!this.enabled)
            {
                return;
            }

            var openXRSettings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            var baseRuntimeFeature = openXRSettings?.GetFeature<BaseRuntimeFeature>();
            if (baseRuntimeFeature == null || !baseRuntimeFeature.enabled)
            {
                return;
            }

            var settings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Android);
            if (settings == null || settings.Manager == null)
            {
                return;
            }

            var isOpenXRLoaderActive = settings.Manager.activeLoaders?.Any(loader => loader.GetType() == typeof(OpenXRLoader));

            if (ValidateOpenScene)
            {
                rules.Add(new ValidationRule(this)
                {
                    message = "Scene Recommendation: Dual Render Fusion recommends an AR Session in the scene.",
                    checkPredicate = () =>
                    {
                        return FindObjectOfType<ARSession>(true);
                    },
                    fixIt = () =>
                    {
                        if (FindObjectOfType<ARSession>() == null)
                        {
                            GameObject asObject = new GameObject("AR Session");
                            asObject.AddComponent<ARSession>();
                            Undo.RegisterCreatedObjectUndo(asObject, "Create AR Session");

                            if (FindObjectOfType<ARInputManager>() == null)
                            {
                                asObject.AddComponent<ARInputManager>();
                            }
                            Debug.Log("Added AR Session Object to the Scene (" + asObject.name + ")");
                        }
                    },
                    error = false,
                });


                rules.Add(new ValidationRule(this)
                {
                    message = "Scene Recommendation: URP Projects need to manually check the Mobile Camera to set the Target Eye to None in the Inspector. 'Fix' will not handle this.",
                    checkPredicate = () =>
                    {
                        return UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset == null;
                    },
                    fixIt = () =>
                    {
                        Debug.Log("Camera Target Eye checks cannot be done programmatically for URP at this time. Manually check the Cameras for Target Eye and ensure all non-XR Cameras are set to 'None' instead of 'Both'.");
                    },
                    error = false,
                });

                rules.Add(new ValidationRule(this)
                {
                    message = "Scene Recommendation: For Dual Render Fusion, each non-XR Camera needs to be set to Target Eye (none).",
                    checkPredicate = () =>
                    {
                        Camera xrCamera = FindXRCamera();

                        Camera[] cameras = FindObjectsOfType<Camera>(true);
                        foreach (Camera camera in cameras)
                        {
                            if (xrCamera != camera && camera.targetTexture == null)
                            {
                                if (camera.stereoTargetEye != StereoTargetEyeMask.None)
                                {
                                    return false;
                                }

                            //UniversalAdditionalCameraData urpData = camera.GetComponent<UniversalAdditionalCameraData>();
                            //if (urpData != null && urpData.allowXRRendering)
                            //{
                            //    return false;
                            //}
                        }
                        }
                        return true;
                    },
                    fixIt = () =>
                    {
                        Camera xrCamera = FindXRCamera();

                        int group = Undo.GetCurrentGroup();
                        Undo.SetCurrentGroupName("Set Target Eye for non-XR Cameras");
                        Camera[] cameras = FindObjectsOfType<Camera>(true);
                        foreach (Camera camera in cameras)
                        {
                            if (xrCamera != camera)
                            {
                                if (camera.stereoTargetEye != StereoTargetEyeMask.None && camera.targetTexture == null)
                                {
                                    Undo.RecordObject(camera, "Set Target Eye for " + camera.name);
                                    camera.stereoTargetEye = StereoTargetEyeMask.None;
                                    Debug.Log("Updated Mobile Camera Target Eye to None (" + camera.name + ")");
                                }

                                //UniversalAdditionalCameraData urpData = camera.GetComponent<UniversalAdditionalCameraData>();
                                //if (urpData != null && urpData.allowXRRendering)
                                //{
                                //    urpData.allowXRRendering = false;
                                //    Debug.Log("Updated Mobile Camera XR Rendering to false");
                                //}
                            }
                        }
                        Undo.CollapseUndoOperations(group);
                    },
                    error = false,
                });

                rules.Add(new ValidationRule(this)
                {
                    message = "Scene Recommendation: XR Camera and Mobile Display Camera are both tagged as MainCamera. Select 'Fix' to untag the XR Camera.",
                    checkPredicate = () =>
                    {
                        Camera xrCamera = FindXRCamera();
                        Camera[] cameras = FindObjectsOfType<Camera>(true);
                        if (xrCamera != null && !string.IsNullOrEmpty(xrCamera.tag) && xrCamera.tag.Equals(MainCameraTag))
                        {
                            foreach (Camera camera in cameras)
                            {
                                if (camera != xrCamera)
                                {
                                    if (!string.IsNullOrEmpty(camera.tag) && camera.tag.Equals(MainCameraTag))
                                    {
                                        return false;
                                    }
                                }
                            }
                        }
                        return true;
                    },
                    fixIt = () =>
                    {
                        bool clearXRCameraTag = false;
                        Camera xrCamera = FindXRCamera();
                        Camera[] cameras = FindObjectsOfType<Camera>(true);
                        foreach (Camera camera in cameras)
                        {
                            if (camera != xrCamera)
                            {
                                if (!string.IsNullOrEmpty(camera.tag) && camera.tag.Equals(MainCameraTag))
                                {
                                    clearXRCameraTag = true;
                                }
                            }
                        }

                        if (clearXRCameraTag)
                        {
                            Undo.RecordObject(xrCamera, "Untag XR Camera");
                            xrCamera.tag = UntaggedTag;
                            Debug.Log("Untagged XR Camera (" + xrCamera.name + ")");
                        }
                    },
                    error = false,
                });

                rules.Add(new ValidationRule(this)
                {
                    message = "Scene Recommendation: XR Camera and Mobile Display Camera are both tagged as MainCamera. Select 'Fix' to untag Mobile Display Cameras.",
                    checkPredicate = () =>
                    {
                        Camera xrCamera = FindXRCamera();
                        Camera[] cameras = FindObjectsOfType<Camera>(true);
                        if (xrCamera != null && !string.IsNullOrEmpty(xrCamera.tag) && xrCamera.tag.Equals(MainCameraTag))
                        {
                            foreach (Camera camera in cameras)
                            {
                                if (camera != xrCamera)
                                {
                                    if (!string.IsNullOrEmpty(camera.tag) && camera.tag.Equals(MainCameraTag))
                                    {
                                        return false;
                                    }
                                }
                            }
                        }
                        return true;
                    },
                    fixIt = () =>
                    {
                        // Only untag other cameras if MainCamera is still tagged on XR Camera
                        int group = Undo.GetCurrentGroup();
                        Camera xrCamera = FindXRCamera();
                        Camera[] cameras = FindObjectsOfType<Camera>(true);
                        int changes = 0;
                        if (xrCamera != null && !string.IsNullOrEmpty(xrCamera.tag) && xrCamera.tag.Equals(MainCameraTag))
                        {
                            foreach (Camera camera in cameras)
                            {
                                if (camera != xrCamera)
                                {
                                    if (!string.IsNullOrEmpty(camera.tag) && camera.tag.Equals(MainCameraTag))
                                    {
                                        Undo.RecordObject(camera, "Untag camera " + camera.name);
                                        camera.tag = UntaggedTag;
                                        Debug.Log("Untagged Camera (" + camera.name + ")");
                                        changes++;
                                    }
                                }
                            }
                        }
                        if (changes > 1)
                        {
                            Undo.SetCurrentGroupName("Untag non-XR cameras");
                            Undo.CollapseUndoOperations(group);
                        }
                    },
                    error = false,
                });

                rules.Add(new ValidationRule(this)
                {
                    message = "Scene Recommendation: XR Camera target display is not Display 1, recommend adding Fusion Logic for adjusting the target display at Runtime.",
                    checkPredicate = () =>
                    {
                        Camera xrCamera = FindXRCamera();
                        if (xrCamera != null)
                        {
                            if (xrCamera.targetDisplay != 0)
                            {
                                return FindObjectOfType<FusionLogic>() != null;
                            }
                        }
                        return true;
                    },
                    fixIt = () =>
                    {
                        AddFusionLogic();
                    },
                    error = false,
                });

                rules.Add(new ValidationRule(this)
                {
                    message = "Scene Recommendation: There are two audio listeners active. Since glasses are required, 'Fix' will disable the Mobile Camera audio listener.",
                    checkPredicate = () =>
                    {
                        if (RequireSpacesServices)
                        {
                            Camera xrCamera = FindXRCamera();
                            if (xrCamera != null)
                            {
                                AudioListener al = xrCamera.gameObject.GetComponent<AudioListener>();
                                if (al != null && al.enabled)
                                {
                                    Camera[] otherCameras = FindObjectsOfType<Camera>();
                                    foreach (Camera otherCamera in otherCameras)
                                    {
                                        if (otherCamera != xrCamera)
                                        {
                                            al = otherCamera.gameObject.GetComponent<AudioListener>();
                                            if (al != null && al.enabled)
                                            {
                                                return false;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        return true;
                    },
                    fixIt = () =>
                    {
                        if (RequireSpacesServices)
                        {
                            Camera xrCamera = FindXRCamera();
                            if (xrCamera != null)
                            {
                                AudioListener al = xrCamera.gameObject.GetComponent<AudioListener>();
                                if (al != null && al.enabled)
                                {
                                    Camera[] otherCameras = FindObjectsOfType<Camera>();
                                    foreach (Camera otherCamera in otherCameras)
                                    {
                                        if (otherCamera != xrCamera)
                                        {
                                            al = otherCamera.gameObject.GetComponent<AudioListener>();
                                            if (al != null && al.enabled)
                                            {
                                                Undo.RecordObject(xrCamera, "Disable AudioListener for " + otherCamera.name);
                                                al.enabled = false;
                                                Debug.Log("Disabling Audio Listener for Camera (" + otherCamera.name + ")");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    },
                    error = false,
                });

                rules.Add(new ValidationRule(this)
                {
                    message = "Scene Recommendation: There are two audio listeners active. Since glasses are not required, 'Fix' will disable the XR Camera audio listener.",
                    checkPredicate = () =>
                    {
                        if (!RequireSpacesServices)
                        {
                            Camera xrCamera = FindXRCamera();
                            if (xrCamera != null)
                            {
                                AudioListener al = xrCamera.gameObject.GetComponent<AudioListener>();
                                if (al != null && al.enabled)
                                {
                                    Camera[] otherCameras = FindObjectsOfType<Camera>();
                                    foreach (Camera otherCamera in otherCameras)
                                    {
                                        if (otherCamera != xrCamera)
                                        {
                                            al = otherCamera.gameObject.GetComponent<AudioListener>();
                                            if (al != null && al.enabled)
                                            {
                                                return false;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        return true;
                    },
                    fixIt = () =>
                    {
                        if (!RequireSpacesServices)
                        {
                            Camera xrCamera = FindXRCamera();
                            if (xrCamera != null)
                            {
                                AudioListener al = xrCamera.gameObject.GetComponent<AudioListener>();
                                if (al != null && al.enabled)
                                {
                                    Undo.RecordObject(xrCamera, "Disable AudioListener for " + xrCamera.name);
                                    al.enabled = false;
                                    Debug.Log("Disabling Audio Listener for XR Camera (" + xrCamera.name + ")");
                                }
                            }
                        }
                    },
                    error = false,
                });

                rules.Add(new ValidationRule(this)
                {
                    message = "Scene Recommendation: There is no EventSystem in the current scene.",
                    checkPredicate = () =>
                    {
                        return FindObjectOfType<EventSystem>() != null;
                    },
                    fixIt = () =>
                    {
                        if (FindObjectOfType<EventSystem>() == null)
                        {
                            GameObject go = new GameObject("EventSystem");
                            go.AddComponent<EventSystem>();
                            go.AddComponent<InputSystemUIInputModule>();
                            Undo.RegisterCreatedObjectUndo(go, "EventSystem");
                        }
                    },
                    error = false,
                });

                rules.Add(new ValidationRule(this)
                {
                    message = "Scene Requirement: There should be only one active XR Origin in the scene.",
                    checkPredicate = () =>
                    {
                        XROrigin[] origins = FindObjectsOfType<XROrigin>();
                        return origins == null || origins.Length < 2;
                    },
                    fixIt = () =>
                    {
                        XROrigin[] origins = FindObjectsOfType<XROrigin>();
                        if (origins != null && origins.Length > 1)
                        {
                            string[] names = new string[origins.Length];
                            for (int i = 0; i < origins.Length; i++)
                            {
                                names[i] = origins[i].gameObject.name;
                            }
                            Debug.LogError("Please manually disable or remove unneeded XR origin objects in the scene ([" + string.Join("],[", names) + "]).");
                        }
                    },
                    error = true,
                });

                rules.Add(new ValidationRule(this)
                {
                    message = "Scene Requirement: Dual Render Fusion requires the mobile Camera to render after the XR Camera.",
                    checkPredicate = () =>
                    {
                        Camera xrCamera = FindXRCamera();

                        float xrCameraDepth = -1;
                        if (xrCamera != null)
                        {
                            xrCameraDepth = xrCamera.depth;

                            Camera[] cameras = FindObjectsOfType<Camera>(true);
                            foreach (Camera camera in cameras)
                            {
                                if (xrCamera != camera && camera.targetTexture == null && camera.stereoTargetEye == StereoTargetEyeMask.None)
                                {
                                    if (camera.depth <= xrCameraDepth)
                                    {
                                        return false;
                                    }
                                }
                            }
                        }
                        return true;

                    },
                    fixIt = () =>
                    {
                        Camera xrCamera = FindXRCamera();

                        int group = Undo.GetCurrentGroup();
                        float xrCameraDepth = -1;
                        if (xrCamera != null)
                        {
                            xrCameraDepth = xrCamera.depth;

                            Camera[] cameras = FindObjectsOfType<Camera>(true);
                            foreach (Camera camera in cameras)
                            {
                                if (xrCamera != camera && camera.targetTexture == null && camera.stereoTargetEye == StereoTargetEyeMask.None)
                                {
                                    if (camera.depth <= xrCameraDepth)
                                    {
                                        Undo.RecordObject(camera, "Modified Depth in " + camera.name);

                                        float oldDepth = camera.depth;
                                        camera.depth = xrCameraDepth + 1;
                                        Debug.Log("Fixed Camera (" + camera.name + ") depth to " + camera.depth + " from " + oldDepth);
                                        Debug.Log("Add XR Origin and Camera");
                                    }
                                }
                            }
                        }
                        Undo.CollapseUndoOperations(group);
                    },
                    error = true,
                });

                rules.Add(new ValidationRule(this)
                {
                    message = "Scene Requirement: Dual Render Fusion requires a non-XR Camera in the scene for the mobile display.",
                    checkPredicate = () =>
                    {
                        Camera xrCamera = FindXRCamera();

                        Camera[] cameras = FindObjectsOfType<Camera>(false);
                        foreach (Camera camera in cameras)
                        {
                            if (xrCamera != camera && camera.targetTexture == null)
                            {
                                return true;
                            }
                        }
                        return false;
                    },
                    fixIt = () =>
                    {
                        Camera xrCamera = FindXRCamera();

                        int group = Undo.GetCurrentGroup();

                        GameObject CameraObject = new GameObject("Mobile Camera");
                        Camera mobileCamera = CameraObject.AddComponent<Camera>();
                        Undo.RegisterCreatedObjectUndo(CameraObject, "Add Mobile Camera");

                        bool useMainCameraTagForMobileCamera = true;
                        if (xrCamera != null)
                        {
                            if (xrCamera.tag.Equals(MainCameraTag))
                            {
                                useMainCameraTagForMobileCamera = false;
                            }
                            mobileCamera.depth = xrCamera.depth + 1;
                        }
                        mobileCamera.tag = useMainCameraTagForMobileCamera ? MainCameraTag : UntaggedTag;
                        mobileCamera.stereoTargetEye = StereoTargetEyeMask.None;
                        mobileCamera.targetDisplay = 0;

                        CameraObject.AddComponent<AudioListener>();

                        Debug.Log("Added Mobile Camera to the Scene (" + CameraObject.name + ") with tag " + CameraObject.tag);
                        if (xrCamera != null)
                        {
                            Undo.RecordObject(xrCamera, " set Camera Target Display");
                            xrCamera.targetDisplay = 1;
                            if (xrCamera.GetComponent<AudioListener>() != null)
                            {
                                xrCamera.GetComponent<AudioListener>().enabled = false;
                            }
                            AddFusionLogic();
                        }
                        Undo.CollapseUndoOperations(group);
                    },
                    error = true,
                });

                rules.Add(new ValidationRule(this)
                {
                    message = "Scene Requirement: Dual Render Fusion requires a camera attached to an AR Session Origin or XR Origin.",
                    checkPredicate = () =>
                    {
                        return FindXRCamera() != null;
                    },
                    fixIt = () =>
                    {
                        Camera xrCamera = FindXRCamera();
                        if (xrCamera != null)
                        {
                            return;
                        }

                        ARSessionOrigin aso = FindObjectOfType<ARSessionOrigin>(true);
                        XROrigin xro = FindObjectOfType<XROrigin>(true);
                        if (aso != null && aso.camera != null)
                        {
                            return;
                        }

                        if (xro != null && xro.Camera != null)
                        {
                            return;
                        }

                        int group = Undo.GetCurrentGroup();

                        Transform cameraParentTransform = null;

                        // If no origins, add XROrigin
                        if (aso == null && xro == null)
                        {
                            GameObject originObject = new GameObject("XR Origin");
                            xro = originObject.AddComponent<XROrigin>();
                            Debug.Log("Added XR Origin to the Scene (" + originObject.name + ")");

                            Undo.RegisterCreatedObjectUndo(originObject, "Create XR Origin (AR)");

                            Undo.SetCurrentGroupName("Create XR Origin (AR)");

                            InputActionManager inputActionManager = FindObjectOfType<InputActionManager>();
                            if (inputActionManager == null)
                            {
                                originObject.AddComponent<InputActionManager>();
                            }

                        }
                        else
                        {
                            Undo.SetCurrentGroupName("Add XR Camera");
                        }

                        GameObject xrCameraObject = new GameObject("XR Camera");
                        xrCamera = xrCameraObject.AddComponent<Camera>();
                        xrCamera.tag = UntaggedTag;

                        if (aso != null)
                        {
                            aso.camera = xrCamera;
                            cameraParentTransform = aso.transform;
                        }
                        else
                        {
                            if (xro.CameraFloorOffsetObject == null)
                            {
                                xro.CameraFloorOffsetObject = new GameObject("Camera Offset");
                                xro.CameraFloorOffsetObject.transform.SetParent(xro.transform, false);
                                xro.CameraFloorOffsetObject.transform.position = new Vector3(0, xro.CameraYOffset, 0);
                                xro.CameraFloorOffsetObject.transform.rotation = Quaternion.identity;
                            }
                            xro.Camera = xrCamera;
                            cameraParentTransform = xro.CameraFloorOffsetObject.transform;
                        }

                        // No cameras. Add one
                        Camera mobileCamera = FindActiveMobileCamera(xrCamera);

                        xrCameraObject.transform.SetParent(cameraParentTransform, false);

                        xrCamera.clearFlags = CameraClearFlags.SolidColor;
                        xrCamera.backgroundColor = Color.black;
                        xrCamera.farClipPlane = 1000;
                        xrCamera.stereoTargetEye = StereoTargetEyeMask.Both;
                        xrCamera.targetDisplay = 1;

                        AudioListener arAudioListener = xrCameraObject.AddComponent<AudioListener>();
                        if (mobileCamera != null && arAudioListener != null)
                        {
                            if (mobileCamera.GetComponent<AudioListener>() != null)
                            {
                                arAudioListener.enabled = !mobileCamera.GetComponent<AudioListener>().enabled;
                            }
                            xrCamera.depth = mobileCamera.depth - 1;
                        }

                        xrCameraObject.AddComponent<ARCameraManager>();
                        xrCameraObject.AddComponent<ARCameraBackground>();
                        Debug.Log("Added XR Camera to the Scene (" + xrCamera.name + ")");

                        TrackedPoseDriver trackedPoseDriver = xrCameraObject.AddComponent<TrackedPoseDriver>();
                        var positionAction = new InputAction("Position", binding: "<XRHMD>/centerEyePosition", expectedControlType: "Vector3");
                        positionAction.AddBinding("<HandheldARInputDevice>/devicePosition");
                        var rotationAction = new InputAction("Rotation", binding: "<XRHMD>/centerEyeRotation", expectedControlType: "Quaternion");
                        rotationAction.AddBinding("<HandheldARInputDevice>/deviceRotation");
                        trackedPoseDriver.positionInput = new InputActionProperty(positionAction);
                        trackedPoseDriver.rotationInput = new InputActionProperty(rotationAction);

                        FusionLogic fl = FindObjectOfType<FusionLogic>();
                        if (fl == null)
                        {
                            GameObject go = new GameObject("Fusion Logic");
                            go.AddComponent<FusionLogic>();
                            Debug.Log("Added FusionLogic to the Scene (" + go.name + ")");

                            Undo.RegisterCreatedObjectUndo(go, "Add Fusion Logic");
                        }

                        Undo.CollapseUndoOperations(group);
                    },
                    error = true,
                });

            }

            rules.Add(new ValidationRule(this)
            {
                message = "If Spaces Services is required, 'Check Installed Runtime' in Base Runtime Feature should be checked.",
                checkPredicate = () =>
                {
                    return !RequireSpacesServices || baseRuntimeFeature.CheckInstalledRuntime;
                },
                fixIt = () => {
                    baseRuntimeFeature.CheckInstalledRuntime = true;
                },
                error = true,
            });

            rules.Add(new ValidationRule(this)
            {
                message = "Dual Render Fusion requires Base Runtime options 'Launch on Viewer' and 'Launch Controller on Host' to be disabled.",
                checkPredicate = () =>
                {
                    return !baseRuntimeFeature.LaunchAppOnViewer && !baseRuntimeFeature.LaunchControllerOnHost;
                },
                fixIt = () => {
                    baseRuntimeFeature.LaunchAppOnViewer = baseRuntimeFeature.LaunchControllerOnHost = false;
                },
                error = true,
            });
        }

        void AddFusionLogic()
        {
            if (FindObjectOfType<FusionLogic>() == null)
            {
                GameObject go = new GameObject("Fusion Logic");
                go.AddComponent<FusionLogic>();
                Debug.Log("Added FusionLogic to the Scene (" + go.name + ")");
                Undo.RegisterCreatedObjectUndo(go, "Fusion Logic");
            }
        }
    }
}
#endif