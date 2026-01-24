# Changelog

## [1.0.0] - 2026-01-24
### Added
- Barebones modular locomotion contracts (`ICharacterMotor`, `ICharacterInputSource`, `IMotorModule`, `MotorModuleBehaviour`, `IMovementReferenceFrame`, `MotorConfigAsset`, and `MotorPipeline`).
- Three physics motor backends (`CharacterControllerMotor3D`, `RigidbodyMotor3D`, and `RigidbodyMotor2D` supporting `Platformer` and `TopDown` modes).
- Pluggable input adapters (`LegacyInputSource` and optional `NewInputSystemSource` via `versionDefines`).
- UI Toolkit `CharacterMotorInspector`, `MotorDebuggerWindow` with live module toggles, and `GameObject > CharacterMotor` rig creation menu items.
- Sample custom `IMotorModule` scripts, barebones FPS/TPS camera samples, and NUnit EditMode tests.
