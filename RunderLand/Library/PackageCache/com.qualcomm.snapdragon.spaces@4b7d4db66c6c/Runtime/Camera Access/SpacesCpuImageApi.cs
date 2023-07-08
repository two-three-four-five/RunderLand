/******************************************************************************
 * File: SpacesCpuImageApi.cs
 * Copyright (c) 2023 Qualcomm Technologies, Inc. and/or its subsidiaries. All rights reserved.
 *
 * Confidential and Proprietary - Qualcomm Technologies, Inc.
 *
 ******************************************************************************/

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Qualcomm.Snapdragon.Spaces;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.OpenXR;

public class SpacesCpuImageApi : XRCpuImage.Api
{
    private bool _deviceIsA3;
    private List<XRCpuImage.Format> _supportedInputFormats = new List<XRCpuImage.Format> { XRCpuImage.Format.AndroidYuv420_888 };

    private List<TextureFormat> _supportedOutputFormats = new List<TextureFormat> { TextureFormat.RGB24, TextureFormat.RGBA32, TextureFormat.BGRA32 };

    private CameraAccessFeature _underlyingFeature = OpenXRSettings.Instance.GetFeature<CameraAccessFeature>();
    public static SpacesCpuImageApi instance { get; private set; }

    public static SpacesCpuImageApi CreateInstance()
    {
        instance ??= new SpacesCpuImageApi();
        instance._deviceIsA3 = SystemInfo.deviceModel.ToLower().Contains("motorola edge");
        return instance;
    }

    public override bool NativeHandleValid(int nativeHandle)
    {
        return nativeHandle == (int)_underlyingFeature.LastFrame.Handle;
    }

    public override bool FormatSupported(XRCpuImage image, TextureFormat format)
    {
        if (!_supportedInputFormats.Contains(image.format))
        {
            return false;
        }

        if (!_supportedOutputFormats.Contains(format))
        {
            return false;
        }

        return true;
    }

    public override bool TryGetPlane(int nativeHandle, int planeIndex, out XRCpuImage.Plane.Cinfo planeCinfo)
    {
        planeCinfo = new XRCpuImage.Plane.Cinfo();

        if (!NativeHandleValid(nativeHandle))
        {
            Debug.LogWarning("Native handle [" + nativeHandle + "] is not valid. The frame might have expired.");
            return false;
        }

        // Extract sensor properties and frame buffers
        XrCameraSensorPropertiesQCOM sensor = _underlyingFeature.SensorProperties[0];
        Vector2Int imageOffset = sensor.ImageOffset.ToVector2Int();
        Vector2Int dimensions = sensor.ImageDimensions.ToVector2Int();

        XrCameraFrameBufferQCOM[] frameBufferArray = _underlyingFeature.FrameBuffers;

        // As we only support a 1-frame frame buffer, consider only the first frame buffer.

        XrCameraFramePlaneQCOM targetPlane = frameBufferArray[0].PlanesArray[planeIndex];

        IntPtr dataPtr = frameBufferArray[0].Buffer + (int)targetPlane.Offset;
        int dataLength;
        int pixelStride;

        switch (targetPlane.PlaneType)
        {
            case XrCameraFramePlaneTypeQCOM.XR_CAMERA_FRAME_PLANE_TYPE_Y_QCOMX:
                dataLength = (int)((imageOffset.y + dimensions.y) * (imageOffset.x + targetPlane.Stride));
                pixelStride = 1;
                break;
            case XrCameraFramePlaneTypeQCOM.XR_CAMERA_FRAME_PLANE_TYPE_U_QCOMX:
            case XrCameraFramePlaneTypeQCOM.XR_CAMERA_FRAME_PLANE_TYPE_V_QCOMX:
                dataLength = (int)((imageOffset.y + dimensions.y) / 2 * ((imageOffset.x + targetPlane.Stride) / 2));
                pixelStride = 1;
                break;
            case XrCameraFramePlaneTypeQCOM.XR_CAMERA_FRAME_PLANE_TYPE_UV_QCOMX:
                dataLength = (int)((imageOffset.y + dimensions.y) / 2 * (imageOffset.x + targetPlane.Stride) / 2 * 2);
                pixelStride = 2;
                break;
            default:
                Debug.LogWarning($"Plane type {targetPlane.PlaneType} is not supported.");
                return false;
        }

        planeCinfo = new XRCpuImage.Plane.Cinfo(dataPtr, dataLength, (int)targetPlane.Stride, pixelStride);
        return true;
    }

    public override bool TryGetConvertedDataSize(int nativeHandle, Vector2Int dimensions, TextureFormat format, out int size)
    {
        size = 0;

        if (!NativeHandleValid(nativeHandle) || dimensions.x < 0 || dimensions.y < 0)
        {
            return false;
        }

        if (!_supportedOutputFormats.Contains(format))
        {
            return false;
        }

        switch (format)
        {
            case TextureFormat.RGB24:
                size = dimensions.x * dimensions.y * 3;
                break;
            case TextureFormat.RGBA32:
            case TextureFormat.BGRA32:
                size = dimensions.x * dimensions.y * 4;
                break;
        }

        return true;
    }

    public override bool TryConvert(int nativeHandle, XRCpuImage.ConversionParams conversionParams, IntPtr destinationBuffer, int bufferLength)
    {
        if (!NativeHandleValid(nativeHandle) || !_supportedOutputFormats.Contains(conversionParams.outputFormat))
        {
            return false;
        }

        // Extract sensor properties and frame buffers
        XrCameraSensorPropertiesQCOM sensor = _underlyingFeature.SensorProperties[0];
        Vector2Int imageOffset = sensor.ImageOffset.ToVector2Int();
        Vector2Int dimensions = sensor.ImageDimensions.ToVector2Int();

        XrCameraFrameDataQCOM frameData = _underlyingFeature.LastFrame;
        XrCameraFrameBufferQCOM[] frameBufferArray = _underlyingFeature.FrameBuffers;
        int bufferCount = frameBufferArray.Length;

        // For each Frame Buffer, extract pixel data & image plane properties
        byte[][] frameDataArray = new byte[bufferCount][];
        XrCameraFramePlaneQCOM[][] framePlaneArray = new XrCameraFramePlaneQCOM[bufferCount][];

        for (int i = 0; i < bufferCount; i++)
        {
            // Pixel data
            int bufferSize = (int)frameBufferArray[i].BufferSize;
            frameDataArray[i] = new byte[bufferSize];
            Marshal.Copy(frameBufferArray[i].Buffer, frameDataArray[i], 0, bufferSize);

            // Image planes
            framePlaneArray[i] = frameBufferArray[i].PlanesArray;
        }

        // Image conversion: YUV420 -> RGB/BGR
        if (conversionParams.outputFormat == TextureFormat.RGB24 ||
            conversionParams.outputFormat == TextureFormat.RGBA32 ||
            conversionParams.outputFormat == TextureFormat.BGRA32)
        {
            XrCameraFramePlaneQCOM yPlane = new XrCameraFramePlaneQCOM();
            XrCameraFramePlaneQCOM uvPlane = new XrCameraFramePlaneQCOM();

            /* As we only support a 1-frame frame buffer, consider only the first frame buffer. */
            for (int i = 0; i < (int)frameBufferArray[0].PlaneCount; i++)
            {
                XrCameraFramePlaneQCOM plane = framePlaneArray[0][i];
                if (plane.PlaneType == XrCameraFramePlaneTypeQCOM.XR_CAMERA_FRAME_PLANE_TYPE_Y_QCOMX)
                {
                    yPlane = plane;
                }

                if (plane.PlaneType == XrCameraFramePlaneTypeQCOM.XR_CAMERA_FRAME_PLANE_TYPE_UV_QCOMX)
                {
                    uvPlane = plane;
                }
            }

            // Example of image buffer layout for a 2x4 image with YUV420 format variants:
            // YUV420_NV12 --> YYYYYYYY,UVUV
            // YUV420_NV21 --> YYYYYYYY,VUVU
            // A3+Rogue device wrongly inverts NV12 and NV21 formats, so we need to flip swapUV on the device
            bool swapUV = frameData.Format == XrCameraFrameFormatQCOM.XR_CAMERA_FRAME_FORMAT_YUV420_NV21_QCOMX;
            swapUV = swapUV != _deviceIsA3;

            byte[] framePixels = new byte[bufferLength];

            for (int row = 0; row < dimensions.y; row++)
            {
                for (int col = 0; col < dimensions.x; col++)
                {
                    byte y = frameDataArray[0][yPlane.Offset + ((imageOffset.y + row) * yPlane.Stride) + imageOffset.x + col];

                    var rowOffset = (imageOffset.y + row) / 2 * uvPlane.Stride;
                    var colOffset = (imageOffset.x + col) / 2 * 2;
                    var offset = uvPlane.Offset + rowOffset + colOffset;

                    // YUV NV21 to RGB conversion
                    // https://en.wikipedia.org/wiki/YUV#Y%E2%80%B2UV420sp_(NV21)_to_RGB_conversion_(Android)

                    sbyte u = (sbyte)(frameDataArray[0][offset] - 128);
                    sbyte v = (sbyte)(frameDataArray[0][offset + 1] - 128);
                    if (swapUV)
                    {
                        (u, v) = (v, u);
                    }

                    var r = y + (1.370705f * v);
                    var g = y - (0.698001f * v) - (0.337633f * u);
                    var b = y + (1.732446f * u);

                    r = r > 255 ? 255 : r < 0 ? 0 : r;
                    g = g > 255 ? 255 : g < 0 ? 0 : g;
                    b = b > 255 ? 255 : b < 0 ? 0 : b;

                    int pixelIndex = ((dimensions.y - row - 1) * dimensions.x) + col;

                    switch (conversionParams.outputFormat)
                    {
                        case TextureFormat.RGB24:
                            framePixels[3 * pixelIndex] = (byte)r;
                            framePixels[(3 * pixelIndex) + 1] = (byte)g;
                            framePixels[(3 * pixelIndex) + 2] = (byte)b;
                            break;
                        case TextureFormat.RGBA32:
                            framePixels[4 * pixelIndex] = (byte)r;
                            framePixels[(4 * pixelIndex) + 1] = (byte)g;
                            framePixels[(4 * pixelIndex) + 2] = (byte)b;
                            framePixels[(4 * pixelIndex) + 3] = 255;
                            break;
                        case TextureFormat.BGRA32:
                            framePixels[4 * pixelIndex] = (byte)b;
                            framePixels[(4 * pixelIndex) + 1] = (byte)g;
                            framePixels[(4 * pixelIndex) + 2] = (byte)r;
                            framePixels[(4 * pixelIndex) + 3] = 255;
                            break;
                    }
                }
            }

            Marshal.Copy(framePixels, 0, destinationBuffer, bufferLength);
            return true;
        }

        return false;
    }

    public override void DisposeImage(int nativeHandle)
    {
        if (!NativeHandleValid(nativeHandle))
        {
            Debug.LogWarning("Could not dispose image with handle [" + nativeHandle + "].");
        }

        if (!_underlyingFeature.TryReleaseFrame())
        {
            Debug.LogError("Could not release frame with handle [" + nativeHandle + "].");
        }
    }
}
