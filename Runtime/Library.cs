using System;
using System.Runtime.InteropServices;
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

        public delegate void UnityLogCallback(string message);

        public static Instance CreateInstance()
        {
            var error = CreateContext(out var ptr);
            if (error != ErrorCode.Success)
            {
                throw new Exception($"Failed to create font context: {error}");
            }
            return new Instance
            {
                Ptr = ptr
            };
        }

        [DllImport("fontlib", CallingConvention = CallingConvention.Cdecl)]
        public static extern ErrorCode CreateContext(out IntPtr ctx);

        [DllImport("fontlib", CallingConvention = CallingConvention.Cdecl)]
        public static extern ErrorCode DestroyContext(IntPtr ctx);

        [DllImport("fontlib", CallingConvention = CallingConvention.Cdecl)]
        public static extern ErrorCode LoadFont(
            IntPtr ctx,
            out int fontIndex,
            byte[] fontData,
            int fontDataSize
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
            IntPtr text,
            int textLen,
            int maxGlyphs,
            IntPtr refGlyphs,
            out int outGlyphCount
        );

        [DllImport("fontlib", CallingConvention = CallingConvention.Cdecl)]
        public static extern ErrorCode DrawMTSDFGlyph(
            IntPtr ctx,
            int fontIndex,
            int glyphIndex,
            IntPtr refTexture,
            int textureWidth
        );

        [DllImport("fontlib", CallingConvention = CallingConvention.Cdecl)]
        public static extern ErrorCode DrawAtlas(
            IntPtr ctx,
            int fontIndex,
            IntPtr text,
            int textLen,
            int textureSize,
            int pixelSize,
            IntPtr outTexture
        );

        [DllImport("fontlib", CallingConvention = CallingConvention.Cdecl)]
        public static extern ErrorCode GetDebug(IntPtr ctx, IntPtr strPtr, IntPtr outPtrSize);

        [DllImport("fontlib", CallingConvention = CallingConvention.Cdecl)]
        public static extern ErrorCode SetUnityLogCallback(IntPtr ctx, UnityLogCallback callback);

        [AOT.MonoPInvokeCallback(typeof(UnityLogCallback))]
        public static void StandardLogger(string message)
        {
            Debug.Log("TextLib | " + message);
        }
    }
}