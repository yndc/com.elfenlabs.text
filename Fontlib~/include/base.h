#ifndef BASE_H
#define BASE_H

typedef unsigned char byte;

namespace Flag
{
    bool has(int flags, int flag)
    {
        return (flags & flag) != 0;
    }
}

#endif