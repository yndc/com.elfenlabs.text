#ifndef API_H
#define API_H

#include "error.h"
#include "shape.h"
#include "context.h"
#include "atlas.h"
#include "msdfgen.h"
#include "msdfgen-ext.h"

extern "C" __declspec(dllexport) Text::ErrorCode CreateContext(void **ctx)
{
    *ctx = new Text::Context();
    return Text::ErrorCode::Success;
}

extern "C" __declspec(dllexport) Text::ErrorCode DestroyContext(void *ctx)
{
    delete (Text::Context *)ctx;
    return Text::ErrorCode::Success;
}

extern "C" __declspec(dllexport) Text::ErrorCode LoadFont(
    void *ctx,                     // Context
    void *outFontIndex,            // Out font index
    const unsigned char *fontData, // Font file data (e.g., TTF)
    size_t fontDataSize            // Size of font data
)
{
    auto context = (Text::Context *)ctx;
    auto result = context->LoadFont(fontData, fontDataSize);
    if (result.IsError())
        return Text::ErrorCode::Failure;
    *(int *)outFontIndex = result.GetValue();
    return Text::ErrorCode::Success;
};

extern "C" __declspec(dllexport) Text::ErrorCode UnloadFont(
    void *ctx,    // Context
    int fontIndex // Font index
)
{
    auto context = (Text::Context *)ctx;
    auto result = context->UnloadFont(fontIndex);
    if (result.IsError())
        return Text::ErrorCode::Failure;
    return Text::ErrorCode::Success;
};

extern "C" __declspec(dllexport) Text::ErrorCode ShapeText(
    void *ctx,              // Context
    int fontIndex,          // Font index
    const char *text,       // Text to shape
    int textLen,            // Text length
    int maxGlyphs,          // Maximum number of glyphs
    Text::Glyph *refGlyphs, // Output glyphs
    int *outGlyphCount      // Output glyph count
)
{
    auto context = (Text::Context *)ctx;
    auto result = context->ShapeText(fontIndex, text, textLen, maxGlyphs, refGlyphs, outGlyphCount);
    if (result.IsError())
        return Text::ErrorCode::Failure;
    return Text::ErrorCode::Success;
};

extern "C" __declspec(dllexport) Text::ErrorCode DrawMTSDFGlyph(
    void *ctx,                  // Context
    int fontIndex,              // Font index
    int glyphIndex,             // Glyph index
    Text::RGBA32Pixel *texture, // Texture
    int textureWidth            // Texture width
)
{
    auto context = (Text::Context *)ctx;
    auto result = context->DrawMTSDFGlyph(fontIndex, glyphIndex, texture, textureWidth);
    if (result.IsError())
        return Text::ErrorCode::Failure;
    return Text::ErrorCode::Success;
};

extern "C" __declspec(dllexport) Text::ErrorCode DrawAtlas(
    void *ctx,
    int fontIndex,
    const char *text,
    int textLen,
    int textureSize,
    int pixelSize,
    Text::RGBA32Pixel *outTexture)
{
    auto context = (Text::Context *)ctx;

    // Shape the text to get the glyphs
    int maxGlyphs = textLen * 2;
    int glyphCount;
    auto shapingResult = (Text::Glyph *)malloc(sizeof(Text::Glyph) * maxGlyphs);
    context->ShapeText(fontIndex, text, textLen, maxGlyphs, shapingResult, &glyphCount);

    context->debug << "Glyph count: " << glyphCount << std::endl;

    // Extract unique glyph indices
    auto glyphIndexSet = std::set<int>();
    for (int i = 0; i < glyphCount; ++i)
    {
        glyphIndexSet.insert(shapingResult[i].codePoint);
        context->debug << "Glyph " << i << ": " << shapingResult[i].codePoint << std::endl;
    }

    auto glyphs = std::vector<Text::GlyphRect>();
    for (auto glyphIndex : glyphIndexSet)
    {
        glyphs.push_back({glyphIndex, 0, 0, 0, 0});
        context->debug << "Unique glyph: " << glyphIndex << std::endl;
    }

    // Obtain metrics for all glyphs
    FT_Face face = context->faces[fontIndex]->ftFace;
    FT_Set_Pixel_Sizes(face, 0, pixelSize);
    for (auto &glyph : glyphs)
    {
        FT_Load_Glyph(face, glyph.index, FT_LOAD_DEFAULT);
        auto metrics = face->glyph->metrics;
        glyph.w = metrics.width >> 6; // Convert 26.6 to pixels
        glyph.h = metrics.height >> 6;
        context->debug << "Glyph " << glyph.index << " metrics: " << glyph.w << "x" << glyph.h << std::endl;
    }

    // Prepare the atlas
    auto atlas = Text::AtlasBuilder(textureSize);
    auto atlasResult = atlas.Add(glyphs);

    // Draw the atlas
    for (const auto &glyph : glyphs)
    {
        msdfgen::Shape shape;
        msdfgen::FontHandle font;
        font.adoptFreetypeFont(face);
        // msdfgen::loadGlyph(shape, )

        auto r = std::rand() % 255;
        auto g = std::rand() % 255;
        auto b = std::rand() % 255;
        context->debug << "Drawing glyph " << glyph.index << " at " << glyph.x << ", " << glyph.y << std::endl;
        for (int dx = 0; dx < glyph.w; ++dx)
        {
            for (int dy = 0; dy < glyph.h; ++dy)
            {
                int x = glyph.x + dx;
                int y = glyph.y + dy;
                if (x >= 0 && x < textureSize && y >= 0 && y < textureSize)
                {
                    auto pixel = outTexture + y * textureSize + x;
                    pixel->r = r;
                    pixel->g = g;
                    pixel->b = b;
                    pixel->a = 255;
                }
            }
        }
    }

    // Clean up
    free(shapingResult);
    return Text::ErrorCode::Success;
}

extern "C" __declspec(dllexport) Text::ErrorCode GetDebug(
    void *ctx,
    void *outBuffer, int *outBufferSize)
{
    auto context = (Text::Context *)ctx;
    context->GetDebug(outBuffer, outBufferSize);
    return Text::ErrorCode::Success;
};

// extern "C" __declspec(dllexport) Text::ErrorCode AddGlyphsToDynamicAtlas(
//     void *ctx,                    // Context
//     int fontIndex,                // Font index
//     int *glyphIndices,            // Pointer to array of glyph indices
//     int glyphIndicesLen,          // Length of glyph indices array
//     int textureSize,              // Texture size
//     Text::RGBA32Pixel *outTexture // Output texture pointer
// )
// {
//     auto context = (Text::Context *)ctx;
//     auto result = context->AddGlyphsToDynamicAtlas(fontIndex, glyphIndices, glyphIndicesLen, textureSize, outTexture);
//     if (result.IsError())
//         return Text::ErrorCode::Failure;
//     return Text::ErrorCode::Success;
// };

#endif