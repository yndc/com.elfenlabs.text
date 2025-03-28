#ifndef TEXTURE_H
#define TEXTURE_H

#include <buffer.h>
#include <ft2build.h>
#include FT_FREETYPE_H
#include FT_OUTLINE_H
#include <msdfgen.h>
#include <msdfgen-ext.h>
#include <mathematics.h>
#include <glyph.h>

using namespace math;

namespace Text
{
    struct RGBA32Pixel
    {
        uint8_t r, g, b, a;
    };

    inline byte pixelFloatToByte(float x)
    {
        return byte(~int(255.5f - 255.f * clamp(x)));
    }

    void DrawGlyph(FT_Face face, GlyphRect glyphRect, int textureSize, int glyphSize, int padding, Buffer<RGBA32Pixel> *refTexture)
    {
        auto fontHandle = msdfgen::adoptFreetypeFont(face);
        msdfgen::Shape shape;
        msdfgen::loadGlyph(
            shape,
            fontHandle,
            msdfgen::GlyphIndex(glyphRect.index),
            msdfgen::FontCoordinateScaling::FONT_SCALING_EM_NORMALIZED);
        shape.normalize();
        edgeColoringSimple(shape, 3.0);

        float scale = glyphSize;
        msdfgen::Vector2 translate(padding / scale);
        msdfgen::Bitmap<float, 3> outBitmap(glyphSize + padding * 2, glyphSize + padding * 2);
        msdfgen::Projection projection = msdfgen::Projection(scale, translate);
        msdfgen::SDFTransformation transform(projection, msdfgen::Range(0.25));
        msdfgen::MSDFGeneratorConfig config(true);
        msdfgen::generateMSDF(outBitmap, shape, transform, config);
        for (int dx = 0; dx < glyphRect.w; ++dx)
        {
            for (int dy = 0; dy < glyphRect.h; ++dy)
            {
                int destX = glyphRect.x + dx;
                int destY = glyphRect.y + dy;
                int srcX = dx;
                int srcY = dy;
                if (destX >= 0 && destX < textureSize && destY >= 0 && destY < textureSize)
                {
                    auto dest = refTexture->Data() + destY * textureSize + destX;
                    auto src = outBitmap(srcX, srcY);
                    dest->r = pixelFloatToByte(src[0]);
                    dest->g = pixelFloatToByte(src[1]);
                    dest->b = pixelFloatToByte(src[2]);
                    dest->a = 255;
                }
            }
        }
    }
}

#endif