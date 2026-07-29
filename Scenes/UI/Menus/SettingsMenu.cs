using Godot;

namespace Parkour.UI;

public partial class SettingsMenu : CanvasLayer
{
	[ExportGroup("Tab Navigation")]
	[Export] public TabContainer ContentTabContainer { get; set; }
	[Export] public Button GameButton { get; set; }
	[Export] public Button MouseKBButton { get; set; }
	[Export] public Button ControllerButton { get; set; }
	[Export] public Button GraphicsButton { get; set; }
	[Export] public Button AudioButton { get; set; }

	[ExportGroup("Footer")]
	[Export] public Button BackButton { get; set; }

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;

		// Connect each top button to switch to its tab index
		if (GameButton != null) 
			GameButton.Pressed += () => SwitchToTab(0);

		if (MouseKBButton != null) 
			MouseKBButton.Pressed += () => SwitchToTab(1);

		if (ControllerButton != null) 
			ControllerButton.Pressed += () => SwitchToTab(2);

		if (GraphicsButton != null) 
			GraphicsButton.Pressed += () => SwitchToTab(3);

		if (AudioButton != null) 
			AudioButton.Pressed += () => SwitchToTab(4);

		if (BackButton != null)
			BackButton.Pressed += OnBackButtonPressed;

		// Default to Game tab on open
		SwitchToTab(0);
	}

	private void SwitchToTab(int tabIndex)
	{
		if (ContentTabContainer != null)
		{
			ContentTabContainer.CurrentTab = tabIndex;
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("ui_cancel"))
		{
			OnBackButtonPressed();
			GetViewport().SetInputAsHandled();
		}
	}

	private void OnBackButtonPressed()
	{
		Visible = false;
	}
}
