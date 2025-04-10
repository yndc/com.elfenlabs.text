#ifndef CONTEXT_H
#define CONTEXT_H

#include "base.h"
#include "font.h"
#include <stdint.h>
#include "atlas.h"
#include "buffer.h"
#include "shape.h"
#include "texture.h"
#include "error.h"
#include "hb.h"
#include <log.h>
#include <ft2build.h>
#include FT_FREETYPE_H
#include <vector>
#include <sstream>
#include <stdexcept>

namespace Text
{
    class Context
    {
    public:
        Context(LogCallback logCallback, AllocCallback allocCallback, DisposeCallback disposeCallback)
        {
            this->logCallback = logCallback;
            this->allocCallback = allocCallback;
            this->disposeCallback = disposeCallback;
            FT_Init_FreeType(&ftLib);
        }

        ~Context()
        {
            FT_Done_FreeType(ftLib);
        }

        ReturnCode LoadFont(Buffer<byte> inFontData, FontDescription *outFontDescription)
        {
            *outFontDescription = FontDescription(ftLib, inFontData);
            return Success;
        }

        ReturnCode UnloadFont(FontHandle *fontHandle)
        {
            fontHandle->Dispose();
            return Success;
        }

        ReturnCode ShapeText(FontHandle *fontHandle, Allocator allocator, Buffer<char> *inText, Buffer<Glyph> *outGlyphs)
        {
            // Shape the text
            auto buffer = hb_buffer_create();
            hb_buffer_add_utf8(buffer, inText->Data(), inText->SizeInBytes(), 0, inText->SizeInBytes());
            hb_buffer_set_direction(buffer, HB_DIRECTION_LTR);
            hb_buffer_set_script(buffer, HB_SCRIPT_LATIN);
            hb_buffer_set_language(buffer, hb_language_from_string("en", -1));
            hb_shape(fontHandle->hb, buffer, nullptr, 0);

            // Get glyph info and positions
            unsigned int glyphCount;
            hb_glyph_info_t *glyphInfo = hb_buffer_get_glyph_infos(buffer, &glyphCount);
            hb_glyph_position_t *glyphPos = hb_buffer_get_glyph_positions(buffer, &glyphCount);

            Log() << "Shaped " << glyphCount << " glyphs" << "\n";
            for (unsigned int i = 0; i < glyphCount; ++i)
            {
                Log() << "Glyph " << i << ": codepoint: " << glyphInfo[i].codepoint << ", x_offset: " << glyphPos[i].x_offset << ", y_offset: " << glyphPos[i].y_offset << ", x_advance: " << glyphPos[i].x_advance << ", y_advance: " << glyphPos[i].y_advance << "\n";
            }

            // Write glyph data
            *outGlyphs = Alloc<Glyph>(glyphCount, allocator);
            for (unsigned int i = 0; i < glyphCount; ++i)
            {
                auto ptr = &(*outGlyphs)[i];
                ptr->index = glyphInfo[i].codepoint;
                ptr->offset_x_fu = glyphPos[i].x_offset;
                ptr->offset_y_fu = glyphPos[i].y_offset;
                ptr->advance_x_fu = glyphPos[i].x_advance;
                ptr->advance_y_fu = glyphPos[i].y_advance;
            }

            // Clean up
            hb_buffer_destroy(buffer);

            return ReturnCode::Success;
        }

        std::vector<int> ShapeText(FontHandle *fontHandle, Buffer<char> *inText)
        {
            // Shape the text
            auto buffer = hb_buffer_create();
            hb_buffer_add_utf8(buffer, inText->Data(), inText->SizeInBytes(), 0, inText->SizeInBytes());
            hb_buffer_set_direction(buffer, HB_DIRECTION_LTR);
            hb_buffer_set_script(buffer, HB_SCRIPT_LATIN);
            hb_buffer_set_language(buffer, hb_language_from_string("en", -1));
            hb_shape(fontHandle->hb, buffer, nullptr, 0);

            // Get glyph info and positions
            unsigned int glyphCount;
            hb_glyph_info_t *glyphInfo = hb_buffer_get_glyph_infos(buffer, &glyphCount);

            // Write glyph data
            auto result = std::vector<int>(glyphCount);
            for (unsigned int i = 0; i < glyphCount; ++i)
            {
                result[i] = glyphInfo[i].codepoint;
            }

            // Clean up
            hb_buffer_destroy(buffer);

            Log() << "Shaped " << glyphCount << " glyphs";

            return result;
        }

        ReturnCode DrawAtlas(
            FontHandle *fontHandle,
            int textureSize,
            int glyphSize,
            int padding,
            int margin,
            float distanceMappingRange,
            int glyphRenderFlags,
            int compactFlags,
            Allocator allocator,
            Buffer<char> *inText,
            Buffer<RGBA32Pixel> *refTexture,
            Buffer<GlyphMetrics> *outGlyphMetrics)
        {
            auto glyphs = CreateGlyphPixelMetricsBuffer(fontHandle, allocator, inText);

            Log() << "Unique glyph count: " << glyphs.Count();

            // Obtain metrics for all glyphs
            FT_Face face = fontHandle->ft;
            for (int i = 0; i < glyphs.Count(); ++i)
            {
                auto &glyph = glyphs[i];
                FT_Load_Glyph(face, glyph.index, FT_LOAD_NO_SCALE);

                auto metrics = face->glyph->metrics;
                auto units_per_em = face->units_per_EM;
                glyph.width_fu = metrics.width;
                glyph.height_fu = metrics.height;
                glyph.atlas_width_px = metrics.width * glyphSize / units_per_em;
                glyph.atlas_height_px = metrics.height * glyphSize / units_per_em;
                glyph.left_fu = metrics.horiBearingX;
                glyph.top_fu = metrics.horiBearingY;
            }

            // Prepare the atlas
            auto atlasConfig = AtlasConfig{textureSize, padding, margin, compactFlags};
            auto atlas = AtlasBuilder(atlasConfig, glyphs);
            auto atlasResult = atlas.Build();

            // Draw the atlas
            for (int i = 0; i < glyphs.Count(); i++)
            {
                auto &glyph = glyphs[i];

                // Draw the glyph
                DrawGlyph(face, glyph, textureSize, glyphSize, padding, distanceMappingRange, glyphRenderFlags, refTexture);
            }

            *outGlyphMetrics = glyphs;

            return ReturnCode::Success;
        }

        Buffer<GlyphMetrics> CreateGlyphPixelMetricsBuffer(FontHandle *fontHandle, Allocator allocator, Buffer<char> *inText)
        {
            auto shapingResult = ShapeText(fontHandle, inText);

            // Extract unique glyph indices
            auto glyphIndexSet = std::set<int>();
            for (int i = 0; i < shapingResult.size(); ++i)
            {
                glyphIndexSet.insert(shapingResult[i]);
            }

            Buffer<GlyphMetrics> result = Alloc<GlyphMetrics>(glyphIndexSet.size(), allocator);
            int index = 0;
            for (auto glyphIndex : glyphIndexSet)
            {
                result[index] = GlyphMetrics(glyphIndex);
                index++;
            }

            return result;
        }

    private:
        LogStream Log()
        {
            return LogStream(logCallback);
        }

        /// @brief Allocates a buffer of a given size and allocator
        /// @tparam T
        /// @param sizeBytes
        /// @param allocator
        /// @return
        template <typename T>
        Buffer<T> Alloc(int length, Allocator allocator)
        {
            auto size = length * sizeof(T);
            auto ptr = allocCallback(size, 4, allocator);
            if (ptr == nullptr)
                throw std::bad_alloc();
            return Buffer<T>(ptr, size, allocator);
        }

        /// @brief Allocates an output buffer
        /// @tparam T
        /// @param outBuffer
        /// @param sizeBytes
        /// @param allocator
        template <typename T>
        void Alloc(OutBuffer<T> outBuffer, int length, Allocator allocator)
        {
            auto buffer = this->Alloc<T>(length, allocator);
            outBuffer.Assign(buffer);
        }

    private:
        FT_Library ftLib;
        AllocCallback allocCallback;
        DisposeCallback disposeCallback;
        LogCallback logCallback;
    };
}

#endif