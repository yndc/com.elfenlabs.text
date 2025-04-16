#ifndef RENDER_H
#define RENDER_H

#include <buffer.h>
#include <shape.h>
#include <ft2build.h>
#include FT_FREETYPE_H
#include FT_OUTLINE_H
#include <msdfgen.h>
#include <mathematics.h>
#include <math.h>
#include <glyph.h>

using namespace math;

enum GlyphRenderFlag
{
    ResolveIntersections = 1 << 0
};

struct RenderConfig
{
    float distance_mapping_range = 0.5f; // Distance mapping range for MSDF generation
    int flags = 0;                       // Render flags (e.g., resolve intersections)
};

struct RGBA32Pixel
{
    uint8_t r, g, b, a;
};

inline byte pixelFloatToByte(float x)
{
    return byte(~int(255.5f - 255.f * clamp(x)));
}

void RenderGlyph(FontHandle *fontHandle, GlyphMetrics glyph, AtlasConfig atlas_config, RenderConfig render_config, Buffer<RGBA32Pixel> *refTexture)
{
    msdfgen::Shape shape;
    if (Flag::has(render_config.flags, GlyphRenderFlag::ResolveIntersections))
    {
        shape = GetResolvedShape(fontHandle->ft, glyph.index);
    }
    else
    {
        shape = GetShape(fontHandle->ft, glyph.index);
    }

    edgeColoringSimple(shape, 3.0);

    float scale = static_cast<float>(atlas_config.glyph_size);

    // Get glyph bounds from Shape (normalized 0-1 units)
    msdfgen::Shape::Bounds bounds = shape.getBounds();
    float translate_x = (atlas_config.padding - bounds.l * scale) / scale;
    float translate_y = (atlas_config.padding - bounds.b * scale) / scale;
    msdfgen::Vector2 translate(translate_x, translate_y);
    msdfgen::Bitmap<float, 4> tempBitmap(glyph.atlas_width_px, glyph.atlas_height_px);
    msdfgen::Projection projection = msdfgen::Projection(scale, translate);
    msdfgen::SDFTransformation transform(projection, msdfgen::Range(render_config.distance_mapping_range));
    msdfgen::MSDFGeneratorConfig config(true);
    msdfgen::generateMTSDF(tempBitmap, shape, transform, config);

    // Copy from the temp bitmap to the atlas texture
    for (int dx = 0; dx < glyph.atlas_width_px; ++dx)
    {
        for (int dy = 0; dy < glyph.atlas_height_px; ++dy)
        {
            int dest_x = glyph.atlas_x_px + dx;
            int dest_y = glyph.atlas_y_px + dy;
            int src_x = dx;
            int src_y = dy;
            if (dest_x >= 0 && dest_x < atlas_config.size && dest_y >= 0 && dest_y < atlas_config.size)
            {
                auto dest = refTexture->Data() + dest_y * atlas_config.size + dest_x;
                auto src = tempBitmap(src_x, src_y);
                dest->r = pixelFloatToByte(src[0]);
                dest->g = pixelFloatToByte(src[1]);
                dest->b = pixelFloatToByte(src[2]);
                dest->a = pixelFloatToByte(src[3]);
            }
        }
    }
}

#endif