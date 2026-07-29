using Godot;
using System;
using System.Collections.Generic;

public partial class KeybindManager : VBoxContainer
{
	[Export] private PackedScene _keybindRowScene;
	[Export] private Control _keybindListContainer; // Your KeybindList VBoxContainer
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

		if (_restoreDefaultsButton != null)
		{
			_restoreDefaultsButton.Pressed += OnRestoreDefaultsPressed;
		}
	}

	private void PopulateKeybinds()
	{
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

			_instancedRows.Add(rowInstance);
		}
	}

	private void OnRestoreDefaultsPressed()
	{
		// Load default project settings input map
		InputMap.LoadFromProjectSettings();

		// Update all UI rows to reflect restored defaults
		foreach (var row in _instancedRows)
		{
			row.UpdateUI();
		}
	}
}
