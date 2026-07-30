using Godot;
using System;
using System.Collections.Generic;
using Parkour.UI.Settings;

public partial class KeybindManager : VBoxContainer
{
	[ExportGroup("External Dependencies")]
	[Export] private DescriptionPanel _descriptionPanel;
	[Export] private Parkour.Movement.CameraController _cameraController;

	[ExportGroup("Internal Sections")]
	[Export] private MouseSettingsSection _mouseSection;
	[Export] private PackedScene _keybindRowScene;
	[Export] private Control _keybindListContainer;
	[Export] private Button _restoreDefaultsButton;

	// Map internal InputMap action names -> clean UI display names
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

	private readonly List<KeybindRow> _instancedRows = new();

	public override void _Ready()
	{
		PopulateKeybinds();

		// Inject dependencies into the MouseSection sub-scene!
		if (_mouseSection != null && _descriptionPanel != null)
		{
			_mouseSection.Initialize(_descriptionPanel, _cameraController);
		}
		else if (_mouseSection == null || _descriptionPanel == null)
		{
			GD.PrintErr("[KeybindManager] Missing _mouseSection or _descriptionPanel export reference!");
		}

		if (_restoreDefaultsButton != null)
		{
			_restoreDefaultsButton.Pressed += OnRestoreDefaultsPressed;
		}
	}

	private void PopulateKeybinds()
	{
		if (_keybindListContainer == null || _keybindRowScene == null)
		{
			GD.PrintErr("[KeybindManager] _keybindListContainer or _keybindRowScene is null!");
			return;
		}

		// Clear any old UI rows
		foreach (Node child in _keybindListContainer.GetChildren())
		{
			child.QueueFree();
		}
		_instancedRows.Clear();

		// Loop over configured actions
		foreach (var (actionName, displayName) in _customActions)
		{
			if (!InputMap.HasAction(actionName)) continue;

			var rowInstance = _keybindRowScene.Instantiate<KeybindRow>();
			_keybindListContainer.AddChild(rowInstance);
			rowInstance.Setup(displayName, actionName);

			// Connect signal to handle global duplicate unbinding
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
			// Skip the action we just bound
			if (actionName == changedAction) continue;

			var existingEvents = InputMap.ActionGetEvents(actionName);
			foreach (var evt in existingEvents)
			{
				// 2. If another action shares the exact same key/mouse button, remove it
				if (IsSameInput(evt, newEvent))
				{
					InputMap.ActionEraseEvent(actionName, evt);
					break;
				}
			}
		}

		// 3. Refresh all UI rows so cleared slots show [ Unbound ]
		foreach (var row in _instancedRows)
		{
			row.UpdateUI();
		}
	}

	private bool IsSameInput(InputEvent e1, InputEvent e2)
	{
		// Keyboard comparison
		if (e1 is InputEventKey k1 && e2 is InputEventKey k2)
		{
			var key1 = k1.PhysicalKeycode != Key.None ? k1.PhysicalKeycode : k1.Keycode;
			var key2 = k2.PhysicalKeycode != Key.None ? k2.PhysicalKeycode : k2.Keycode;
			return key1 == key2;
		}

		// Mouse button comparison
		if (e1 is InputEventMouseButton m1 && e2 is InputEventMouseButton m2)
		{
			return m1.ButtonIndex == m2.ButtonIndex;
		}

		return false;
	}

	private void OnRestoreDefaultsPressed()
	{
		// Load default project settings input map
		InputMap.LoadFromProjectSettings();

		// Also reset mouse section if attached
		if (_mouseSection != null)
		{
			_mouseSection.ResetToDefaults();
		}

		// Update all UI rows to reflect restored defaults
		foreach (var row in _instancedRows)
		{
			row.UpdateUI();
		}
	}
}
