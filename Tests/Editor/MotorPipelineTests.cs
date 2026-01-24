using System.Collections.Generic;
using CharacterMotor.Core;
using NUnit.Framework;
using UnityEngine;

namespace CharacterMotor.Tests
{
    public sealed class MotorPipelineTests
    {
        private sealed class OrderRecorderModule : IMotorModule
        {
            public string ModuleId { get; }
            public int ExecutionOrder { get; }
            public bool ModuleEnabled { get; set; } = true;

            private readonly List<string> _log;

            public OrderRecorderModule(string id, int order, List<string> log)
            {
                ModuleId = id;
                ExecutionOrder = order;
                _log = log;
            }

            public void OnAttach(ICharacterMotor motor) { }
            public void OnDetach(ICharacterMotor motor) { }
            public void BeforeVelocitySolve(ref MotorContext ctx) => _log.Add($"before:{ModuleId}");
            public void ModifyVelocity(ref MotorContext ctx) => _log.Add($"modify:{ModuleId}");
            public void AfterMove(ref MotorContext ctx) => _log.Add($"after:{ModuleId}");
        }

        private sealed class DashOverrideModule : IMotorModule
        {
            public string ModuleId => "TestDash";
            public int ExecutionOrder => -10;
            public bool ModuleEnabled { get; set; } = true;

            public void OnAttach(ICharacterMotor motor) { }
            public void OnDetach(ICharacterMotor motor) { }

            public void BeforeVelocitySolve(ref MotorContext ctx)
            {
                if (ctx.Input.HasAction(1))
                {
                    ctx.SuppressBaseMovement = true;
                    ctx.ApplyGravity = false;
                }
            }

            public void ModifyVelocity(ref MotorContext ctx)
            {
                if (ctx.Input.HasAction(1))
                {
                    ctx.DesiredVelocity = new Vector3(20f, 0f, 0f);
                }
            }

            public void AfterMove(ref MotorContext ctx) { }
        }

        [Test]
        public void Pipeline_BaseMovementJumpAndGravityWorkDeterministically()
        {
            var pipeline = new MotorPipeline();
            var settings = MotorSettings.Default3D;

            var ctx = new MotorContext
            {
                DeltaTime = 0.5f,
                Dimension = MotorDimension.Mode3D,
                IsTopDown2D = false,
                Input = new MotorInput { Move = new Vector2(1f, 0f), JumpPressed = true },
                MoveDirectionWorld = Vector3.right,
                State = new MotorState { IsGrounded = true, Velocity = Vector3.zero, GroundNormal = Vector3.up },
                Settings = settings
            };

            pipeline.SolveVelocity(ref ctx);

            Assert.IsTrue(ctx.JumpedThisStep);
            Assert.AreEqual(settings.maxSpeed, ctx.DesiredVelocity.x, 0.01f);
            // Jump applied, then gravity integrated over dt
            Assert.AreEqual(settings.jumpSpeed - (settings.gravity * ctx.DeltaTime), ctx.DesiredVelocity.y, 0.01f);
        }

        [Test]
        public void Pipeline_ExecutesModulesSortedByExecutionOrderAndSkipsDisabled()
        {
            var log = new List<string>();
            var pipeline = new MotorPipeline();
            var late = new OrderRecorderModule("Late", 20, log);
            var early = new OrderRecorderModule("Early", -5, log);
            var disabled = new OrderRecorderModule("Disabled", 0, log) { ModuleEnabled = false };

            pipeline.AddModule(late);
            pipeline.AddModule(early);
            pipeline.AddModule(disabled);

            var ctx = new MotorContext
            {
                DeltaTime = 0.02f,
                Dimension = MotorDimension.Mode3D,
                Settings = MotorSettings.Default3D,
                State = new MotorState { IsGrounded = true }
            };

            pipeline.SolveVelocity(ref ctx);
            pipeline.NotifyAfterMove(ref ctx);

            CollectionAssert.AreEqual(
                new[] { "before:Early", "before:Late", "modify:Early", "modify:Late", "after:Early", "after:Late" },
                log);
        }

        [Test]
        public void Pipeline_ModuleCanSuppressBaseMovementAndApplyCustomVelocityAndImpulses()
        {
            var pipeline = new MotorPipeline();
            pipeline.AddModule(new DashOverrideModule());
            pipeline.QueueImpulse(new Vector3(0f, 5f, 0f));

            var input = new MotorInput { Move = new Vector2(1f, 0f) };
            input.SetAction(1, true);

            var ctx = new MotorContext
            {
                DeltaTime = 0.02f,
                Dimension = MotorDimension.Mode2D,
                IsTopDown2D = false,
                Input = input,
                Settings = MotorSettings.DefaultPlatformer2D,
                State = new MotorState { IsGrounded = false }
            };

            pipeline.SolveVelocity(ref ctx);
            Assert.AreEqual(20f, ctx.DesiredVelocity.x, 0.01f);
            Assert.AreEqual(5f, ctx.DesiredVelocity.y, 0.01f);
        }

        [Test]
        public void Pipeline_TopDown2DMovesOmnidirectionallyWithoutGravity()
        {
            var pipeline = new MotorPipeline();
            var settings = MotorSettings.DefaultTopDown2D;

            var ctx = new MotorContext
            {
                DeltaTime = 1f,
                Dimension = MotorDimension.Mode2D,
                IsTopDown2D = true,
                Input = new MotorInput { Move = new Vector2(1f, 1f) },
                Settings = settings,
                State = new MotorState { IsGrounded = true, Velocity = Vector3.zero }
            };

            pipeline.SolveVelocity(ref ctx);
            Vector2 v = new Vector2(ctx.DesiredVelocity.x, ctx.DesiredVelocity.y);
            Assert.AreEqual(settings.maxSpeed, v.magnitude, 0.01f);
            Assert.AreEqual(0f, ctx.DesiredVelocity.z, 0.001f);
        }
    }
}
