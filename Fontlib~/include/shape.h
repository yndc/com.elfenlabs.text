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
}

#endif