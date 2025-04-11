#ifndef API_H
#define API_H

#include <set>
#include "base.h"
#include "error.h"
#include "context.h"
#include "shape.h"
#include "atlas.h"
#include "msdfgen.h"
#include <ft2build.h>
#include FT_FREETYPE_H
#include FT_OUTLINE_H
#include "mathematics.h"

#define TEXTLIB_DEBUG

#define EXPORT_DLL extern "C" __declspec(dllexport) Text::ReturnCode

using namespace math;
using namespace Text;

/// @brief Creates a new library context, all library functions require a context
/// @param logCallback Log callback
/// @param allocCallback Buffer allocation callback
/// @param disposeCallback Buffer disposal callback
/// @param outCtx Out context
/// @return
EXPORT_DLL CreateContext(
    LogCallback logCallback,
    AllocCallback allocCallback,
    DisposeCallback disposeCallback,
    Context **outCtx
)
{
    *outCtx = new Text::Context(logCallback, allocCallback, disposeCallback);
    return Text::ReturnCode::Success;
}

/// @brief Destroys a library context
/// @param ctx
/// @return
EXPORT_DLL DestroyContext(Context *ctx)
{
    delete ctx;
    return Text::ReturnCode::Success;
}

/// @brief Loads a font from a byte buffer
/// @param ctx Context
/// @param inFontData Font data
/// @param outFontDescription Out font description
/// @return
EXPORT_DLL LoadFont(
    Context *ctx,
    Buffer<byte> inFontData,
    FontDescription *outFontDescription
)
{
    return ctx->LoadFont(inFontData, outFontDescription);
};

/// @brief Unloads a font
/// @param ctx Context
/// @param fontHandle Font index
/// @return
EXPORT_DLL UnloadFont(
    Context *ctx,
    FontHandle *fontHandle
)
{
    return ctx->UnloadFont(fontHandle);
};

/// @brief Shapes a text sample into glyph arrangement
/// @param ctx Context
/// @param fontHandle Font index
/// @param allocator Allocator
/// @param inText Text sample to shape
/// @param outGlyphs Reference to the glyph buffer
/// @return
EXPORT_DLL ShapeText(
    Context *ctx,
    FontHandle *fontHandle,
    Allocator allocator,
    Buffer<char> *inText,
    Buffer<Text::Glyph> *outGlyphs
)
{
    return ctx->ShapeText(fontHandle, allocator, inText, outGlyphs);
};

/// @brief Draws an atlas of glyphs into a texture
/// @param ctx Context
/// @param fontHandle Font index
/// @param textureSize Texture size
/// @param glyphSize Glyph size in pixels
/// @param padding Padding between glyphs
/// @param margin Margin between glyphs
/// @param distanceMappingRange Distance mapping range
/// @param glyphRenderFlags Glyph render flags
/// @param compactFlags Compact flags
/// @param allocator Allocator for the output glyph rects
/// @param inText Text to shape
/// @param refTexture Output texture pointer
/// @param outGlyphPixelMetrics Output glyph rects
/// @return
EXPORT_DLL DrawAtlas(
    Context *ctx,
    FontHandle *fontHandle,
    int textureSize,
    int glyphSize,
    int padding,
    int margin,
    float distanceMappingRange,
    int glyphRenderFlags,
    int compactFlags,
    Allocator allocator,
    Buffer<char> *inText,
    Buffer<RGBA32Pixel> *refTexture,
    Buffer<GlyphMetrics> *outGlyphPixelMetrics
)
{
    return ctx->DrawAtlas(
        fontHandle,
        textureSize,
        glyphSize,
        padding,
        margin,
        distanceMappingRange,
        glyphRenderFlags,
        compactFlags,
        allocator,
        inText,
        refTexture,
        outGlyphPixelMetrics);
}

#endif