#ifndef API_H
#define API_H

#include <set>
#include "types.h"
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

EXPORT_DLL CreateContext(LogCallback logCallback, AllocCallback allocCallback, void **outCtx)
{
    *outCtx = new Text::Context(logCallback, allocCallback);
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
    void *ctx,                       // Context
    int fontIndex,                   // Font index
    Allocator allocator,             // Allocator
    Buffer<char> inText,             // Text sample to shape
    OutBuffer<Text::Glyph> outGlyphs // Reference to the glyph buffer
)
{
    auto context = (Text::Context *)ctx;
    return context->ShapeText(fontIndex, allocator, inText, outGlyphs);
};

struct FtContext
{
    double scale;
    float2 position;
    msdfgen::Shape *shape;
    msdfgen::Contour *contour;
};

msdfgen::EdgeHolder edgeHolder(float2 p0, float2 p1)
{
    return msdfgen::EdgeHolder(msdfgen::Point2(p0.x, p0.y), msdfgen::Point2(p1.x, p1.y));
}

msdfgen::EdgeHolder edgeHolder(float2 p0, float2 p1, float2 p2)
{
    return msdfgen::EdgeHolder(msdfgen::Point2(p0.x, p0.y), msdfgen::Point2(p1.x, p1.y), msdfgen::Point2(p2.x, p2.y));
}

msdfgen::EdgeHolder edgeHolder(float2 p0, float2 p1, float2 p2, float2 p3)
{
    return msdfgen::EdgeHolder(msdfgen::Point2(p0.x, p0.y), msdfgen::Point2(p1.x, p1.y), msdfgen::Point2(p2.x, p2.y), msdfgen::Point2(p3.x, p3.y));
}

static float2 ftPoint2(const FT_Vector &vector, double scale)
{
    return float2(scale * vector.x, scale * vector.y);
}

static int ftMoveTo(const FT_Vector *to, void *user)
{
    FtContext *context = reinterpret_cast<FtContext *>(user);
    if (!(context->contour && context->contour->edges.empty()))
        context->contour = &context->shape->addContour();
    context->position = ftPoint2(*to, context->scale);
    return 0;
}

static int ftLineTo(const FT_Vector *to, void *user)
{
    FtContext *context = reinterpret_cast<FtContext *>(user);
    float2 endpoint = ftPoint2(*to, context->scale);
    if (endpoint != context->position)
    {
        context->contour->addEdge(edgeHolder(context->position, endpoint));
        context->position = endpoint;
    }
    return 0;
}

static int ftConicTo(const FT_Vector *control, const FT_Vector *to, void *user)
{
    FtContext *context = reinterpret_cast<FtContext *>(user);
    float2 endpoint = ftPoint2(*to, context->scale);
    if (endpoint != context->position)
    {
        context->contour->addEdge(edgeHolder(context->position, ftPoint2(*control, context->scale), endpoint));
        context->position = endpoint;
    }
    return 0;
}

static int ftCubicTo(const FT_Vector *control1, const FT_Vector *control2, const FT_Vector *to, void *user)
{
    FtContext *context = reinterpret_cast<FtContext *>(user);
    float2 endpoint = ftPoint2(*to, context->scale);
    if (endpoint != context->position || math::cross(ftPoint2(*control1, context->scale) - endpoint, ftPoint2(*control2, context->scale) - endpoint))
    {
        context->contour->addEdge(edgeHolder(context->position, ftPoint2(*control1, context->scale), ftPoint2(*control2, context->scale), endpoint));
        context->position = endpoint;
    }
    return 0;
}

inline byte pixelFloatToByte(float x)
{
    return byte(~int(255.5f - 255.f * clamp(x)));
}

EXPORT_DLL DrawAtlas(
    void *ctx,                     // Context
    int fontIndex,                 // Font index
    const char *text,              // Text sample to shape
    int textLen,                   // Text sample length
    int textureSize,               // Texture size
    int glyphSize,                 // Glyph size in pixels
    int padding,                   // Padding between glyphs
    Text::RGBA32Pixel *outTexture, // Output texture pointer
    Text::GlyphRect *outGlyphRects // Output glyph rects
)
{
    auto context = (Text::Context *)ctx;

    return Text::ErrorCode::Success;

    // // Obtain metrics for all glyphs
    // FT_Face face = context->faces[fontIndex]->ftFace;
    // FT_Set_Pixel_Sizes(face, 0, glyphSize);
    // for (auto &glyph : glyphs)
    // {
    //     FT_Load_Glyph(face, glyph.index, FT_LOAD_DEFAULT);
    //     auto metrics = face->glyph->metrics;
    //     glyph.w = metrics.width >> 6;
    //     glyph.h = metrics.height >> 6;
    //     context->Log() << "Glyph " << glyph.index << " metrics: " << glyph.w << "x" << glyph.h;
    // }

    // // Prepare the atlas
    // auto atlas = Text::AtlasBuilder(textureSize);
    // auto atlasResult = atlas.Add(glyphs);

    // // Draw the atlas
    // for (int i = 0; i < glyphs.size(); i++)
    // {
    //     auto &glyph = glyphs[i];

    //     // Write the glyph rect to the output map
    //     outGlyphRects[i] = glyph;

    //     // Draw the glyph
    //     msdfgen::Shape shape;
    //     FT_Load_Glyph(face, glyph.index, FT_LOAD_NO_SCALE);
    //     double scale = 1.0 / (face->units_per_EM ? face->units_per_EM : 1);
    //     auto outline = &face->glyph->outline;
    //     shape.contours.clear();
    //     shape.inverseYAxis = false;
    //     FtContext ftc = {};
    //     ftc.scale = scale;
    //     ftc.shape = &shape;
    //     FT_Outline_Funcs ftFunctions;
    //     ftFunctions.move_to = &ftMoveTo;
    //     ftFunctions.line_to = &ftLineTo;
    //     ftFunctions.conic_to = &ftConicTo;
    //     ftFunctions.cubic_to = &ftCubicTo;
    //     ftFunctions.shift = 0;
    //     ftFunctions.delta = 0;
    //     FT_Error error = FT_Outline_Decompose(outline, &ftFunctions, &ftc);
    //     if (!shape.contours.empty() && shape.contours.back().edges.empty())
    //         shape.contours.pop_back();
    //     shape.normalize();
    //     edgeColoringSimple(shape, 3.0);
    //     context->Log() << "Drawing glyph " << glyph.index << " at " << glyph.x << ", " << glyph.y;
    //     msdfgen::Bitmap<float, 3> outBitmap(glyphSize, glyphSize);
    //     msdfgen::SDFTransformation t(msdfgen::Projection(glyphSize, msdfgen::Vector2(0.125, 0.125)), msdfgen::Range(0.125));
    //     msdfgen::generateMSDF(outBitmap, shape, t);
    //     for (int dx = 0; dx < glyph.w; ++dx)
    //     {
    //         for (int dy = 0; dy < glyph.y; ++dy)
    //         {
    //             int destX = glyph.x + dx;
    //             int destY = glyph.y + dy;
    //             int srcX = (glyphSize - glyph.w) / 2 + dx;
    //             int srcY = (glyphSize - glyph.h) / 2 + dy;
    //             if (destX >= 0 && destX < textureSize && destY >= 0 && destY < textureSize)
    //             {
    //                 auto dest = outTexture + destY * textureSize + destX;
    //                 auto src = outBitmap(srcX, srcY);
    //                 dest->r = pixelFloatToByte(src[0]);
    //                 dest->g = pixelFloatToByte(src[1]);
    //                 dest->b = pixelFloatToByte(src[2]);
    //                 dest->a = 255;
    //             }
    //         }
    //     }
    // }

    // // Clean up
    // free(shapingResult);
    // return Text::ErrorCode::Success;
}

#endif