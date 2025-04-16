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

#define EXPORT_DLL extern "C" __declspec(dllexport) ReturnCode

using namespace math;

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
    Context **outCtx)
{
    *outCtx = new Context(logCallback, allocCallback, disposeCallback);
    return ReturnCode::Success;
}

/// @brief Destroys a library context
/// @param ctx
/// @return
EXPORT_DLL DestroyContext(Context *ctx)
{
    delete ctx;
    return ReturnCode::Success;
}

/// @brief Loads a font from a byte buffer
/// @param ctx Context
/// @param inFontData Font data
/// @param outFontDescription Out font description
/// @return
EXPORT_DLL LoadFont(
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
EXPORT_DLL UnloadFont(
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
EXPORT_DLL ShapeText(
    Context *ctx,
    FontHandle *font_handle,
    Allocator allocator,
    Buffer<char> *inText,
    Buffer<GlyphShape> *outGlyphs)
{
    return ctx->ShapeText(font_handle, allocator, inText, outGlyphs);
};

/// @brief Create an atlas packer
/// @param ctx
/// @param config
/// @param out_atlas_handle
/// @return
EXPORT_DLL AtlasCreate(
    Context *ctx,
    AtlasConfig config,
    DynamicAtlasPacker **out_atlas_handle)
{
    *out_atlas_handle = new DynamicAtlasPacker(config);

    return ReturnCode::Success;
}

EXPORT_DLL AtlasSerialize(
    Context *ctx,
    DynamicAtlasPacker *atlas_handle,
    Allocator allocator,
    Buffer<byte> *out_buffer)
{
    *out_buffer = ctx->Alloc<byte>(atlas_handle->GetSerializedSize(), allocator);
    atlas_handle->Serialize(out_buffer);
    return ReturnCode::Success;
}

EXPORT_DLL AtlasDeserialize(
    Context *ctx,
    Buffer<byte> *in_buffer,
    DynamicAtlasPacker **out_atlas_handle)
{
    *out_atlas_handle = DynamicAtlasPacker::Deserialize(in_buffer);
    return ReturnCode::Success;
}

/// @brief Destroys an atlas packer
/// @param ctx
/// @param atlas_handle
/// @return
EXPORT_DLL AtlasDestroy(
    Context *ctx,
    DynamicAtlasPacker *atlas_handle)
{
    delete atlas_handle;
    return ReturnCode::Success;
}

/// @brief Packs glyphs into the atlas
/// @param ctx
/// @param font_handle
/// @param atlas_handle
/// @param render_config
/// @param ref_glyphs
/// @param ref_texture
/// @param out_packed_count
/// @return
EXPORT_DLL AtlasPackGlyphs(
    Context *ctx,
    FontHandle *font_handle,
    DynamicAtlasPacker *atlas_handle,
    RenderConfig render_config,
    Buffer<GlyphMetrics> *ref_glyphs,
    Buffer<RGBA32Pixel> *ref_texture,
    int *out_packed_count)
{
    // Get glyph metrics
    auto atlas_config = atlas_handle->GetConfig();
    GetGlyphMetrics(*ref_glyphs, font_handle, atlas_config.size, atlas_config.margin);

    // Pack the glyphs into the atlas
    int packed_count = atlas_handle->PackGlyphs(*ref_glyphs);
    *out_packed_count = packed_count;

    // Render the glyph to the atlas texture
    for (int i = 0; i < ref_glyphs->Count(); i++)
    {
        RenderGlyph(font_handle, (*ref_glyphs)[i], atlas_handle->GetConfig(), render_config, ref_texture);
    }

    return ReturnCode::Success;
}

#endif