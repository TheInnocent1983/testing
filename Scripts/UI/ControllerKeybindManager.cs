using Godot;
using System;
using System.Collections.Generic;

namespace Parkour.UI;

public partial class ControllerKeybindManager : VBoxContainer
{
    [Export] private PackedScene _controllerKeybindRowScene; // Drag ControllerKeybindRow.tscn here
    [Export] private Control _keybindListContainer;          // Drag Controller KeybindList here
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

        if (_restoreDefaultsButton != null)
        {
            _restoreDefaultsButton.Pressed += OnRestoreDefaultsPressed;
        }
    }

    private void PopulateKeybinds()
    {
        if (_keybindListContainer == null || _controllerKeybindRowScene == null)
        {
            GD.PrintErr("ControllerKeybindManager: Missing export references!");
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

            _instancedRows.Add(rowInstance);
        }
    }

    private void OnRestoreDefaultsPressed()
    {
        InputMap.LoadFromProjectSettings();

        foreach (var row in _instancedRows)
        {
            row.UpdateUI();
        }
    }
}