# VEBuild

[![NuGet](https://img.shields.io/nuget/v/VEBuild.msvc.svg)](https://www.nuget.org/packages/VEBuild.msvc)
![Status](https://img.shields.io/badge/status-RC1-green)
![Platform](https://img.shields.io/badge/platform-windows--x64-blue)

**VEBuild** is a modern, modular build system for **C++20** written in C# (.NET 9.0+).

It replaces CMake logic with a **fluent, type-safe C# API**. It treats the file system as the source of truth, handles C++20 Modules natively, and integrates seamlessly with Visual Studio 2022 without constant project reloading.

---

## ✨ Why VEBuild?

* **C# as a Build Script:** Use the full power of .NET to describe your build logic. No more obscure CMake syntax.
* **C++20 Modules First:** Out-of-the-box support for `.ixx`, dependency scanning, and BMI generation (`.ifc`).
* **Frictionless Workflow:** Add a file to your folder -> Hit Build. No generation steps, no "Reload Project".
* **Smart IDE Integration:** Generates VS Solutions (`.sln`) that link your C++ code with your C# build tool. Debug your build script and your game in the same session.
* **Incremental:** Fast, hash-based change detection for compilation and linking.

---

## 🚀 Getting Started

To start a new C++ project (e.g., a Game Engine or Application), you don't need `CMakeLists.txt`. You create a C# Console App.

### 1. Create a "Build" Project

```bash
# Create a folder for your solution
mkdir MyGame && cd MyGame

# Create the C# build tool
dotnet new console -n MyGame.Build

# Install the MSVC Toolchain package
cd MyGame.Build
dotnet add package ve.build.msvc
````

### 2\. Configure Your Build (`Program.cs`)

Describe your C++ projects using the fluent API.

```csharp
using ve.build.core;
using ve.build.cpp.cpp;   // C++ extensions
using ve.build.link.link; // Linker extensions
using ve.build.msvc;      // MSVC Toolchain

return await new HostBuilder()
    // 1. Define a Shared Library (DLL)
    .project("ve.core", p => p
        .Type(ProjectType.DLL)
        .Sources("src/core") // Scans for .cpp, .ixx, .h recursively
    )

    // 2. Define an Executable (Game)
    .project("my.game", p => p
        .Type(ProjectType.EXE)
        .Sources("src/game")
        // Link against the core library
        // Automatically propagates includes, module interfaces (.ifc), and defines
        .dependsOf("ve.core") 
    )

    // 3. Use the MSVC Toolchain (cl.exe, link.exe)
    .useMsvcToolchain()
    
    .build()
    .run(args);
```

### 3\. Run the Build

```bash
# Compiles and Links everything (default: DEBUG, windows-x64)
dotnet run -- build
```

### 4\. Generate Visual Studio Solution

To work comfortably in IDE with IntelliSense and Debugging:

```bash
dotnet run -- generateProjectFiles
```

This creates `MyGame.sln`. Open it in Visual Studio 2022.

  * **F5** will build your C\# tool, then build your C++ project, and finally launch the game.
  * **IntelliSense** works correctly for `#include` and `import module`.

-----

## 📦 Packages

VEBuild is distributed as a set of modular NuGet packages.

| Package | Description |
| :--- | :--- |
| **`ve.build.msvc`** | **Start Here.** The meta-package that includes the Compiler, Linker, and Generator for Windows. |
| `ve.build.core` | The core task engine and DAG builder. |
| `ve.build.cpp` | C++ language abstractions and configuration API. |
| `ve.build.link` | Linker abstractions (StaticLib, DLL, EXE). |
| `ve.build.vcxprojgenerator` | Generator for Visual Studio `.vcxproj` and `.sln` files. |

-----

## 🏗️ Architecture

### Dependency Graph (DAG)

Every build operation (Compile source, Link exe, Copy dll) is a node in a Directed Acyclic Graph. VEBuild executes independent nodes in parallel across all available CPU cores.

### C++20 Module Scanning

The system uses `cl.exe /scanDependencies` to discover module import/export relationships dynamically. It ensures that module interfaces (`.ifc`) are compiled before the code that consumes them.

### "Ghost" References

For NMake projects in Visual Studio, VEBuild automatically injects module source files from dependent projects as links. This forces VS IntelliSense to index them, enabling "Go To Definition" across project boundaries.

-----

## 🗺️ Roadmap

  * **v1.0.0 RC (Current):** Windows (MSVC) support complete.
  * **v1.1.0:** Clang support (Windows/Linux) & C++26 Reflection integration.
  * **v1.2.0:** MacOS support.
  * **Future:** WASM, Android, Consoles.

## License

© 2025 **VassalStudio**. All rights reserved.