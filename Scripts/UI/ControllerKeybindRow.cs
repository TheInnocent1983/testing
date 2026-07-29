using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class ControllerKeybindRow : PanelContainer
{
    [Signal]
    public delegate void BindingChangedEventHandler(string actionName, InputEvent newEvent, int index);

    [Export] private Label _actionLabel;
    [Export] private Button _primaryButton;
    [Export] private Button _secondaryButton;

    private string _actionName;

    // Static listener tracking across controller row instances
    private static ControllerKeybindRow _currentlyListeningRow = null;
    private static int _currentlyListeningIndex = -1;

    public override void _Ready()
    {
        _primaryButton.GuiInput += (evt) => OnButtonGuiInput(evt, 0);
        _secondaryButton.GuiInput += (evt) => OnButtonGuiInput(evt, 1);
    }

    public void Setup(string actionDisplayName, string actionName)
    {
        _actionName = actionName;
        _actionLabel.Text = actionDisplayName;
        UpdateUI();
    }

    /// <summary>
    /// STRICT FILTER: Filters strictly for Joypad Button & Joypad Motion events.
    /// Ignores all Keyboard/Mouse events!
    /// </summary>
    private List<InputEvent> GetControllerEvents()
    {
        var allEvents = InputMap.ActionGetEvents(_actionName);
        return allEvents.Where(e => e is InputEventJoypadButton || e is InputEventJoypadMotion).ToList();
    }

    public void UpdateUI()
    {
        var controllerEvents = GetControllerEvents();

        _primaryButton.Text = controllerEvents.Count > 0 ? FormatJoyEvent(controllerEvents[0]) : "[ Unbound ]";
        _secondaryButton.Text = controllerEvents.Count > 1 ? FormatJoyEvent(controllerEvents[1]) : "[ Unbound ]";
    }

    private void StartListening(int index)
    {
        if (_currentlyListeningRow != null && _currentlyListeningRow != this)
        {
            _currentlyListeningRow.CancelListening();
        }

        _currentlyListeningRow = this;
        _currentlyListeningIndex = index;

        Button targetButton = index == 0 ? _primaryButton : _secondaryButton;
        targetButton.Text = "... Press Button / Axis ...";
    }

    public void CancelListening()
    {
        _currentlyListeningRow = null;
        _currentlyListeningIndex = -1;
        UpdateUI();
    }

    private void OnButtonGuiInput(InputEvent @event, int index)
    {
        // Right-click unbinds the controller slot
        if (_currentlyListeningRow == null && @event is InputEventMouseButton rightClick && rightClick.Pressed && rightClick.ButtonIndex == MouseButton.Right)
        {
            ClearBinding(index);
            GetViewport().SetInputAsHandled();
            return;
        }

        // Left-click activates listening mode
        if (_currentlyListeningRow == null && @event is InputEventMouseButton leftClick && leftClick.Pressed && leftClick.ButtonIndex == MouseButton.Left)
        {
            StartListening(index);
            GetViewport().SetInputAsHandled();
            return;
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_currentlyListeningRow != this) return;

        // 1. Cancel on Keyboard Escape
        if (@event is InputEventKey escapeEvt && escapeEvt.Pressed && escapeEvt.Keycode == Key.Escape)
        {
            CancelListening();
            GetViewport().SetInputAsHandled();
            return;
        }

        // 2. Cancel on Controller Select / Share / Back or Start / Options
        if (@event is InputEventJoypadButton cancelBtn && cancelBtn.Pressed)
        {
            if (cancelBtn.ButtonIndex == JoyButton.Back || cancelBtn.ButtonIndex == JoyButton.Start)
            {
                CancelListening();
                GetViewport().SetInputAsHandled();
                return;
            }

            // Capture any OTHER Joypad Button press as the new keybind
            ApplyNewBinding(cancelBtn, _currentlyListeningIndex);
            _currentlyListeningRow = null;
            _currentlyListeningIndex = -1;
            GetViewport().SetInputAsHandled();
            return;
        }

        // 3. Capture Joypad Motion (Triggers & Thumbstick axes)
        if (@event is InputEventJoypadMotion joyMotion && Mathf.Abs(joyMotion.AxisValue) > 0.5f)
        {
            ApplyNewBinding(joyMotion, _currentlyListeningIndex);
            _currentlyListeningRow = null;
            _currentlyListeningIndex = -1;
            GetViewport().SetInputAsHandled();
            return;
        }
    }

    private void ApplyNewBinding(InputEvent newEvent, int index)
    {
        var controllerEvents = GetControllerEvents();

        // Preserve all KBM events intact
        var kbmEvents = InputMap.ActionGetEvents(_actionName)
            .Where(e => e is InputEventKey || e is InputEventMouseButton)
            .ToList();

        if (index < controllerEvents.Count)
            controllerEvents[index] = newEvent;
        else
            controllerEvents.Add(newEvent);

        if (controllerEvents.Count > 2)
            controllerEvents = controllerEvents.Take(2).ToList();

        InputMap.ActionEraseEvents(_actionName);

        // Re-add preserved KBM events first, then updated Controller events
        foreach (var evt in kbmEvents)
            InputMap.ActionAddEvent(_actionName, evt);

        foreach (var evt in controllerEvents)
            InputMap.ActionAddEvent(_actionName, evt);

        UpdateUI();
        EmitSignal(SignalName.BindingChanged, _actionName, newEvent, index);
    }

    private void ClearBinding(int index)
    {
        var controllerEvents = GetControllerEvents();
        if (index >= controllerEvents.Count) return;

        var targetEvent = controllerEvents[index];
        InputMap.ActionEraseEvent(_actionName, targetEvent);

        UpdateUI();
    }

    private string FormatJoyEvent(InputEvent @event)
    {
        if (@event is InputEventJoypadButton joyBtn)
        {
            return joyBtn.ButtonIndex switch
            {
                JoyButton.A => "Button A (Cross)",
                JoyButton.B => "Button B (Circle)",
                JoyButton.X => "Button X (Square)",
                JoyButton.Y => "Button Y (Triangle)",
                JoyButton.LeftShoulder => "LB / L1",
                JoyButton.RightShoulder => "RB / R1",
                JoyButton.LeftStick => "LS Click",
                JoyButton.RightStick => "RS Click",
                JoyButton.Back => "Select / View",
                JoyButton.Start => "Start / Options",
                JoyButton.DpadUp => "D-Pad Up",
                JoyButton.DpadDown => "D-Pad Down",
                JoyButton.DpadLeft => "D-Pad Left",
                JoyButton.DpadRight => "D-Pad Right",
                _ => $"Joy Button {joyBtn.ButtonIndex}"
            };
        }

        if (@event is InputEventJoypadMotion joyMotion)
        {
            bool isPositive = joyMotion.AxisValue > 0;

            return joyMotion.Axis switch
            {
                JoyAxis.LeftX => isPositive ? "L-Stick Right" : "L-Stick Left",
                JoyAxis.LeftY => isPositive ? "L-Stick Down" : "L-Stick Up",      // Negative Y is Up in Godot!
                JoyAxis.RightX => isPositive ? "R-Stick Right" : "R-Stick Left",
                JoyAxis.RightY => isPositive ? "R-Stick Down" : "R-Stick Up",
                JoyAxis.TriggerLeft => "LT / L2 Trigger",
                JoyAxis.TriggerRight => "RT / R2 Trigger",
                _ => $"Axis {joyMotion.Axis}"
            };
        }

        return "[ Unbound ]";
    }
}