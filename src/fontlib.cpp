// fontlib.cpp
#include "fontlib.h"
#include <ft2build.h>
#include FT_FREETYPE_H
#include <hb.h>

extern "C" __declspec(dllexport) int LoadFont(const unsigned char *fontData, size_t fontDataSize)
{

}

extern "C" __declspec(dllexport) void UnloadFont(int fontIndex)
{

}

// extern "C" __declspec(dllexport) int RenderText(
//     const char *text,
//     const unsigned char *fontData,
//     size_t fontDataSize,
//     int pixelSize,
//     GlyphData **outGlyphs,
//     int *outGlyphCount)
// {
//     // Initialize FreeType
//     FT_Library ftLib;
//     FT_Face ftFace;
//     if (FT_Init_FreeType(&ftLib))
//         return 1; // Error
//     if (FT_New_Memory_Face(ftLib, fontData, fontDataSize, 0, &ftFace))
//     {
//         FT_Done_FreeType(ftLib);
//         return 1; // Error
//     }
//     FT_Set_Pixel_Sizes(ftFace, 0, pixelSize);

//     // Initialize HarfBuzz
//     hb_blob_t *blob = hb_blob_create((const char *)fontData, fontDataSize, HB_MEMORY_MODE_READONLY, nullptr, nullptr);
//     hb_face_t *hbFace = hb_face_create(blob, 0);
//     hb_font_t *hbFont = hb_font_create(hbFace);
//     hb_font_set_scale(hbFont, pixelSize * 64, pixelSize * 64); // 26.6 fixed-point

//     // Shape the text
//     hb_buffer_t *buffer = hb_buffer_create();
//     hb_buffer_add_utf8(buffer, text, -1, 0, -1);
//     hb_buffer_guess_segment_properties(buffer);
//     hb_shape(hbFont, buffer, nullptr, 0);

//     // Get glyph info and positions
//     unsigned int glyphCount;
//     hb_glyph_info_t *glyphInfo = hb_buffer_get_glyph_infos(buffer, &glyphCount);
//     hb_glyph_position_t *glyphPos = hb_buffer_get_glyph_positions(buffer, &glyphCount);

//     // Allocate GlyphData array
//     *outGlyphs = (GlyphData *)malloc(sizeof(GlyphData) * glyphCount);
//     *outGlyphCount = glyphCount;

//     // Render glyphs
//     for (unsigned int i = 0; i < glyphCount; ++i)
//     {
//         FT_Load_Glyph(ftFace, glyphInfo[i].codepoint, FT_LOAD_DEFAULT);
//         FT_Render_Glyph(ftFace->glyph, FT_RENDER_MODE_NORMAL);
//         FT_Bitmap bitmap = ftFace->glyph->bitmap;

//         // Copy bitmap data
//         size_t bitmapSize = bitmap.width * bitmap.rows;
//         (*outGlyphs)[i].bitmap = (unsigned char *)malloc(bitmapSize);
//         memcpy((*outGlyphs)[i].bitmap, bitmap.buffer, bitmapSize);

//         // Set glyph properties
//         (*outGlyphs)[i].width = bitmap.width;
//         (*outGlyphs)[i].height = bitmap.rows;
//         (*outGlyphs)[i].xOffset = glyphPos[i].x_offset / 64; // Convert from 26.6
//         (*outGlyphs)[i].yOffset = glyphPos[i].y_offset / 64;
//     }

//     // Clean up
//     hb_buffer_destroy(buffer);
//     hb_font_destroy(hbFont);
//     hb_face_destroy(hbFace);
//     hb_blob_destroy(blob);
//     FT_Done_Face(ftFace);
//     FT_Done_FreeType(ftLib);

//     return 0; // Success
// }
