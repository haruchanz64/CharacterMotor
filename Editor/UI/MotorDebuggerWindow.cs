using System.Collections.Generic;
using CharacterMotor.Core;
using CharacterMotor.Editor.Inspectors;
using CharacterMotor.Motors;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace CharacterMotor.Editor.UI
{
    /// <summary>
    /// Lean UI Toolkit debugger displaying live state and toggleable IMotorModule pipelines
    /// for active ICharacterMotor instances in Play Mode.
    /// </summary>
    public sealed class MotorDebuggerWindow : EditorWindow
    {
        private DropdownField _motorDropdown;
        private Label _groundedBadge;
        private Label _dimensionBadge;
        private Label _telemetryLabel;
        private VisualElement _modulesContainer;
        private int _selectedIndex;

        [MenuItem("Tools/HaruChanZ64/Gameplay/CharacterMotor/Motor Debugger", false, 130)]
        public static void ShowWindow()
        {
            var window = GetWindow<MotorDebuggerWindow>("Motor Debugger");
            window.minSize = new Vector2(300f, 240f);
            window.Show();
        }

        public void CreateGUI()
        {
            rootVisualElement.Clear();
            rootVisualElement.AddToClassList("cm-root");
            CharacterMotorInspector.AttachStyles(rootVisualElement);

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1f;
            scroll.style.minHeight = 0f;

            var topCard = new VisualElement();
            topCard.AddToClassList("cm-card");

            var title = new Label("Active CharacterMotor");
            title.AddToClassList("cm-section-title");
            topCard.Add(title);

            _motorDropdown = new DropdownField("Motor", new List<string> { "(None Active)" }, 0);
            _motorDropdown.RegisterValueChangedCallback(evt =>
            {
                _selectedIndex = Mathf.Max(0, _motorDropdown.index);
                RefreshView();
            });
            topCard.Add(_motorDropdown);

            var badgeRow = new VisualElement();
            badgeRow.style.flexDirection = FlexDirection.Row;
            badgeRow.style.flexWrap = Wrap.Wrap;
            badgeRow.style.marginTop = 6f;
            _groundedBadge = new Label("IDLE");
            _groundedBadge.AddToClassList("cm-badge");
            _dimensionBadge = new Label("-");
            _dimensionBadge.AddToClassList("cm-badge");
            badgeRow.Add(_groundedBadge);
            badgeRow.Add(_dimensionBadge);
            topCard.Add(badgeRow);

            _telemetryLabel = new Label("Enter Play Mode with an active CharacterMotor component.");
            _telemetryLabel.AddToClassList("cm-mono");
            _telemetryLabel.style.marginTop = 6f;
            topCard.Add(_telemetryLabel);
            scroll.Add(topCard);

            var modCard = new VisualElement();
            modCard.AddToClassList("cm-card");
            var modTitle = new Label("Pipeline Modules (Sorted by ExecutionOrder)");
            modTitle.AddToClassList("cm-section-title");
            modCard.Add(modTitle);

            _modulesContainer = new VisualElement();
            modCard.Add(_modulesContainer);
            scroll.Add(modCard);
            rootVisualElement.Add(scroll);

            rootVisualElement.schedule.Execute(RefreshView).Every(100);
            RefreshView();
        }

        private void RefreshView()
        {
            if (_motorDropdown == null) return;

            var active = CharacterMotorBase.ActiveMotors;
            if (active.Count == 0)
            {
                _motorDropdown.choices = new List<string> { "(None Active)" };
                _motorDropdown.SetValueWithoutNotify("(None Active)");
                _groundedBadge.text = "IDLE";
                _groundedBadge.EnableInClassList("cm-badge--ok", false);
                _groundedBadge.EnableInClassList("cm-badge--air", false);
                _dimensionBadge.text = "-";
                _telemetryLabel.text = "No active ICharacterMotor registered. Enter Play Mode to inspect.";
                _modulesContainer.Clear();
                return;
            }

            var names = new List<string>(active.Count);
            for (int i = 0; i < active.Count; i++)
            {
                names.Add(active[i].MotorName);
            }

            _selectedIndex = Mathf.Clamp(_selectedIndex, 0, active.Count - 1);
            _motorDropdown.choices = names;
            _motorDropdown.SetValueWithoutNotify(names[_selectedIndex]);

            ICharacterMotor motor = active[_selectedIndex];
            MotorState s = motor.State;

            _groundedBadge.text = s.IsGrounded ? "GROUNDED" : "AIRBORNE";
            _groundedBadge.EnableInClassList("cm-badge--ok", s.IsGrounded);
            _groundedBadge.EnableInClassList("cm-badge--air", !s.IsGrounded);
            _dimensionBadge.text = motor.Dimension == MotorDimension.Mode2D ? "2D" : "3D";

            float planarSpeed = motor.Dimension == MotorDimension.Mode2D
                ? Mathf.Abs(s.Velocity.x)
                : new Vector2(s.Velocity.x, s.Velocity.z).magnitude;

            _telemetryLabel.text =
                $"Position: ({s.Position.x:F2}, {s.Position.y:F2}, {s.Position.z:F2})\n" +
                $"Velocity: ({s.Velocity.x:F2}, {s.Velocity.y:F2}, {s.Velocity.z:F2}) | Planar: {planarSpeed:F2} u/s\n" +
                $"Slope: {s.SlopeAngle:F1}° | TimeSinceGrounded: {s.TimeSinceGrounded:F2}s";

            RebuildModuleToggles(motor);
        }

        private void RebuildModuleToggles(ICharacterMotor motor)
        {
            _modulesContainer.Clear();
            var mods = motor.Modules;
            if (mods.Count == 0)
            {
                var empty = new Label("No modules attached to this motor.");
                empty.AddToClassList("cm-muted");
                _modulesContainer.Add(empty);
                return;
            }

            for (int i = 0; i < mods.Count; i++)
            {
                IMotorModule mod = mods[i];
                var toggle = new Toggle($"[{mod.ExecutionOrder}] {mod.ModuleId}")
                {
                    value = mod.ModuleEnabled
                };
                toggle.RegisterValueChangedCallback(evt => mod.ModuleEnabled = evt.newValue);
                _modulesContainer.Add(toggle);
            }
        }
    }
}
