# VEBuild (pre-release)

> 🚧 **STATUS:** active development. The API is unstable; breaking changes are expected. **Not production-ready.**

VEBuild is a modular .NET build system that composes projects via declarative tasks, extensions, and platforms.  
This repo contains the core abstractions and a sandbox used to validate the architecture.

---

## Requirements

- **.NET SDK 9.0+**
- Windows / Linux / macOS (the sandbox sample sets `windows-x64` as the default platform)

## Quick Start

Minimal sample used for architecture testing.  
It declares a `build` task, a `sample` project, copies `test.txt`, and logs a message.

```csharp
using ve.build.core;
using ve.build.core.projects;

await new HostBuilder()
    .task("build", "Build sample task",
        builder => builder.eachProject(pbuilder =>
            pbuilder.buildAction("defaultAction", "Default Action", [],
                ctx => ctx.log(LogLevel.INFO, "BUILD", "Test Build"))))
    .project("sample", PROJECT_TYPE.APPLICATION, pbuilder => pbuilder.file("test.txt").copy())
    .platform("windows-x64", true, builder => { })
    .build()
    .run(args);
````

**Run:**

```bash
dotnet run -- build
```

## Key Concepts

* **Tasks** — Declarative pipelines executed per project.
* **Projects** — Describe artifacts and actions (`buildAction`, `file().copy()`, etc.).
* **Platforms** — Target environments (e.g., `windows-x64`) with a configurable default.
* **Extensions** — External packages that plug in compilers/linkers/codegen.
  First up: a **C++ compiler abstraction** (MSVC/Clang providers will be separate extensions).

## Current Limitations

* API is evolving; breaking changes across `0.x` versions are likely.
* C++ integration: base abstraction in progress; MSVC/Clang providers are upcoming.
* Documentation is minimal; examples live in the sandbox for now.

## Roadmap

* [ ] Base C++ abstraction: compile/link models and provider interfaces
* [ ] Providers: `VEBuild.Cpp.MSVC`, `VEBuild.Cpp.Clang`
* [ ] Build cache & incremental compilation; dependency graph for compilation
* [ ] CI integration (GitHub Actions) and NuGet publishing
* [ ] Pluggable code generation system

## Versioning

* **SemVer** within **`0.x`** (breaking changes permitted)
* Pre-releases: `-alpha.N`, `-beta.N`, `-rc.N`

## License

TBD.
© 2025 **VassalStudio**. All rights reserved.