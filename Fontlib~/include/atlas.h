#ifndef ATLAS_H
#define ATLAS_H

#include <algorithm>

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
        int Add(std::vector<GlyphRect> &items)
        {
            if (items.empty())
                return 0;

            // Sort items by height descending
            std::sort(items.begin(), items.end(), [](const GlyphRect &a, const GlyphRect &b)
                      { return a.h > b.h; });

            // Wrap items into rows
            rows.push_back(Row());
            auto row = 0;
            auto totalHeight = 0;
            for (auto &item : items)
            {
                if (rows[row].width + item.w > this->size)
                {
                    if (totalHeight + rows[row].height + item.h > this->size)
                    {
                        return -1; // Atlas is full
                    }
                    totalHeight += rows[row].height;
                    rows.push_back(Row());
                    row++;
                }
                item.x = rows[row].width;
                item.y = totalHeight;
                rows[row].width += item.w;
                rows[row].height = std::max(rows[row].height, item.h);
                rows[row].items.push_back(item);
            }

            // If we reached this far, this means that the items fit into the atlas.
            // Further optimization are not necessary

            return 0;
        }

    protected:
        int size;
        std::vector<Row> rows;
    };

    /// @brief Get a list of unique glyphs from a string
    /// @param str
    /// @return
    std::vector<GlyphRect> GetUniqueGlyphs(std::string str);
}

// For each row, sort items by width
// for (auto &row : rows)
// {
//     std::sort(row.items.begin(), row.items.end(), [](Rect a, Rect b)
//               { return a.w > b.w; });
// }

// Try adding the last items of each row to the previous row
// for (auto i = 0; i < rows.size() - 1; i++)
// {
//     auto &row = rows[i];
//     auto &nextRow = rows[i + 1];
//     for (auto j = 0; j < row.items.size(); j++)
//     {
//         auto &item = row.items[j];
//         if (nextRow.width + item.w <= this->size)
//         {
//             nextRow.items.push_back(item);
//             nextRow.width += item.w;
//             nextRow.height = std::max(nextRow.height, item.h);
//             row.items.erase(row.items.begin() + j);
//             row.width -= item.w;
//             j--;
//         }
//     }
// }

// std::vector<GlyphRect> GetUniqueGlyphs(Context* ctx, int fontIndex, std::string str)
// {
//     // Shape the text to get the glyphs
//     int glyphCount;

//     ctx->ShapeText(fontIndex, text, textLen, maxGlyphs, shapingResult, &glyphCount);

//     context->Log() << "Atlas character set: " << std::string(text, textLen);

//     // Extract unique glyph indices
//     auto glyphIndexSet = std::set<int>();
//     for (int i = 0; i < glyphCount; ++i)
//     {
//         glyphIndexSet.insert(shapingResult[i].codePoint);
//     }

//     auto glyphs = std::vector<Text::GlyphRect>();
//     for (auto glyphIndex : glyphIndexSet)
//     {
//         glyphs.push_back({glyphIndex, 0, 0, 0, 0});
//     }
//     context->Log() << "Unique glyph count: " << glyphs.size();
// }

#endif