using System;
using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace Elfenlabs.Text
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct GlyphPixelMetrics
    {
        public int CodePoint;
        public int X;
        public int Y;
        public int Width;
        public int Height;
        public int Left;
        public int Top;
    }

    public struct GlyphRuntimeData
    {
        public int CodePoint;
        public float4 AtlasUV;
        public float TopEM;
        public float LeftEM;
        public GlyphPixelMetrics PixelMetrics;
        public GlyphRuntimeData(GlyphPixelMetrics pixelMetrics, float glyphSize, float atlasSize)
        {
            PixelMetrics = pixelMetrics;
            CodePoint = pixelMetrics.CodePoint;
            AtlasUV = new float4(
                    pixelMetrics.X / atlasSize,
                    pixelMetrics.Y / atlasSize,
                    pixelMetrics.Width / atlasSize,
                    pixelMetrics.Height / atlasSize
            );
            TopEM = pixelMetrics.Top / glyphSize;
            LeftEM = pixelMetrics.Left / glyphSize;
        }
    }
}