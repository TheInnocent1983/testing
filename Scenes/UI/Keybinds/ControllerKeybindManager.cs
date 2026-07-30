using Godot;
using System;
using System.Collections.Generic;
using Parkour.UI.Settings;
using Parkour.Movement;

namespace Parkour.UI;

public partial class ControllerKeybindManager : VBoxContainer
{
	[ExportGroup("External Dependencies")]
	[Export] private DescriptionPanel _descriptionPanel;
	[Export] private CameraController _cameraController;

	[ExportGroup("Internal Sections")]
	[Export] private ControllerSettingsSection _aimingSection;
	[Export] private PackedScene _controllerKeybindRowScene;
	[Export] private Control _keybindListContainer;
	[Export] private Button _restoreDefaultsButton;

	private readonly Dictionary<string, string> _customActions = new()
	{
		{ "move_forward", "Move Forward" },
		{ "move_backwards", "Move Backwards" },
		{ "move_left", "Move Left" },
		{ "move_right", "Move Right" },
		{ "jump", "Jump" },
		{ "sprint", "Sprint" },
		{ "crouch_toggle", "Crouch" },
		{ "slide", "Slide" },
		{ "noclip", "Noclip"},
		{ "restart", "Restart"},
	};

	private readonly List<ControllerKeybindRow> _instancedRows = new();

	public override void _Ready()
	{
		PopulateKeybinds();

		// Inject dependencies into ControllerSettingsSection
		if (_aimingSection != null && _descriptionPanel != null)
		{
			_aimingSection.Initialize(_descriptionPanel, _cameraController);
		}
		else if (_aimingSection == null)
		{
			GD.PrintErr("[ControllerKeybindManager] Missing _aimingSection export reference!");
		}

		if (_restoreDefaultsButton != null)
		{
			_restoreDefaultsButton.Pressed += OnRestoreDefaultsPressed;
		}
	}

	private void PopulateKeybinds()
	{
		if (_keybindListContainer == null || _controllerKeybindRowScene == null)
		{
			GD.PrintErr("[ControllerKeybindManager] Missing export references!");
			return;
		}

		foreach (Node child in _keybindListContainer.GetChildren())
		{
			child.QueueFree();
		}
		_instancedRows.Clear();

		foreach (var (actionName, displayName) in _customActions)
		{
			if (!InputMap.HasAction(actionName)) continue;

			var rowInstance = _controllerKeybindRowScene.Instantiate<ControllerKeybindRow>();
			_keybindListContainer.AddChild(rowInstance);
			rowInstance.Setup(displayName, actionName);

			// Connect signal to handle global controller duplicate unbinding
			rowInstance.BindingChanged += OnBindingChanged;

			_instancedRows.Add(rowInstance);
		}
	}

	private void OnBindingChanged(string changedAction, InputEvent newEvent, int index)
	{
		if (newEvent == null) return;

		// 1. Look through all other custom actions
		foreach (var (actionName, _) in _customActions)
		{
			// Skip the action currently being bound
			if (actionName == changedAction) continue;

			var existingEvents = InputMap.ActionGetEvents(actionName);
			foreach (var evt in existingEvents)
			{
				// 2. Erase from other actions if the same button/trigger/stick axis is already used
				if (IsSameControllerInput(evt, newEvent))
				{
					InputMap.ActionEraseEvent(actionName, evt);
					break;
				}
			}
		}

		// 3. Refresh all controller UI rows
		foreach (var row in _instancedRows)
		{
			row.UpdateUI();
		}
	}

	private bool IsSameControllerInput(InputEvent e1, InputEvent e2)
	{
		// Joypad Buttons (A, B, X, Y, LB, RB, D-Pad, etc.)
		if (e1 is InputEventJoypadButton b1 && e2 is InputEventJoypadButton b2)
		{
			return b1.ButtonIndex == b2.ButtonIndex;
		}

		// Joypad Triggers or Analog Motion (LT, RT, Left Stick Axis, Right Stick Axis)
		if (e1 is InputEventJoypadMotion m1 && e2 is InputEventJoypadMotion m2)
		{
			// Compare axis (e.g. Axis 4 for LT, Axis 5 for RT) and direction (+1 or -1)
			return m1.Axis == m2.Axis && Mathf.Sign(m1.AxisValue) == Mathf.Sign(m2.AxisValue);
		}

		return false;
	}

	private void OnRestoreDefaultsPressed()
	{
		InputMap.LoadFromProjectSettings();

		if (_aimingSection != null)
		{
			_aimingSection.ResetToDefaults();
		}

		foreach (var row in _instancedRows)
		{
			row.UpdateUI();
		}
	}
}
