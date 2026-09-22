# Repository Guidelines

## Project Structure & Module Organization

This is a .NET 8 WPF desktop music player. The project file is
`PotatoMusicPlayer.csproj`; application startup and the main window live in
`App.xaml` and `MainWindow.xaml`. Keep responsibilities separated by folder:

- `Models/`: data and playback state objects.
- `Services/`: media playback, settings persistence, waveform, and file logic.
- `ViewModels/`: UI state and commands (MVVM).
- `Views/`: secondary WPF windows.
- `Converters/`, `Utils/`, and `Resources/Themes/`: binding helpers, shared
  infrastructure, and XAML styling.

`PotatoMusicPlayer_Specification.md` describes expected behavior. Build output
is generated in `bin/` and `obj/`; do not edit or commit it.

## Build, Test, and Development Commands

Run from the repository root:

```powershell
dotnet restore
dotnet build PotatoMusicPlayer.csproj
dotnet run --project PotatoMusicPlayer.csproj
```

`restore` retrieves LibVLC and other NuGet dependencies. `build` compiles the
Windows-only `net8.0-windows` app; `run` launches it. There is currently no
test project, so validate UI and playback changes manually and add automated
tests with new non-UI logic where practical.

## Coding Style & Naming Conventions

Follow the existing C# style: four-space indentation, braces on their own
lines, PascalCase for types, methods, and public properties, and camelCase
parameters. Private fields use `_camelCase` (for example, `_mediaService`).
Keep XAML bindings and command names descriptive, such as
`PlayPauseCommand`. Place UI behavior in view models/services rather than
code-behind when possible. Nullable reference types are disabled; still check
external inputs and handle failures explicitly.

## Testing Guidelines

Name future test projects `PotatoMusicPlayer.Tests` and test methods after the
behavior being checked, e.g. `IsSupportedFormat_ReturnsFalse_ForUnknownFile`.
Prioritize `Services/`, `Utils/`, and view-model behavior that can be tested
without a WPF window or LibVLC runtime. Run all tests with `dotnet test` once a
test project exists.

## Commit & Pull Request Guidelines

Recent commits use short Japanese, imperative summaries (for example,
`ループ機能を修正`). Keep each commit focused and describe the changed feature.
Pull requests should explain user-visible behavior, list validation performed,
link relevant issues, and include screenshots for XAML/UI changes. Call out
new dependencies, configuration changes, or playback limitations.
