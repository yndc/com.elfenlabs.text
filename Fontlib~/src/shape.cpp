#include <hb.h>
#include <error.h>
#include <context.h>

namespace Text
{
    Result<void> Context::ShapeText(
        int fontIndex,
        const char *text,
        int textLen,
        int maxGlyphs,
        Glyph *refGlyphs,
        int *outGlyphCount)
    {
        auto font = faces[fontIndex];
        if (font == nullptr)
            return Result<void>::Fail(ErrorCode::FontNotFound);

        // Shape the text
        auto buffer = hb_buffer_create();
        hb_buffer_add_utf8(buffer, text, textLen, 0, textLen);
        hb_buffer_set_direction(buffer, HB_DIRECTION_LTR);
        hb_buffer_set_script(buffer, HB_SCRIPT_LATIN);
        hb_buffer_set_language(buffer, hb_language_from_string("en", -1));
        hb_shape(font->hb, buffer, nullptr, 0);

        // Get glyph info and positions
        unsigned int glyphCount;
        hb_glyph_info_t *glyphInfo = hb_buffer_get_glyph_infos(buffer, &glyphCount);
        hb_glyph_position_t *glyphPos = hb_buffer_get_glyph_positions(buffer, &glyphCount);

        if (glyphCount > maxGlyphs)
            return Result<void>::Fail(ErrorCode::ShapingOutTooSmall);

        // Write glyph data
        *outGlyphCount = glyphCount;
        for (unsigned int i = 0; i < glyphCount; ++i)
        {
            auto ptr = refGlyphs + i;
            ptr->codePoint = glyphInfo[i].codepoint;
            ptr->xOffset = glyphPos[i].x_offset;
            ptr->yOffset = glyphPos[i].y_offset;
            ptr->xAdvance = glyphPos[i].x_advance;
            ptr->yAdvance = glyphPos[i].y_advance;
        }

        // Clean up
        hb_buffer_destroy(buffer);

        return Result<void>::Success();
    }
}
