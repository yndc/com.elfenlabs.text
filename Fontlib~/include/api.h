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
/// @brief Creates a new dynamic atlas packer instance.
/// @param ctx Plugin context (optional, unused in snippet).
/// @param config Configuration for the atlas (size, margin).
/// @param out_atlas_handle Output parameter: Pointer to store the handle (pointer) of the created packer.
/// @return ReturnCode::Success on success, error code otherwise.
/// @note The caller is responsible for destroying the packer using AtlasDestroy when no longer needed.
EXPORT_DLL ReturnCode AtlasCreate(
    Context *ctx,
    AtlasConfig config,
    DynamicAtlasPacker **out_atlas_handle)
{
    *out_atlas_handle = new DynamicAtlasPacker(config);

    return ReturnCode::Success;
}

/// @brief Serializes the state of the atlas packer into a byte buffer.
/// The buffer is allocated by the plugin using the provided allocator via the context.
/// @param ctx Plugin context (used for allocation).
/// @param atlas_handle Handle (pointer) to the atlas packer instance to serialize.
/// @param allocator Allocator type to use for the output buffer.
/// @param out_buffer Output parameter: Pointer to a Buffer struct that will be filled
/// with the pointer and size of the newly allocated buffer containing serialized data.
/// @return ReturnCode::Success on success, error code otherwise.
/// @note The caller is responsible for freeing the memory allocated for out_buffer
/// using the appropriate mechanism corresponding to the allocator used via the context.
EXPORT_DLL ReturnCode AtlasSerialize(
    Context *ctx,
    DynamicAtlasPacker *atlas_handle,
    Allocator allocator,
    Buffer<byte> *out_buffer) // Use byte or std::byte
{
    *out_buffer = ctx->Alloc<byte>(atlas_handle->GetSerializedSize(), allocator);
    atlas_handle->Serialize(out_buffer);
    return ReturnCode::Success;
}

/// @brief Creates a new atlas packer by deserializing state from a byte buffer.
/// @param ctx Plugin context (optional, unused in snippet).
/// @param in_buffer Pointer to a Buffer struct containing the serialized data.
/// @param out_atlas_handle Output parameter: Pointer to store the handle (pointer) of the created packer.
/// @return ReturnCode::Success on success, error code otherwise.
/// @note The caller is responsible for destroying the packer using AtlasDestroy when no longer needed.
EXPORT_DLL ReturnCode AtlasDeserialize(
    Context *ctx,
    const Buffer<byte> *in_buffer, // Input buffer should be const
    DynamicAtlasPacker **out_atlas_handle)
{
    *out_atlas_handle = DynamicAtlasPacker::Deserialize(in_buffer);
    auto config = (*out_atlas_handle)->GetConfig();
    auto node_count = (*out_atlas_handle)->GetNodeCount();
    ctx->Log() << "Atlas node count: " << node_count << "\n";
    ctx->Log() << "Atlas size: " << config.size << "\n";
    ctx->Log() << "Atlas padding: " << config.padding << "\n";
    ctx->Log() << "Atlas margin: " << config.margin << "\n";
    ctx->Log() << "Atlas glyph size: " << config.glyph_size << "\n";
    ctx->Log() << "Atlas flags: " << config.flags << "\n";
    return ReturnCode::Success;
}

/// @brief Destroys an atlas packer instance previously created by AtlasCreate or AtlasDeserialize.
/// @param ctx Plugin context (optional, unused in snippet).
/// @param atlas_handle Handle (pointer) to the atlas packer instance to destroy.
/// @return ReturnCode::Success (usually).
EXPORT_DLL ReturnCode AtlasDestroy(
    Context *ctx,
    DynamicAtlasPacker *atlas_handle)
{
    delete atlas_handle;
    return ReturnCode::Success;
}

/// @brief Gets metrics, packs glyphs, and renders them into the atlas texture.
/// @param ctx Plugin context (optional, unused in snippet).
/// @param font_handle Handle to the loaded font resource needed for metrics and rendering.
/// @param atlas_handle Handle to the atlas packer instance managing the layout.
/// @param render_config Configuration specific to how glyphs should be rendered (e.g., MSDF parameters).
/// @param ref_glyphs Input/Output buffer. Input should have glyph indices. Output will have
/// dimensions filled by GetGlyphMetrics and positions (atlas_x_px, atlas_y_px) filled by the packer.
/// @param ref_texture Input buffer wrapping the raw pixel data of the target atlas texture slice.
/// RenderGlyph will write into this buffer.
/// @param out_packed_count Output parameter: The number of glyphs successfully packed in this call.
/// @return ReturnCode::Success on success, error code otherwise.
/// @note The caller MUST ensure thread safety if calling this from anywhere other than the main thread,
/// especially concerning access to ref_texture data.
/// @note The caller MUST synchronize the texture with the GPU after this call returns
/// (e.g., by calling Texture.Apply() in Unity) if ref_texture was modified.
EXPORT_DLL ReturnCode AtlasPackGlyphs(
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
    GetGlyphMetrics(*ref_glyphs, font_handle, atlas_config.glyph_size, atlas_config.padding);

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