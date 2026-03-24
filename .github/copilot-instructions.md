# Copilot Instructions — Simple Animate

## Project Overview

Simple Animate is a Windows desktop application that lets very young children (ages 5+) create animations. The UX must be **dead simple** — large colorful buttons, drag-and-drop, minimal text, no menus or dialogs that require reading. Think "finger-painting with motion."

## Tech Stack

- **Framework:** WPF on .NET 8 (LTS)
- **Language:** C# 12
- **Architecture:** MVVM using `CommunityToolkit.Mvvm` (ObservableObject, RelayCommand, source generators)
- **DI Container:** `Microsoft.Extensions.DependencyInjection`
- **Solution name:** `SimpleAnimate.sln`

## Build & Test

```powershell
# Restore, build, run
dotnet restore
dotnet build
dotnet run --project src/SimpleAnimate

# Tests (xUnit)
dotnet test                              # full suite
dotnet test --filter "FullyQualifiedName~MyTestClass.MyMethod"  # single test
```

## Architecture

```
src/
  SimpleAnimate/          # Main WPF application (thin shell — wires DI, loads plugins, hosts views)
    Models/               # Domain objects (Element, Frame, Animation, Project)
    ViewModels/           # MVVM ViewModels (one per major view)
    Views/                # XAML views/user controls
    Controls/             # Custom WPF controls (canvas, timeline, toolbar)
    Converters/           # IValueConverter implementations
    Assets/               # Icons, sounds, bundled stamps/stickers
    App.xaml / App.xaml.cs
  SimpleAnimate.Core/     # Class library — interfaces, models, services, shared logic (NO WPF references)
    Interfaces/           # Service contracts (IUndoService, IProjectService, IExportService, etc.)
    Models/               # Domain models shared across app and plugins
    Services/             # Default service implementations
  SimpleAnimate.Plugins/  # Class library — plugin infrastructure
    IPlugin.cs            # Plugin contract
    PluginLoader.cs       # Discovers and loads plugins at startup
plugins/                  # Drop-in plugin assemblies (each plugin is its own project/DLL)
  SimpleAnimate.Plugin.Stamps/   # Example: extra stamp packs
  SimpleAnimate.Plugin.Export/   # Example: GIF/video export
tests/
  SimpleAnimate.Tests/           # xUnit — tests against Core and ViewModels
```

### Modularity & Plugin System

The solution is split into multiple projects to keep concerns separate and enable plugins:

- **SimpleAnimate.Core** is the heart — all interfaces, models, and service logic live here with **zero WPF dependencies**. This makes it testable and referenceable by plugins.
- **SimpleAnimate** (the WPF app) is a thin host. It registers services in DI, loads plugins, and provides the UI.
- **Plugins** reference only `SimpleAnimate.Core`. Each plugin implements `IPlugin`, which provides a `Configure(IServiceCollection)` method to register its own services/tools. Plugins are loaded from the `plugins/` folder at startup via `PluginLoader`.

When adding new functionality, prefer putting logic in `Core` behind an interface. Only put WPF-specific code (views, controls, converters) in the main app project.

### Key Domain Concepts

- **Project** — a saved animation file (serialized to JSON).
- **Frame** — a single snapshot in time; an animation is a sequence of frames.
- **Element** — a visual object on the canvas (shape, stamp/sticker, drawing stroke). Each element has position, scale, rotation, color, and per-frame keyframe data.
- **Timeline** — visual strip of frame thumbnails at the bottom; child taps a frame to edit it.

### Data Flow

Views bind to ViewModels via `{Binding}`. ViewModels expose `ObservableProperty` fields and `RelayCommand` methods (using CommunityToolkit source generators — use `[ObservableProperty]` and `[RelayCommand]` attributes instead of manual INotifyPropertyChanged). Services are injected via constructor DI registered in `App.xaml.cs` using `Microsoft.Extensions.DependencyInjection`.

### Dependency Rules

```
SimpleAnimate (WPF app)  →  references Core + Plugins
SimpleAnimate.Plugins    →  references Core only
Any plugin project       →  references Core only
SimpleAnimate.Core       →  references nothing (standalone)
```

No circular references. Plugins and Core must **never** reference the WPF app project.

## Key Conventions

### UX Principles (enforce in all UI work)

- **No reading required.** Every action must be achievable via icons, colors, and spatial cues alone.
- **Big touch targets.** Minimum 48×48 dp for any interactive element; prefer 64×64+ for primary tools.
- **Bright, high-contrast palette.** Use the app's defined color resources in `App.xaml`.
- **Undo is always available.** Every canvas mutation must go through the undo/redo service.
- **Sounds for feedback.** Actions should play short audio cues (pop, whoosh, click) so the child knows something happened.

### Code Style

- Use file-scoped namespaces (`namespace Foo;`).
- Use CommunityToolkit.Mvvm source generators: `[ObservableProperty]` for bindable fields, `[RelayCommand]` for commands. Do **not** write manual `OnPropertyChanged` boilerplate.
- Name ViewModels as `{View}ViewModel` (e.g., `CanvasView` → `CanvasViewModel`).
- Keep ViewModels free of WPF types (`UIElement`, `Canvas`, etc.) — pass data, not controls.
- XAML views should have minimal code-behind; use behaviors or attached properties when interaction logic is needed.
- **Keep classes small and focused.** One class = one responsibility. If a class grows beyond ~150 lines, split it.
- **Depend on interfaces, not concrete classes.** All services must have an `I{Name}` interface in Core. Register implementations in DI; consume via constructor injection.
- **No static helpers or singletons.** Use DI for shared state. This keeps code testable and plugin-friendly.
- **Simple > clever.** Favor readable, straightforward code over abstractions. If a pattern doesn't earn its complexity, don't use it.

### Serialization

- Save/load projects as JSON via `System.Text.Json` with source-generated serializer contexts for AOT compatibility.
- File extension: `.sanimate`

### Animation Playback

- Default frame rate: 6 FPS (adjustable). Use `DispatcherTimer` for playback loop.
- Interpolation between keyframes is linear by default.
