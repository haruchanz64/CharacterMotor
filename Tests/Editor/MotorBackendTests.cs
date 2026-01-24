using CharacterMotor.Core;
using CharacterMotor.Motors;
using NUnit.Framework;
using UnityEngine;

namespace CharacterMotor.Tests
{
    public sealed class MotorBackendTests
    {
        private sealed class LifecycleModule : IMotorModule
        {
            public string ModuleId => "Lifecycle";
            public int ExecutionOrder => 0;
            public bool ModuleEnabled { get; set; } = true;
            public bool Attached { get; private set; }

            public void OnAttach(ICharacterMotor motor) => Attached = motor != null;
            public void OnDetach(ICharacterMotor motor) => Attached = false;
            public void BeforeVelocitySolve(ref MotorContext ctx) { }
            public void ModifyVelocity(ref MotorContext ctx) { }
            public void AfterMove(ref MotorContext ctx) { }
        }

        [Test]
        public void AllThreeMotorBackends_ImplementICharacterMotorAndModuleLifecycle()
        {
            var goCC = new GameObject("TestCC3D");
            var goRB3D = new GameObject("TestRB3D");
            var goRB2D = new GameObject("TestRB2D");

            try
            {
                var ccMotor = goCC.AddComponent<CharacterControllerMotor3D>();
                var rb3dMotor = goRB3D.AddComponent<RigidbodyMotor3D>();
                var rb2dMotor = goRB2D.AddComponent<RigidbodyMotor2D>();

                Assert.AreEqual(MotorDimension.Mode3D, ccMotor.Dimension);
                Assert.AreEqual(MotorDimension.Mode3D, rb3dMotor.Dimension);
                Assert.AreEqual(MotorDimension.Mode2D, rb2dMotor.Dimension);

                var mod = new LifecycleModule();
                rb2dMotor.AddModule(mod);
                Assert.IsTrue(mod.Attached);
                Assert.AreSame(mod, rb2dMotor.GetModule<LifecycleModule>());

                rb2dMotor.RemoveModule(mod);
                Assert.IsFalse(mod.Attached);
                Assert.IsNull(rb2dMotor.GetModule<LifecycleModule>());
            }
            finally
            {
                Object.DestroyImmediate(goCC);
                Object.DestroyImmediate(goRB3D);
                Object.DestroyImmediate(goRB2D);
            }
        }

        [Test]
        public void TransformReferenceFrame_TransformsCameraRelativeMoveOnXZPlane()
        {
            var camGo = new GameObject("MockCam");
            try
            {
                // Facing +X (90 degrees yaw): forward input (+Y) should map to +X, right input (+X) to -Z
                camGo.transform.rotation = Quaternion.Euler(25f, 90f, 0f);
                var frame = new TransformReferenceFrame(camGo.transform);

                Vector3 forwardMove = frame.TransformMoveInput(new Vector2(0f, 1f), Vector3.up);
                Vector3 rightMove = frame.TransformMoveInput(new Vector2(1f, 0f), Vector3.up);

                Assert.AreEqual(1f, forwardMove.x, 0.01f);
                Assert.AreEqual(0f, forwardMove.y, 0.01f);
                Assert.AreEqual(0f, forwardMove.z, 0.01f);

                Assert.AreEqual(0f, rightMove.x, 0.01f);
                Assert.AreEqual(0f, rightMove.y, 0.01f);
                Assert.AreEqual(-1f, rightMove.z, 0.01f);
            }
            finally
            {
                Object.DestroyImmediate(camGo);
            }
        }
    }
}
