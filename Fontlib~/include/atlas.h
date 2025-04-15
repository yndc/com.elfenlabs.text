#ifndef ATLAS_H
#define ATLAS_H

#include <algorithm>
#include <limits>
#include <vector>
#include <glyph.h>
#include <buffer.h>
#include <base.h>
#include <mathematics.h>

namespace Text
{
    /**
     * @brief Implements a dynamic 2D texture atlas packer using the Skyline (Bottom-Left) algorithm.
     *
     * Manages packing rectangles into a single square atlas slice. Input dimensions are assumed
     * to include any desired padding around the glyph. The 'margin' config setting ensures
     * minimum spacing between packed rectangles and between rectangles and the atlas border.
     * Designed for incremental additions. Assumes an internal coordinate system where Y=0 is the top edge.
     * Outputs coordinates where Y=0 is the bottom edge.
     */
    class DynamicAtlasPacker
    {
    private:
        /**
         * @brief Represents a horizontal segment of the skyline.
         */
        struct SkylineNode
        {
            int x;     // Left coordinate (inclusive) of the skyline segment.
            int y;     // Height (y-coordinate) of the segment's top edge (the floor below it).
            int width; // Width of the skyline segment.
        };

    public:
        /**
         * @brief Configuration for the atlas packer.
         */
        struct Config
        {
            int size = 1024; // Width and Height of the square atlas slice
            int margin = 1;  // Minimum distance between packed rectangles and the atlas border.
        };

        /**
         * @brief Constructs a packer for a single atlas slice.
         * @param config Configuration settings for the atlas dimensions and margin.
         */
        DynamicAtlasPacker(Config config) : config(config)
        {
            // No config validation as requested. Assume valid inputs.
            // Initialize the skyline respecting the margin.
            if (config.size > 2 * config.margin)
            {
                skyline.push_back({config.margin, config.margin, config.size - 2 * config.margin});
            }
            // If size <= 2*margin, skyline remains empty and packing will fail.
        }

        /**
         * @brief Attempts to add multiple glyphs into the current atlas slice.
         *
         * Iterates through the provided buffer of glyphs. Assumes all glyphs need placement.
         * Input dimensions (atlas_width_px, atlas_height_px) should include padding.
         * The margin is added *internally* before packing to ensure spacing.
         * If successful, updates the glyph's atlas_x_px and atlas_y_px fields
         * (representing the bottom-left corner of the margin-added rectangle in a Y-up coordinate system).
         *
         * @param glyphs A buffer containing glyph metrics. Input dimensions are read,
         * output coordinates (atlas_x_px, atlas_y_px) are written back.
         * @return The number of glyphs successfully placed into *this slice* during this call.
         */
        int PackGlyphs(Buffer<GlyphMetrics> &glyphs)
        {
            int placed_count = 0;
            if (skyline.empty())
            {
                return 0;
            }

            for (size_t i = 0; i < glyphs.Count(); ++i)
            {
                GlyphMetrics &glyph = glyphs[i];

                // Calculate node dimensions including the margin for placement spacing
                int node_w = glyph.atlas_width_px + config.margin;
                int node_h = glyph.atlas_height_px + config.margin;

                // Skip glyphs with invalid dimensions (or zero size after margin)
                // Note: Input atlas_width/height_px should be >0 if glyph exists
                if (node_w <= config.margin || node_h <= config.margin)
                {
                    glyph.atlas_x_px = -1;
                    glyph.atlas_y_px = -1;
                    continue;
                }

                // Try to find the best position (bottom-left heuristic) for this margin-added node
                int best_x = -1, best_y = -1; // Internal coords (Top-Left, Y-Down)
                if (FindPosition(node_w, node_h, best_x, best_y))
                {
                    // Position found! Place the node and update the skyline structure
                    PlaceNodeAndUpdateSkyline(best_x, best_y, node_w, node_h);

                    // --- Assign final position, converting Y coordinate ---
                    // best_x is the left edge of the margin-added rectangle.
                    glyph.atlas_x_px = best_x;
                    // best_y is the top edge (Y-down) of the margin-added rectangle.
                    // Convert its bottom edge to Y-up coordinate system.
                    glyph.atlas_y_px = config.size - (best_y + node_h);
                    // --- The output coords (x,y) are the bottom-left of the margin-added box ---

                    placed_count++;
                }
                else
                {
                    // Mark glyph as not placed *in this slice*
                    glyph.atlas_x_px = -1;
                    glyph.atlas_y_px = -1;
                }
            }
            return placed_count;
        }

        /**
         * @brief Provides a simple heuristic to estimate if the atlas is likely full.
         * Checks if the highest point on the skyline is close to the bottom edge.
         * @param estimated_max_glyph_height An estimate of the height of glyphs still to be placed (should include padding+margin).
         * @return True if the atlas is considered full based on the heuristic, false otherwise.
         */
        bool IsFull(int estimated_max_glyph_height) const
        {
            if (skyline.empty())
                return true;
            int max_y = 0;
            for (const auto &node : skyline)
            {
                max_y = std::max(max_y, node.y);
            }
            estimated_max_glyph_height = std::max(0, estimated_max_glyph_height);
            // Check against the available height inside the margins
            return max_y + estimated_max_glyph_height > (config.size - config.margin);
        }

        /**
         * @brief Resets the packer state to an empty atlas (single skyline node at the top).
         */
        void Reset()
        {
            skyline.clear();
            if (config.size > 2 * config.margin)
            {
                skyline.push_back({config.margin, config.margin, config.size - 2 * config.margin});
            }
        }

        /**
         * @brief Gets the configuration used by the packer.
         */
        const Config &GetConfig() const
        {
            return config;
        }

        /**
         * @brief Gets the current skyline state (for debugging or visualization).
         * Uses the fully qualified name for the nested struct. Coordinates are internal (Y-down).
         */
        const std::vector<DynamicAtlasPacker::SkylineNode> &GetSkyline() const
        {
            return skyline;
        }

    private:
        // SkylineNode struct definition moved above public section

        std::vector<SkylineNode> skyline; // Stores skyline segments, kept sorted by x coordinate. Uses Y-down coords.
        Config config;                    // Packer configuration.

        // --- Internal Helper Methods (Operate in Y-down coordinate system) ---

        /**
         * @brief Finds the best position (bottom-left heuristic) for a rectangle.
         * @param node_w Width of the rectangle to place (includes margin).
         * @param node_h Height of the rectangle to place (includes margin).
         * @param out_x Output parameter for the best X coordinate found (top-left).
         * @param out_y Output parameter for the best Y coordinate found (top-left, Y-down).
         * @return True if a suitable position was found, false otherwise.
         */
        bool FindPosition(int node_w, int node_h, int &out_x, int &out_y)
        {
            if (skyline.empty())
                return false;

            int best_y = config.size; // Initialize best Y to max height
            int best_x = -1;
            int best_node_index = -1;

            for (size_t i = 0; i < skyline.size(); ++i)
            {
                int current_y = 0; // Floor height if placement starts at skyline_[i].x

                if (CanPlaceHorizontally(i, node_w, current_y))
                {
                    // Check vertical fit (top_y + height <= max_height - margin)
                    if (current_y + node_h <= config.size - config.margin)
                    {
                        // Check if better than current best (lower Y, then leftmost X)
                        if (current_y < best_y || (current_y == best_y && skyline[i].x < best_x))
                        {
                            best_y = current_y;
                            best_x = skyline[i].x;
                            best_node_index = static_cast<int>(i);
                        }
                    }
                }
            }

            if (best_x != -1)
            {
                out_x = best_x;
                out_y = best_y;
                return true;
            }

            out_x = -1;
            out_y = -1;
            return false;
        }

        /**
         * @brief Checks if a rectangle of width 'node_w' fits horizontally starting at 'start_node_index'.
         * @param start_node_index The index of the skyline node to start checking from.
         * @param node_w The width of the rectangle to fit (includes margin).
         * @param out_y Output parameter updated with the maximum Y (highest floor height) in the span.
         * @return True if the rectangle fits horizontally, false otherwise.
         */
        bool CanPlaceHorizontally(size_t start_node_index, int node_w, int &out_y)
        {
            if (start_node_index >= skyline.size())
                return false;

            int current_x = skyline[start_node_index].x;
            // Check horizontal boundary (respecting right margin)
            if (current_x + node_w > config.size - config.margin)
            {
                return false;
            }

            int max_y_in_span = 0;
            int width_covered = 0;
            for (size_t i = start_node_index; i < skyline.size(); ++i)
            {
                max_y_in_span = std::max(max_y_in_span, skyline[i].y);
                width_covered = (skyline[i].x + skyline[i].width) - current_x;

                if (width_covered >= node_w)
                {
                    out_y = max_y_in_span;
                    return true; // Fits
                }
                // Check for horizontal gap or end of skyline
                if (i + 1 >= skyline.size() || skyline[i + 1].x != (skyline[i].x + skyline[i].width))
                {
                    return false; // Doesn't fit contiguously
                }
            }
            return false; // Shouldn't be reached if initial width check is correct
        }

        /**
         * @brief Updates the skyline structure after placing a rectangle.
         * @param x The left coordinate of the placed rectangle (top-left X).
         * @param y The top coordinate of the placed rectangle (top-left Y, Y-down).
         * @param node_w The width of the placed rectangle (includes margin).
         * @param node_h The height of the placed rectangle (includes margin).
         */
        void PlaceNodeAndUpdateSkyline(int x, int y, int node_w, int node_h)
        {
            SkylineNode placed_node_top = {x, y + node_h, node_w};
            size_t current_index = 0;

            // Phase 1: Remove/Split existing nodes covered by the new node's footprint
            while (current_index < skyline.size())
            {
                SkylineNode &skyline_node = skyline[current_index];
                int intersect_start = std::max(placed_node_top.x, skyline_node.x);
                int intersect_end = std::min(placed_node_top.x + placed_node_top.width, skyline_node.x + skyline_node.width);

                if (intersect_start >= intersect_end)
                { // No overlap
                    if (skyline_node.x >= placed_node_top.x + placed_node_top.width)
                        break; // Past the placed node
                    current_index++;
                    continue;
                }
                if (placed_node_top.y <= skyline_node.y)
                { // Placed node is below this segment
                    current_index++;
                    continue;
                }

                // Overlap detected and new node is higher
                if (placed_node_top.x <= skyline_node.x && (placed_node_top.x + placed_node_top.width) >= (skyline_node.x + skyline_node.width))
                {
                    // Case 1: Full cover -> Erase and re-evaluate index
                    skyline.erase(skyline.begin() + current_index);
                    continue;
                }
                else if (placed_node_top.x > skyline_node.x && (placed_node_top.x + placed_node_top.width) < (skyline_node.x + skyline_node.width))
                {
                    // Case 2: Split -> Create right part, adjust left part, insert right, then break loop
                    SkylineNode right_part = {placed_node_top.x + placed_node_top.width, skyline_node.y, (skyline_node.x + skyline_node.width) - (placed_node_top.x + placed_node_top.width)};
                    skyline_node.width = placed_node_top.x - skyline_node.x; // Adjust left part
                    skyline.insert(skyline.begin() + current_index + 1, right_part);
                    break;
                }
                else if (placed_node_top.x <= skyline_node.x && (placed_node_top.x + placed_node_top.width) < (skyline_node.x + skyline_node.width))
                {
                    // Case 3: Overlap left -> Adjust node to become the right remainder, continue check
                    int original_end_x = skyline_node.x + skyline_node.width;
                    skyline_node.x = placed_node_top.x + placed_node_top.width;
                    skyline_node.width = original_end_x - skyline_node.x;
                    current_index++;
                    continue;
                }
                else if (placed_node_top.x > skyline_node.x && (placed_node_top.x + placed_node_top.width) >= (skyline_node.x + skyline_node.width))
                {
                    // Case 4: Overlap right -> Adjust node to become the left remainder, continue check
                    skyline_node.width = placed_node_top.x - skyline_node.x;
                    current_index++;
                    continue;
                }
                else
                {
                    current_index++; // Should not happen
                }
            }

            // Phase 2: Insert the new node for the top edge
            size_t insert_pos = 0;
            while (insert_pos < skyline.size() && skyline[insert_pos].x < placed_node_top.x)
            {
                insert_pos++;
            }
            skyline.insert(skyline.begin() + insert_pos, placed_node_top);

            // Phase 3: Merge adjacent nodes
            MergeSkyline();
        }

        /**
         * @brief Merges adjacent skyline nodes that have the same y-coordinate.
         */
        void MergeSkyline()
        {
            for (size_t i = 0; i + 1 < skyline.size(); /* no increment */)
            {
                if (skyline[i].y == skyline[i + 1].y && (skyline[i].x + skyline[i].width) == skyline[i + 1].x)
                {
                    skyline[i].width += skyline[i + 1].width;
                    skyline.erase(skyline.begin() + i + 1);
                    // Re-check merge possibility at index i
                }
                else
                {
                    ++i; // Move to next node only if no merge occurred
                }
            }
        }
    };
}

#endif