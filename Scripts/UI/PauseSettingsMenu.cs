using Godot;
using Parkour.Movement;

namespace Parkour.UI;

public partial class PauseSettingsMenu : CanvasLayer
{
	[ExportGroup("Component Reference")]
	[Export] public WallRunComponent WallRunComp { get; set; }

	[ExportGroup("Sliders")]
	[Export] public HSlider SpeedSlider { get; set; }
	[Export] public HSlider GravitySlider { get; set; }

	[ExportGroup("Buttons")]
	[Export] public Button ResumeButton { get; set; }
	[Export] public Button MainMenuButton { get; set; }

	[Export(PropertyHint.File, "*.tscn")] 
	public string MainMenuPath { get; set; } = "res://Scenes/UI/MainMenu.tscn";

	private bool _isPaused;

	public override void _Ready()
	{
		Visible = false;
		
		ProcessMode = ProcessModeEnum.Always;

		if (SpeedSlider != null && WallRunComp != null)
		{
			SpeedSlider.MinValue = 5.0f;
			SpeedSlider.MaxValue = 30.0f;
			SpeedSlider.Value = WallRunComp.WallRunSpeed;
			SpeedSlider.ValueChanged += (double value) => WallRunComp.WallRunSpeed = (float)value;
		}

		if (GravitySlider != null && WallRunComp != null)
		{
			GravitySlider.MinValue = 0.0f;
			GravitySlider.MaxValue = 10.0f;
			GravitySlider.Value = WallRunComp.WallRunGravity;
			GravitySlider.ValueChanged += (double value) => WallRunComp.WallRunGravity = (float)value;
		}

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
