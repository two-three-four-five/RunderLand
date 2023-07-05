/******************************************************************************
 * File: SpacesQCHTSDK.cs
 * Copyright (c) 2022 Qualcomm Technologies, Inc. and/or its subsidiaries. All rights reserved.
 *
 * Confidential and Proprietary - Qualcomm Technologies, Inc.
 *
 ******************************************************************************/

#if QCHT_UNITY_CORE
using System;
using System.Linq;
using QCHT.Core;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;

namespace Qualcomm.Snapdragon.Spaces
{
    [Obsolete("This item is now obsolete and will be remove in future release, please consider updating your project. More information: https://docs.spaces.qualcomm.com/unity/setup/SetupGuideUnity.html", false)]
    internal sealed class SpacesQCHTSDK : QCHTSDK
    {
        private Transform headTransform;
        private Transform arSessionTransform;

        public override void StopQcht()
        {
            _data.GetHand(true).Clear();
            _data.GetHand(false).Clear();
        }

        public override void UpdateData()
        {
            UpdateQCHTHand(true);
            UpdateQCHTHand(false);
        }

        private void UpdateQCHTHand(bool isLeft)
        {
            headTransform ??= Camera.main.transform;
            arSessionTransform ??= OriginLocationUtility.GetOriginTransform();
            SpacesHand spacesHand = null;
            if (SpacesHandManager.instance != null)
            {
                spacesHand = isLeft ? SpacesHandManager.instance.LeftHand : SpacesHandManager.instance.RightHand;
            }

            QCHTHand hand = _data.GetHand(isLeft);
            if (spacesHand == null)
            {
                hand.IsDetected = false;
                hand.gesture = GestureId.UNKNOWN;
                hand.gestureRatio = 0f;
                hand.flipRatio = 0;
                return;
            }

            hand.IsDetected = spacesHand.trackingState == TrackingState.Tracking;
            var handRotation = hand.rotations;
            var handPosition = hand.points;
            var baseOrientation = Quaternion.AngleAxis(90f, Vector3.right);
            var adjustedRotations = spacesHand.RawJoints.Select(pose => Quaternion.Inverse(headTransform.rotation) * arSessionTransform.rotation * pose.rotation * baseOrientation).ToArray();
            for (var i = 0; i < (int)QCHTPointId.POINT_COUNT; i++)
            {
                var _rotationLookup = QCHTJointLookupTables.RotationLookup[i];
                var _pointsLookup = QCHTJointLookupTables.PointsLookup[i];
                var rotation = adjustedRotations[(int)_rotationLookup[0]];
                handRotation[i] = _rotationLookup.Length > 1 ? Quaternion.Inverse(adjustedRotations[(int)_rotationLookup[1]]) * rotation : rotation;
                handPosition[i] = headTransform.InverseTransformPoint(arSessionTransform.TransformPoint(spacesHand.RawJoints[(int)_pointsLookup].position));
            }

            hand.gesture = (GestureId)spacesHand.RawGesture;
            hand.gestureRatio = Mathf.Clamp01(spacesHand.RawGestureRatio);
            hand.flipRatio = Mathf.Clamp(spacesHand.RawFlipRatio, -1f, 1f);
        }
    }
}
#endif
