/******************************************************************************
 * File: BaseRuntimeFeature.InterceptInstanceProcAddr.cs
 * Copyright (c) 2022 Qualcomm Technologies, Inc. and/or its subsidiaries. All rights reserved.
 *
 * Confidential and Proprietary - Qualcomm Technologies, Inc.
 *
 ******************************************************************************/

using System;
using System.Runtime.InteropServices;

namespace Qualcomm.Snapdragon.Spaces
{
    public partial class BaseRuntimeFeature
    {
        private const string InterceptOpenXRLibrary = "libInterceptOpenXR";

        [DllImport(InterceptOpenXRLibrary, EntryPoint = "GetInterceptedInstanceProcAddr")]
        private static extern IntPtr GetInterceptedInstanceProcAddr(IntPtr func);

        protected override IntPtr HookGetInstanceProcAddr(IntPtr func)
        {
            return GetInterceptedInstanceProcAddr(func);
        }
    }
}
