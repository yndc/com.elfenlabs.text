#include <context.h>
#include <error.h>
#include <atlas.h>
#include <algorithm>

namespace Text
{
    // Result<void> Context::GenerateAtlas(int fontIndex)
    // {
    //     std::vector<GlyphGeometry> glyphs;
    //     // FontGeometry is a helper class that loads a set of glyphs from a single font.
    //     // It can also be used to get additional font metrics, kerning information, etc.
    //     FontGeometry fontGeometry(&glyphs);

    //     auto font = faces[fontIndex];
    //     auto msdfFont = msdfgen::FontHandle::adoptFreetypeFont(font->ftFace);
    //     fontGeometry.loadCharset(font->ftFace, 1.0, Charset::ASCII);

    //     const double maxCornerAngle = 3.0;
    //     for (GlyphGeometry &glyph : glyphs)
    //         glyph.edgeColoring(&msdfgen::edgeColoringInkTrap, maxCornerAngle, 0);
    //     // TightAtlasPacker class computes the layout of the atlas.
    //     TightAtlasPacker packer;
    //     // Set atlas parameters:
    //     // setDimensions or setDimensionsConstraint to find the best value
    //     packer.setDimensionsConstraint(DimensionsConstraint::SQUARE);
    //     // setScale for a fixed size or setMinimumScale to use the largest that fits
    //     packer.setMinimumScale(24.0);
    //     // setPixelRange or setUnitRange
    //     packer.setPixelRange(2.0);
    //     packer.setMiterLimit(1.0);
    //     // Compute atlas layout - pack glyphs
    //     packer.pack(glyphs.data(), glyphs.size());
    //     // Get final atlas dimensions
    //     int width = 0, height = 0;
    //     packer.getDimensions(width, height);
    //     // The ImmediateAtlasGenerator class facilitates the generation of the atlas bitmap.
    //     ImmediateAtlasGenerator<
    //         float,                      // pixel type of buffer for individual glyphs depends on generator function
    //         3,                          // number of atlas color channels
    //         msdfGenerator,              // function to generate bitmaps for individual glyphs
    //         BitmapAtlasStorage<byte, 3> // class that stores the atlas bitmap
    //         // For example, a custom atlas storage class that stores it in VRAM can be used.
    //         >
    //         generator(width, height);

    //     // GeneratorAttributes can be modified to change the generator's default settings.
    //     GeneratorAttributes attributes;
    //     generator.setAttributes(attributes);
    //     generator.setThreadCount(4);
    //     // Generate atlas bitmap
    //     generator.generate(glyphs.data(), glyphs.size());
    //     // The atlas bitmap can now be retrieved via atlasStorage as a BitmapConstRef.
    //     // The glyphs array (or fontGeometry) contains positioning data for typesetting text.
    //     success = my_project::submitAtlasBitmapAndLayout(generator.atlasStorage(), glyphs);
    //     // Cleanup
    //     msdfgen::destroyFont(font);

    //     return Result<void>::Success();
    // }

    AtlasBuilder::~AtlasBuilder()
    {
    }

    int AtlasBuilder::Add(std::vector<GlyphRect> &items)
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
}
