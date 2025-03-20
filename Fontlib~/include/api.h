#ifndef API_H
#define API_H

#include "error.h"
#include "shape.h"
#include "context.h"

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
    void *ctx,               // Context
    int fontIndex,           // Font index
    const char *text,        // Text to shape
    int textLen,             // Text length
    Text::Glyph **outGlyphs, // Output glyphs
    int *outGlyphCount       // Output glyph count
)
{
    auto context = (Text::Context *)ctx;
    auto result = context->ShapeText(fontIndex, text, textLen, outGlyphs, outGlyphCount);
    if (result.IsError())
        return Text::ErrorCode::Failure;
    return Text::ErrorCode::Success;
};

#endif