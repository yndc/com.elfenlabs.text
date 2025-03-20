#ifndef SHAPE_H
#define SHAPE_H

namespace Text
{
    struct Glyph
    {
        int codePoint;
        float xOffset;
        float yOffset;
        float xAdvance;
        float yAdvance;
    };
}

#endif