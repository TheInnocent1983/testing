using Godot;
using Parkour.Movement;

namespace Parkour.UI;

public partial class PauseSettingsMenu : CanvasLayer
{
	[ExportGroup("Component Reference")]
	[Export] public WallRunComponent WallRunComp { get; set; }

	[ExportGroup("Buttons")]
	[Export] public Button ResumeButton { get; set; }
	[Export] public Button SettingsButton { get; set; }
	[Export] public Button MainMenuButton { get; set; }

	[Export(PropertyHint.File, "*.tscn")] 
	public string MainMenuPath { get; set; } = "res://Scenes/UI/MainMenu.tscn";

	private bool _isPaused;

	public override void _Ready()
	{
		Visible = false;
		
		ProcessMode = ProcessModeEnum.Always;

		if (ResumeButton != null)
			ResumeButton.Pressed += TogglePause;

		if (MainMenuButton != null)
			MainMenuButton.Pressed += OnMainMenuPressed;
	}

	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("ui_cancel"))
		{
			TogglePause();
			GetViewport().SetInputAsHandled(); // Prevents other scripts from eating it
		}
	}

	public void TogglePause()
	{
		_isPaused = !_isPaused;
		GetTree().Paused = _isPaused;
		
		Visible = _isPaused;

		Input.MouseMode = _isPaused ? Input.MouseModeEnum.Visible : Input.MouseModeEnum.Captured;
	}

	private void OnMainMenuPressed()
	{
		GetTree().Paused = false;
		GetTree().ChangeSceneToFile(MainMenuPath);
	}
}
