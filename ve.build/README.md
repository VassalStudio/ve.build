# VEBuild (v1.0.0 RC)

> 🚀 **STATUS:** Release Candidate 1. Core architecture, C++20 Modules support, incremental builds, and IDE integration are feature-complete.

**VEBuild** is a next-generation, modular build system for **Modern C++ (C++20)** written in .NET. It prioritizes **Developer Experience (DX)**, performance, and seamless integration with Visual Studio.

It eliminates the pain of CMake by treating the **filesystem as the source of truth** and providing a fluent, type-safe C# DSL for configuration.

---

## ✨ Key Features

* **C++20 Modules First:** Automatic dependency scanning, DAG construction, and BMI (`.ifc`) management out of the box.
* **Incremental Builds:** Smart change detection (timestamp & content hashing) for compilation and linking.
* **Project-to-Project Dependencies:** Automatic propagation of include paths, module interfaces, and defines between libraries and executables.
* **IDE Integration:** Generates **Visual Studio 2022** solutions (`.sln`) and projects (`.vcxproj`) with working **IntelliSense** for modules and cross-project navigation (Go To Definition).
* **No "Reload Project":** Add files to your `src` folder, hit Build, and it just works.
* **Modular Architecture:** Functionality is composed via NuGet packages.

---

## 📦 Modular Ecosystem

VEBuild is designed as a set of decoupled extensions. You only pull what you need.

| Package | Role | Extensions Provided |
| :--- | :--- | :--- |
| **`ve.build.core`** | The engine, Task Graph, HostBuilder | Base `project()`, `task()` |
| **`ve.build.cpp`** | C++ compilation rules | `.Sources()`, `.Include()` |
| **`ve.build.link`** | Linker abstractions | `.Type(ProjectType...)`, `.dependsOf()` |
| **`ve.build.msvc`** | MSVC Toolchain implementation | `.useMsvcToolchain()` |

---

## 🚀 Quick Start

This example demonstrates a multi-project setup: a Shared Library (`sample.lib`) and an Executable (`sample`) that links against it.

### 1. Project Setup

Create a new Console Application and reference the required NuGet packages.

### 2. Configuration (`Program.cs`)

```csharp
using ve.build.core;
using ve.build.cpp.cpp;   // Extends ProjectBuilder with C++ features
using ve.build.link.link; // Extends ProjectBuilder with Linking features (Type, dependsOf)
using ve.build.msvc;      // Provides MSVC Toolchain

return await new HostBuilder()
    // Define a Dynamic Library (DLL)
    // Source files are automatically scanned from the "lib" folder
    .project("sample.lib", pbuilder => pbuilder
        .Type(ProjectType.DLL)
        .Sources("lib")
    )
    
    // Define an Executable
    // Depends on "sample.lib": automatically inherits includes and modules
    .project("sample", pbuilder => pbuilder
        .Type(ProjectType.EXE)
        .Sources() // Scans "src" by default
        .dependsOf("sample.lib")
    )
    
    // Register the MSVC Toolchain (cl.exe, link.exe)
    .useMsvcToolchain()
    
    .build()
    .run(args);
````

-----

## 🛠️ CLI Commands

Run your build script to execute tasks:

```bash
# Build the project (default config: DEBUG, platform: windows-x64)
dotnet run -- build

# Rebuild (Clean + Build)
dotnet run -- rebuild

# Clean artifacts
dotnet run -- clean

# Generate Visual Studio Solution (.sln) and Projects (.vcxproj)
dotnet run -- generate
```

-----

## 🏗️ Architecture Highlights

### The "Unified DAG"

VEBuild constructs a Directed Acyclic Graph (DAG) that represents the entire build process. Tasks like `build` or `clean` generate their own sub-graphs, which are merged and synchronized via barrier nodes. This ensures maximum parallelism (e.g., compiling independent `.cpp` files on all cores) while respecting dependencies (linking happens only after compilation).

### C++20 Module Support

The system natively handles the complexity of C++ modules:

1.  **Scanning:** Uses `/scanDependencies` to discover module imports/exports.
2.  **Ordering:** Compiles module interfaces (`.ixx`) before their consumers.
3.  **Propagation:** Passes generated `.ifc` files to dependent projects automatically.

-----

## 🗺️ Roadmap

  * **v1.0.0 RC (Current):** Windows (MSVC), C++20 Modules, VS Generator.
  * **v1.1.0:** Clang support (Windows/Linux) and C++26 Reflection integration.
  * **v1.2.0:** MacOS support and Xcode generation.
  * **Future:** WASM, Android, and Console support.

## License

© 2025 **VassalStudio**. All rights reserved.