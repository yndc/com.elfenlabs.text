#ifndef ERROR_H
#define ERROR_H

#include <variant>

namespace Text
{
    enum ErrorCode
    {
        Success,

        // General errors
        Failure = 0001,

        // Font errors
        FontNotFound = 1000
    };

    template <class T>
    class Result
    {
        std::variant<T, ErrorCode> data;

    public:
        static Result<T> Fail(ErrorCode code) { return Result<T>(code); }
        static Result<T> Success(T value) { return Result<T>(std::move(value)); }
        Result(T value) : data(std::move(value)) {}
        Result(ErrorCode error) : data(error) {}
        bool IsError() { return std::holds_alternative<ErrorCode>(data); }
        T GetValue() { return std::get<T>(data); }
        ErrorCode GetError() { return std::get<ErrorCode>(data); }
    };

    template <>
    class Result<void>
    {
        Text::ErrorCode error;

    public:
        static Result<void> Fail(ErrorCode code) { return Result<void>(code); }
        static Result<void> Success() { return Result<void>(Text::ErrorCode::Success); }
        Result(Text::ErrorCode error) : error(error) {}
        bool IsError() { return error != Text::ErrorCode::Success; }
        Text::ErrorCode GetError() { return error; }
    };
}

#endif