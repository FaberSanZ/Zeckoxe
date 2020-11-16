﻿// Copyright (c) 2019-2020 Faber Leonardo. All Rights Reserved. https://github.com/FaberSanZ
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)


/*=============================================================================
	CommandList.cs
=============================================================================*/


using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

namespace Zeckoxe.Graphics
{
    public enum CommandBufferType
    {
        Generic,

        AsyncGraphics,

        AsyncCompute,

        AsyncTransfer,

        Count
    }

    //TODO: CommandBufferType
    public unsafe class CommandBuffer : GraphicsResource
    {

        internal uint imageIndex;
        internal VkCommandBuffer handle;
        internal VkFence waitFences; // TODO: VkFence -> Fence(0)



        public CommandBuffer(Device graphicsDevice, CommandBufferType type) : base(graphicsDevice)
        {
            Type = type;

            Recreate();
        }

        public CommandBufferType Type { get; set; }


        public void Recreate()
        {
            handle = NativeDevice.CreateCommandBufferPrimary();

            // Fences (Used to check draw command buffer completion)
            VkFenceCreateInfo fenceCreateInfo = new VkFenceCreateInfo()
            {
                sType = VkStructureType.FenceCreateInfo
            };

            // Create in signaled state so we don't wait on first render of each command buffer
            fenceCreateInfo.flags = VkFenceCreateFlags.Signaled;
            vkCreateFence(NativeDevice.handle, &fenceCreateInfo, null, out waitFences);

        }

        public CommandBufferType GetPhysicalQueueType(CommandBufferType type)
        {
            if (type != CommandBufferType.AsyncGraphics)
            {
                return type;
            }

            else
            {
                if (NativeDevice.GraphicsFamily == NativeDevice.ComputeFamily && NativeDevice.queueFamilyProperties /*graphics_queue*/ != NativeDevice.queueFamilyProperties /*compute_queue*/)
                {
                    return CommandBufferType.AsyncCompute;
                }
                else
                {
                    return CommandBufferType.Generic;
                }
            }
        }

        public void Begin(SwapChain swapChain)
        {
            // By setting timeout to UINT64_MAX we will always wait until the next image has been acquired or an actual error is thrown
            // With that we don't have to handle VK_NOT_READY
            vkAcquireNextImageKHR(NativeDevice.handle, swapChain.handle, ulong.MaxValue, NativeDevice.imageAvailableSemaphore, new VkFence(), out uint i);
            imageIndex = i;



            // Use a fence to wait until the command buffer has finished execution before using it again
            fixed (VkFence* ptrfence = &waitFences)
            {
                vkWaitForFences(NativeDevice.handle, 1, ptrfence, true, ulong.MaxValue);
                vkResetFences(NativeDevice.handle, 1, ptrfence);
            }


            VkCommandBufferBeginInfo beginInfo = new VkCommandBufferBeginInfo()
            {
                sType = VkStructureType.CommandBufferBeginInfo,
                flags = VkCommandBufferUsageFlags.RenderPassContinue,
            };

            vkBeginCommandBuffer(handle, &beginInfo);



        }


        public void BeginFramebuffer(Framebuffer framebuffer, float r = 0, float g = .2f, float b = .4f, float a = 1.0f)
        {
            // Set clear values for all framebuffer attachments with loadOp set to clear
            // We use two attachments (color and depth) that are cleared at the start of the subpass and as such we need to set clear values for both
            VkClearValue* clearValues = stackalloc VkClearValue[2];
            clearValues[0].color = new(r, g, b, a);
            clearValues[1].depthStencil = new(1, 0)
            {
                //depth = 1.0f,
                //stencil = 0
            };

            int h = NativeDevice.NativeParameters.BackBufferHeight;
            int w = NativeDevice.NativeParameters.BackBufferWidth;
            int x = 0;
            int y = 0;

            VkRenderPassBeginInfo renderPassBeginInfo = new VkRenderPassBeginInfo()
            {
                sType = VkStructureType.RenderPassBeginInfo,
                renderArea = new(x, y, w, h),

                renderPass = framebuffer.renderPass,
                clearValueCount = 2,
                pClearValues = clearValues,
                framebuffer = framebuffer.framebuffers[imageIndex], // Set target frame buffer
            };

            vkCmdBeginRenderPass(handle, &renderPassBeginInfo, VkSubpassContents.Inline);
        }


        public void Clear(float R, float G, float B, float A = 1.0f)
        {
            VkClearColorValue clearValue = new VkClearColorValue(R, G, B, A);

            VkImageSubresourceRange clearRange = new VkImageSubresourceRange()
            {
                aspectMask = VkImageAspectFlags.Color,
                baseMipLevel = 0,
                baseArrayLayer = 0,
                layerCount = 1,
                levelCount = 1
            };

            //vkCmdClearColorImage(NativeCommandBuffer, NativeDevice.SwapChain.Images[(int)imageIndex], VkImageLayout.ColorAttachmentOptimal, &clearValue, 1, &clearRange);
        }



        public void FillBuffer(Buffer dst, int value)
        {
            fill_buffer(dst.handle, (uint)value, 0, WholeSize);
        }

        internal void fill_buffer(VkBuffer dst, uint value, ulong offset, ulong size)
        {
            vkCmdFillBuffer(handle, dst, offset, size, value);
        }


        internal void copy_buffer(Buffer dst, ulong dst_offset, Buffer src, ulong src_offset, ulong size)
        {
            VkBufferCopy region = new()
            {
                size = size,
                srcOffset = src_offset,
                dstOffset = dst_offset,
            };

            vkCmdCopyBuffer(handle, src.handle, dst.handle, 1, &region);
        }

        public void CopyBuffer(Buffer dst, Buffer src)
        {
            if (dst.size == src.size)
            {
                // TODO: CopyBuffer - Assert
            }
            copy_buffer(dst, 0, src, 0, (uint)dst.size);
        }


        internal void copy_buffer(Buffer dst, Buffer src, VkBufferCopy* copies, uint count)
        {
            vkCmdCopyBuffer(handle, src.handle, dst.handle, count, copies);
        }


        internal void Copy_Buffer(VkBuffer sourceBuffer, VkBuffer destinationBuffer, VkBufferCopy bufferCopy)
        {

            VkBufferMemoryBarrier* bufferBarriers = stackalloc VkBufferMemoryBarrier[2];
            bufferBarriers[0x0] = new()
            {
                sType = VkStructureType.BufferMemoryBarrier,
                pNext = null,
            };

            bufferBarriers[0x1] = new()
            {
                sType = VkStructureType.BufferMemoryBarrier,
                pNext = null,
            };
            //vkCmdPipelineBarrier()
            vkCmdCopyBuffer(handle, sourceBuffer, destinationBuffer, 1, &bufferCopy);

        }

        void copy_image(Texture dst, Texture src, VkOffset3D dst_offset,
                        VkOffset3D src_offset, VkExtent3D extent,
                        VkImageSubresourceLayers dst_subresource,
                        VkImageSubresourceLayers src_subresource)
        {

            VkImageCopy region = new() 
            {
                dstOffset = dst_offset,
                srcOffset = src_offset,
                extent = extent,
                srcSubresource = src_subresource,
                dstSubresource = dst_subresource,
            };


            vkCmdCopyImage(handle, src.handle, src.get_layout(VkImageLayout.TransferSrcOptimal), dst.handle, dst.get_layout(VkImageLayout.TransferDstOptimal), 1, &region);
        }


        internal void copy_image(Texture dst, Texture src)
        {
            VkImageCopy* regions = stackalloc VkImageCopy[32];

            int levels = src.Levels;


            for (uint i = 0; i < levels; i++)
            {
                regions[i] = new VkImageCopy
                {
                    extent = new(src.Width, src.Height, src.Depth),
                    
                    dstSubresource = new()
                    {
                        mipLevel = i,
                        aspectMask = VulkanConvert.format_to_aspect_mask(dst.format),
                        layerCount = dst.layers,
                    },
                    srcSubresource = new()
                    {
                        mipLevel = i,
                        aspectMask = VulkanConvert.format_to_aspect_mask(src.format),
                        layerCount = src.layers,
                    }
                };


                if (regions[i].srcSubresource.aspectMask == regions[i].dstSubresource.aspectMask)
                {
                    // TODO: copy_image - Assert
                }
            }

            vkCmdCopyImage(handle, src.handle, src.get_layout(VkImageLayout.TransferSrcOptimal), dst.handle, dst.get_layout(VkImageLayout.TransferDstOptimal), (uint)levels, regions);
        }


        public void CopyTexture(Texture dst, Texture src)
        {
            copy_image(dst, src);
        }



        internal void copy_buffer_to_image(Texture image, Buffer buffer, uint num_blits, VkBufferImageCopy* blits)
        {
            vkCmdCopyBufferToImage(handle, buffer.handle ,image.handle, image.get_layout(VkImageLayout.TransferDstOptimal), num_blits, blits);
        }

        internal void copy_image_to_buffer(Buffer buffer, Texture image, uint num_blits, VkBufferImageCopy* blits)
        {

            vkCmdCopyImageToBuffer(handle, image.handle, image.get_layout(VkImageLayout.TransferSrcOptimal), buffer.handle, num_blits, blits);
        }



        internal void copy_buffer_to_image(Texture image, Buffer src, ulong buffer_offset,VkOffset3D offset, VkExtent3D extent, uint row_length, uint slice_height, VkImageSubresourceLayers subresource)
        {
            VkBufferImageCopy region = new VkBufferImageCopy()
            {
                bufferOffset = buffer_offset,
                bufferRowLength = row_length,
                bufferImageHeight = slice_height,
                imageSubresource = subresource,
                imageOffset = offset,
                imageExtent = extent,
            };

            vkCmdCopyBufferToImage(handle, src.handle, image.handle, image.get_layout(VkImageLayout.TransferDstOptimal),1, &region);
        }

        internal void copy_buffer_to_image(Buffer src, Texture image, ulong buffer_offset, VkOffset3D offset, VkExtent3D extent, uint row_length, uint slice_height, VkImageSubresourceLayers subresource)
        {
            VkBufferImageCopy region = new VkBufferImageCopy()
            {
                bufferOffset = buffer_offset,
                bufferRowLength = row_length,
                bufferImageHeight = slice_height,
                imageSubresource = subresource,
                imageOffset = offset,
                imageExtent = extent,
            };

            vkCmdCopyBufferToImage(handle, src.handle, image.handle, image.get_layout(VkImageLayout.TransferSrcOptimal), 1, &region);
        }


        // TODO: ALL_GPUS  
        internal void set_current_gpu(int gpu_index)
        {
            //if (NativeDevice.device_count > 1)
            //{
            //    if (gpu_index == ALL_GPUS)
            //        vkCmdSetDeviceMaskKHR(handle, (1 << NativeDevice.device_count) - 1);
            //    else
            //        qvkCmdSetDeviceMaskKHR(handle, 1 << gpu_index);
            //}
        }


        public void SetCullMode(CullMode mode)
        {
            vkCmdSetCullModeEXT(handle, mode.ToCullMode());
        }

        public void SetLineWidth(float lineWidth)
        {
            vkCmdSetLineWidth(handle, lineWidth);
        }

        public void SetFrontFace(FrontFace frontFace)
        {
            vkCmdSetFrontFaceEXT(handle, frontFace.ToFrontFace());
        }

        public void SetPrimitiveTopology(PrimitiveType type)
        {
            vkCmdSetPrimitiveTopologyEXT(handle, type.ToPrimitiveType());
        }


        public void SetGraphicPipeline(GraphicsPipelineState pipelineState)
        {
            vkCmdBindPipeline(handle, VkPipelineBindPoint.Graphics, pipelineState.graphicsPipeline);
        }


        public void SetComputePipeline(GraphicsPipelineState pipelineState)
        {
            //vkCmdBindPipeline(NativeCommandBuffer, VkPipelineBindPoint.Compute, pipelineState.computesPipeline);
        }

        public void SetRayTracinPipeline(GraphicsPipelineState pipelineState)
        {
            //vkCmdBindPipeline(NativeCommandBuffer, VkPipelineBindPoint.RayTracingNV, pipelineState.rayTracinPipeline);
        }


        public void SetScissor(int width, int height, int x, int y)
        {
            // Update dynamic scissor state
            VkRect2D scissor = new(x,y,width,height);

            vkCmdSetScissor(handle, 0, 1, &scissor);
        }

        public void SetViewport(float Width, float Height, float X, float Y, float MinDepth = 0.0f, float MaxDepth = 1.0f)
        {
            VkViewport Viewport = new(X, Y, Width, Height, MinDepth, MaxDepth);

            vkCmdSetViewport(handle, 0, 1, &Viewport);
        }

        public void SetVertexBuffer(Buffer buffer, ulong offsets = 0)
        {
            fixed (VkBuffer* bufferptr = &buffer.handle)
            {
                vkCmdBindVertexBuffers(handle, 0, 1, bufferptr, &offsets);
            }
        }

        public void SetVertexBuffers(Buffer[] buffers, ulong offsets = 0)
        {
            VkBuffer* buffer = stackalloc VkBuffer[buffers.Length];

            for (int i = 0; i < buffers.Length; i++)
            {
                buffer[i] = buffers[i].handle;
            }

            //fixed(VkBuffer* bufferptr = &buffers[0].Handle)
            //{

            //}

            vkCmdBindVertexBuffers(handle, 0, 1, buffer, &offsets);
        }

        public void SetIndexBuffer(Buffer buffer, ulong offsets = 0, IndexType indexType = IndexType.Uint32)
        {
            if (buffer.handle != VkBuffer.Null)
            {
                vkCmdBindIndexBuffer(handle, buffer.handle, offsets, (VkIndexType)indexType);
            }
        }

        public void Draw(int vertexCount, int instanceCount, int firstVertex, int firstInstance)
        {
            vkCmdDraw(handle, (uint)vertexCount, (uint)instanceCount, (uint)firstVertex, (uint)firstInstance);
        }



        public void DrawIndexed(int indexCount, int instanceCount, int firstIndex, int vertexOffset, int firstInstance)
        {
            vkCmdDrawIndexed(handle, (uint)indexCount, (uint)instanceCount, (uint)firstIndex, vertexOffset, (uint)firstInstance);
        }



        public void Close()
        {
            CleanupRenderPass();
            vkEndCommandBuffer(handle);
        }


        internal unsafe void CleanupRenderPass()
        {
            vkCmdEndRenderPass(handle);
        }


        public void BindDescriptorSets(DescriptorSet descriptor)
        {
            // Bind descriptor sets describing shader binding points
            VkDescriptorSet descriptor_set = descriptor._descriptorSet;
            VkPipelineLayout pipeline_layout = descriptor.PipelineState._pipelineLayout;

            vkCmdBindDescriptorSets(handle, VkPipelineBindPoint.Graphics, pipeline_layout, 0, 1, &descriptor_set, 0, null);
        }



        public void Submit()
        {
            VkSemaphore signalSemaphore = NativeDevice.renderFinishedSemaphore;
            VkSemaphore waitSemaphore = NativeDevice.imageAvailableSemaphore;
            VkPipelineStageFlags waitStages = VkPipelineStageFlags.ColorAttachmentOutput;
            VkCommandBuffer commandBuffer = handle;


            VkSubmitInfo submitInfo = new()
            {
                sType = VkStructureType.SubmitInfo,
                waitSemaphoreCount = 1,
                pWaitSemaphores = &waitSemaphore,
                pWaitDstStageMask = &waitStages,
                pNext = null,
                commandBufferCount = 1,
                pCommandBuffers = &commandBuffer,
                signalSemaphoreCount = 1,
                pSignalSemaphores = &signalSemaphore,
            };

            vkQueueSubmit(NativeDevice.nativeCommandQueue, 1, &submitInfo, waitFences);
        }


    }
}
