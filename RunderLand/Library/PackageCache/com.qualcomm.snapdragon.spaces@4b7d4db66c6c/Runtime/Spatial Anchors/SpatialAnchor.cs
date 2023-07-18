/******************************************************************************
 * File: SpatialAnchor.cs
 * Copyright (c) 2022 Qualcomm Technologies, Inc. and/or its subsidiaries. All rights reserved.
 *
 * Confidential and Proprietary - Qualcomm Technologies, Inc.
 *
 ******************************************************************************/

using System;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;

namespace Qualcomm.Snapdragon.Spaces
{
    internal class SpatialAnchor
    {
        public XRAnchor SubsystemAnchor;
        public string SavedName;

        public SpatialAnchor(ulong anchorHandle, ulong anchorSpaceHandle, Pose pose, string name = "")
        {
            SavedName = name;
            SubsystemAnchor = new XRAnchor(new TrackableId(anchorHandle, anchorSpaceHandle), pose, TrackingState.None, IntPtr.Zero);
        }

        public ulong AnchorHandle => SubsystemAnchor.trackableId.subId1;
        public ulong AnchorSpaceHandle => SubsystemAnchor.trackableId.subId2;

        public bool UpdateSubsystemAnchorPoseAndTrackingState(Tuple<Pose, TrackingState> poseAndState)
        {
            // Return true if there was something to update, otherwise return false.
            if (poseAndState.Item1.Equals(SubsystemAnchor.pose) && poseAndState.Item2 == SubsystemAnchor.trackingState)
            {
                return false;
            }

            // Since pose is read only we have to replace the actual XRAnchor object.
            SubsystemAnchor = new XRAnchor(SubsystemAnchor.trackableId, poseAndState.Item1, poseAndState.Item2, SubsystemAnchor.nativePtr);
            return true;
        }
    }
}
