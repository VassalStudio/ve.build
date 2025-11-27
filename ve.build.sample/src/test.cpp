import test2;
import testlib;
#include "test.h"
int test();
int __stdcall WinMain(void*, void*, const char*, int)
{
	MyFunc();
	TestLib();
	const auto testå = NULL;
	return test();
}