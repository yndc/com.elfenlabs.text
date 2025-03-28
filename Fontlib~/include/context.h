#ifndef CONTEXT_H
#define CONTEXT_H

#include <stdint.h>
#include "types.h"
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
    class Face
    {
    public:
        FT_Face ftFace;
        hb_font_t *hb;
        Face(FT_Library ftLib, const unsigned char *fontData, size_t fontDataSize)
        {
            FT_New_Memory_Face(ftLib, fontData, fontDataSize, 0, &ftFace);
            auto blob = hb_blob_create((const char *)fontData, fontDataSize, HB_MEMORY_MODE_READONLY, nullptr, nullptr);
            auto face = hb_face_create(blob, 0);
            hb = hb_font_create(face);
        }
        ~Face()
        {
            FT_Done_Face(ftFace);
            hb_font_destroy(hb);
        }
    };

    class Context
    {
    public:
        Context(LogCallback logCallback, AllocCallback allocCallback, DisposeCallback disposeCallback)
        {
            this->logCallback = logCallback;
            this->allocCallback = allocCallback;
            this->disposeCallback = disposeCallback;
            FT_Init_FreeType(&ftLib);
            faces.reserve(16);
        }

        ~Context()
        {
            for (int i = 0; i < faces.size(); ++i)
            {
                auto font = faces[i];
                if (font == nullptr)
                    continue;
                delete faces[i];
            }
            faces.clear();
            FT_Done_FreeType(ftLib);
        }

        ErrorCode LoadFont(Buffer<byte> inFontData, int *outFontIndex)
        {
            auto face = new Face(ftLib, inFontData.Data(), inFontData.SizeInBytes());
            faces.push_back(face);
            *outFontIndex = faces.size() - 1;
            return Success;
        }

        ErrorCode UnloadFont(int fontIndex)
        {
            auto face = faces[fontIndex];
            if (face == nullptr)
                return ErrorCode::Failure;
            faces[fontIndex] = nullptr;
            delete face;
            return Success;
        }

        ErrorCode ShapeText(int fontIndex, Allocator allocator, Buffer<char> *inText, Buffer<Glyph> *outGlyphs)
        {
            auto font = faces[fontIndex];
            if (font == nullptr)
                return ErrorCode::FontNotFound;

            // Shape the text
            auto buffer = hb_buffer_create();
            hb_buffer_add_utf8(buffer, inText->Data(), inText->SizeInBytes(), 0, inText->SizeInBytes());
            hb_buffer_set_direction(buffer, HB_DIRECTION_LTR);
            hb_buffer_set_script(buffer, HB_SCRIPT_LATIN);
            hb_buffer_set_language(buffer, hb_language_from_string("en", -1));
            hb_shape(font->hb, buffer, nullptr, 0);

            // Get glyph info and positions
            unsigned int glyphCount;
            hb_glyph_info_t *glyphInfo = hb_buffer_get_glyph_infos(buffer, &glyphCount);
            hb_glyph_position_t *glyphPos = hb_buffer_get_glyph_positions(buffer, &glyphCount);

            Log() << "Shaped " << glyphCount << " glyphs";

            // Write glyph data
            *outGlyphs = Alloc<Glyph>(glyphCount, allocator);
            for (unsigned int i = 0; i < glyphCount; ++i)
            {
                auto ptr = &(*outGlyphs)[i];
                ptr->codePoint = glyphInfo[i].codepoint;
                ptr->xOffset = glyphPos[i].x_offset;
                ptr->yOffset = glyphPos[i].y_offset;
                ptr->xAdvance = glyphPos[i].x_advance;
                ptr->yAdvance = glyphPos[i].y_advance;
            }

            // Clean up
            hb_buffer_destroy(buffer);

            return ErrorCode::Success;
        }

        std::vector<int> ShapeText(int fontIndex, Buffer<char> *inText)
        {
            auto font = faces[fontIndex];

            // Shape the text
            auto buffer = hb_buffer_create();
            hb_buffer_add_utf8(buffer, inText->Data(), inText->SizeInBytes(), 0, inText->SizeInBytes());
            hb_buffer_set_direction(buffer, HB_DIRECTION_LTR);
            hb_buffer_set_script(buffer, HB_SCRIPT_LATIN);
            hb_buffer_set_language(buffer, hb_language_from_string("en", -1));
            hb_shape(font->hb, buffer, nullptr, 0);

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

        ErrorCode DrawAtlas(
            int fontIndex,
            int textureSize,
            int glyphSize,
            int padding,
            Allocator allocator,
            Buffer<char> *inText,
            Buffer<RGBA32Pixel> *refTexture,
            Buffer<GlyphRect> *outGlyphRects)
        {
            auto glyphs = CreateGlyphRectBuffer(fontIndex, allocator, inText);

            Log() << "Unique glyph count: " << glyphs.Count();

            // Obtain metrics for all glyphs
            FT_Face face = faces[fontIndex]->ftFace;
            FT_Set_Pixel_Sizes(face, 0, glyphSize);
            for (int i = 0; i < glyphs.Count(); ++i)
            {
                auto &glyph = glyphs[i];
                FT_Load_Glyph(face, glyph.index, FT_LOAD_DEFAULT);

                auto metrics = face->glyph->metrics;
                glyph.w = metrics.width >> 6;
                glyph.h = metrics.height >> 6;
                Log() << "Glyph " << glyph.index << " metrics: " << glyph.w << "x" << glyph.h;
            }

            // Prepare the atlas
            auto atlas = AtlasBuilder(textureSize, padding);
            auto atlasResult = atlas.Build(glyphs);

            // Draw the atlas
            for (int i = 0; i < glyphs.Count(); i++)
            {
                auto &glyph = glyphs[i];

                Log() << "Drawing Glyph " << glyph.index << " rect: " << glyph.x << ", " << glyph.y << ", " << glyph.w << ", " << glyph.h;

                // Draw the glyph
                DrawGlyph(face, glyph, textureSize, glyphSize, padding, refTexture);
            }

            *outGlyphRects = glyphs;

            return Text::ErrorCode::Success;
        }

        Buffer<GlyphRect> CreateGlyphRectBuffer(int fontIndex, Allocator allocator, Buffer<char> *inText)
        {
            auto shapingResult = ShapeText(fontIndex, inText);

            // Extract unique glyph indices
            auto glyphIndexSet = std::set<int>();
            for (int i = 0; i < shapingResult.size(); ++i)
            {
                glyphIndexSet.insert(shapingResult[i]);
            }

            Buffer<GlyphRect> result = Alloc<GlyphRect>(glyphIndexSet.size(), allocator);
            int index = 0;
            for (auto glyphIndex : glyphIndexSet)
            {
                result[index] = {glyphIndex, 0, 0, 0, 0};
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
        std::vector<Face *> faces;
        AllocCallback allocCallback;
        DisposeCallback disposeCallback;
        LogCallback logCallback;
    };
}

#endif