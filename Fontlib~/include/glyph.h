#ifndef GLYPH_H
#define GLYPH_H

#include <stdint.h>

/// @brief Glyph shape in font units
struct GlyphShape
{
    int32_t codepoint;
    int32_t cluster;
    int32_t offset_x_fu;
    int32_t offset_y_fu;
    int32_t advance_x_fu;
    int32_t advance_y_fu;
};

/// @brief Glyph metrics in font units
struct GlyphMetrics
{
    int index;
    int atlas_x_px;
    int atlas_y_px;
    int atlas_width_px;
    int atlas_height_px;
    int width_fu;
    int height_fu;
    int left_fu;
    int top_fu;

    GlyphMetrics(int index) : index(index), atlas_x_px(0), atlas_y_px(0), width_fu(0), height_fu(0), left_fu(0), top_fu(0) {}
};

void GetGlyphMetrics(Buffer<GlyphMetrics> glyphs, FontHandle *font_handle, int glyph_size, int padding)
{
    FT_Face face = font_handle->ft;
    for (int i = 0; i < glyphs.Count(); ++i)
    {
        auto &glyph = glyphs[i];
        FT_Load_Glyph(face, glyph.index, FT_LOAD_NO_SCALE);

        auto metrics = face->glyph->metrics;
        auto units_per_em = face->units_per_EM;
        glyph.width_fu = metrics.width;
        glyph.height_fu = metrics.height;
        glyph.left_fu = metrics.horiBearingX;
        glyph.top_fu = metrics.horiBearingY;
        glyph.atlas_width_px = (metrics.width * glyph_size / units_per_em) + 2 * padding;
        glyph.atlas_height_px = (metrics.height * glyph_size / units_per_em) + 2 * padding;
    }
}

#endif