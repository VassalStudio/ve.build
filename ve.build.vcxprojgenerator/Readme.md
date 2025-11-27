# ve.build.vcxprojgenerator

> 🚀 **STATUS:** v1.0.0 RC. Visual Studio Generator for VEBuild.

**ve.build.vcxprojgenerator** implements the `IProjectGenerator` interface to create **Visual Studio 2022** project files (`.vcxproj`) and solutions (`.sln`).

It bridges the gap between the custom `ve.build` system and the Visual Studio IDE, providing a first-class development experience with full IntelliSense, Debugging, and C++20 Modules support.

## 📦 Features

### 1. NMake Project Generation
It generates `Makefile Project` configurations (`.vcxproj`) that delegate build commands back to `ve.build`:
* **Build Command:** Calls `ve.build build ...`
* **Rebuild Command:** Calls `ve.build rebuild ...`
* **Clean Command:** Calls `ve.build clean ...`

### 2. IntelliSense & C++20 Modules
It employs advanced techniques to make VS IntelliSense work with custom build artifacts:
* **Forced Includes:** Generates a header (`ve_intellisense.h`) to pass complex preprocessor definitions (like `__declspec(dllexport)`) that typically break NMake XML parsers.
* **Module Visibility:** Automatically adds module interface files (`.ixx`) from dependent projects as links, ensuring cross-project code navigation (Go To Definition) works correctly.

### 3. Smart Solution Generation (`.sln`)
It generates a `.sln` file that intelligently combines your C++ projects with the C# build tool itself:
* **Hybrid Solution:** Includes both the target C++ projects and the `ve.build` C# project.
* **Build Dependencies:** Configures the Solution so that the C# build tool is compiled *before* the C++ projects. This ensures you always run the latest version of your build script.
* **Configuration Mapping:** Maps solution configurations (e.g., `DEBUG|windows-x64`) to the correct C# configuration (`Debug|Any CPU`) and C++ configuration.

### 4. Debugger Integration
Automatically configures the **Local Windows Debugger**:
* Sets the `Command` to the generated executable path.
* Sets the `WorkingDirectory` to the project root.
* Enables `NativeOnly` debugging for maximum performance.

---

## 💻 Usage

Register this generator in your `HostBuilder`.

```csharp
using ve.build.core;
using ve.build.vcxprojgenerator; // Extension namespace

return await new HostBuilder()
    .project("my_game", ...)
    
    // Register the VS2022 Generator
    .useVcxprojGenerator() 
    
    .build()
    .run(args);
````

After registration, run the generate command:

```bash
dotnet run -- generateProjectFiles
```

This will produce:

  * `MyGame.sln`
  * `obj/my_game.vcxproj`
  * `obj/my_game.vcxproj.filters`

Open `MyGame.sln` in Visual Studio 2022, and you are ready to code, build (F7), and debug (F5).

## 🔧 Architecture

The generator operates in two phases:

1.  **Project Generation (`.vcxproj`):**

      * Iterates over all defined projects.
      * Resolves source files and dependencies.
      * Generates XML with `<NMakeBuildCommandLine>`, `<ClCompile>` includes, and `<ProjectReference>` to link dependencies for IntelliSense.

2.  **Solution Generation (`.sln`):**

      * Discovers the `.csproj` of the build tool itself.
      * Creates a Solution file linking the C\# tool and C++ projects.
      * Sets up `ProjectDependencies` to enforce the build order: `C# Tool -> C++ Libs -> C++ Exe`.

## License

© 2025 **VassalStudio**. All rights reserved.