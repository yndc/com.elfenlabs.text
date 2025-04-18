#ifndef ATLAS_H
#define ATLAS_H

#include <algorithm>
#include <limits>
#include <vector>
#include <glyph.h>
#include <buffer.h>
#include <base.h>
#include <mathematics.h>

/// @brief Configuration for the atlas packer.
struct AtlasConfig
{
    int size = 1024;     // Width and Height of the square atlas slice
    int padding = 0;     // Padding around each glyph in pixels
    int margin = 1;      // Minimum distance between packed rectangles and the atlas border.
    int glyph_size = 32; // Size of the glyph in pixels (used for scaling)
    int flags;
};

#endif