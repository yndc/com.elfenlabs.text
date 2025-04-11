using System;
using System.Runtime.InteropServices;
using Elfenlabs.Collections;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Elfenlabs.Text
{
    /// <summary>
    /// Provides bindings to native font library functions for text rendering and font management.
    /// </summary>
    public static class FontLibrary
    {
        /// <summary>
        /// Represents a handle to a font instance in the native library.
        /// </summary>
        public struct Instance
        {
            /// <summary>
            /// Pointer to the native font handle.
            /// </summary>
            public IntPtr Ptr;
        }

        /// <summary>
        /// Delegate for memory allocation in the native library.
        /// </summary>
        /// <param name="size">Size of memory to allocate in bytes.</param>
        /// <param name="alignment">Memory alignment requirement.</param>
        /// <param name="allocator">Unity allocator type to use.</param>
        /// <returns>Pointer to the allocated memory.</returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr AllocCallback(int size, int alignment, Allocator allocator);

        /// <summary>
        /// Delegate for memory deallocation in the native library.
        /// </summary>
        /// <param name="ptr">Pointer to memory to free.</param>
        /// <param name="allocator">Unity allocator type that was used for allocation.</param>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void DisposeCallback(IntPtr ptr, Allocator allocator);

        /// <summary>
        /// Delegate for handling log messages from the native library.
        /// </summary>
        /// <param name="message">Log message from native code.</param>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void UnityLogCallback(string message);

        /// <summary>
        /// Creates a new library context. All library functions require a context to operate.
        /// </summary>
        /// <param name="logCallback">Function to handle logging from the native library.</param>
        /// <param name="allocCallback">Function to handle memory allocation.</param>
        /// <param name="disposeCallback">Function to handle memory deallocation.</param>
        /// <param name="ctx">Output parameter that receives the created context pointer.</param>
        /// <returns>ErrorCode indicating success or failure.</returns>
        [DllImport("fontlib", CallingConvention = CallingConvention.Cdecl)]
        public static extern ErrorCode CreateContext(
            UnityLogCallback logCallback,
            AllocCallback allocCallback,
            DisposeCallback disposeCallback,
            out IntPtr ctx
        );

        /// <summary>
        /// Destroys a previously created library context and releases all associated resources.
        /// </summary>
        /// <param name="ctx">Pointer to the context to destroy.</param>
        /// <returns>ErrorCode indicating success or failure.</returns>
        [DllImport("fontlib", CallingConvention = CallingConvention.Cdecl)]
        public static extern ErrorCode DestroyContext(IntPtr ctx);

        /// <summary>
        /// Loads a font from binary data.
        /// </summary>
        /// <param name="ctx">Library context.</param>
        /// <param name="fontData">Buffer containing the font file data.</param>
        /// <param name="fontDescription">Output parameter that receives information about the loaded font.</param>
        /// <returns>ErrorCode indicating success or failure.</returns>
        [DllImport("fontlib", CallingConvention = CallingConvention.Cdecl)]
        public static extern ErrorCode LoadFont(
            IntPtr ctx,
            NativeBuffer<byte> fontData,
            out FontDescription fontDescription
        );

        /// <summary>
        /// Unloads a previously loaded font and releases associated resources.
        /// </summary>
        /// <param name="ctx">Library context.</param>
        /// <param name="fontHandle">Handle to the font to unload.</param>
        /// <returns>ErrorCode indicating success or failure.</returns>
        [DllImport("fontlib", CallingConvention = CallingConvention.Cdecl)]
        public static extern ErrorCode UnloadFont(
            IntPtr ctx,
            IntPtr fontHandle
        );

        /// <summary>
        /// Shapes text using a loaded font, generating glyph information.
        /// </summary>
        /// <param name="ctx">Library context.</param>
        /// <param name="fontHandle">Handle to the font to use for shaping.</param>
        /// <param name="allocator">Unity memory allocator to use for output buffer.</param>
        /// <param name="text">Text to shape, as a buffer of bytes.</param>
        /// <param name="outGlyphs">Output buffer containing the shaped glyphs.</param>
        /// <returns>ErrorCode indicating success or failure.</returns>
        [DllImport("fontlib", CallingConvention = CallingConvention.Cdecl)]
        public static extern ErrorCode ShapeText(
            IntPtr ctx,
            IntPtr fontHandle,
            Allocator allocator,
            NativeBuffer<byte> text,
            out NativeBuffer<ShapingGlyph> outGlyphs
        );

        /// <summary>
        /// Draws a texture atlas of glyphs from a loaded font.
        /// </summary>
        /// <param name="ctx">Library context.</param>
        /// <param name="fontHandle">Handle to the font to use for atlas generation.</param>
        /// <param name="textureSize">Size of the output texture (width and height, assuming square texture).</param>
        /// <param name="glyphSize">Size of each glyph in pixels.</param>
        /// <param name="padding">Padding between glyphs in the atlas.</param>
        /// <param name="distanceMappingRange">Range for distance field mapping.</param>
        /// <param name="glyphRenderFlags">Flags controlling glyph rendering options.</param>
        /// <param name="compactFlags">Flags controlling atlas compactness.</param>
        /// <param name="allocator">Unity memory allocator to use for output buffers.</param>
        /// <param name="inText">Text containing characters to include in the atlas.</param>
        /// <param name="refTexture">Reference to buffer that will receive the texture data.</param>
        /// <param name="outGlyphs">Output buffer containing metrics for each glyph in the atlas.</param>
        /// <returns>ErrorCode indicating success or failure.</returns>
        [DllImport("fontlib", CallingConvention = CallingConvention.Cdecl)]
        public static extern ErrorCode DrawAtlas(
            IntPtr ctx,
            IntPtr fontHandle,
            int textureSize,
            int glyphSize,
            int padding,
            int margin,
            float distanceMappingRange,
            int glyphRenderFlags,
            int compactFlags,
            Allocator allocator,
            in NativeBuffer<byte> inText,
            ref NativeBuffer<Color32> refTexture,
            out NativeBuffer<GlyphMetrics> outGlyphs
        );

        /// <summary>
        /// Retrieves debug information from the library.
        /// </summary>
        /// <param name="ctx">Library context.</param>
        /// <param name="strPtr">Pointer to receive the debug string.</param>
        /// <param name="outPtrSize">Pointer to receive the size of the debug string.</param>
        /// <returns>ErrorCode indicating success or failure.</returns>
        [DllImport("fontlib", CallingConvention = CallingConvention.Cdecl)]
        public static extern ErrorCode GetDebug(IntPtr ctx, IntPtr strPtr, IntPtr outPtrSize);

        /// <summary>
        /// Sets or updates the Unity log callback function.
        /// </summary>
        /// <param name="ctx">Library context.</param>
        /// <param name="callback">New log callback function.</param>
        /// <returns>ErrorCode indicating success or failure.</returns>
        [DllImport("fontlib", CallingConvention = CallingConvention.Cdecl)]
        public static extern ErrorCode SetUnityLogCallback(IntPtr ctx, UnityLogCallback callback);

        /// <summary>
        /// Default implementation of the Unity log callback. Prefixes messages with "TextLib |".
        /// </summary>
        /// <param name="message">Log message from native code.</param>
        [AOT.MonoPInvokeCallback(typeof(UnityLogCallback))]
        public static void UnityLog(string message)
        {
            Debug.Log("TextLib | " + message);
        }

        /// <summary>
        /// Default implementation of memory allocation callback using Unity's UnsafeUtility.
        /// </summary>
        /// <param name="size">Size of memory to allocate in bytes.</param>
        /// <param name="alignment">Memory alignment requirement.</param>
        /// <param name="allocator">Unity allocator type to use.</param>
        /// <returns>Pointer to the allocated memory.</returns>
        [AOT.MonoPInvokeCallback(typeof(AllocCallback))]
        public static IntPtr UnityAllocator(int size, int alignment, Allocator allocator)
        {
            unsafe
            {
                return (IntPtr)UnsafeUtility.Malloc(size, alignment, allocator);
            }
        }

        /// <summary>
        /// Default implementation of memory deallocation callback using Unity's UnsafeUtility.
        /// </summary>
        /// <param name="ptr">Pointer to memory to free.</param>
        /// <param name="allocator">Unity allocator type that was used for allocation.</param>
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