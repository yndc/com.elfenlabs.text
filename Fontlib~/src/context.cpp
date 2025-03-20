// fontlib.cpp
#include <vector>
#include "context.h"
#include "error.h"
#include <hb.h>
#include <ft2build.h>
#include FT_FREETYPE_H

namespace Text
{
    Context::Context()
    {
        FT_Init_FreeType(&ftLib);

        faces.reserve(16);
    }

    Context::~Context()
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

    Result<int> Context::LoadFont(const unsigned char *fontData, size_t fontDataSize)
    {
        auto face = new Face(ftLib, fontData, fontDataSize);
        faces.push_back(face);
        return Result<int>::Success(faces.size() - 1);
    }

    Result<void> Context::UnloadFont(int fontIndex)
    {
        auto face = faces[fontIndex];
        if (face == nullptr)
            return Result<void>::Fail(ErrorCode::Failure);
        faces[fontIndex] = nullptr;
        delete face;
        return Result<void>::Success();
    }

    Text::Face::Face(FT_Library ftLib, const unsigned char *fontData, size_t fontDataSize)
    {
        FT_New_Memory_Face(ftLib, fontData, fontDataSize, 0, &ftFace);
        auto blob = hb_blob_create((const char *)fontData, fontDataSize, HB_MEMORY_MODE_READONLY, nullptr, nullptr);
        auto face = hb_face_create(blob, 0);
        hb = hb_font_create(face);
        //hb_face_destroy(face);
    }

    Face::~Face()
    {
        FT_Done_Face(ftFace);
        hb_font_destroy(hb);
    }
}