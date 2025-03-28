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

    // struct FtContext
    // {
    //     double scale;
    //     float2 position;
    //     msdfgen::Shape *shape;
    //     msdfgen::Contour *contour;
    // };

    // msdfgen::EdgeHolder edgeHolder(float2 p0, float2 p1)
    // {
    //     return msdfgen::EdgeHolder(msdfgen::Point2(p0.x, p0.y), msdfgen::Point2(p1.x, p1.y));
    // }

    // msdfgen::EdgeHolder edgeHolder(float2 p0, float2 p1, float2 p2)
    // {
    //     return msdfgen::EdgeHolder(msdfgen::Point2(p0.x, p0.y), msdfgen::Point2(p1.x, p1.y), msdfgen::Point2(p2.x, p2.y));
    // }

    // msdfgen::EdgeHolder edgeHolder(float2 p0, float2 p1, float2 p2, float2 p3)
    // {
    //     return msdfgen::EdgeHolder(msdfgen::Point2(p0.x, p0.y), msdfgen::Point2(p1.x, p1.y), msdfgen::Point2(p2.x, p2.y), msdfgen::Point2(p3.x, p3.y));
    // }

    // static float2 ftPoint2(const FT_Vector &vector, double scale)
    // {
    //     return float2(scale * vector.x, scale * vector.y);
    // }

    // static int ftMoveTo(const FT_Vector *to, void *user)
    // {
    //     FtContext *context = reinterpret_cast<FtContext *>(user);
    //     if (!(context->contour && context->contour->edges.empty()))
    //         context->contour = &context->shape->addContour();
    //     context->position = ftPoint2(*to, context->scale);
    //     return 0;
    // }

    // static int ftLineTo(const FT_Vector *to, void *user)
    // {
    //     FtContext *context = reinterpret_cast<FtContext *>(user);
    //     float2 endpoint = ftPoint2(*to, context->scale);
    //     if (endpoint != context->position)
    //     {
    //         context->contour->addEdge(edgeHolder(context->position, endpoint));
    //         context->position = endpoint;
    //     }
    //     return 0;
    // }

    // static int ftConicTo(const FT_Vector *control, const FT_Vector *to, void *user)
    // {
    //     FtContext *context = reinterpret_cast<FtContext *>(user);
    //     float2 endpoint = ftPoint2(*to, context->scale);
    //     if (endpoint != context->position)
    //     {
    //         context->contour->addEdge(edgeHolder(context->position, ftPoint2(*control, context->scale), endpoint));
    //         context->position = endpoint;
    //     }
    //     return 0;
    // }

    // static int ftCubicTo(const FT_Vector *control1, const FT_Vector *control2, const FT_Vector *to, void *user)
    // {
    //     FtContext *context = reinterpret_cast<FtContext *>(user);
    //     float2 endpoint = ftPoint2(*to, context->scale);
    //     if (endpoint != context->position || math::cross(ftPoint2(*control1, context->scale) - endpoint, ftPoint2(*control2, context->scale) - endpoint))
    //     {
    //         context->contour->addEdge(edgeHolder(context->position, ftPoint2(*control1, context->scale), ftPoint2(*control2, context->scale), endpoint));
    //         context->position = endpoint;
    //     }
    //     return 0;
    // }

    inline byte pixelFloatToByte(float x)
    {
        return byte(~int(255.5f - 255.f * clamp(x)));
    }

    void DrawGlyph(FT_Face face, GlyphRect glyphRect, int textureSize, int glyphSize, int padding, Buffer<RGBA32Pixel> *refTexture)
    {
        // msdfgen::Shape shape;
        // FT_Load_Glyph(face, glyphRect.index, FT_LOAD_NO_SCALE);
        // double scale = 1.0 / (face->units_per_EM ? face->units_per_EM : 1);
        // auto outline = &face->glyph->outline;
        // shape.contours.clear();
        // shape.inverseYAxis = false;
        // FtContext ftc = {};
        // ftc.scale = scale;
        // ftc.shape = &shape;
        // FT_Outline_Funcs ftFunctions;
        // ftFunctions.move_to = &ftMoveTo;
        // ftFunctions.line_to = &ftLineTo;
        // ftFunctions.conic_to = &ftConicTo;
        // ftFunctions.cubic_to = &ftCubicTo;
        // ftFunctions.shift = 0;
        // ftFunctions.delta = 0;
        // FT_Error error = FT_Outline_Decompose(outline, &ftFunctions, &ftc);
        // if (!shape.contours.empty() && shape.contours.back().edges.empty())
        //     shape.contours.pop_back();
        // shape.normalize();
        // edgeColoringSimple(shape, 3.0);
        // msdfgen::Bitmap<float, 3> outBitmap(glyphSize, glyphSize);
        // msdfgen::SDFTransformation t(msdfgen::Projection(glyphSize, msdfgen::Vector2(0.125, 0.125)), msdfgen::Range(0.125));
        // msdfgen::generateMSDF(outBitmap, shape, t);

        auto fontHandle = msdfgen::adoptFreetypeFont(face);
        msdfgen::Shape shape;
        msdfgen::loadGlyph(
            shape,
            fontHandle,
            msdfgen::GlyphIndex(glyphRect.index),
            msdfgen::FontCoordinateScaling::FONT_SCALING_EM_NORMALIZED);
        shape.normalize();
        edgeColoringSimple(shape, 3.0);

        // Center the glyph in the 32x32 temporary bitmap
        msdfgen::Shape::Bounds bounds = shape.getBounds();
        double maxDim = std::max(bounds.r - bounds.l, bounds.t - bounds.b); // Largest dimension
        double scale = glyphSize;
        msdfgen::Vector2 translate(padding / scale / 2);

        msdfgen::Bitmap<float, 3> outBitmap(glyphSize + padding, glyphSize + padding);
        msdfgen::Projection projection = msdfgen::Projection(scale, translate);
        msdfgen::SDFTransformation transform(projection, msdfgen::Range(0.25));
        msdfgen::MSDFGeneratorConfig config(true);
        msdfgen::generateMSDF(outBitmap, shape, transform, config);

        int offsetX = (glyphSize - glyphRect.w) / 2;
        int offsetY = (glyphSize - glyphRect.h) / 2;
        for (int dx = 0; dx < glyphRect.w; ++dx)
        {
            for (int dy = 0; dy < glyphRect.h; ++dy)
            {
                int destX = glyphRect.x + dx;
                int destY = glyphRect.y + dy;
                int srcX = dx; // + offsetX;
                int srcY = dy; // + offsetY;
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