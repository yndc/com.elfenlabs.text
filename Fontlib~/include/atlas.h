#ifndef ATLAS_H
#define ATLAS_H

#include <algorithm>
#include <vector>
#include <glyph.h>
#include <buffer.h>

namespace Text
{
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
        AtlasBuilder(int size, int padding) : size(size), padding(padding) {}

        /// @brief Builds an atlas for the given items
        /// @param items
        /// @return
        int Build(Buffer<GlyphRect> glyphs)
        {
            // Sort items by height descending
            std::sort(glyphs.begin(), glyphs.end(), [](const GlyphRect &a, const GlyphRect &b)
                      { return a.h > b.h; });

            // Wrap items into rows
            rows.push_back(Row());
            auto row = 0;
            auto totalHeight = 0;
            for (auto &glyph : glyphs)
            {
                glyph.w = glyph.w + padding;
                glyph.h = glyph.h + padding;
                if (rows[row].width + glyph.w > this->size)
                {
                    if (totalHeight + rows[row].height + glyph.h > this->size)
                    {
                        return -1; // Atlas is full
                    }
                    totalHeight += rows[row].height;
                    rows.push_back(Row());
                    row++;
                }
                glyph.x = rows[row].width;
                glyph.y = totalHeight;
                rows[row].width += glyph.w;
                rows[row].height = std::max(rows[row].height, glyph.h);
                rows[row].items.push_back(glyph);
            }

            // If we reached this far, this means that the items fit into the atlas.
            // Further optimization are not necessary

            return 0;
        }

    private:
        int size;
        int padding;
        std::vector<Row> rows;
    };
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

#endif