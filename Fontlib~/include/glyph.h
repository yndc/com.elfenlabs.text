#ifndef SHAPE_H
#define SHAPE_H

#include <stdint.h>

namespace Text
{
    struct Glyph
    {
        int32_t codePoint;
        int32_t xOffset;
        int32_t yOffset;
        int32_t xAdvance;
        int32_t yAdvance;
    };

    struct GlyphPixelMetrics
    {
        int index;
        int x;
        int y;
        int w;
        int h;
        int l;
        int t;
        
        GlyphPixelMetrics(int index) : index(index), x(0), y(0), w(0), h(0), l(0), t(0) {}
    };
}

#endif