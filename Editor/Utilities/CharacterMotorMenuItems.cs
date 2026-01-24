using CharacterMotor.Core;
using CharacterMotor.Motors;
using UnityEditor;
using UnityEngine;

namespace CharacterMotor.Editor.Utilities
{
    public static class CharacterMotorMenuItems
    {
        [MenuItem("GameObject/CharacterMotor/3D CharacterController Rig", false, 10)]
        private static void Create3DCharacterControllerRig(MenuCommand command)
        {
            var go = new GameObject("Character_CC3D");
            GameObjectUtility.SetParentAndAlign(go, command.context as GameObject);
            var cc = go.AddComponent<UnityEngine.CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            go.AddComponent<CharacterControllerMotor3D>();
            FinalizeRig(go);
        }

        [MenuItem("GameObject/CharacterMotor/3D Rigidbody Rig", false, 11)]
        private static void Create3DRigidbodyRig(MenuCommand command)
        {
            var go = new GameObject("Character_RB3D");
            GameObjectUtility.SetParentAndAlign(go, command.context as GameObject);
            var col = go.AddComponent<CapsuleCollider>();
            col.height = 1.8f;
            col.radius = 0.35f;
            col.center = new Vector3(0f, 0.9f, 0f);
            var rb = go.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            go.AddComponent<RigidbodyMotor3D>();
            FinalizeRig(go);
        }

        [MenuItem("GameObject/CharacterMotor/2D Platformer Rig (Rigidbody2D)", false, 12)]
        private static void Create2DPlatformerRig(MenuCommand command)
        {
            var go = new GameObject("Character_Platformer2D");
            GameObjectUtility.SetParentAndAlign(go, command.context as GameObject);
            go.AddComponent<BoxCollider2D>();
            var rb2d = go.AddComponent<Rigidbody2D>();
            rb2d.gravityScale = 0f;
            rb2d.freezeRotation = true;
            rb2d.interpolation = RigidbodyInterpolation2D.Interpolate;
            var motor = go.AddComponent<RigidbodyMotor2D>();
            motor.Mode = Motor2DMode.Platformer;
            motor.Settings = MotorSettings.DefaultPlatformer2D;
            FinalizeRig(go);
        }

        [MenuItem("GameObject/CharacterMotor/2D Top-Down Rig (Rigidbody2D)", false, 13)]
        private static void Create2DTopDownRig(MenuCommand command)
        {
            var go = new GameObject("Character_TopDown2D");
            GameObjectUtility.SetParentAndAlign(go, command.context as GameObject);
            go.AddComponent<CircleCollider2D>();
            var rb2d = go.AddComponent<Rigidbody2D>();
            rb2d.gravityScale = 0f;
            rb2d.freezeRotation = true;
            rb2d.interpolation = RigidbodyInterpolation2D.Interpolate;
            var motor = go.AddComponent<RigidbodyMotor2D>();
            motor.Mode = Motor2DMode.TopDown;
            motor.Settings = MotorSettings.DefaultTopDown2D;
            FinalizeRig(go);
        }

        private static void FinalizeRig(GameObject go)
        {
            Undo.RegisterCreatedObjectUndo(go, $"Create {go.name}");
            Selection.activeGameObject = go;
        }
    }
}
