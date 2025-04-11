using System;
using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace Elfenlabs.Text
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct GlyphMetrics
    {
        public int CodePoint;
        public int AtlasXPx;
        public int AtlasYPx;
        public int AtlasWidthPx;
        public int AtlasHeightPx;
        public int WidthFontUnits;
        public int HeightFontUnits;
        public int LeftFontUnits;
        public int TopFontUnits;
    }

    public struct GlyphRuntimeData
    {
        public int CodePoint;
        public float4 AtlasUV;
        public GlyphMetrics Metrics;
        public GlyphRuntimeData(GlyphMetrics pixelMetrics, float glyphSize, float atlasSize)
        {
            Metrics = pixelMetrics;
            CodePoint = pixelMetrics.CodePoint;
            AtlasUV = new float4(
                    pixelMetrics.AtlasXPx / atlasSize,
                    pixelMetrics.AtlasYPx / atlasSize,
                    pixelMetrics.AtlasWidthPx / atlasSize,
                    pixelMetrics.AtlasHeightPx / atlasSize
            );
            // TopEM = pixelMetrics.TopFontUnits / (glyphSize * 64f);
            // TopEM = (pixelMetrics.TopFracPx >> 6) / glyphSize;
            // LeftEM = pixelMetrics.LeftFontUnits / (glyphSize * 64f);
        }
    }
}