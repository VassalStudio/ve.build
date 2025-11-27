# ve.build.link.msvc

> 🚀 **STATUS:** v1.0.0 RC. MSVC Linker Provider for VEBuild.

**ve.build.link.msvc** is the implementation of linker abstractions for the Microsoft Visual C++ (**MSVC**) toolchain.

This package is responsible for invoking `link.exe` (for creating `.exe` and `.dll`) and `lib.exe` (for creating static libraries `.lib`). It automatically discovers paths to Windows SDK system libraries and configures the environment.

## 📦 Requirements

* **OS:** Windows (x64/ARM64).
* **Tools:** Visual Studio 2022 (or Build Tools) with C++ components installed.
* **SDK:** Windows 10/11 SDK (automatically discovered via Registry).

## ⚙️ Features

This package implements the `ILinkTool` interface and provides the following capabilities:

### 1. Auto-Discovery
You do not need to manually specify paths to `link.exe` or system `.lib` files.
The package uses `vswhere.exe` and the Windows Registry to locate:
* The latest version of MSVC Tools.
* Windows SDK (`ucrt` and `um` folders).

### 2. Platform Support
The package automatically registers platforms in `ve.build` depending on the host architecture and installed components:

* **Desktop:** `windows-x64`, `windows-x86`, `windows-arm64`.
* **UEFI Development:** `efi-x64`, `efi-x86`, `efi-arm64` (generates files with `.efi` extension and `EFI_APPLICATION` subsystem).

### 3. Configuration Mapping
Translates abstract settings from `ve.build.link` into specific MSVC flags:

| Abstraction (`ILinkConfigurator`) | MSVC Flag |
| :--- | :--- |
| `.enableDebugInformation(...)` | `/DEBUG:FULL`, `/DEBUG:FASTLINK` |
| `.enableASLR(true)` | `/DYNAMICBASE` |
| `.ltcg(true)` | `/LTCG` |
| `.entryPoint("main")` | `/ENTRY:main` |
| `.subsystem(...)` | `/SUBSYSTEM:WINDOWS`, `/SUBSYSTEM:CONSOLE`, etc. |
| `.stack(reserve, commit)` | `/STACK:reserve,commit` |

---

## 💻 Usage

Typically, this package is used as part of the `ve.build.msvc` meta-package.
However, you can reference it separately:

```csharp
using ve.build.core;
using ve.build.link.link;
using ve.build.link.msvc.msvc; // Extension namespace

return await new HostBuilder()
    .project("sample", p => p
        .Type(ProjectType.EXE)
        .Sources("src")
    )
    // Register MSVC Linker
    .useMsvcLink(
        // Global configuration for LIB (Static Library)
        lib => {
            lib.extraFlag("/NOLOGO");
        },
        // Global configuration for LINK (Exe/Dll)
        link => {
            link.enableDebugInformation(DebugInformation.FULL);
            link.enableASLR(true);
        }
    )
    .build()
    .run(args);
````

## 🔧 Internal Logic

The `MsvcLinkerExtension` class performs the following steps:

1.  Checks if running on Windows.
2.  Searches for Visual Studio and Windows SDK installation paths.
3.  Registers platforms (`windows-x64`, etc.).
4.  Creates instances of `MsvcLinkTool` for each platform with correct paths to `bin` (host/target) and `lib`.

## License

© 2025 **VassalStudio**. All rights reserved.