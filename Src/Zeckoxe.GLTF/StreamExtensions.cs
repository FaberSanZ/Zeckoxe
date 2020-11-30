﻿

using System.IO;

namespace GltfLoader
{
    internal static class StreamExtensions
    {
        public static void Align(this Stream stream, int size, byte fillByte = 0)
        {
            long num = stream.Position % size;
            if (num == 0L)
            {
                return;
            }

            for (int index = 0; index < size - num; ++index)
            {
                stream.WriteByte(fillByte);
            }
        }
    }
}