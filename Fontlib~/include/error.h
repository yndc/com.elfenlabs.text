#ifndef ERROR_H
#define ERROR_H

#include <variant>

enum ReturnCode
{
    Success,

    // General errors
    Failure = 0001,

    // Font errors
    FontNotFound = 1000,

    // Shaping errors
    ShapingOutTooSmall = 2000
};

template <class T>
class Result
{
    std::variant<T, ReturnCode> data;

public:
    static Result<T> Fail(ReturnCode code) { return Result<T>(code); }
    static Result<T> Success(T value) { return Result<T>(std::move(value)); }
    Result(T value) : data(std::move(value)) {}
    Result(ReturnCode error) : data(error) {}
    bool IsError() { return std::holds_alternative<ReturnCode>(data); }
    T GetValue() { return std::get<T>(data); }
    ReturnCode GetError() { return std::get<ReturnCode>(data); }
};

template <>
class Result<void>
{
    ReturnCode error;

public:
    static Result<void> Fail(ReturnCode code) { return Result<void>(code); }
    static Result<void> Success() { return Result<void>(ReturnCode::Success); }
    Result(ReturnCode error) : error(error) {}
    bool IsError() { return error != ReturnCode::Success; }
    ReturnCode GetError() { return error; }
};

#endif