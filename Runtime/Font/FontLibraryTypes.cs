using System;
using System.Runtime.InteropServices;
using Elfenlabs.Collections;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Elfenlabs.Text
{
    public enum ReturnCode
    {
        Success,

        // General errors
        Failure = 0001,

        // Font errors
        FontNotFound = 1000
    }

    public enum GlyphRenderFlags : int
    {
        None = 0,
        ResolveIntersection = 1 << 0,
        Test = 1 << 1,
    }

    public enum AtlasCompactFlags : int
    {
        None = 0,
        FillEnd = 1 << 0,
        Gravity = 1 << 1,
        ZigZag = 1 << 2,
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct AtlasConfig : IEquatable<AtlasConfig>
    {
        // Width and Height of the square atlas slice
        public int Size;

        // Padding around each glyph in pixels
        public int Padding;

        // Minimum distance between packed rectangles and the atlas border.
        public int Margin;

        // Size of the glyph in pixels (used for scaling)
        public int GlyphSize;
        
        public int Flags;

        public readonly bool Equals(AtlasConfig other)
        {
            return Size == other.Size && Padding == other.Padding && Margin == other.Margin && GlyphSize == other.GlyphSize && Flags == other.Flags;
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(Size, Padding, Margin, GlyphSize, Flags);
        }
    };

    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct RenderConfig
    {
        // Distance mapping range (in pixels) for the MSDF font
        public float DistanceMappingRange;

        // Flags for rendering the glyphs
        public GlyphRenderFlags Flags;
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FontDescription
    {
        public readonly IntPtr Handle;
        public readonly int UnitsPerEM;
        public readonly int Ascender;
        public readonly int Descender;
        public readonly int Height;
        public readonly int MaxAdvanceWidth;
        public readonly int MaxAdvanceHeight;
        public readonly int UnderlinePosition;
        public readonly int UnderlineThickness;
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct ShapingGlyph
    {
        public int CodePoint;
        public int Cluster;
        public int XOffset;
        public int YOffset;
        public int XAdvance;
        public int YAdvance;
    }
}