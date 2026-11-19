# CharacterMotor

Barebones modular character controller framework for Unity 6. It provides minimal locomotion contracts and three swappable physics backends (`CharacterController`, `Rigidbody`, and `Rigidbody2D`), leaving genre-specific mechanics such as dashing, coyote time, crouching, or climbing to developer-written `IMotorModule` plugins.

## Features

- **Deterministic locomotion pipeline (`MotorPipeline`):** Runs ordered `IMotorModule` hooks (`BeforeVelocitySolve`, `ModifyVelocity`, `AfterMove`) around base acceleration, deceleration, grounded jump, gravity, and queued velocity impulses.
- **Three swappable physics backends (`ICharacterMotor`):**
  - `CharacterControllerMotor3D` for 3D `UnityEngine.CharacterController` (ticks in `Update`).
  - `RigidbodyMotor3D` for 3D `Rigidbody` (ticks in `FixedUpdate` using Unity 6 `linearVelocity` and walkable slope projection).
  - `RigidbodyMotor2D` for 2D `Rigidbody2D` in `Platformer` or `TopDown` mode (ticks in `FixedUpdate` using Unity 6 `linearVelocity`).
- **Camera and orientation agnostic (`IMovementReferenceFrame`):** 3D motors project planar input through `TransformReferenceFrame` against an assignable `ReferenceTransform` (Cinemachine pivot, standard `Camera`, or custom rig) and fall back to `Camera.main` when unassigned. `MotorInput.Look` exposes optional look delta for custom camera scripts.
- **Pluggable input (`ICharacterInputSource`):** Decouples locomotion from hardware devices so player input, AI agents, replays, or networked controllers drive the same motor via `SetInput` or a bound source. Ships with `LegacyInputSource` and an optional `NewInputSystemSource` compiled via `versionDefines` when `com.unity.inputsystem` is installed.
- **Reusable configuration (`MotorConfigAsset`):** Tune locomotion parameters inline on the motor or share a `MotorConfigAsset` ScriptableObject across multiple characters (`Default3D`, `DefaultPlatformer2D`, and `DefaultTopDown2D` presets).
- **UI Toolkit inspection and rig setup:** Includes a UI Toolkit custom inspector (`CharacterMotorInspector`), a Play Mode `MotorDebuggerWindow` (`Tools > HaruChanZ64 > Gameplay > CharacterMotor > Motor Debugger`) with live module toggles and telemetry, and rig creation menus under `GameObject > CharacterMotor`.

## Requirements

- Unity `6000.0` or newer
- Zero required external dependencies
- Optional: `com.unity.inputsystem` (`1.0.0+`) to compile `CharacterMotor.InputSystem` (`NewInputSystemSource`) automatically via `CHARACTER_MOTOR_INPUT_SYSTEM`

## Installation

### Unity Package Manager (Git URL)

1. Open **Window > Package Manager** in Unity 6 (`6000.0+`).
2. Click **+** (top-left) and choose **Install package from git URL...**.
3. Enter:

```text
https://github.com/haruchanz64/CharacterMotor.git
```

### Local package

Add the package path to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.haruchanz64.character-motor": "file:../Plugins/CharacterMotor"
  }
}
```

## Quick start

1. Create a preconfigured rig from **GameObject > CharacterMotor** (`3D CharacterController Rig`, `3D Rigidbody Rig`, `2D Platformer Rig (Rigidbody2D)`, or `2D Top-Down Rig (Rigidbody2D)`).
2. Attach `NewInputSystemSource` (from `CharacterMotor.InputSystemBridge`, if `com.unity.inputsystem` is installed) or `LegacyInputSource` (from `CharacterMotor.Input`) to the same GameObject. Motors automatically discover sibling `ICharacterInputSource` and `IMotorModule` components on `Awake` when `autoDiscoverComponents` is enabled.
3. For 3D rigs, assign your camera or Cinemachine pivot to `Reference Transform` on the motor, or leave it empty to fall back to `Camera.main`.
4. *(Optional)* Create a shared config asset via **Assets > Create > CharacterMotor > Motor Config** and assign it to `Config Asset` to override inline settings.
5. Enter Play Mode and open **Tools > HaruChanZ64 > Gameplay > CharacterMotor > Motor Debugger** to inspect velocity, slope angle, grounded state, and toggle attached modules live.

## Core concepts

### Per-step execution pipeline

Each call to `Step(deltaTime)` resolves grounding and input, builds a mutable `MotorContext`, and runs `MotorPipeline.SolveVelocity(ref ctx)` in four stages:

1. **Pre-solve (`IMotorModule.BeforeVelocitySolve`):** Enabled modules run in ascending `ExecutionOrder` (tie-broken by ordinal `ModuleId`). Set `ctx.SuppressBaseMovement`, `ctx.SuppressBaseJump`, `ctx.ApplyGravity`, or `ctx.GravityScale` here.
2. **Base locomotion:** Integrates planar or horizontal acceleration/deceleration (`MotorSettings`), applies grounded jump (`jumpSpeed`), and integrates gravity (clamped to `maxFallSpeed`) or `groundStickVelocity`.
3. **Post-solve (`IMotorModule.ModifyVelocity`):** Enabled modules adjust `ctx.DesiredVelocity` for sprints, dashes, knockback dampening, or custom friction.
4. **Impulses and physics move:** Adds queued impulses from `motor.AddImpulse(...)`, applies the velocity via the active physics backend, updates rotation (`CharacterRotationMode`), calls `IMotorModule.AfterMove(ref ctx)`, and fires `OnGroundedChanged`, `OnJumped`, and `OnAfterMove`.

### Action bitmask (`MotorInput.ActionMask`)

`MotorInput` includes a 32-bit `ActionMask` for genre abilities without hardcoding ability names into the motor. Use `input.SetAction(bitIndex, active)` and `input.HasAction(bitIndex)` for indices `0` to `31`. Both built-in input sources map `Left Shift` to bit `0` (held) and `Left Ctrl` to bit `1` (pressed this frame) by default.

## Code examples

### 1. Write a custom `IMotorModule`

Subclass `MotorModuleBehaviour` to expose parameters in the Inspector and have the motor auto-discover the component on `Awake`:

```csharp
using CharacterMotor.Core;
using UnityEngine;

public sealed class SpeedBoostModule : MotorModuleBehaviour
{
    [SerializeField] private int actionBit = 0;
    [SerializeField, Min(1f)] private float multiplier = 1.5f;

    public override void ModifyVelocity(ref MotorContext ctx)
    {
        if (!ctx.Input.HasAction(actionBit)) return;
        ctx.DesiredVelocity.x *= multiplier;
        ctx.DesiredVelocity.z *= multiplier;
    }
}
```

Pure C# modules implementing `IMotorModule` directly can be attached and removed at runtime with `motor.AddModule(module)` and `motor.RemoveModule(module)`.

### 2. Drive a motor from AI, cutscenes, or network scripts

Call `SetInput` before a motor step to override any bound `ICharacterInputSource` for that tick, and subscribe to motor events for animation or audio triggers:

```csharp
using CharacterMotor.Core;
using CharacterMotor.Motors;
using UnityEngine;

public sealed class PatrolAgent : MonoBehaviour
{
    [SerializeField] private CharacterMotorBase motor;

    private void OnEnable()
    {
        motor.OnGroundedChanged += HandleGroundedChanged;
        motor.OnJumped += HandleJumped;
    }

    private void OnDisable()
    {
        motor.OnGroundedChanged -= HandleGroundedChanged;
        motor.OnJumped -= HandleJumped;
    }

    private void Update()
    {
        var input = new MotorInput
        {
            Move = new Vector2(0f, 1f),
            JumpPressed = false
        };
        motor.SetInput(in input);
    }

    public void Knockback(Vector3 direction, float force)
    {
        motor.AddImpulse(direction.normalized * force);
    }

    public void RespawnAt(Vector3 spawnPoint)
    {
        motor.Teleport(spawnPoint, resetVelocity: true);
    }

    private void HandleGroundedChanged(bool grounded) => Debug.Log($"Grounded: {grounded}");
    private void HandleJumped() => Debug.Log("Jumped");
}
```

### 3. Supply a custom movement reference frame

Implement `IMovementReferenceFrame` for fixed isometric angles, spline cameras, or custom gravity planes, and pass it to `motor.SetReferenceFrame(...)`:

```csharp
using CharacterMotor.Core;
using UnityEngine;

public sealed class IsometricReferenceFrame : IMovementReferenceFrame
{
    private readonly Quaternion _yaw = Quaternion.Euler(0f, 45f, 0f);

    public Vector3 GetForwardOnPlane(Vector3 upAxis) => _yaw * Vector3.forward;

    public Vector3 TransformMoveInput(Vector2 moveInput, Vector3 upAxis)
    {
        Vector2 clamped = Vector2.ClampMagnitude(moveInput, 1f);
        return _yaw * new Vector3(clamped.x, 0f, clamped.y);
    }
}
```

## Extension contracts and core API

| Type | Namespace | Purpose | Built-in implementation |
| :--- | :--- | :--- | :--- |
| `ICharacterMotor` | `CharacterMotor.Core` | Unified motor contract for input, impulses, teleports, events, and module lifecycle | `CharacterControllerMotor3D`, `RigidbodyMotor3D`, `RigidbodyMotor2D` |
| `IMotorModule` | `CharacterMotor.Core` | Ordered locomotion hooks (`BeforeVelocitySolve`, `ModifyVelocity`, `AfterMove`) | `MotorModuleBehaviour` (Inspector base class) |
| `ICharacterInputSource` | `CharacterMotor.Core` | Supplies per-step `MotorInput` snapshots | `LegacyInputSource`, `NewInputSystemSource` |
| `IMovementReferenceFrame` | `CharacterMotor.Core` | Projects 2D directional input onto the locomotion plane | `TransformReferenceFrame` |
| `MotorConfigAsset` | `CharacterMotor.Core` | Shared ScriptableObject container for `MotorSettings` | `Default3D`, `DefaultPlatformer2D`, `DefaultTopDown2D` presets |
| `MotorPipeline` | `CharacterMotor.Core` | Pure C# velocity solver usable directly in headless simulations or EditMode tests | `MotorPipeline` |

## Included samples

Import **Custom Modules & Camera Samples** (`Samples~/CustomModules`) from the Package Manager **Samples** tab:

- `SampleCoyoteJumpModule`: adds coyote time and jump input buffering using `MotorState.TimeSinceGrounded`.
- `SampleDashModule`: adds a timed directional dash triggered by `ActionMask` bit `1`, demonstrating `SuppressBaseMovement`, `ApplyGravity = false`, and `ModifyVelocity`.
- `SampleSprintModifierModule`: multiplies planar or horizontal speed while `ActionMask` bit `0` is held.
- `SampleFirstPersonCamera`: reads `MotorInput.Look` for yaw/pitch and binds the camera pivot to `motor.ReferenceTransform`.
- `SampleThirdPersonOrbitCamera`: orbits behind a target motor and assigns itself as `motor.ReferenceTransform`.

## Testing

The package includes EditMode NUnit tests under `Tests/Editor/` runnable from **Window > General > Test Runner** (EditMode tab):

- `Tests/Editor/MotorPipelineTests.cs`: verifies deterministic base movement, jump and gravity integration, ascending `ExecutionOrder` sorting with disabled-module skipping, module velocity overrides with impulses, and 2D top-down omnidirectional movement.
- `Tests/Editor/MotorBackendTests.cs`: verifies `ICharacterMotor` dimensions and `OnAttach`/`OnDetach` lifecycle across all three backends, plus camera-relative XZ projection in `TransformReferenceFrame`.

## Troubleshooting and limitations

- **`NewInputSystemSource` is missing from Add Component:** Ensure `com.unity.inputsystem` (`1.0.0+`) is installed. `CharacterMotor.InputSystem.asmdef` only compiles when `CHARACTER_MOTOR_INPUT_SYSTEM` is defined by its `versionDefines`.
- **`LegacyInputSource` produces no movement:** Under **Project Settings > Player > Active Input Handling**, set input handling to **Input Manager (Old)** or **Both**, or switch to `NewInputSystemSource`.
- **Unity gravity settings on `Rigidbody` / `Rigidbody2D` are ignored:** `RigidbodyMotor3D` sets `useGravity = false` and `RigidbodyMotor2D` sets `gravityScale = 0` on `Awake` (and freezes rotation) because `MotorPipeline` owns vertical velocity. Tune gravity through `MotorSettings.gravity`.
- **Update cadence differs by backend:** `CharacterControllerMotor3D` steps in `Update`, while `RigidbodyMotor3D` and `RigidbodyMotor2D` step in `FixedUpdate`. When calling `motor.SetInput(...)` manually on a Rigidbody motor, call it from `FixedUpdate` or latch one-shot presses until consumed.
- **Ground detection fails on custom layers:** Set `Ground Layers` on the motor so it includes your floor layers and excludes the character's own layer if you see self-hits on 3D probes.
- **Sharing one `ICharacterInputSource` with a camera script:** Built-in input sources clear their latched `JumpPressed` flag inside `ReadInput()`. If a custom camera script also calls `ReadInput()` in `LateUpdate()`, read look input from a separate input action or cache the snapshot once per frame so jump presses are not consumed early.

## License

[MIT](LICENSE.md)
