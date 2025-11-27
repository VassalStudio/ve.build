# ve.build.cpp.msvc

> 🚀 **STATUS:** v1.0.0 RC. MSVC Compiler Provider for VEBuild.

**ve.build.cpp.msvc** is the implementation of the C++ compiler abstractions for the Microsoft Visual C++ compiler (**MSVC** / `cl.exe`).

It bridges the generic `ve.build.cpp` configuration API with the specific command-line switches and behaviors of MSVC on Windows.

## ⚠️ Scope

This package **ONLY** provides the **Compiler** implementation (`ICppTool`).
It does not include:
* **Linker:** See [`ve.build.link.msvc`](https://www.nuget.org/packages/ve.build.link.msvc)
* **Project Generators:** See [`ve.build.vcxprojgenerator`](https://www.nuget.org/packages/ve.build.vcxprojgenerator)

Usually, you would install the aggregate package `ve.build.msvc` which includes all of the above.

---

## 📦 Requirements

* **Windows OS** (x64/ARM64)
* **Visual Studio 2022** (Build Tools or IDE) with "Desktop development with C++" workload installed.

The package automatically locates the latest installed version of `cl.exe` using standard VS discovery mechanisms.

---

## ⚙️ Features

### 1. Flag Translation
It implements the `IClConfigurator` interface, translating abstract settings into MSVC-specific flags:

| Abstract Config | MSVC Flag |
| :--- | :--- |
| `.optimization(SPEED)` | `/O2` |
| `.optimization(SIZE)` | `/O1` |
| `.inlineLevel(FORCE)` | `/Ob2` |
| `.languageStandard(CppLatest)` | `/std:c++latest` |
| `.stackCheck(true)` | `/GS` |
| `.addressSanitizer(true)` | `/fsanitize=address` |

### 2. C++20 Modules Support
Fully implements the modern MSVC modules workflow:
* **Scanning:** Uses `/scanDependencies` to generate JSON dependency graphs.
* **BMI Generation:** Automatically adds `/ifcOutput` for module interfaces (`.ixx`).
* **Consumption:** Adds `/reference` pointing to precompiled `.ifc` files from dependencies.

### 3. Debug Information
Supports parallel-friendly debug info generation (`/Z7`) to avoid `pdb` locking issues during multi-threaded builds.

---

## 💻 Usage

If you are using the main `ve.build.msvc` package, this is registered automatically.
However, if you are composing a custom toolchain manually:

```csharp
using ve.build.core;
using ve.build.cpp.cpp;
using ve.build.cpp.msvc; // Import this namespace

return await new HostBuilder()
    .project("sample", p => p
        .Sources("src")
    )
    // Manually register ONLY the compiler (no linker)
    .useMsvc()
    .build()
    .run(args);
````

## 🔍 Internal Logic

The `MsvcTool` class performs the following steps during a build task:

1.  **Discovery:** Finds `cl.exe` path based on the requested target platform (`x64`, `arm64`, `x86`).
2.  **Scanning:** Runs the scanner pass to determine import/export relationships.
3.  **Planning:** Returns a dependency definition to the DAG builder (so module interfaces compile before consumers).
4.  **Compilation:** Executes `cl.exe` with the generated response file (`.rsp`).

## License

© 2025 **VassalStudio**. All rights reserved.