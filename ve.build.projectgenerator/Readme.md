# ve.build.projectgenerator

> 🚀 **STATUS:** v1.0.0 RC. Project Generation Abstraction Layer for VEBuild.

**ve.build.projectgenerator** defines the interfaces and base logic for generating IDE (Integrated Development Environment) project files.

It acts as a **factory and dispatcher**: it allows you to register a specific generator (e.g., for Visual Studio) and automatically creates a standard `generate` task that iterates through all defined projects to create configuration files for them.

## ⚠️ Important

**This package does not contain implementations for specific IDEs.**
To generate `.vcxproj` or `.sln` files, you must install a corresponding implementation package alongside this one:

* **Visual Studio:** Install [`ve.build.vcxprojgenerator`](https://www.nuget.org/packages/ve.build.vcxprojgenerator)
* **Others (VS Code, Makefiles):** *(In development)*

---

## 📦 Installation

```bash
dotnet add package ve.build.projectgenerator
````

## ⚙️ Architecture

This package introduces the **`IProjectGenerator`** concept. This is the contract that must be implemented by any system wishing to export the `ve.build` configuration to an external format.

### Interface

```csharp
public interface IProjectGenerator
{
    // Unique name of the generator (used in CLI: --generator=Name)
    string Name { get; }

    // Determines the output path for the project file based on project settings
    string projectFile(IProjectBuilder projectBuilder);

    // Main generation logic: writes content to the file
    Task<ActionResult> generateProjectFiles(
        IBuildContext ctx, 
        IProjectBuilder projectBuilder, 
        File[] files, 
        IEnumerable<IProjectBuilder> dependencies
    );

    // Hooks for additional setup (e.g., Solution generation)
    void setupProject(string file);
    void finalStep(ITaskBuilder taskBuilder);
}
```

## 💻 Usage

Typically, you do not use this package directly but rather reference a specific implementation (e.g., `useVcxprojGenerator()`).

However, if you are writing your own generator (e.g., for **CLion** or **Makefile**), you can use this package as a base.

### Example: Registering a Custom Generator

```csharp
using ve.build.core;
using ve.build.core.projects;
using ve.build.core.tasks;
using ve.build.projectgenerator;
using File = ve.build.core.files.File;

// 1. Implement the interface
class MyCustomGenerator : IProjectGenerator
{
    public string Name => "MyGen";

    public string projectFile(IProjectBuilder projectBuilder)
    {
        // Define where the project file will be created
        return Path.Combine(projectBuilder.IntermediateDir, $"{projectBuilder.Name}.myproj");
    }

    public async Task<ActionResult> generateProjectFiles(IBuildContext ctx, IProjectBuilder projectBuilder, File[] files, IEnumerable<IProjectBuilder> dependencies)
    {
        // Write your generation logic here
        // ...
        return ActionResult.SUCCESS;
    }

    public void setupProject(string file) { /* Optional hook */ }
    
    public void finalStep(ITaskBuilder taskBuilder) 
    { 
        // Optional hook to add solution generation tasks or post-processing
    }
}

// 2. Register in HostBuilder
return await new HostBuilder()
    .project("my_app", ...)
    
    // Register your generator instance
    .setupProjectGenerator(new MyCustomGenerator()) 
    
    .build()
    .run(args);
```

-----

## 🛠️ CLI Command

Once this package (and an implementation) is registered, a new command becomes available in your CLI:

```bash
# Runs the generation process for all projects
dotnet run -- generateProjectFiles
```

## License

© 2025 **VassalStudio**. All rights reserved.