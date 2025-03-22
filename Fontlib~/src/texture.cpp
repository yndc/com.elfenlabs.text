#include <context.h>
#include <error.h>

namespace Text
{
    Result<void> Context::DrawMTSDFGlyph(int fontIndex, int glyphIndex, RGBA32Pixel *refTexture, int textureWidth)
    {
        // test only, we draw a 32x32 red square
        for (int y = 0; y < 32; ++y)
        {
            for (int x = 0; x < 32; ++x)
            {
                auto pixel = refTexture + y * textureWidth + x;
                pixel->r = 255;
                pixel->g = 0;
                pixel->b = 0;
                pixel->a = 255;
            }
        }
        
        return Result<void>::Success();
    }
}