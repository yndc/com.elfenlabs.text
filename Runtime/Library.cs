using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Elfenlabs.Text
{
    public enum ErrorCode
    {
        Success,

        // General errors
        Failure = 0001,

        // Font errors
        FontNotFound = 1000
    }

    enum GlyphRenderFlags : int
    {
        None = 0,
        ResolveIntersection = 1 << 0,
        Test = 1 << 1,
    }

    enum AtlasCompactFlags : int
    {
        None = 0,
        FillEnd = 1 << 0,
        Gravity = 1 << 1,
        ZigZag = 1 << 2,
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct Glyph
    {
        public int CodePoint;
        public int XOffset;
        public int YOffset;
        public int XAdvance;
        public int YAdvance;
    }

    public static class FontLibrary
    {
        public struct Instance
        {
            public IntPtr Ptr;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr AllocCallback(int size, int alignment, Allocator allocator);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void DisposeCallback(IntPtr ptr, Allocator allocator);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void UnityLogCallback(string message);

        [DllImport("fontlib", CallingConvention = CallingConvention.Cdecl)]
        public static extern ErrorCode CreateContext(
            UnityLogCallback logCallback,
            AllocCallback allocCallback,
            DisposeCallback disposeCallback,
            out IntPtr ctx
        );

        [DllImport("fontlib", CallingConvention = CallingConvention.Cdecl)]
        public static extern ErrorCode DestroyContext(IntPtr ctx);

        [DllImport("fontlib", CallingConvention = CallingConvention.Cdecl)]
        public static extern ErrorCode LoadFont(
            IntPtr ctx,
            NativeBuffer<byte> fontData,
            out int fontIndex
        );

        [DllImport("fontlib", CallingConvention = CallingConvention.Cdecl)]
        public static extern ErrorCode UnloadFont(
            IntPtr ctx,
            int fontIndex
        );

        [DllImport("fontlib", CallingConvention = CallingConvention.Cdecl)]
        public static extern ErrorCode ShapeText(
            IntPtr ctx,
            int fontIndex,
            Allocator allocator,
            NativeBuffer<byte> text,
            out NativeBuffer<Glyph> outGlyphs
        );

        [DllImport("fontlib", CallingConvention = CallingConvention.Cdecl)]
        public static extern ErrorCode DrawAtlas(
            IntPtr ctx,
            int fontIndex,
            int textureSize,
            int glyphSize,
            int padding,
            float distanceMappingRange,
            int glyphRenderFlags,
            int compactFlags,
            Allocator allocator,
            in NativeBuffer<byte> inText,
            ref NativeBuffer<Color32> refTexture,
            out NativeBuffer<Glyph> outGlyphs
        );

        [DllImport("fontlib", CallingConvention = CallingConvention.Cdecl)]
        public static extern ErrorCode GetDebug(IntPtr ctx, IntPtr strPtr, IntPtr outPtrSize);

        [DllImport("fontlib", CallingConvention = CallingConvention.Cdecl)]
        public static extern ErrorCode SetUnityLogCallback(IntPtr ctx, UnityLogCallback callback);

        [AOT.MonoPInvokeCallback(typeof(UnityLogCallback))]
        public static void UnityLog(string message)
        {
            Debug.Log("TextLib | " + message);
        }

        [AOT.MonoPInvokeCallback(typeof(AllocCallback))]
        public static IntPtr UnityAllocator(int size, int alignment, Allocator allocator)
        {
            unsafe
            {
                return (IntPtr)UnsafeUtility.Malloc(size, alignment, allocator);
            }
        }

        [AOT.MonoPInvokeCallback(typeof(AllocCallback))]
        public static void UnityDisposer(IntPtr ptr, Allocator allocator)
        {
            unsafe
            {
                UnsafeUtility.Free((void*)ptr, allocator);
            }
        }
    }
}