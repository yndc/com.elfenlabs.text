#ifndef LOG_H
#define LOG_H

#include <sstream>

typedef void (*LogCallback)(const char *message);

class LogStream
{
public:
    LogStream(LogCallback logCallback) : logCallback(logCallback) {}
    ~LogStream()
    {
        if (logCallback != nullptr)
            logCallback(ss.str().c_str());
    }

    template <typename T>
    LogStream &operator<<(const T &value)
    {
        ss << value;
        return *this;
    }

    LogStream &operator<<(std::ostream &(*manip)(std::ostream &))
    {
        ss << "\n";
        return *this;
    }

private:
    std::stringstream ss;
    LogCallback logCallback;
};

#endif