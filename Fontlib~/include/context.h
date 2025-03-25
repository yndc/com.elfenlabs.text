#ifndef CONTEXT_H
#define CONTEXT_H

#include <stdint.h>
#include "shape.h"
#include "texture.h"
#include "error.h"
#include "hb.h"
#include <vector>
#include <ft2build.h>
#include FT_FREETYPE_H
#include <sstream>

namespace Text
{
    // #ifdef TEXTLIB_DEBUG
    typedef void (*UnityLogCallback)(const char *message);
    // #endif

    class Face
    {
    public:
        FT_Face ftFace;
        hb_font_t *hb;
        Face(FT_Library ftLib, const unsigned char *fontData, size_t fontDataSize);
        ~Face();
    };

    class Context
    {
        FT_Library ftLib;

    public:
        std::vector<Face *> faces;
        Context();
        ~Context();
        Result<int> LoadFont(const unsigned char *fontData, size_t fontDataSize);
        Result<void> UnloadFont(int fontIndex);
        Result<void> ShapeText(int fontIndex, const char *text, int textLen, int maxGlyphs, Glyph *refGlyphs, int *outGlyphCount);
        Result<void> DrawMTSDFGlyph(int fontIndex, int glyphIndex, RGBA32Pixel *refTexture, int textureWidth);

        // #ifdef TEXTLIB_DEBUG
        class LogStream
        {
        public:
            LogStream(UnityLogCallback callback) : callback(callback) {}
            ~LogStream()
            {
                if (callback != nullptr)
                    callback(ss.str().c_str());
            }

            template <typename T>
            LogStream &operator<<(const T &value)
            {
                ss << value;
                return *this;
            }

            LogStream &operator<<(std::ostream &(*manip)(std::ostream &))
            {
                ss << "\n";
                return *this;
            }

        private:
            std::stringstream ss;
            UnityLogCallback callback;
        };

        Result<void> SetUnityLogCallback(UnityLogCallback callback)
        {
            unityLogCallback = callback;
            return Result<void>::Success();
        }
        LogStream Log() { return LogStream(unityLogCallback); }
        // #endif

    private:
        // #ifdef TEXTLIB_DEBUG
        UnityLogCallback unityLogCallback;
        // #endif
    };
}

#endif