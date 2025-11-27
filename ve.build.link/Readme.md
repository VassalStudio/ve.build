# ve.build.link

 🚀 STATUS v1.0.0 RC. Linker Abstraction Layer for VEBuild.

ve.build.link provides the linker and librarian abstractions for the VEBuild system.

It acts as the Linker Frontend, defining how projects are linked (executable vs shared library vs static library) and managing the dependency graph for the linking stage. It abstracts away the differences between `link.exe` (MSVC), `ld` (GCC), and `lld` (LLVM).

## ⚠️ Important

This package does not contain a linker implementation.
To actually link binaries, you must install a Toolchain implementation package alongside this one

 Windows (MSVC) Install [`ve.build.link.msvc`](httpswww.nuget.orgpackagesve.build.link.msvc)
 Cross-Platform (Coming soon)

---

## 📦 Installation

```bash
dotnet add package ve.build.link
````

## ⚡ Usage

This package extends the `ProjectBuilder` with methods to define the Project Type and Dependencies.

### Defining Artifacts

Use the `.Type()` extension method to specify what this project produces.

```csharp
using ve.build.core;
using ve.build.link.link;  Import extensions

return await new HostBuilder()
     1. Static Library (.lib  .a)
    .project(my_static_lib, p = p
        .Type(ProjectType.StaticLib)  Uses the Librarian (lib.exe  ar)
        .Sources(src)
    )

     2. Dynamic Library (.dll  .so)
    .project(my_shared_lib, p = p
        .Type(ProjectType.DLL)  Uses the Linker (link.exe  ld)
        .Sources(src)
    )

     3. Executable (.exe  elf)
    .project(my_game, p = p
        .Type(ProjectType.EXE)  Uses the Linker
        .Sources(src)
    )
    .useMsvcToolchain()
    .build()
    .run(args);
```

### Managing Dependencies

Use the `.dependsOf()` method to link against other projects.
This handles the Transitive Propagation of

   Link Inputs Automatically passes `.lib` files to the linker.
   Build Order Ensures libraries are built before the executable.

!-- end list --

```csharp
.project(game_client, p = p
    .Type(ProjectType.EXE)
    .Sources(src)
    
     Link against local projects
    .dependsOf(ve.core)
    .dependsOf(ve.render)
    
     Configure Linker flags (Abstract API)
    .Link(config = config
        .subsystem(Subsystem.WINDOWS)
        .incremental(true)
        .libraryPath(libsexternal)
        .library(user32.lib)
    )
)
```

-----

## 🔗 Architecture

This package introduces the following core concepts

### 1. `ProjectType` Enum

Determines the build pipeline for the project

   `EXE` Compiles sources - Links Object Files - Generates Executable.
   `DLL` Compiles sources - Links Object Files - Generates Shared Library (+ Import Library).
   `StaticLib` Compiles sources - Archives Object Files - Generates Static Library.

### 2. `ILinkTool` Interface

The contract that toolchains must implement. It has two main modes

   `link()` For EXEDLL. Configured via `ILinkConfigurator`.
   `lib()` For StaticLib. Configured via `ILibConfigurator`.

### 3. Dependency System

The `.dependsOf()` extension integrates with the VEBuild DAG (Directed Acyclic Graph) to ensure correct topological sort execution order.

## License

© 2025 VassalStudio. All rights reserved.