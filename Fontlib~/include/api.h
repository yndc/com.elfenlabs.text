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

#define EXPORT_DLL extern "C" __declspec(dllexport) Text::ErrorCode

using namespace math;
using namespace Text;

/// @brief Creates a new library context
/// @param logCallback
/// @param allocCallback
/// @param disposeCallback
/// @param outCtx
/// @return
EXPORT_DLL CreateContext(
    LogCallback logCallback,         // Log callback
    AllocCallback allocCallback,     // Buffer allocation callback
    DisposeCallback disposeCallback, // Buffer disposal callback
    Context **outCtx                 // Out context
)
{
    *outCtx = new Text::Context(logCallback, allocCallback, disposeCallback);
    return Text::ErrorCode::Success;
}

EXPORT_DLL DestroyContext(Context *ctx)
{
    delete ctx;
    return Text::ErrorCode::Success;
}

EXPORT_DLL LoadFont(
    Context *ctx,                       // Context
    Buffer<byte> inFontData,            // Font data
    FontDescription *outFontDescription // Out font description
)
{
    return ctx->LoadFont(inFontData, outFontDescription);
};

EXPORT_DLL UnloadFont(
    Context *ctx,          // Context
    FontHandle *fontHandle // Font index
)
{
    return ctx->UnloadFont(fontHandle);
};

EXPORT_DLL ShapeText(
    Context *ctx,                  // Context
    FontHandle *fontHandle,        // Font index
    Allocator allocator,           // Allocator
    Buffer<char> *inText,          // Text sample to shape
    Buffer<Text::Glyph> *outGlyphs // Reference to the glyph buffer
)
{
    return ctx->ShapeText(fontHandle, allocator, inText, outGlyphs);
};

EXPORT_DLL DrawAtlas(
    Context *ctx,                    // Context
    FontHandle *fontHandle,          // Font index
    int textureSize,                 // Texture size
    int glyphSize,                   // Glyph size in pixels
    int padding,                     // Padding between glyphs
    float distanceMappingRange,      // Distance mapping range
    int glyphRenderFlags,            // Glyph render flags
    int compactFlags,                // Compact flags
    Allocator allocator,             // Allocator for the output glyph rects
    Buffer<char> *inText,            // Text to shape
    Buffer<RGBA32Pixel> *refTexture, // Output texture pointer
    Buffer<GlyphRect> *outGlyphRects // Output glyph rects
)
{
    return ctx->DrawAtlas(
        fontHandle,
        textureSize,
        glyphSize,
        padding,
        distanceMappingRange,
        glyphRenderFlags,
        compactFlags,
        allocator,
        inText,
        refTexture,
        outGlyphRects);
}

#endif