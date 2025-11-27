# ve.build.cpp

> 🚀 **STATUS:** v1.0.0 RC. Part of the **VEBuild** ecosystem.

**ve.build.cpp** provides the C++ language abstractions, configuration API, and source management logic for the VEBuild system.

It acts as the **C++ Frontend**: it knows *what* a C++ project looks like (sources, headers, modules, defines, optimization settings) but delegates the actual execution to a specific Toolchain provider.

## ⚠️ Important

**This package does not contain a compiler.**
To build projects, you must install a Toolchain implementation package alongside this one:

* **Windows (MSVC):** Install [`ve.build.msvc`](https://www.nuget.org/packages/ve.build.msvc)
* **Cross-Platform (Clang):** *(Coming soon)*

---

## 📦 Installation

```bash
dotnet add package ve.build.cpp
````

## ⚡ Usage

This package extends the `ProjectBuilder` with methods to define C++ artifacts and compilation rules.

### Basic Setup

```csharp
using ve.build.core;
using ve.build.cpp.cpp; // Import extensions

return await new HostBuilder()
    .project("my_library", p => p
        // Define source directory (recursive scan)
        .Sources("src") 
        // Add Preprocessor Definitions
        .Define("MY_LIB_EXPORTS")
        .Define("VERSION", "1.0.0")
    )
    .useMsvcToolchain() // Required toolchain (from ve.build.msvc)
    .build()
    .run(args);
```

### Advanced Compiler Configuration

You can fine-tune compilation options via the configuration lambda in `.Sources()`. This API is strongly typed and toolchain-agnostic.

```csharp
.project("game_engine", p => p
    .Sources("src", config => config
        // Optimization & Code Gen
        .optimization(OptimizationLevel.SPEED)
        .favorOptimization(FavorOptimization.SPEED)
        .inlineLevel(InlineLevel.FORCE)
        .linkTimeCodeGeneration(true)
        
        // Language Standard
        .languageStandard(LanguageStandard.CppLatest) // C++20/23
        .rtti(false) // Disable RTTI
        .exceptionHandling(ExceptionHandling.NONE) // Disable Exceptions
        
        // Security & Debugging
        .addressSanitizer(true) // Enable ASan
        .securityCheckers(true) // /GS
        .stackCheck(true)
        
        // Concurrency
        .openMP(false)
        
        // Math
        .floatModel(FloatModel.FAST)
        .fastTranscendentals(true)
    )
)
```

-----

## 📂 Supported File Types

The package automatically scans and categorizes files based on extensions:

  * **C++ Source:** `.cpp`, `.cxx`, `.cc`, `.c`, `.c++`
  * **C++ Modules (C++20):** `.ixx`, `.mxx` (Automatically scanned for dependencies)
  * **Headers:** `.h`, `.hpp`, `.hxx`, `.hh`, `.inl`

-----

## 🔗 Architecture

This package provides the following core components:

1.  **`ClExtension`**: Extends the build graph to handle scanning, compiling, and dependency tracking for C++ files.
2.  **`IClConfigurator`**: A fluent interface for configuring compiler flags abstractly.
3.  **Dependency Scanner**: Logic to parse C++20 `import/export` statements and `#include` directives to build a precise DAG (Directed Acyclic Graph).

## License

© 2025 **VassalStudio**. All rights reserved.