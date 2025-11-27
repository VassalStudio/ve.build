int __stdcall WinMain(void*, void*, const char*, int);
int __cdecl invoke_main()
{
    return WinMain(nullptr,
        nullptr,
        nullptr,
        0);
}