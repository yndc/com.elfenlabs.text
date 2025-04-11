#ifndef BUFFER_H
#define BUFFER_H

#include <cassert>
#include <cstdlib>
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
    Buffer<T>(void *ptr, int32_t sizeBytes, Allocator allocator)
    {
        this->ptr = ptr;
        this->allocator = allocator;
        this->size_bytes = sizeBytes;
    }

    Buffer<T>()
    {
        this->ptr = nullptr;
        this->allocator = Allocator::Invalid;
        this->size_bytes = 0;
    }

    static_assert(std::is_trivially_copyable<T>::value, "T must be a trivially copyable type");

    void Push(const T &value)
    {
        Resize(Count() + 1);
        Data()[Count() - 1] = value;
    }

    /// @brief Disposes the buffer
    void Dispose()
    {
        free(this->ptr);
        this->ptr = nullptr;
        this->size_bytes = 0;
    }

    /// @brief Resizes the buffer, if the new size is smaller than the current size, the function does nothing
    /// @param newSize
    void Resize(int32_t newLength)
    {
        auto newSizeBytes = newLength * sizeof(T);
        if (newSizeBytes <= this->size_bytes)
            return;
        if (this->ptr == nullptr)
            this->ptr = malloc(newSizeBytes);
        else
            this->ptr = realloc(this->ptr, newSizeBytes);
        if (this->ptr == nullptr)
            throw std::bad_alloc();
        this->size_bytes = newSizeBytes;
    }

    /// @brief Casts the buffer to a different type, the new type must be a trivially copyable and the buffer size must be a multiple of the new type's size
    /// @tparam K
    /// @return
    template <typename K>
    Buffer<K> *As()
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