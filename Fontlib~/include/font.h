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
        FontHandle *font_handle;
        int units_per_em;
        int ascender;
        int descender;
        int height;
        int max_advance_width;
        int max_advance_height;
        int underline_pos;
        int underline_thickness;

        FontDescription(FT_Library ftLib, Buffer<byte> fontData)
        {
            font_handle = new FontHandle(ftLib, fontData);
            FT_Face ftFace = font_handle->ft;
            units_per_em = ftFace->units_per_EM;
            ascender = ftFace->ascender;
            descender = ftFace->descender;
            height = ftFace->height;
            max_advance_width = ftFace->max_advance_width;
            max_advance_height = ftFace->max_advance_height;
            underline_pos = ftFace->underline_position;
            underline_thickness = ftFace->underline_thickness;
        }
    };
}

#endif