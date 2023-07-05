/****************************************************************
 * File: XrCameraFrameBufferQCOM.cs
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
    internal struct XrCameraFrameBufferQCOM
    {
        private XrStructureType _type;
        private IntPtr _next;
        private uint _bufferSize;

        // byte[]
        private IntPtr _buffer;
        private uint _planeCount;

        // Marshal.SizeOf(XrCameraFramePlaneQCOMX) == 32
        // XR_CAMERA_FRAME_PLANES_SIZE_QCOMX == 4
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32 * 4)]
        // XrCameraFramePlaneQCOM[]
        private byte[] _planes;

        public XrCameraFrameBufferQCOM(uint bufferSize, IntPtr /* byte[] */ buffer, uint planeCount, byte[] /* XrCameraFramePlaneQCOM[] */ planes)
        {
            _type = XrStructureType.XR_TYPE_CAMERA_FRAME_BUFFER_QCOMX;
            _next = IntPtr.Zero;
            _bufferSize = bufferSize;
            _buffer = buffer;
            _planeCount = planeCount;
            _planes = planes;
        }

        public uint BufferSize => _bufferSize;
        public IntPtr Buffer => _buffer;
        public uint PlaneCount => _planeCount;

        public override string ToString()
        {
            return String.Join("\n",
                "[XrCameraFrameBufferQCOM]",
                $"Type:\t{_type}",
                $"Next:\t{_next}",
                $"BufferSize:\t{_bufferSize}",
                $"Buffer:\t{_buffer}",
                $"PlaneCount:\t{_planeCount}",
                $"Planes:\t{_planes}");
        }

        public XrCameraFramePlaneQCOM[] PlanesArray
        {
            get
            {
                // Shift the byte stream to discard 4 leading padding bytes.
                byte[] shiftedPlanesBytes = new byte[32 * _planeCount];
                Array.Copy(_planes, 4, shiftedPlanesBytes, 0, (32 * _planeCount) - 4);
                // Move the stream back to unmanaged memory to read it back as XrCameraFramePlaneQCOM.
                GCHandle planeArrayBytesHandle = GCHandle.Alloc(shiftedPlanesBytes, GCHandleType.Pinned);
                XrCameraFramePlaneQCOM[] planesArray = new XrCameraFramePlaneQCOM[_planeCount];
                for (int i = 0; i < _planeCount; i++)
                {
                    planesArray[i] = Marshal.PtrToStructure<XrCameraFramePlaneQCOM>(planeArrayBytesHandle.AddrOfPinnedObject() + (Marshal.SizeOf(typeof(XrCameraFramePlaneQCOM)) * i));
                }

                planeArrayBytesHandle.Free();
                return planesArray;
            }
        }
    }
}
