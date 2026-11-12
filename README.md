# CharacterMotor (Unity 6)

Barebones modular character controller framework for Unity 6. It provides minimal locomotion contracts and three swappable physics backends (`CharacterController`, `Rigidbody`, and `Rigidbody2D`), leaving genre-specific mechanics such as dashing, coyote time, crouching, or climbing to developer-written `IMotorModule` plugins.

## Core architecture

- **Minimal locomotion pipeline (`MotorPipeline`):** Runs ordered `IMotorModule` hooks (`BeforeVelocitySolve`, `ModifyVelocity`, `AfterMove`) around barebones acceleration, deceleration, grounded jump, gravity, and impulse application.
- **Three physics motor backends (`ICharacterMotor`):**
  - `CharacterControllerMotor3D` for 3D `UnityEngine.CharacterController`
  - `RigidbodyMotor3D` for 3D `Rigidbody` (`FixedUpdate` and Unity 6 `linearVelocity`)
  - `RigidbodyMotor2D` for 2D `Rigidbody2D` in `Platformer` or `TopDown` mode
- **Camera and orientation agnostic (`IMovementReferenceFrame`):** 3D motors project planar input against an assignable `ReferenceTransform` (compatible with Cinemachine, a standard `Camera`, or a custom rig) and fall back to `Camera.main` when unassigned. `MotorInput.Look` exposes optional aim/look delta for custom camera scripts.
- **Pluggable input (`ICharacterInputSource`):** Decouples locomotion from hardware devices so player input, AI agents, replays, or networked inputs drive the same motor via `SetInput`. Includes `LegacyInputSource` and an optional `NewInputSystemSource` enabled via `versionDefines` when `com.unity.inputsystem` is installed.
- **UI Toolkit inspection:** Includes a UI Toolkit custom inspector (`CharacterMotorInspector`), a Play Mode `MotorDebuggerWindow` (`Tools > HaruChanZ64 > Gameplay > CharacterMotor > Motor Debugger`) with live module toggles, and rig creation menus under `GameObject > CharacterMotor`.

## Installation

1. Open **Window > Package Manager** in Unity 6 (`6000.0+`).
2. Click **+** (top-left) > **Install package from git URL...**
3. Enter:
   ```text
   https://github.com/haruchanz64/CharacterMotor.git
   ```

## Quick start

1. Create a barebones rig from **GameObject > CharacterMotor** (`3D CharacterController Rig`, `3D Rigidbody Rig`, `2D Platformer Rig`, or `2D Top-Down Rig`).
2. Attach `NewInputSystemSource` (or `LegacyInputSource`), or feed inputs from your own script with `motor.SetInput(input)`.
3. Assign a camera or Cinemachine pivot to `Reference Transform` on 3D motors, or leave it empty to use `Camera.main`.
4. Add custom movement behaviors by attaching components that inherit from `MotorModuleBehaviour` or by calling `motor.AddModule(...)`.

## Writing a custom module

```csharp
using CharacterMotor.Core;
using UnityEngine;

public sealed class SpeedBoostModule : MotorModuleBehaviour
{
    [SerializeField] private float multiplier = 1.5f;

    public override void ModifyVelocity(ref MotorContext ctx)
    {
        if (!ctx.Input.HasAction(0)) return;
        ctx.DesiredVelocity.x *= multiplier;
        ctx.DesiredVelocity.z *= multiplier;
    }
}
```

Import **Custom Modules & Camera Samples** from Package Manager for complete examples of dash, coyote jump, sprint modifiers, first-person camera, and third-person orbit camera scripts.

## License

[MIT](LICENSE.md)
