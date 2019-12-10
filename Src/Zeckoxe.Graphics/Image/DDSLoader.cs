﻿// Copyright (c) 2019-2020 Faber Leonardo. All Rights Reserved.

/*=============================================================================
	DDSLoader.cs
=============================================================================*/



using System;
using System.Collections.Generic;
using System.Text;

namespace Zeckoxe.Image
{
    public class DDSLoader
    {
        public enum Caps
        {
            Complex = 0x8,

            Mipmap = 0x400000,

            Texture = 0x1000,
        }


        [Flags]
        enum Caps2
        {
            Cubemap = 0x200,

            CubemapPositiveX = 0x400,
 
            CubemapNegativeX = 0x800,
 
            CubemapPositiveY = 0x1000,

            CubemapNegativeY = 0x2000,

            CubemapPositiveZ = 0x4000,

            CubemapNegativeZ = 0x8000,

            Volume = 0x200000,

            AllFaces = 0x200 | CubemapPositiveX | CubemapNegativeX | CubemapPositiveY | CubemapNegativeY | CubemapPositiveZ | CubemapNegativeZ,
        }



        enum FlagTypes
        {
            Caps = 0x1,

            Height = 0x2,

            Width = 0x4,

            Pitch = 0x8,

            PixelFormat = 0x1000,

            MipmapCount = 0x20000,

            LinearSize = 0x80000,

            Depth = 0x800000,
        }
    }
}