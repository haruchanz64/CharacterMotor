using System.Collections.Generic;
using CharacterMotor.Core;
using CharacterMotor.Editor.UI;
using CharacterMotor.Motors;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace CharacterMotor.Editor.Inspectors
{
    [CustomEditor(typeof(CharacterMotorBase), true)]
    public sealed class CharacterMotorInspector : UnityEditor.Editor
    {
        private VisualElement _moduleList;
        private Label _runtimeStateLabel;

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            AttachStyles(root);

            InspectorElement.FillDefaultInspector(root, serializedObject, this);

            var card = new VisualElement();
            card.AddToClassList("cm-card");

            var title = new Label("Modular Pipeline (IMotorModule)");
            title.AddToClassList("cm-section-title");
            card.Add(title);

            _moduleList = new VisualElement();
            card.Add(_moduleList);

            _runtimeStateLabel = new Label();
            _runtimeStateLabel.AddToClassList("cm-mono");
            _runtimeStateLabel.style.marginTop = 6f;
            card.Add(_runtimeStateLabel);

            var openDbgBtn = new Button(MotorDebuggerWindow.ShowWindow) { text = "Open Motor Debugger" };
            openDbgBtn.style.marginTop = 6f;
            card.Add(openDbgBtn);

            root.Add(card);

            RefreshSummary();
            root.schedule.Execute(RefreshSummary).Every(150);
            return root;
        }

        private void RefreshSummary()
        {
            if (target == null || _moduleList == null) return;
            var motor = (CharacterMotorBase)target;

            _moduleList.Clear();
            var modules = CollectModulesForPreview(motor);
            if (modules.Count == 0)
            {
                var empty = new Label("No IMotorModule attached. Attach a MotorModuleBehaviour or call motor.AddModule() at runtime.");
                empty.AddToClassList("cm-muted");
                empty.style.whiteSpace = WhiteSpace.Normal;
                _moduleList.Add(empty);
            }
            else
            {
                for (int i = 0; i < modules.Count; i++)
                {
                    var mod = modules[i];
                    var row = new VisualElement();
                    row.AddToClassList("cm-row");
                    var lbl = new Label($"[{mod.ExecutionOrder}] {mod.ModuleId}");
                    lbl.AddToClassList("cm-mono");
                    var state = new Label(mod.ModuleEnabled ? "ON" : "OFF");
                    state.AddToClassList("cm-badge");
                    if (mod.ModuleEnabled) state.AddToClassList("cm-badge--ok");
                    row.Add(lbl);
                    row.Add(state);
                    _moduleList.Add(row);
                }
            }

            if (Application.isPlaying)
            {
                MotorState s = motor.State;
                _runtimeStateLabel.text =
                    $"Vel: ({s.Velocity.x:F2}, {s.Velocity.y:F2}, {s.Velocity.z:F2}) | " +
                    $"Grounded: {s.IsGrounded} | Slope: {s.SlopeAngle:F1}° | AirTime: {s.TimeSinceGrounded:F2}s";
            }
            else
            {
                _runtimeStateLabel.text = "Enter Play Mode to inspect live velocity and grounding state.";
            }
        }

        private static List<IMotorModule> CollectModulesForPreview(CharacterMotorBase motor)
        {
            var result = new List<IMotorModule>();
            if (Application.isPlaying)
            {
                var active = motor.Modules;
                for (int i = 0; i < active.Count; i++) result.Add(active[i]);
                return result;
            }

            var behaviours = motor.GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IMotorModule mod) result.Add(mod);
            }
            result.Sort((a, b) => a.ExecutionOrder.CompareTo(b.ExecutionOrder));
            return result;
        }

        internal static void AttachStyles(VisualElement target)
        {
            const string pkgPath = "Packages/com.haruchanz64.character-motor/Editor/UI/CharacterMotorStyles.uss";
            var sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(pkgPath);
            if (sheet == null)
            {
                string[] guids = AssetDatabase.FindAssets("CharacterMotorStyles t:StyleSheet");
                if (guids.Length > 0)
                {
                    sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(AssetDatabase.GUIDToAssetPath(guids[0]));
                }
            }
            if (sheet != null) target.styleSheets.Add(sheet);
        }
    }
}
