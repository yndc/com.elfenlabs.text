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
    void **outCtx                    // Out context
)
{
    *outCtx = new Text::Context(logCallback, allocCallback, disposeCallback);
    return Text::ErrorCode::Success;
}

EXPORT_DLL DestroyContext(void *ctx)
{
    delete (Text::Context *)ctx;
    return Text::ErrorCode::Success;
}

EXPORT_DLL LoadFont(
    void *ctx,               // Context
    Buffer<byte> inFontData, // Font data
    int *outFontIndex        // Out font index
)
{
    auto context = (Text::Context *)ctx;
    return context->LoadFont(inFontData, outFontIndex);
};

EXPORT_DLL UnloadFont(
    void *ctx,    // Context
    int fontIndex // Font index
)
{
    auto context = (Text::Context *)ctx;
    return context->UnloadFont(fontIndex);
};

EXPORT_DLL ShapeText(
    void *ctx,                     // Context
    int fontIndex,                 // Font index
    Allocator allocator,           // Allocator
    Buffer<char> *inText,          // Text sample to shape
    Buffer<Text::Glyph> *outGlyphs // Reference to the glyph buffer
)
{
    auto context = (Text::Context *)ctx;
    return context->ShapeText(fontIndex, allocator, inText, outGlyphs);
};

EXPORT_DLL DrawAtlas(
    void *ctx,                       // Context
    int fontIndex,                   // Font index
    int textureSize,                 // Texture size
    int glyphSize,                   // Glyph size in pixels
    int padding,                     // Padding between glyphs
    int compactFlags,                // Compact flags
    Allocator allocator,             // Allocator for the output glyph rects
    Buffer<char> *inText,            // Text to shape
    Buffer<RGBA32Pixel> *refTexture, // Output texture pointer
    Buffer<GlyphRect> *outGlyphRects // Output glyph rects
)
{
    auto context = (Text::Context *)ctx;
    return context->DrawAtlas(
        fontIndex,
        textureSize,
        glyphSize,
        padding,
        compactFlags,
        allocator,
        inText,
        refTexture,
        outGlyphRects);
}

#endif