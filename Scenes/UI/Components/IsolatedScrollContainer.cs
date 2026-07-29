using Godot;

public partial class IsolatedScrollContainer : ScrollContainer
{
    private bool _isHovered = false;

    // Adjust scroll speed (in pixels) to match your UI feel
    [Export] private int _scrollStep = 30;

    public override void _Ready()
    {
        MouseEntered += () => _isHovered = true;
        MouseExited += () => _isHovered = false;
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseBtn && mouseBtn.Pressed)
        {
            if (mouseBtn.ButtonIndex == MouseButton.WheelUp)
            {
                // Manually move the scrollbar up
                ScrollVertical = Mathf.Max(0, ScrollVertical - _scrollStep);
                
                // Stop Godot from sending this event up to the parent page!
                GetViewport().SetInputAsHandled();
                return;
            }
            else if (mouseBtn.ButtonIndex == MouseButton.WheelDown)
            {
                // Get maximum possible scroll value
                var maxScroll = (int)GetVScrollBar().MaxValue - (int)Size.Y;
                
                // Manually move the scrollbar down
                ScrollVertical = Mathf.Min(maxScroll, ScrollVertical + _scrollStep);
                
                // Stop Godot from sending this event up to the parent page!
                GetViewport().SetInputAsHandled();
                return;
            }
        }

        base._GuiInput(@event);
    }
}