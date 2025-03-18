#ifndef FONTLIB_H
#define FONTLIB_H

struct GlyphData
{
    unsigned char *bitmap; // Pixel data for the glyph
    int width;             // Bitmap width
    int height;            // Bitmap height
    int xOffset;           // X offset from HarfBuzz shaping
    int yOffset;           // Y offset from HarfBuzz shaping
};

extern "C" __declspec(dllexport) int RenderText(
    const char *text,              // Input text (UTF-8)
    const unsigned char *fontData, // Font file data (e.g., TTF)
    size_t fontDataSize,           // Size of font data
    int pixelSize,                 // Font size in pixels
    GlyphData **outGlyphs,         // Pointer to array of GlyphData
    int *outGlyphCount             // Number of glyphs rendered
);

#endif