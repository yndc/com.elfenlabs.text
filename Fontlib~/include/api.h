#ifndef API_H
#define API_H

#include <set>
#include "error.h"
#include "shape.h"
#include "context.h"
#include "atlas.h"
#include "msdfgen.h"
#include <ft2build.h>
#include FT_FREETYPE_H
#include FT_OUTLINE_H
#include "mathx.h"

using namespace math;

extern "C" __declspec(dllexport) Text::ErrorCode CreateContext(void **ctx)
{
    *ctx = new Text::Context();
    return Text::ErrorCode::Success;
}

extern "C" __declspec(dllexport) Text::ErrorCode DestroyContext(void *ctx)
{
    delete (Text::Context *)ctx;
    return Text::ErrorCode::Success;
}

extern "C" __declspec(dllexport) Text::ErrorCode LoadFont(
    void *ctx,                     // Context
    void *outFontIndex,            // Out font index
    const unsigned char *fontData, // Font file data (e.g., TTF)
    size_t fontDataSize            // Size of font data
)
{
    auto context = (Text::Context *)ctx;
    auto result = context->LoadFont(fontData, fontDataSize);
    if (result.IsError())
        return Text::ErrorCode::Failure;
    *(int *)outFontIndex = result.GetValue();
    return Text::ErrorCode::Success;
};

extern "C" __declspec(dllexport) Text::ErrorCode UnloadFont(
    void *ctx,    // Context
    int fontIndex // Font index
)
{
    auto context = (Text::Context *)ctx;
    auto result = context->UnloadFont(fontIndex);
    if (result.IsError())
        return Text::ErrorCode::Failure;
    return Text::ErrorCode::Success;
};

extern "C" __declspec(dllexport) Text::ErrorCode ShapeText(
    void *ctx,              // Context
    int fontIndex,          // Font index
    const char *text,       // Text to shape
    int textLen,            // Text length
    int maxGlyphs,          // Maximum number of glyphs
    Text::Glyph *refGlyphs, // Output glyphs
    int *outGlyphCount      // Output glyph count
)
{
    auto context = (Text::Context *)ctx;
    auto result = context->ShapeText(fontIndex, text, textLen, maxGlyphs, refGlyphs, outGlyphCount);
    if (result.IsError())
        return Text::ErrorCode::Failure;
    return Text::ErrorCode::Success;
};

extern "C" __declspec(dllexport) Text::ErrorCode DrawMTSDFGlyph(
    void *ctx,                  // Context
    int fontIndex,              // Font index
    int glyphIndex,             // Glyph index
    Text::RGBA32Pixel *texture, // Texture
    int textureWidth            // Texture width
)
{
    auto context = (Text::Context *)ctx;
    auto result = context->DrawMTSDFGlyph(fontIndex, glyphIndex, texture, textureWidth);
    if (result.IsError())
        return Text::ErrorCode::Failure;
    return Text::ErrorCode::Success;
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

extern "C" __declspec(dllexport) Text::ErrorCode DrawAtlas(
    void *ctx,
    int fontIndex,
    const char *text,
    int textLen,
    int textureSize,
    int pixelSize,
    Text::RGBA32Pixel *outTexture)
{
    auto context = (Text::Context *)ctx;

    // Shape the text to get the glyphs
    int maxGlyphs = textLen * 2;
    int glyphCount;
    auto shapingResult = (Text::Glyph *)malloc(sizeof(Text::Glyph) * maxGlyphs);
    context->ShapeText(fontIndex, text, textLen, maxGlyphs, shapingResult, &glyphCount);

    context->debug << "Glyph count: " << glyphCount << std::endl;

    // Extract unique glyph indices
    auto glyphIndexSet = std::set<int>();
    for (int i = 0; i < glyphCount; ++i)
    {
        glyphIndexSet.insert(shapingResult[i].codePoint);
        context->debug << "Glyph " << i << ": " << shapingResult[i].codePoint << std::endl;
    }

    auto glyphs = std::vector<Text::GlyphRect>();
    for (auto glyphIndex : glyphIndexSet)
    {
        glyphs.push_back({glyphIndex, 0, 0, 0, 0});
        context->debug << "Unique glyph: " << glyphIndex << std::endl;
    }

    // Obtain metrics for all glyphs
    FT_Face face = context->faces[fontIndex]->ftFace;
    FT_Set_Pixel_Sizes(face, 0, pixelSize);
    for (auto &glyph : glyphs)
    {
        FT_Load_Glyph(face, glyph.index, FT_LOAD_DEFAULT);
        auto metrics = face->glyph->metrics;
        glyph.w = metrics.width >> 6; // Convert 26.6 to pixels
        glyph.h = metrics.height >> 6;
        context->debug << "Glyph " << glyph.index << " metrics: " << glyph.w << "x" << glyph.h << std::endl;
    }

    // Prepare the atlas
    auto atlas = Text::AtlasBuilder(textureSize);
    auto atlasResult = atlas.Add(glyphs);

    // Draw the atlas
    for (const auto &glyph : glyphs)
    {
        msdfgen::Shape shape;
        FT_Load_Glyph(face, glyph.index, FT_LOAD_DEFAULT);
        double scale = 1.0 / (1 << 6);
        auto outline = &face->glyph->outline;
        shape.contours.clear();
        shape.inverseYAxis = false;
        FtContext ftc = {};
        ftc.scale = scale;
        ftc.shape = &shape;
        FT_Outline_Funcs ftFunctions;
        ftFunctions.move_to = &ftMoveTo;
        ftFunctions.line_to = &ftLineTo;
        ftFunctions.conic_to = &ftConicTo;
        ftFunctions.cubic_to = &ftCubicTo;
        ftFunctions.shift = 0;
        ftFunctions.delta = 0;
        FT_Error error = FT_Outline_Decompose(outline, &ftFunctions, &ftc);
        if (!shape.contours.empty() && shape.contours.back().edges.empty())
            shape.contours.pop_back();
        shape.normalize();
        edgeColoringSimple(shape, 3.0);

        // Draw the glyph
        context->debug << "Drawing glyph " << glyph.index << " at " << glyph.x << ", " << glyph.y << std::endl;
        msdfgen::Bitmap<float, 4> outBitmap(32, 32);
        msdfgen::SDFTransformation t(msdfgen::Projection(32.0, msdfgen::Vector2(0.125, 0.125)), msdfgen::Range(0.125));
        msdfgen::generateMTSDF(outBitmap, shape, t);
        for (int dx = 0; dx < glyph.w; ++dx)
        {
            for (int dy = 0; dy < glyph.h; ++dy)
            {
                int x = glyph.x + dx;
                int y = glyph.y + dy;
                if (x >= 0 && x < textureSize && y >= 0 && y < textureSize)
                {
                    auto dest = outTexture + y * textureSize + x;
                    auto src = outBitmap(dx, dy);
                    dest->r = static_cast<uint8_t>(*(src + 0) * 255.0f + 0.5f);
                    dest->g = static_cast<uint8_t>(*(src + 1) * 255.0f + 0.5f);
                    dest->b = static_cast<uint8_t>(*(src + 2) * 255.0f + 0.5f);
                    dest->a = static_cast<uint8_t>(*(src + 3) * 255.0f + 0.5f);
                }
            }
        }
    }

    // Clean up
    free(shapingResult);
    return Text::ErrorCode::Success;
}

extern "C" __declspec(dllexport) Text::ErrorCode GetDebug(
    void *ctx,
    void *outBuffer, int *outBufferSize)
{
    auto context = (Text::Context *)ctx;
    context->GetDebug(outBuffer, outBufferSize);
    return Text::ErrorCode::Success;
};

// extern "C" __declspec(dllexport) Text::ErrorCode AddGlyphsToDynamicAtlas(
//     void *ctx,                    // Context
//     int fontIndex,                // Font index
//     int *glyphIndices,            // Pointer to array of glyph indices
//     int glyphIndicesLen,          // Length of glyph indices array
//     int textureSize,              // Texture size
//     Text::RGBA32Pixel *outTexture // Output texture pointer
// )
// {
//     auto context = (Text::Context *)ctx;
//     auto result = context->AddGlyphsToDynamicAtlas(fontIndex, glyphIndices, glyphIndicesLen, textureSize, outTexture);
//     if (result.IsError())
//         return Text::ErrorCode::Failure;
//     return Text::ErrorCode::Success;
// };

#endif