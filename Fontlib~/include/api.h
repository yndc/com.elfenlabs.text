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

#define EXPORT_DLL extern "C" __declspec(dllexport)

using namespace math;

/// @brief Creates a new library context, all library functions require a context
/// @param logCallback Log callback
/// @param allocCallback Buffer allocation callback
/// @param disposeCallback Buffer disposal callback
/// @param outCtx Out context
/// @return
EXPORT_DLL ReturnCode CreateContext(
    LogCallback logCallback,
    AllocCallback allocCallback,
    DisposeCallback disposeCallback,
    Context **outCtx)
{
    *outCtx = new Context(logCallback, allocCallback, disposeCallback);
    return ReturnCode::Success;
}

/// @brief Destroys a library context
/// @param ctx
/// @return
EXPORT_DLL ReturnCode DestroyContext(Context *ctx)
{
    delete ctx;
    return ReturnCode::Success;
}

/// @brief Loads a font from a byte buffer
/// @param ctx Context
/// @param inFontData Font data
/// @param outFontDescription Out font description
/// @return
EXPORT_DLL ReturnCode LoadFont(
    Context *ctx,
    Buffer<byte> inFontData,
    FontDescription *outFontDescription)
{
    return ctx->LoadFont(inFontData, outFontDescription);
};

/// @brief Unloads a font
/// @param ctx Context
/// @param font_handle Font index
/// @return
EXPORT_DLL ReturnCode UnloadFont(
    Context *ctx,
    FontHandle *font_handle)
{
    return ctx->UnloadFont(font_handle);
};

/// @brief Shapes a text sample into glyph arrangement
/// @param ctx Context
/// @param font_handle Font index
/// @param allocator Allocator
/// @param inText Text sample to shape
/// @param outGlyphs Reference to the glyph buffer
/// @return
EXPORT_DLL ReturnCode ShapeText(
    Context *ctx,
    FontHandle *font_handle,
    Allocator allocator,
    Buffer<char> *inText,
    Buffer<GlyphShape> *outGlyphs)
{
    return ctx->ShapeText(font_handle, allocator, inText, outGlyphs);
};

/// @brief Fills the glyph metrics buffer with the metrics of the glyphs in the font
/// @param ctx 
/// @param font_handle 
/// @param glyph_size 
/// @param padding 
/// @param ref_glyphs 
/// @return 
EXPORT_DLL ReturnCode GetGlyphMetrics(
    Context *ctx,
    FontHandle *font_handle,
    int glyph_size,
    int padding,
    Buffer<GlyphMetrics> *ref_glyphs)
{
    GetGlyphMetrics(*ref_glyphs, font_handle, glyph_size, padding);
    return ReturnCode::Success;
}

/// @brief Renders glyphs into the atlas texture using the specified font and rendering configuration
/// @param ctx 
/// @param font_handle 
/// @param atlas_config 
/// @param render_config 
/// @param in_glyphs 
/// @param ref_texture 
/// @return 
EXPORT_DLL ReturnCode RenderGlyphsToAtlas(
    Context *ctx,
    FontHandle *font_handle,
    AtlasConfig atlas_config,
    RenderConfig render_config,
    Buffer<GlyphMetrics> *in_glyphs,
    Buffer<RGBA32Pixel> *ref_texture)
{
    for (int i = 0; i < in_glyphs->Count(); i++)
    {
        RenderGlyph(font_handle, (*in_glyphs)[i], atlas_config, render_config, ref_texture);
    }

    return ReturnCode::Success;
}

#endif