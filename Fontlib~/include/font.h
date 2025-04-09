#ifndef FONT_H
#define FONT_H

#include "base.h"
#include "buffer.h"
#include "hb.h"
#include <log.h>
#include <ft2build.h>
#include FT_FREETYPE_H

namespace Text
{
    class FontHandle
    {
    public:
        FT_Face ft;
        hb_font_t *hb;

        FontHandle(FT_Library ftLib, Buffer<byte> fontData)
        {
            FT_New_Memory_Face(ftLib, fontData.Data(), fontData.SizeInBytes(), 0, &ft);
            auto blob = hb_blob_create((const char *)fontData.Data(), fontData.SizeInBytes(), HB_MEMORY_MODE_READONLY, nullptr, nullptr);
            auto face = hb_face_create(blob, 0);
            hb = hb_font_create(face);
        }

        void Dispose()
        {
            FT_Done_Face(ft);
            hb_font_destroy(hb);
        }
    };

    class FontDescription
    {
    public:
        FontHandle *fontHandle;
        int unitsPerEM;
        int ascender;
        int descender;
        int height;
        int maxAdvanceWidth;
        int maxAdvanceHeight;
        int underlinePosition;
        int underlineThickness;

        FontDescription(FT_Library ftLib, Buffer<byte> fontData)
        {
            fontHandle = new FontHandle(ftLib, fontData);
            FT_Face ftFace = fontHandle->ft;
            unitsPerEM = ftFace->units_per_EM;
            ascender = ftFace->ascender;
            descender = ftFace->descender;
            height = ftFace->height;
            maxAdvanceWidth = ftFace->max_advance_width;
            maxAdvanceHeight = ftFace->max_advance_height;
            underlinePosition = ftFace->underline_position;
            underlineThickness = ftFace->underline_thickness;
        }
    };
}

#endif