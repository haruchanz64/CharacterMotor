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

        public IReadOnlyList<IMotorModule> Modules
        {
            get
            {
                EnsureSorted();
                return _modules;
            }
        }

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

        public void QueueImpulse(Vector3 impulse)
        {
            _pendingImpulse += impulse;
        }

        public void ClearPendingImpulse()
        {
            _pendingImpulse = Vector3.zero;
        }

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
