#ifndef MATHEMATICS_H
#define MATHEMATICS_H

namespace math
{
    struct float2
    {
        float x;
        float y;
        float2(float x, float y) : x(x), y(y) {}
        float2() : x(0), y(0) {}
        inline float2 operator-() const { return float2(-x, -y); }
        inline float2 operator+(const float2 &rhs) const { return float2(x + rhs.x, y + rhs.y); }
        inline float2 operator-(const float2 &rhs) const { return float2(x - rhs.x, y - rhs.y); }
        inline bool operator==(const float2 &rhs) const { return x == rhs.x && y == rhs.y; }
        inline bool operator!=(const float2 &rhs) const { return !(*this == rhs); }
    };

    inline float dot(float2 a, float2 b)
    {
        return a.x * b.x + a.y * b.y;
    }

    inline float cross(float2 a, float2 b)
    {
        return a.x * b.y - a.y * b.x;
    }

    template <typename T>
    inline T clamp(T n)
    {
        return n >= T(0) && n <= T(1) ? n : T(n > T(0));
    }

    inline double fracPixelToPixel(int frac)
    {
        return frac / 64.0;
    }

    inline int roundFracPixel(int frac)
    {
        return frac << 6;
    }

    inline int pixelToFracPixel(int pixel)
    {
        return pixel * 64;
    }
}

#endif