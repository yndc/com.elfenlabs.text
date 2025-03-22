#ifndef CONTEXT_H
#define CONTEXT_H

#include <stdint.h>
#include "shape.h"
#include "texture.h"
#include "error.h"
#include "hb.h"
#include <vector>
#include <ft2build.h>
#include FT_FREETYPE_H
#include <sstream>

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

    public:
        std::vector<Face *> faces;
        std::stringstream debug;
        Context();
        ~Context();
        Result<int> LoadFont(const unsigned char *fontData, size_t fontDataSize);
        Result<void> UnloadFont(int fontIndex);
        Result<void> ShapeText(int fontIndex, const char *text, int textLen, int maxGlyphs, Glyph *refGlyphs, int *outGlyphCount);
        Result<void> DrawMTSDFGlyph(int fontIndex, int glyphIndex, RGBA32Pixel *refTexture, int textureWidth);
        Result<void> GetDebug(void *outBuffer, int *outBufferSize)
        {
            auto str = debug.str();
            auto ptr = str.c_str();
            auto size = str.size();
            memcpy(outBuffer, ptr, size);
            *outBufferSize = size;
            debug.clear();
            return Result<void>::Success();
        }
    };
}

#endif