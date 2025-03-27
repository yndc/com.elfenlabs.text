using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;

namespace Elfenlabs.Text
{
    /// <summary>
    /// Array that can be passed to and from C++ plugins.
    /// Best practice to manage the lifetime of the buffer in the C# land.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct NativeBuffer<T> where T : unmanaged
    {
        private readonly IntPtr ptr;
        private readonly Allocator allocator;
        private readonly int size;

        public static NativeBuffer<byte> FromString(string str, Allocator allocator)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(str);
            return NativeBuffer<byte>.FromBytes(bytes, allocator);
        }

        public static NativeBuffer<byte> FromBytes(byte[] bytes, Allocator allocator)
        {
            var buffer = new NativeBuffer<byte>(bytes.Length, allocator);
            Marshal.Copy(bytes, 0, buffer.ptr, bytes.Length);
            return buffer;
        }

        public NativeBuffer(int length, Allocator allocator)
        {
            size = length * UnsafeUtility.SizeOf<T>();
            this.allocator = allocator;
            unsafe
            {
                ptr = (IntPtr)UnsafeUtility.Malloc(size, 4, allocator);
            }
        }

        public readonly int SizeBytes()
        {
            return size;
        }

        public readonly int Count()
        {
            return size / ItemSize();
        }

        public readonly int ItemSize()
        {
            unsafe
            {
                return sizeof(T);
            }
        }

        public readonly T this[int index]
        {
            get
            {
                unsafe
                {
                    return Marshal.PtrToStructure<T>(IntPtr.Add(ptr, index * sizeof(T)));
                }
            }
            set
            {
                unsafe
                {
                    Marshal.StructureToPtr(value, IntPtr.Add(ptr, index * sizeof(T)), false);
                }
            }
        }

        public readonly void Dispose()
        {
            unsafe
            {
                UnsafeUtility.Free((void*)ptr, allocator);
            }
        }

        public unsafe JobHandle Dispose(JobHandle inputDeps)
        {
            var job = new BufferDisposalJob
            {
                Ptr = ptr,
                Allocator = allocator
            };
            return job.Schedule(inputDeps);
        }

        struct BufferDisposalJob : IJob
        {
            public IntPtr Ptr;
            public Allocator Allocator;

            public readonly void Execute()
            {
                unsafe
                {
                    UnsafeUtility.Free((void*)Ptr, Allocator);
                }
            }
        }
    }
}