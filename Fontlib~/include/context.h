#ifndef CONTEXT_H
#define CONTEXT_H

#include "base.h"
#include "font.h"
#include <stdint.h>
#include "atlas.h"
#include "buffer.h"
#include "shape.h"
#include "render.h"
#include "error.h"
#include "hb.h"
#include <log.h>
#include <ft2build.h>
#include FT_FREETYPE_H
#include <vector>
#include <sstream>
#include <stdexcept>

class Context
{
public:
    FT_Library ftLib;
    AllocCallback allocCallback;
    DisposeCallback disposeCallback;
    LogCallback logCallback;
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

    ReturnCode UnloadFont(FontHandle *font_handle)
    {
        font_handle->Dispose();
        return Success;
    }

    ReturnCode ShapeText(FontHandle *font_handle, Allocator allocator, Buffer<char> *inText, Buffer<GlyphShape> *outGlyphs)
    {
        // Shape the text
        auto buffer = hb_buffer_create();
        hb_buffer_add_utf8(buffer, inText->Data(), inText->SizeInBytes(), 0, inText->SizeInBytes());
        hb_buffer_set_direction(buffer, HB_DIRECTION_LTR);
        hb_buffer_set_script(buffer, HB_SCRIPT_LATIN);
        hb_buffer_set_language(buffer, hb_language_from_string("en", -1));
        hb_shape(font_handle->hb, buffer, nullptr, 0);

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
        *outGlyphs = Alloc<GlyphShape>(glyphCount, allocator);
        for (unsigned int i = 0; i < glyphCount; ++i)
        {
            auto ptr = &(*outGlyphs)[i];
            ptr->codepoint = glyphInfo[i].codepoint;
            ptr->cluster = glyphInfo[i].cluster;
            ptr->offset_x_fu = glyphPos[i].x_offset;
            ptr->offset_y_fu = glyphPos[i].y_offset;
            ptr->advance_x_fu = glyphPos[i].x_advance;
            ptr->advance_y_fu = glyphPos[i].y_advance;
        }

        // Clean up
        hb_buffer_destroy(buffer);

        return ReturnCode::Success;
    }

    std::vector<int> ShapeText(FontHandle *font_handle, Buffer<char> *inText)
    {
        // Shape the text
        auto buffer = hb_buffer_create();
        hb_buffer_add_utf8(buffer, inText->Data(), inText->SizeInBytes(), 0, inText->SizeInBytes());
        hb_buffer_set_direction(buffer, HB_DIRECTION_LTR);
        hb_buffer_set_script(buffer, HB_SCRIPT_LATIN);
        hb_buffer_set_language(buffer, hb_language_from_string("en", -1));
        hb_shape(font_handle->hb, buffer, nullptr, 0);

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

        return result;
    }

    Buffer<GlyphMetrics> CreateGlyphPixelMetricsBuffer(FontHandle *font_handle, Allocator allocator, Buffer<char> *inText)
    {
        auto shapingResult = ShapeText(font_handle, inText);

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

    /// @brief Logs a message to the log callback
    /// @return 
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
};

#endif