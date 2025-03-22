#ifndef ATLAS_H
#define ATLAS_H

namespace Text
{
    struct GlyphRect
    {
        int index;
        int x, y, w, h;
    };

    struct Row
    {
        int height;
        int width;
        std::vector<GlyphRect> items;
        Row() : height(0), width(0)
        {
            items = std::vector<GlyphRect>();
        }
    };

    class Atlas
    {
        std::vector<GlyphRect> rects;
    };

    class AtlasBuilder
    {
    public:
        AtlasBuilder(int size) : size(size) {}
        ~AtlasBuilder();

        /// @brief Add multiple rectangles to the atlas
        /// @param w widths of the rectangles
        /// @param h heights of the rectangles
        /// @param count number of rectangles to add
        /// @return number of rectangles added
        int Add(std::vector<GlyphRect> &items);

    protected:
        int size;
        std::vector<Row> rows;
    };
}

#endif