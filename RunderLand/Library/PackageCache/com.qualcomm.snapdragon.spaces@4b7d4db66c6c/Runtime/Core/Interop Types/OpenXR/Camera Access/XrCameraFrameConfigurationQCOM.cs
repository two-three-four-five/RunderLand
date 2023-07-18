/****************************************************************
 * File: XrCameraFrameConfigurationQCOM.cs
 * Copyright (c) 2022 Qualcomm Technologies, Inc. and/or its subsidiaries. All rights reserved.
 *
 * Confidential and Proprietary - Qualcomm Technologies, Inc.
 *
 ****************************************************************/

using System;
using System.Runtime.InteropServices;

namespace Qualcomm.Snapdragon.Spaces
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct XrCameraFrameConfigurationQCOM
    {
        private XrStructureType _type;
        private IntPtr _next;
        private XrCameraFrameFormatQCOM _format;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        private string _resolutionName;

        private XrExtent2Di _dimensions;
        private uint _minFPS;
        private uint _maxFPS;
        private uint _frameBufferCount;
        private uint _frameHardwareBufferCount;

        public XrCameraFrameConfigurationQCOM(XrCameraFrameFormatQCOM format, string resolutionName, XrExtent2Di dimensions, uint minFPS, uint maxFPS, uint frameBufferCount, uint frameHardwareBufferCount)
        {
            _type = XrStructureType.XR_TYPE_CAMERA_FRAME_CONFIGURATION_QCOMX;
            _next = IntPtr.Zero;
            _format = format;
            _resolutionName = resolutionName;
            _dimensions = dimensions;
            _minFPS = minFPS;
            _maxFPS = maxFPS;
            _frameBufferCount = frameBufferCount;
            _frameHardwareBufferCount = frameHardwareBufferCount;
        }

        public XrCameraFrameFormatQCOM Format => _format;
        public string ResolutionName => _resolutionName;
        public XrExtent2Di Dimensions => _dimensions;
        public uint MinFPS => _minFPS;
        public uint MaxFPS => _maxFPS;
        public uint FrameBufferCount => _frameBufferCount;

        public override string ToString()
        {
            return String.Join("\n",
                "[XrCameraSensorPropertiesQCOM]",
                $"Type:\t{_type}",
                $"Next:\t{_next}",
                $"Format:\t{_format}",
                $"ResolutionName:\t{_resolutionName}",
                $"Dimensions:\t{_dimensions}",
                $"MinFPS:\t{_minFPS}",
                $"MaxFPS:\t{_maxFPS}",
                $"FrameBufferCount:\t{_frameBufferCount}",
                $"FrameHardwareBufferCount:\t{_frameHardwareBufferCount}");
        }
    }
}
