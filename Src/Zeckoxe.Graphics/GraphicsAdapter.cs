﻿// Copyright (c) 2019-2020 Faber Leonardo. All Rights Reserved.

/*=============================================================================
	GraphicsAdapter.cs
=============================================================================*/



using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Zeckoxe.Core;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;
using Interop = Zeckoxe.Core.Interop;

namespace Zeckoxe.Graphics
{
    public unsafe class GraphicsAdapter
    {
        public GraphicsInstance DefaultInstance { get; private set; }

        public DeviceType DeviceType => (DeviceType)Properties.deviceType;

        public uint VendorId => Properties.vendorID;

        public string DeviceName
        {
            get
            {
                var deviceProperties = Properties;
                return Interop.String.FromPointer(deviceProperties.deviceName);
            }
        }







        internal VkPhysicalDevice NativePhysicalDevice { get; private set; }
        internal List<VkPhysicalDevice> NativePhysicalDevices { get; private set; }
        internal VkPhysicalDeviceProperties Properties { get; private set; }


        public GraphicsAdapter(GraphicsInstance Instance)
        {

            DefaultInstance = Instance;

            Recreate();
        }

        public void Recreate()
        {
            NativePhysicalDevices = new List<VkPhysicalDevice>();

            NativePhysicalDevices = GetPhysicalDevices();

            foreach (var item in NativePhysicalDevices)
                NativePhysicalDevice = item;
            

            Properties = GetProperties();
        }

        internal VkPhysicalDeviceProperties GetProperties()
        {
            VkPhysicalDeviceProperties physicalDeviceProperties;
            vkGetPhysicalDeviceProperties(NativePhysicalDevice, out physicalDeviceProperties);
            return physicalDeviceProperties;
        }


        internal List<VkPhysicalDevice> GetPhysicalDevices()
        {
            // Physical Device
            uint Count = 0;
            vkEnumeratePhysicalDevices(DefaultInstance.NativeInstance, &Count, null);

            // Enumerate devices
            VkPhysicalDevice* physicalDevices = stackalloc VkPhysicalDevice[(int)Count];
            vkEnumeratePhysicalDevices(DefaultInstance.NativeInstance, &Count, physicalDevices);

            List<VkPhysicalDevice> vkPhysicalDevices = new List<VkPhysicalDevice>();

            for (int i = 0; i < Count; i++)
                vkPhysicalDevices.Add(physicalDevices[i]);

            return vkPhysicalDevices /*vkEnumeratePhysicalDevices(DefaultInstance.NativeInstance).ToList()*/;
        }

    }
}
