/******************************************************************************
 * File: SpatialAnchorsSubsystem.cs
 * Copyright (c) 2022 Qualcomm Technologies, Inc. and/or its subsidiaries. All rights reserved.
 *
 * Confidential and Proprietary - Qualcomm Technologies, Inc.
 *
 ******************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.OpenXR;

namespace Qualcomm.Snapdragon.Spaces
{
    internal class SpatialAnchorsSubsystem : XRAnchorSubsystem
    {
        internal class SpatialAnchorsProvider : Provider
        {
            private SpatialAnchorsFeature _underlyingFeature;
            private List<SpatialAnchor> _activeSpatialAnchors;
            private List<XRAnchor> _xrAnchorsToAdd;
            private List<TrackableId> _trackablesToRemove;
            private SpatialAnchor _persistentAnchorCanditate;

            public override void Start()
            {
                _underlyingFeature = OpenXRSettings.Instance.GetFeature<SpatialAnchorsFeature>();
                _activeSpatialAnchors = new List<SpatialAnchor>();
                _xrAnchorsToAdd = new List<XRAnchor>();
                _trackablesToRemove = new List<TrackableId>();
            }

            public override void Stop()
            {
                Destroy();
            }

            public override void Destroy()
            {
                // Note GÖ: After updating to the OpenXR 1.4.2 plugin, it seems that Destroy is being called after the
                // runtime is being exited and some values are destroyed already. That leads to an error log on
                // application exit. This condition was added to prevent that.
                if (_activeSpatialAnchors == null)
                {
                    return;
                }

                SpatialAnchor[] activeAnchorsToRemove = new SpatialAnchor[_activeSpatialAnchors.Count];
                _activeSpatialAnchors.CopyTo(activeAnchorsToRemove);
                foreach (var anchor in activeAnchorsToRemove)
                {
                    TryRemoveAnchor(anchor.SubsystemAnchor.trackableId);
                }
            }

            public override TrackableChanges<XRAnchor> GetChanges(XRAnchor defaultAnchor, Allocator allocator)
            {
                // Update the poses of active spatial anchors.
                var updatedAnchors = new List<XRAnchor>();
                foreach (var spatialAnchor in _activeSpatialAnchors.Where(spatialAnchor => !_xrAnchorsToAdd.Contains(spatialAnchor.SubsystemAnchor)))
                {
                    var poseAndState = _underlyingFeature.TryGetSpatialAnchorSpacePoseAndTrackingState(spatialAnchor.AnchorSpaceHandle);
                    if (spatialAnchor.UpdateSubsystemAnchorPoseAndTrackingState(poseAndState))
                    {
                        updatedAnchors.Add(spatialAnchor.SubsystemAnchor);
                    }
                }

                var trackableChanges = TrackableChanges<XRAnchor>.CopyFrom(new NativeArray<XRAnchor>(_xrAnchorsToAdd.ToArray(), allocator),
                    new NativeArray<XRAnchor>(updatedAnchors.ToArray(), allocator),
                    new NativeArray<TrackableId>(_trackablesToRemove.ToArray(), allocator),
                    allocator);
                _xrAnchorsToAdd.Clear();
                _trackablesToRemove.Clear();
                return trackableChanges;
            }

            public override bool TryAddAnchor(Pose pose, out XRAnchor anchor)
            {
                if (_persistentAnchorCanditate != null)
                {
                    _activeSpatialAnchors.Add(_persistentAnchorCanditate);
                    _xrAnchorsToAdd.Add(_persistentAnchorCanditate.SubsystemAnchor);
                    anchor = _persistentAnchorCanditate.SubsystemAnchor;
                    _persistentAnchorCanditate = null;
                    return true;
                }

                ulong anchorHandle = _underlyingFeature.TryCreateSpatialAnchorHandle(pose);
                if (anchorHandle != 0)
                {
                    ulong anchorSpaceHandle = _underlyingFeature.TryCreateSpatialAnchorSpaceHandle(anchorHandle);
                    if (anchorSpaceHandle != 0)
                    {
                        var newAnchor = new SpatialAnchor(anchorHandle, anchorSpaceHandle, pose);
                        _activeSpatialAnchors.Add(newAnchor);
                        _xrAnchorsToAdd.Add(newAnchor.SubsystemAnchor);
                        anchor = newAnchor.SubsystemAnchor;
                        return true;
                    }
                }

                anchor = XRAnchor.defaultValue;
                return false;
            }

            [Obsolete]
            public bool TryAddAnchorFromPersistentName(ulong spatialAnchorStore, string spatialAnchorName)
            {
                if (_underlyingFeature.TryCreateSpatialAnchorFromPersistedNameMSFT(spatialAnchorStore, spatialAnchorName, out ulong spatialAnchorHandle))
                {
                    ulong anchorSpaceHandle = _underlyingFeature.TryCreateSpatialAnchorSpaceHandle(spatialAnchorHandle);
                    if (anchorSpaceHandle != 0)
                    {
                        _persistentAnchorCanditate = new SpatialAnchor(spatialAnchorHandle, anchorSpaceHandle, Pose.identity, spatialAnchorName);
                        return true;
                    }
                }

                return false;
            }

            public void SetPersistentAnchorCandidate(SpatialAnchor persistentAnchorCandidate)
            {
                _persistentAnchorCanditate = persistentAnchorCandidate;
            }

            public override bool TryRemoveAnchor(TrackableId anchorId)
            {
                try
                {
                    var anchorToRemove = _activeSpatialAnchors.SingleOrDefault(anchor => anchor.SubsystemAnchor.trackableId == anchorId);
                    if (_underlyingFeature.TryDestroySpatialAnchor(anchorToRemove.AnchorHandle))
                    {
                        _trackablesToRemove.Add(anchorToRemove.SubsystemAnchor.trackableId);
                        _activeSpatialAnchors.Remove(anchorToRemove);
                        return true;
                    }
                }
                catch (InvalidOperationException)
                {
                    Debug.LogError("Trying to remove XRAnchor with an invalid Trackable ID: " + anchorId);
                }

                return false;
            }

            public string TryGetSavedNameFromTrackableId(TrackableId trackableId)
            {
                foreach (var activeAnchor in _activeSpatialAnchors)
                {
                    if (activeAnchor.SubsystemAnchor.trackableId == trackableId)
                    {
                        return activeAnchor.SavedName;
                    }
                }

                return string.Empty;
            }

            public void UpdateAnchorSavedName(TrackableId anchorId, string savedName)
            {
                foreach (var activeAnchor in _activeSpatialAnchors)
                {
                    if (activeAnchor.SubsystemAnchor.trackableId == anchorId)
                    {
                        activeAnchor.SavedName = savedName;
                        return;
                    }
                }
            }

            public void ClearAllAnchorSavedNames()
            {
                foreach (var activeAnchor in _activeSpatialAnchors)
                {
                    activeAnchor.SavedName = string.Empty;
                }
            }
        }

        public const string ID = "Spaces-SpatialAnchorsSubsystem";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterDescriptor()
        {
            XRAnchorSubsystemDescriptor.Create(new XRAnchorSubsystemDescriptor.Cinfo
            {
                id = ID,
                providerType = typeof(SpatialAnchorsProvider),
                subsystemTypeOverride = typeof(SpatialAnchorsSubsystem),
                supportsTrackableAttachments = false
            });
        }
    }
}
