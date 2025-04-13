#ifndef SHAPE_H
#define SHAPE_H

#include <stdint.h>

namespace Text
{
    struct Glyph
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
}

#endif