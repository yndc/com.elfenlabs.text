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
#include "clipper2/clipper.h"

using namespace math;

namespace Text
{
    enum GlyphRenderFlag
    {
        ResolveIntersections = 1 << 0
    };

    struct RGBA32Pixel
    {
        uint8_t r, g, b, a;
    };

    inline byte pixelFloatToByte(float x)
    {
        return byte(~int(255.5f - 255.f * clamp(x)));
    }

    const int FLATTEN_STEPS_PER_CURVE = 10;   // How many line segments per Bezier curve
    const double CLIPPER_SCALE_FACTOR = 32.0; // Scale factor for integer coords

    // --- Data Structures ---

    // Using Clipper's Point structure for convenience during decomposition
    using PointD = Clipper2Lib::Point<double>;

    struct DecomposeData
    {
        std::vector<Clipper2Lib::PathD> contours; // Store contours as paths of doubles
        PointD current_pos = {0.0, 0.0};
        bool contour_started = false;

        void StartContour(double x, double y)
        {
            contours.push_back({}); // Start a new contour path
            current_pos = {x, y};
            contours.back().push_back(current_pos);
            contour_started = true;
        }

        void AddLine(double x, double y)
        {
            if (!contour_started)
                return; // Should not happen with valid outlines
            current_pos = {x, y};
            // Avoid duplicate consecutive points if possible (Clipper might handle it)
            if (contours.back().empty() || contours.back().back() != current_pos)
            {
                contours.back().push_back(current_pos);
            }
        }

        // Simple quadratic Bezier flattening
        void FlattenConic(PointD ctrl, PointD to)
        {
            if (!contour_started)
                return;
            PointD start = current_pos;
            for (int i = 1; i <= FLATTEN_STEPS_PER_CURVE; ++i)
            {
                double t = static_cast<double>(i) / FLATTEN_STEPS_PER_CURVE;
                double u = 1.0 - t;
                double x = u * u * start.x + 2 * u * t * ctrl.x + t * t * to.x;
                double y = u * u * start.y + 2 * u * t * ctrl.y + t * t * to.y;
                AddLine(x, y); // AddLine handles current_pos update
            }
            // Ensure endpoint is exactly added (current_pos is already updated by last AddLine)
        }

        // Simple cubic Bezier flattening
        void FlattenCubic(PointD ctrl1, PointD ctrl2, PointD to)
        {
            if (!contour_started)
                return;
            PointD start = current_pos;
            for (int i = 1; i <= FLATTEN_STEPS_PER_CURVE; ++i)
            {
                double t = static_cast<double>(i) / FLATTEN_STEPS_PER_CURVE;
                double u = 1.0 - t;
                double p_x = std::pow(u, 3) * start.x +
                             3 * std::pow(u, 2) * t * ctrl1.x +
                             3 * u * std::pow(t, 2) * ctrl2.x +
                             std::pow(t, 3) * to.x;
                double p_y = std::pow(u, 3) * start.y +
                             3 * std::pow(u, 2) * t * ctrl1.y +
                             3 * u * std::pow(t, 2) * ctrl2.y +
                             std::pow(t, 3) * to.y;
                AddLine(p_x, p_y); // AddLine handles current_pos update
            }
            // Ensure endpoint is exactly added
        }
    };

    // --- FreeType Decomposition Callbacks ---

    int MoveToFunc(const FT_Vector *to, void *user)
    {
        DecomposeData *data = static_cast<DecomposeData *>(user);
        data->StartContour(static_cast<double>(to->x), static_cast<double>(to->y));
        return 0;
    }

    int LineToFunc(const FT_Vector *to, void *user)
    {
        DecomposeData *data = static_cast<DecomposeData *>(user);
        data->AddLine(static_cast<double>(to->x), static_cast<double>(to->y));
        return 0;
    }

    int ConicToFunc(const FT_Vector *control, const FT_Vector *to, void *user)
    {
        DecomposeData *data = static_cast<DecomposeData *>(user);
        PointD ctrl = {static_cast<double>(control->x), static_cast<double>(control->y)};
        PointD p_to = {static_cast<double>(to->x), static_cast<double>(to->y)};
        data->FlattenConic(ctrl, p_to);
        return 0;
    }

    int CubicToFunc(const FT_Vector *control1, const FT_Vector *control2, const FT_Vector *to, void *user)
    {
        DecomposeData *data = static_cast<DecomposeData *>(user);
        PointD ctrl1 = {static_cast<double>(control1->x), static_cast<double>(control1->y)};
        PointD ctrl2 = {static_cast<double>(control2->x), static_cast<double>(control2->y)};
        PointD p_to = {static_cast<double>(to->x), static_cast<double>(to->y)};
        data->FlattenCubic(ctrl1, ctrl2, p_to);
        return 0;
    }

    // --- Clipper Paths to msdfgen::Shape Conversion ---

    /**
     * Converts the output paths from Clipper2 (scaled integers) into an msdfgen::Shape object.
     *
     * @param clipper_paths The Paths64 result from a Clipper2 execution.
     * @param scale_factor The factor used to scale coordinates for Clipper (e.g., 1000.0).
     * @return An msdfgen::Shape containing the geometry.
     */
    msdfgen::Shape ConvertClipperPathsToMsdfShapeEMNormalized( // Renamed for clarity
        const Clipper2Lib::Paths64 &clipper_paths,
        double clipper_scale_factor,
        int units_per_EM) // Added units_per_EM
    {
        msdfgen::Shape shape;
        shape.contours.reserve(clipper_paths.size());

        // Basic check for valid scale factors and units_per_EM
        if (clipper_scale_factor == 0 || units_per_EM <= 0)
        {
            // Return empty shape if inputs are invalid
            return shape;
        }

        // Calculate the combined inverse scale factor to go from Clipper's scaled integers
        // directly to EM-normalized coordinates.
        // Clipper coord = FUnit * clipper_scale_factor
        // Normalized coord = FUnit / units_per_EM
        // => Normalized coord = (Clipper coord / clipper_scale_factor) / units_per_EM
        // => Normalized coord = Clipper coord * (1.0 / (clipper_scale_factor * units_per_EM))
        const double combined_inv_scale = 1.0 / (clipper_scale_factor * static_cast<double>(units_per_EM));

        for (const Clipper2Lib::Path64 &path : clipper_paths)
        {
            if (path.size() < 2)
                continue;

            msdfgen::Contour contour;
            contour.edges.reserve(path.size());

            for (size_t i = 0; i < path.size(); ++i)
            {
                const Clipper2Lib::Point64 &p1_scaled = path[i];
                const Clipper2Lib::Point64 &p2_scaled = path[(i + 1) % path.size()];

                // Apply the combined inverse scale to get EM-normalized coordinates
                msdfgen::Point2 p1(p1_scaled.x * combined_inv_scale, p1_scaled.y * combined_inv_scale);
                msdfgen::Point2 p2(p2_scaled.x * combined_inv_scale, p2_scaled.y * combined_inv_scale);

                const double epsilon = 1e-12; // Use a slightly larger epsilon for normalized coords
                if (fabs(p1.x - p2.x) > epsilon || fabs(p1.y - p2.y) > epsilon)
                {
                    // Use LinearSegment (or LineSegment depending on msdfgen version)
                    contour.addEdge(new msdfgen::LinearSegment(p1, p2));
                    // contour.addEdge(new msdfgen::LineSegment(p1, p2)); // If using older msdfgen
                }
            }

            if (!contour.edges.empty())
            {
                shape.addContour(contour);
            }
        }

        if (!shape.contours.empty())
        {
            shape.normalize();
            shape.orientContours();
        }

        return shape;
    }

    msdfgen::Shape GetResolvedShape(FT_Face face, int glyphIndex)
    {
        FT_Load_Glyph(face, glyphIndex, FT_LOAD_NO_BITMAP | FT_LOAD_NO_HINTING);
        FT_Outline *outline = &face->glyph->outline;
        DecomposeData decompose_data;
        FT_Outline_Funcs decompose_callbacks;
        decompose_callbacks.move_to = MoveToFunc;
        decompose_callbacks.line_to = LineToFunc;
        decompose_callbacks.conic_to = ConicToFunc;
        decompose_callbacks.cubic_to = CubicToFunc;
        decompose_callbacks.shift = 0;
        decompose_callbacks.delta = 0;
        FT_Outline_Decompose(outline, &decompose_callbacks, &decompose_data);
        // if (decompose_data.contours.empty())
        // {
        //     return;
        // }

        Clipper2Lib::Paths64 clipper_paths;
        clipper_paths.reserve(decompose_data.contours.size());
        for (const auto &contour_d : decompose_data.contours)
        {
            Clipper2Lib::Path64 path_i;
            path_i.reserve(contour_d.size());
            for (const auto &pt_d : contour_d)
            {
                path_i.push_back(Clipper2Lib::Point64(
                    static_cast<int64_t>(std::round(pt_d.x * CLIPPER_SCALE_FACTOR)),
                    static_cast<int64_t>(std::round(pt_d.y * CLIPPER_SCALE_FACTOR))));
            }
            // Clipper often benefits from simpler polygons if possible
            // path_i = Clipper2Lib::SimplifyPath(path_i, 0.1 * CLIPPER_SCALE_FACTOR); // Optional simplification
            clipper_paths.push_back(path_i);
        }

        Clipper2Lib::Clipper64 clipper;
        Clipper2Lib::Paths64 solution_paths;      // To store the result
        Clipper2Lib::Paths64 open_solution_paths; // Not used for union

        // AddPaths takes r-value ref, so move is efficient
        clipper.AddSubject(std::move(clipper_paths));

        // FillRule::NonZero is standard for fonts
        // Use FillRule::Positive for only positively wound contours if needed
        clipper.Execute(Clipper2Lib::ClipType::Union,
                        Clipper2Lib::FillRule::NonZero, // Or ::Positive, ::EvenOdd depending on need
                        solution_paths);

        return ConvertClipperPathsToMsdfShapeEMNormalized(
            solution_paths,
            CLIPPER_SCALE_FACTOR,
            face->units_per_EM);
    }

    msdfgen::Shape GetShape(FT_Face face, int glyphIndex)
    {
        msdfgen::Shape shape;
        auto fontHandle = msdfgen::adoptFreetypeFont(face);
        msdfgen::loadGlyph(
            shape,
            fontHandle,
            msdfgen::GlyphIndex(glyphIndex),
            msdfgen::FontCoordinateScaling::FONT_SCALING_EM_NORMALIZED);
        shape.normalize();
        return shape;
    }

    void DrawGlyph(FT_Face face, GlyphRect glyphRect, int textureSize, int glyphSize, int padding, float distanceMappingRange, int renderFlags, Buffer<RGBA32Pixel> *refTexture)
    {
        msdfgen::Shape shape;
        if (Flag::has(renderFlags, GlyphRenderFlag::ResolveIntersections))
        {
            shape = GetResolvedShape(face, glyphRect.index);
        }
        else
        {
            shape = GetShape(face, glyphRect.index);
        }

        edgeColoringSimple(shape, 3.0);

        float scale = static_cast<float>(glyphSize);
        int bitmapSize = glyphSize + padding * 2;

        // Get glyph bounds from Shape (normalized 0-1 units)
        msdfgen::Shape::Bounds bounds = shape.getBounds();
        float glyphLeft = bounds.l;
        float glyphBottom = bounds.b;
        float translateX = (padding - glyphLeft * scale) / scale;
        float translateY = (padding - glyphBottom * scale) / scale;

        msdfgen::Vector2 translate(translateX, translateY);
        msdfgen::Bitmap<float, 4> outBitmap(bitmapSize, bitmapSize);
        msdfgen::Projection projection = msdfgen::Projection(scale, translate);
        msdfgen::SDFTransformation transform(projection, msdfgen::Range(distanceMappingRange));
        msdfgen::MSDFGeneratorConfig config(true);
        msdfgen::generateMTSDF(outBitmap, shape, transform, config);
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
                    dest->a = pixelFloatToByte(src[3]);
                }
            }
        }
    }
}

#endif