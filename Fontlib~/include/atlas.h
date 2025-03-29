#ifndef ATLAS_H
#define ATLAS_H

#include <algorithm>
#include <vector>
#include <glyph.h>
#include <buffer.h>
#include <base.h>

namespace Text
{
    struct Row
    {
        int height;
        int width;
        std::vector<int> items;
        Row() : height(0), width(0)
        {
            items = std::vector<int>();
        }
    };

    struct GlyphReference
    {
        int index;
    };

    class Atlas
    {
        std::vector<GlyphRect> rects;
    };

    enum CompactMode
    {
        None = 0,
        FillEnd = 1 << 0,
        Gravity = 1 << 1,
        ZigZag = 1 << 2,
    };

    struct AtlasConfig
    {
        int size;
        int padding;
        int compactFlags;
    };

    class AtlasBuilder
    {
    private:
        AtlasConfig config;
        std::vector<Row> rows;
        Buffer<GlyphRect> glyphs;

    public:
        AtlasBuilder(AtlasConfig config, Buffer<GlyphRect> glyphs) : config(config), glyphs(glyphs) {}

        /// @brief Builds an atlas for the given items
        /// @param items
        /// @return
        int Build()
        {
            auto padding = config.padding;
            auto size = config.size;

            // Create an an array of glyph buffer indices
            std::vector<int> indices(glyphs.Count());
            for (int i = 0; i < glyphs.Count(); i++)
            {
                indices[i] = i;
            }

            // Sort indices by height descending
            std::sort(indices.begin(), indices.end(), [this](const int &a, const int &b)
                      { return glyphs[a].h > glyphs[b].h; });

            // Wrap items into rows
            rows.push_back(Row());
            auto row = 0;
            auto totalHeight = 0;
            for (int i = 0; i < indices.size(); i++)
            {
                auto index = indices[i];
                auto &glyph = glyphs[index];

                // Add padding to glyph dimensions
                glyph.w = glyph.w + padding * 2;
                glyph.h = glyph.h + padding * 2;

                // Wrap excess items into new rows
                if (rows[row].width + glyph.w > size)
                {
                    if (totalHeight + rows[row].height + glyph.h > size)
                    {
                        return -1; // TODO: Handle when the atlas is too small
                    }
                    totalHeight += rows[row].height;
                    rows.push_back(Row());
                    row++;
                }

                // Set glyph position and row dimensions
                glyph.x = rows[row].width;
                glyph.y = totalHeight;
                rows[row].width += glyph.w;
                rows[row].height = std::max(rows[row].height, glyph.h);
                rows[row].items.push_back(index);
            }

            // If we reached this far, this means that the items fit into the atlas.
            // Further optimization are not necessary

            Compact();

            return 0;
        }

        /// @brief Compacts current items to minimize the height of the rows
        void Compact()
        {
            if (Flag::has(config.compactFlags, CompactMode::FillEnd))
            {
                // Try inserting the thinnest items on every row into the rows before it
                for (auto i = 1; i < rows.size(); i++)
                {
                    auto &srcRow = rows[i];
                    for (auto j = 0; j < i; j++)
                    {
                        auto &destRow = rows[j];
                        auto destSpaceLeft = config.size - destRow.width;
                        for (auto k = 0; k < srcRow.items.size(); k++)
                        {
                            auto index = srcRow.items[k];
                            auto &glyph = glyphs[index];
                            if (glyph.w <= destSpaceLeft)
                            {
                                // Move item to target row
                                destRow.items.push_back(index);
                                srcRow.items.erase(srcRow.items.begin() + k);
                                destSpaceLeft -= glyph.w;
                                destRow.width += glyph.w;
                                srcRow.width -= glyph.w;
                                k--;
                            }
                        }
                    }
                }
            }

            // Update glyph positions
            auto h = 0;
            for (auto i = 0; i < rows.size(); i++)
            {
                auto &row = rows[i];
                auto x = 0;
                auto itemMaxHeight = 0;
                for (auto j = 0; j < row.items.size(); j++)
                {
                    auto &index = row.items[j];
                    auto &glyph = glyphs[index];
                    glyph.x = x;
                    glyph.y = h;
                    x += glyph.w;
                    itemMaxHeight = std::max(itemMaxHeight, glyph.h);
                }
                row.width = x;
                row.height = itemMaxHeight;
                h += itemMaxHeight;
            }
        }
    };
}

#endif