using System;
using System.Collections.Generic;
using UnityEngine;

namespace CharacterMotor.Core
{
    /// <summary>
    /// Pure C# deterministic locomotion pipeline shared by all 2D and 3D motor backends.
    /// Executes BeforeVelocitySolve -> barebones base movement/jump/gravity -> ModifyVelocity -> impulses.
    /// </summary>
    public sealed class MotorPipeline
    {
        private readonly List<IMotorModule> _modules = new List<IMotorModule>(8);
        private bool _orderDirty;
        private Vector3 _pendingImpulse;

        /// <summary>
        /// Registered modules sorted by <see cref="IMotorModule.ExecutionOrder"/> then <see cref="IMotorModule.ModuleId"/>.
        /// </summary>
        public IReadOnlyList<IMotorModule> Modules
        {
            get
            {
                EnsureSorted();
                return _modules;
            }
        }

        /// <summary>
        /// Registers a module if not already present, marks sort order dirty, and invokes <see cref="IMotorModule.OnAttach"/> when <paramref name="owner"/> is supplied.
        /// </summary>
        /// <param name="module">Module to register; ignored when null or already in the pipeline.</param>
        /// <param name="owner">Optional owning motor passed to <see cref="IMotorModule.OnAttach"/>.</param>
        public void AddModule(IMotorModule module, ICharacterMotor owner = null)
        {
            if (module == null || _modules.Contains(module)) return;
            _modules.Add(module);
            _orderDirty = true;
            if (owner != null)
            {
                module.OnAttach(owner);
            }
        }

        /// <summary>
        /// Removes a registered module and invokes <see cref="IMotorModule.OnDetach"/> when <paramref name="owner"/> is supplied.
        /// </summary>
        /// <param name="module">Module instance to remove.</param>
        /// <param name="owner">Optional owning motor passed to <see cref="IMotorModule.OnDetach"/>.</param>
        /// <returns>True if the module was found and removed; otherwise false.</returns>
        public bool RemoveModule(IMotorModule module, ICharacterMotor owner = null)
        {
            if (module == null) return false;
            if (_modules.Remove(module))
            {
                if (owner != null)
                {
                    module.OnDetach(owner);
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Detaches and removes all registered modules in reverse order.
        /// </summary>
        /// <param name="owner">Optional owning motor passed to each module's <see cref="IMotorModule.OnDetach"/>.</param>
        public void ClearModules(ICharacterMotor owner = null)
        {
            if (owner != null)
            {
                for (int i = _modules.Count - 1; i >= 0; i--)
                {
                    _modules[i].OnDetach(owner);
                }
            }
            _modules.Clear();
            _orderDirty = false;
        }

        /// <summary>
        /// Returns the first registered module assignable to <typeparamref name="T"/>, or null if none exists.
        /// </summary>
        /// <typeparam name="T">Concrete or interface module type to locate.</typeparam>
        /// <returns>The matching module, or null.</returns>
        public T GetModule<T>() where T : class, IMotorModule
        {
            for (int i = 0; i < _modules.Count; i++)
            {
                if (_modules[i] is T match)
                {
                    return match;
                }
            }
            return null;
        }

        /// <summary>
        /// Accumulates an additive velocity impulse applied at the end of the next <see cref="SolveVelocity"/> call.
        /// </summary>
        /// <param name="impulse">Velocity delta in world units per second.</param>
        public void QueueImpulse(Vector3 impulse)
        {
            _pendingImpulse += impulse;
        }

        /// <summary>
        /// Discards any queued velocity impulses before they are applied.
        /// </summary>
        public void ClearPendingImpulse()
        {
            _pendingImpulse = Vector3.zero;
        }

        /// <summary>
        /// Runs the four-stage velocity solve for one step: <see cref="IMotorModule.BeforeVelocitySolve"/>,
        /// base locomotion/jump/gravity, <see cref="IMotorModule.ModifyVelocity"/>, and queued impulses.
        /// </summary>
        /// <param name="ctx">Per-step context populated by the backend; updated with <see cref="MotorContext.DesiredVelocity"/> and jump flags.</param>
        public void SolveVelocity(ref MotorContext ctx)
        {
            EnsureSorted();

            ctx.DesiredVelocity = ctx.State.Velocity;
            ctx.ApplyGravity = !ctx.IsTopDown2D;
            ctx.GravityScale = 1f;
            ctx.SuppressBaseMovement = false;
            ctx.SuppressBaseJump = false;
            ctx.JumpedThisStep = false;

            // 1. Module pre-solve hooks
            for (int i = 0; i < _modules.Count; i++)
            {
                var m = _modules[i];
                if (m != null && m.ModuleEnabled)
                {
                    m.BeforeVelocitySolve(ref ctx);
                }
            }

            // 2. Barebones base locomotion
            if (!ctx.SuppressBaseMovement)
            {
                SolveBaseMovement(ref ctx);
            }

            if (!ctx.SuppressBaseJump && !ctx.IsTopDown2D && ctx.State.IsGrounded && ctx.Input.JumpPressed && ctx.Settings.jumpSpeed > 0f)
            {
                ctx.DesiredVelocity.y = ctx.Settings.jumpSpeed;
                ctx.JumpedThisStep = true;
            }

            if (ctx.ApplyGravity && ctx.Settings.gravity > 0f)
            {
                if (ctx.State.IsGrounded && !ctx.JumpedThisStep && ctx.DesiredVelocity.y <= 0f)
                {
                    ctx.DesiredVelocity.y = -ctx.Settings.groundStickVelocity;
                }
                else
                {
                    ctx.DesiredVelocity.y -= ctx.Settings.gravity * ctx.GravityScale * ctx.DeltaTime;
                    if (ctx.Settings.maxFallSpeed > 0f && ctx.DesiredVelocity.y < -ctx.Settings.maxFallSpeed)
                    {
                        ctx.DesiredVelocity.y = -ctx.Settings.maxFallSpeed;
                    }
                }
            }

            // 3. Module post-solve velocity modifiers (buffs, dash override, coyote jump, friction, etc.)
            for (int i = 0; i < _modules.Count; i++)
            {
                var m = _modules[i];
                if (m != null && m.ModuleEnabled)
                {
                    m.ModifyVelocity(ref ctx);
                }
            }

            // 4. External impulses
            if (_pendingImpulse.sqrMagnitude > 0f)
            {
                ctx.DesiredVelocity += _pendingImpulse;
                _pendingImpulse = Vector3.zero;
            }
        }

        /// <summary>
        /// Invokes <see cref="IMotorModule.AfterMove"/> on all enabled modules in execution order after the backend finishes moving.
        /// </summary>
        /// <param name="ctx">Post-move context containing the updated <see cref="MotorContext.State"/>.</param>
        public void NotifyAfterMove(ref MotorContext ctx)
        {
            EnsureSorted();
            for (int i = 0; i < _modules.Count; i++)
            {
                var m = _modules[i];
                if (m != null && m.ModuleEnabled)
                {
                    m.AfterMove(ref ctx);
                }
            }
        }

        /// <summary>
        /// Integrates horizontal or planar acceleration and deceleration into <see cref="MotorContext.DesiredVelocity"/>
        /// for 2D top-down, 2D platformer, or 3D XZ locomotion.
        /// </summary>
        private static void SolveBaseMovement(ref MotorContext ctx)
        {
            float dt = Mathf.Max(0f, ctx.DeltaTime);

            if (ctx.Dimension == MotorDimension.Mode2D && ctx.IsTopDown2D)
            {
                Vector2 target = Vector2.ClampMagnitude(ctx.Input.Move, 1f) * ctx.Settings.maxSpeed;
                Vector2 current = new Vector2(ctx.DesiredVelocity.x, ctx.DesiredVelocity.y);
                float accel = target.sqrMagnitude > 1e-4f ? ctx.Settings.groundAcceleration : ctx.Settings.groundDeceleration;
                Vector2 next = Vector2.MoveTowards(current, target, accel * dt);
                ctx.DesiredVelocity = new Vector3(next.x, next.y, 0f);
                return;
            }

            if (ctx.Dimension == MotorDimension.Mode2D)
            {
                float targetX = Mathf.Clamp(ctx.Input.Move.x, -1f, 1f) * ctx.Settings.maxSpeed;
                float accel = ctx.State.IsGrounded
                    ? (Mathf.Abs(targetX) > 1e-4f ? ctx.Settings.groundAcceleration : ctx.Settings.groundDeceleration)
                    : ctx.Settings.airAcceleration;

                ctx.DesiredVelocity.x = Mathf.MoveTowards(ctx.DesiredVelocity.x, targetX, accel * dt);
                ctx.DesiredVelocity.z = 0f;
                return;
            }

            // 3D planar movement (XZ)
            Vector3 planarInput = new Vector3(ctx.MoveDirectionWorld.x, 0f, ctx.MoveDirectionWorld.z);
            if (planarInput.sqrMagnitude > 1f) planarInput.Normalize();

            Vector3 targetPlanar = planarInput * ctx.Settings.maxSpeed;
            Vector3 currentPlanar = new Vector3(ctx.DesiredVelocity.x, 0f, ctx.DesiredVelocity.z);
            float rate = ctx.State.IsGrounded
                ? (targetPlanar.sqrMagnitude > 1e-4f ? ctx.Settings.groundAcceleration : ctx.Settings.groundDeceleration)
                : ctx.Settings.airAcceleration;

            Vector3 nextPlanar = Vector3.MoveTowards(currentPlanar, targetPlanar, rate * dt);
            ctx.DesiredVelocity.x = nextPlanar.x;
            ctx.DesiredVelocity.z = nextPlanar.z;
        }

        /// <summary>
        /// Lazily sorts registered modules by <see cref="IMotorModule.ExecutionOrder"/> with ordinal <see cref="IMotorModule.ModuleId"/> tie-breaking.
        /// </summary>
        private void EnsureSorted()
        {
            if (!_orderDirty) return;
            _orderDirty = false;
            _modules.Sort((a, b) =>
            {
                int cmp = a.ExecutionOrder.CompareTo(b.ExecutionOrder);
                return cmp != 0 ? cmp : string.Compare(a.ModuleId, b.ModuleId, StringComparison.Ordinal);
            });
        }
    }
}
