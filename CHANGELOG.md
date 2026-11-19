# Changelog

## [1.0.1] - 2026-11-19
### Documentation
- Expanded `README.md` into a full developer guide covering Requirements, local package setup, `MotorPipeline` execution stages, `ActionMask` usage, AI/event/reference-frame code examples, Extension Contracts table, Included Samples, Testing, and Troubleshooting.
- Added XML doc comments (`/// <summary>`, `<param>`, `<returns>`, `<inheritdoc/>`) across `Runtime/`, `InputSystem/`, `Editor/`, and `Samples~/` scripts for full IDE IntelliSense coverage.

## [1.0.0] - 2026-01-24
### Added
- Barebones modular locomotion contracts (`ICharacterMotor`, `ICharacterInputSource`, `IMotorModule`, `MotorModuleBehaviour`, `IMovementReferenceFrame`, `MotorConfigAsset`, and `MotorPipeline`).
- Three physics motor backends (`CharacterControllerMotor3D`, `RigidbodyMotor3D`, and `RigidbodyMotor2D` supporting `Platformer` and `TopDown` modes).
- Pluggable input adapters (`LegacyInputSource` and optional `NewInputSystemSource` via `versionDefines`).
- UI Toolkit `CharacterMotorInspector`, `MotorDebuggerWindow` with live module toggles, and `GameObject > CharacterMotor` rig creation menu items.
- Sample custom `IMotorModule` scripts, barebones FPS/TPS camera samples, and NUnit EditMode tests.
