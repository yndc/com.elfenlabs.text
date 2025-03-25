#include "msdfgen.h"

namespace math
{
    struct float2
    {
        float x;
        float y;
        float2(float x, float y) : x(x), y(y) {}
        float2() : x(0), y(0) {}
        // operator msdfgen::Point2() { return msdfgen::Point2(x, y); }
        inline float2 operator-() const { return float2(-x, -y); }
        inline float2 operator+(const float2 &rhs) const { return float2(x + rhs.x, y + rhs.y); }
        inline float2 operator-(const float2 &rhs) const { return float2(x - rhs.x, y - rhs.y); }
        inline bool operator==(const float2 &rhs) const { return x == rhs.x && y == rhs.y; }
        inline bool operator!=(const float2 &rhs) const { return !(*this == rhs); }
    };

    float dot(float2 a, float2 b)
    {
        return a.x * b.x + a.y * b.y;
    }

    float cross(float2 a, float2 b)
    {
        return a.x * b.y - a.y * b.x;
    }
}