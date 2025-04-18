using System;
using System.Runtime.InteropServices;
using Elfenlabs.Texture;
using Unity.Mathematics;

namespace Elfenlabs.Text
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct GlyphMetrics : IRectangle
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

        public int X { readonly get => AtlasXPx; set => AtlasXPx = value; }
        public int Y { readonly get => AtlasYPx; set => AtlasYPx = value; }

        public readonly int Width => AtlasWidthPx;

        public readonly int Height => AtlasHeightPx;
    }

    public struct GlyphRuntimeData
    {
        public int CodePoint;
        public float4 AtlasUV;
        public GlyphMetrics Metrics;
        public GlyphRuntimeData(GlyphMetrics pixelMetrics, float atlasSize)
        {
            Metrics = pixelMetrics;
            CodePoint = pixelMetrics.CodePoint;
            AtlasUV = new float4(
                    pixelMetrics.AtlasXPx / atlasSize,
                    pixelMetrics.AtlasYPx / atlasSize,
                    pixelMetrics.AtlasWidthPx / atlasSize,
                    pixelMetrics.AtlasHeightPx / atlasSize
            );
        }
    }
}