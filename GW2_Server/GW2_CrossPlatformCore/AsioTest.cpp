#include "pch.h"          // 반드시 첫 줄
#define ASIO_STANDALONE
#include <iostream>

int main()
{
    asio::io_context ioc;
    std::cout << "Asio OK" << std::endl;
    return 0;
}
