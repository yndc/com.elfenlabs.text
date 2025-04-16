#ifndef BUFFER_H
#define BUFFER_H

#include <cassert>
#include <cstdlib>
#include <cstring>
#include <stdint.h>
#include <stdexcept>
#include <type_traits>
#include <string>

enum Allocator : std::int32_t
{
    Invalid = 0,
    None = 1,
    Temp = 2,
    TempJob = 3,
    Persistent = 4,
    AudioKernel = 5,
    Domain = 6
};

typedef void *(*AllocCallback)(int size, int alignment, Allocator allocator);
typedef void (*DisposeCallback)(void *ptr, Allocator allocator);

/// @brief Unmanaged general-purpose buffer intended for use with C APIs
/// @tparam T
#pragma pack(push, 8)
template <typename T>
class Buffer
{
private:
    void *ptr;
    Allocator allocator;
    int32_t size_bytes;

public:
    Buffer<T>(void *ptr, int32_t size_bytes, Allocator allocator)
    {
        this->ptr = ptr;
        this->allocator = allocator;
        this->size_bytes = size_bytes;
    }

    Buffer<T>()
    {
        this->ptr = nullptr;
        this->allocator = Allocator::Invalid;
        this->size_bytes = 0;
    }

    static_assert(std::is_trivially_copyable<T>::value, "T must be a trivially copyable type");

    /// @brief Casts the buffer to a different type, the new type must be a trivially copyable and the buffer size must be a multiple of the new type's size
    /// @tparam K
    /// @return
    template <typename K>
    Buffer<K> *ReinterpretCast()
    {
        static_assert(std::is_trivially_copyable<K>::value, "K must be a trivially copyable type");
        return (Buffer<K> *)this;
    }

    const int32_t Count()
    {
        return size_bytes / sizeof(T);
    }

    const int32_t SizeInBytes()
    {
        return size_bytes;
    }

    T *Data()
    {
        return (T *)ptr;
    }

    const T *Data() const
    {
        return (T *)ptr;
    }

    T &operator[](int32_t index)
    {
        assert(index * sizeof(T) < size_bytes);
        return ((T *)ptr)[index];
    }

    const T &operator[](int32_t index) const
    {
        assert(index * sizeof(T) < size_bytes);
        return ((T *)ptr)[index];
    }

    // Iterator support
    T *begin() { return Data(); }
    T *end() { return Data() + Count(); }
    const T *begin() const { return Data(); }
    const T *end() const { return Data() + Count(); }

    // Stream-like writer
    class Writer
    {
    private:
        void *dst;
        int32_t byte_index;
        int byte_size;

    public:
        Writer(Buffer<T> *buffer)
            : byte_index(0),
              byte_size(buffer->size_bytes),
              dst(buffer->ptr)
        {
        }

        template <typename T, typename U>
        void Write(const U &data)
        {
            static_assert(std::is_trivially_copyable<U>::value, "Read target type U must be trivially copyable");

            if (byte_index + static_cast<int32_t>(sizeof(U)) > byte_size)
            {
                throw std::out_of_range("Buffer overflow in Writer::Write");
            }

            memcpy(static_cast<unsigned char *>(dst) + byte_index, &data, sizeof(U));
            byte_index += sizeof(U);
        }
    };

    class Reader
    {
    private:
        const void *src;
        int32_t byte_index;
        int byte_size;

    public:
        Reader(const Buffer<T> *buffer)
            : src(buffer->Data()),
              byte_index(0),
              byte_size(buffer->size_bytes)
        {
            // Ensure buffer pointer is valid if size > 0
            if (byte_size > 0 && src == nullptr)
            {
                throw std::invalid_argument("Cannot create Reader from null buffer with non-zero size.");
            }
        }

        template <typename U>
        U Read()
        {
            static_assert(std::is_trivially_copyable<U>::value, "Read target type U must be trivially copyable");

            if (byte_index + static_cast<int32_t>(sizeof(U)) > byte_size)
            {
                throw std::out_of_range("Buffer underflow in Reader::Read");
            }

            U result;
            const unsigned char *read_ptr = static_cast<const unsigned char *>(src) + byte_index;
            memcpy(&result, read_ptr, sizeof(U));

            byte_index += sizeof(U);

            return result;
        }

        /**
         * @brief Gets the current read position (byte offset from the start).
         */
        int32_t Tell() const
        {
            return byte_index;
        }

        /**
         * @brief Gets the number of bytes remaining in the buffer.
         */
        int32_t BytesRemaining() const
        {
            return byte_size - byte_index;
        }

        /**
         * @brief Checks if there are enough bytes remaining to read a value of type U.
         * @tparam U The type of data to potentially read.
         * @return True if enough bytes remain, false otherwise.
         */
        template <typename U>
        bool CanRead() const
        {
            return byte_index + static_cast<int32_t>(sizeof(U)) <= byte_size;
        }

        /**
         * @brief Skips forward a number of bytes without reading.
         * @param bytes_to_skip The number of bytes to advance the read position.
         * @throws std::out_of_range if skipping would exceed buffer bounds.
         */
        void Skip(int32_t bytes_to_skip)
        {
            if (bytes_to_skip < 0)
            {
                throw std::invalid_argument("Cannot skip negative bytes in Reader::Skip");
            }
            if (byte_index + bytes_to_skip > byte_size)
            {
                throw std::out_of_range("Buffer underflow in Reader::Skip");
            }
            byte_index += bytes_to_skip;
        }
    }
};
#pragma pack(pop)

/// @brief Buffer for output data
/// @tparam T
template <typename T>
class OutBuffer final
{
public:
    OutBuffer(void *dest) : dest(static_cast<Buffer<T> *>(dest)) {}
    OutBuffer(Buffer<T> *dest) : dest(dest) {}

    void Assign(Buffer<T> buffer)
    {
        *dest = buffer;
    }

    T &operator[](int32_t index)
    {
        return (*dest)[index];
    }

    const T &operator[](int32_t index) const
    {
        return (*dest)[index];
    }

    // static_assert(sizeof(OutBuffer<T>) == sizeof(Buffer<T> *), "OutBuffer must remain a single pointer");

private:
    Buffer<T> *dest;
    OutBuffer() = delete; // Prevent accidental use
};

/// @brief Dummy type to check that OutBuffer remains a single pointer
/// @tparam T
template <typename T>
struct OutBufferChecker
{
    static_assert(sizeof(OutBuffer<T>) == sizeof(Buffer<T> *), "OutBuffer must remain a single pointer");
};
static OutBufferChecker<int32_t> dummy; // Forces instantiation

#endif