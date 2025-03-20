#ifndef CONTEXT_H
#define CONTEXT_H

#include "shape.h"
#include "error.h"
#include "hb.h"
#include <vector>
#include <ft2build.h>
#include FT_FREETYPE_H

namespace Text
{
    class Face
    {
    public:
        FT_Face ftFace;
        hb_font_t *hb;
        Face(FT_Library ftLib, const unsigned char *fontData, size_t fontDataSize);
        ~Face();
    };

    class Context
    {
        FT_Library ftLib;
        std::vector<Face *> faces;

    public:
        Context();
        ~Context();
        Result<int> LoadFont(const unsigned char *fontData, size_t fontDataSize);
        Result<void> UnloadFont(int fontIndex);
        Result<void> ShapeText(int fontIndex, const char *text, int textLen, int maxGlyphs, Glyph *refGlyphs, int *outGlyphCount);
    };
}

#endif