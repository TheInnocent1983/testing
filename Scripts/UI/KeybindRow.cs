using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class KeybindRow : PanelContainer
{
    [Signal]
    public delegate void BindingChangedEventHandler(string actionName, InputEvent newEvent, int index);

    [Export] private Label _actionLabel;
    [Export] private Button _primaryButton;
    [Export] private Button _secondaryButton;

    private string _actionName;
    
    // Tracks currently listening button across ALL instances so only 1 can listen at a time!
    private static KeybindRow _currentlyListeningRow = null;
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

    private List<InputEvent> GetKbmEvents()
    {
        var allEvents = InputMap.ActionGetEvents(_actionName);
        return allEvents.Where(e => e is InputEventKey || e is InputEventMouseButton).ToList();
    }

    public void UpdateUI()
    {
        var kbmEvents = GetKbmEvents();

        _primaryButton.Text = kbmEvents.Count > 0 ? FormatInputEvent(kbmEvents[0]) : "[ Unbound ]";
        _secondaryButton.Text = kbmEvents.Count > 1 ? FormatInputEvent(kbmEvents[1]) : "[ Unbound ]";
    }

    private void StartListening(int index)
    {
        // Cancel any active listener first (reverts its text back to keybind)
        if (_currentlyListeningRow != null && _currentlyListeningRow != this)
        {
            _currentlyListeningRow.CancelListening();
        }

        _currentlyListeningRow = this;
        _currentlyListeningIndex = index;

        Button targetButton = index == 0 ? _primaryButton : _secondaryButton;
        targetButton.Text = "... Press Key / Scroll ...";
    }

    public void CancelListening()
    {
        _currentlyListeningRow = null;
        _currentlyListeningIndex = -1;
        UpdateUI();
    }

    private void OnButtonGuiInput(InputEvent @event, int index)
    {
        // If right-clicked while idle -> Clear/Unbind key
        if (_currentlyListeningRow == null && @event is InputEventMouseButton rightClick && rightClick.Pressed && rightClick.ButtonIndex == MouseButton.Right)
        {
            ClearBinding(index);
            GetViewport().SetInputAsHandled();
            return;
        }

        // Left-click on button initiates listening mode
        if (_currentlyListeningRow == null && @event is InputEventMouseButton leftClick && leftClick.Pressed && leftClick.ButtonIndex == MouseButton.Left)
        {
            StartListening(index);
            GetViewport().SetInputAsHandled();
            return;
        }

        // If THIS row is currently listening and receives a mouse click or wheel scroll -> Bind it!
        if (_currentlyListeningRow == this && _currentlyListeningIndex == index && @event is InputEventMouseButton mouseEvt && mouseEvt.Pressed)
        {
            ApplyNewBinding(mouseEvt, index);
            _currentlyListeningRow = null;
            _currentlyListeningIndex = -1;
            GetViewport().SetInputAsHandled();
            return;
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_currentlyListeningRow != this) return;

        // Cancel on Escape key
        if (@event is InputEventKey escapeEvt && escapeEvt.Pressed && escapeEvt.Keycode == Key.Escape)
        {
            CancelListening();
            GetViewport().SetInputAsHandled();
            return;
        }

        // Capture keyboard key presses
        if (@event is InputEventKey keyEvt && keyEvt.Pressed && !keyEvt.IsEcho())
        {
            ApplyNewBinding(keyEvt, _currentlyListeningIndex);
            _currentlyListeningRow = null;
            _currentlyListeningIndex = -1;
            GetViewport().SetInputAsHandled();
            return;
        }
    }

    private void ApplyNewBinding(InputEvent newEvent, int index)
    {
        var kbmEvents = GetKbmEvents();

        // Preserve Joypad inputs untouched
        var controllerEvents = InputMap.ActionGetEvents(_actionName)
            .Where(e => !(e is InputEventKey || e is InputEventMouseButton))
            .ToList();

        if (index < kbmEvents.Count)
            kbmEvents[index] = newEvent;
        else
            kbmEvents.Add(newEvent);

        if (kbmEvents.Count > 2)
            kbmEvents = kbmEvents.Take(2).ToList();

        InputMap.ActionEraseEvents(_actionName);

        foreach (var evt in kbmEvents)
            InputMap.ActionAddEvent(_actionName, evt);

        foreach (var evt in controllerEvents)
            InputMap.ActionAddEvent(_actionName, evt);

        UpdateUI();
        EmitSignal(SignalName.BindingChanged, _actionName, newEvent, index);
    }

    private void ClearBinding(int index)
    {
        var kbmEvents = GetKbmEvents();
        if (index >= kbmEvents.Count) return;

        var targetEvent = kbmEvents[index];
        InputMap.ActionEraseEvent(_actionName, targetEvent);

        UpdateUI();
    }

    private string FormatInputEvent(InputEvent @event)
    {
        if (@event is InputEventKey keyEvt)
        {
            return keyEvt.AsTextPhysicalKeycode();
        }

        if (@event is InputEventMouseButton mouseEvt)
        {
            return mouseEvt.ButtonIndex switch
            {
                MouseButton.Left => "LMB",
                MouseButton.Right => "RMB",
                MouseButton.Middle => "MMB",
                MouseButton.WheelUp => "Wheel Up",
                MouseButton.WheelDown => "Wheel Down",
                MouseButton.WheelLeft => "Wheel Left",
                MouseButton.WheelRight => "Wheel Right",
                MouseButton.Xbutton1 => "Mouse 4",
                MouseButton.Xbutton2 => "Mouse 5",
                _ => $"Mouse {mouseEvt.ButtonIndex}"
            };
        }

        return @event.AsText();
    }
}