# ve.build.msvc

> 🚀 **STATUS:** v1.0.0 RC. The "All-in-One" MSVC Toolchain for VEBuild.

**ve.build.msvc** is a meta-package that aggregates all necessary components to build C++ projects on Windows using the Microsoft Visual C++ toolchain.

Instead of installing and configuring the compiler, linker, and project generator separately, this package provides a single entry point to set up a complete production-ready environment.

---

## 📦 What's Included?

Installing this package automatically pulls in the following dependencies:

| Package | Role | Description |
| :--- | :--- | :--- |
| **[`ve.build.cpp.msvc`](https://www.nuget.org/packages/ve.build.cpp.msvc)** | **Compiler** | Provides `cl.exe` support, C++20 Modules scanning, and flag translation. |
| **[`ve.build.link.msvc`](https://www.nuget.org/packages/ve.build.link.msvc)** | **Linker** | Provides `link.exe` / `lib.exe` support and Windows SDK discovery. |
| **[`ve.build.vcxprojgenerator`](https://www.nuget.org/packages/ve.build.vcxprojgenerator)** | **Generator** | Enables generation of Visual Studio 2022 solutions (`.sln`) and projects (`.vcxproj`). |

---

## 📦 Installation

This is usually the **only** package you need to reference for Windows development.

```bash
dotnet add package ve.build.msvc
````

## ⚡ Usage

This package exposes the `.useMsvcToolchain()` extension method on the `HostBuilder`.

### One-Liner Setup

```csharp
using ve.build.core;
using ve.build.msvc; // Import this namespace

return await new HostBuilder()
    .project("my_app", p => p
        .Type(ProjectType.EXE)
        .Sources("src")
    )
    
    // 👇 Magic happens here
    // Registers Compiler, Linker, and VS Generator in one go.
    .useMsvcToolchain()
    
    .build()
    .run(args);
```

-----

## ⚙️ What `.useMsvcToolchain()` does

When you call this method, it performs the following registration steps internally:

1.  **Registers `MsvcTool`:** Sets up the C++ compiler provider (implementation of `ICppTool`).
2.  **Registers `MsvcLinker`:** Sets up the Linker/Librarian provider (implementation of `ILinkTool`) with auto-discovery of Windows SDK paths.
3.  **Registers `VcxprojGenerator`:** Sets up the Project Generator, enabling the `dotnet run -- generate` command.

This ensures that your build script can:

  * Compile code (`dotnet run -- build`)
  * Link executables
  * Generate IDE files (`dotnet run -- generate`)

...without any additional configuration code.

## 📝 Requirements

  * **OS:** Windows x64/ARM64.
  * **Software:** Visual Studio 2022 (Community/Pro/Ent) or Build Tools for Visual Studio 2022.
  * **Workload:** "Desktop development with C++".

## License

© 2025 **VassalStudio**. All rights reserved.