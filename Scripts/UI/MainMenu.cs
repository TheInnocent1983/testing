using Godot;
using System;

namespace Parkour.UI;

public partial class MainMenu : Control
{
	[Export(PropertyHint.File, "*.tscn")]
	public string FirstLevelPath { get; set; } = "res://Scenes/Levels/TestMaps/area_3d.tscn";


	[Export] public Button StartButton { get; private set; }
	[Export] public Button QuitButton { get; private set; }

	public override void _Ready()
	{
		Input.MouseMode = Input.MouseModeEnum.Visible;

		if (StartButton != null)
			StartButton.Pressed += OnStartButtonPressed;

		if (QuitButton != null)
			QuitButton.Pressed += OnQuitButtonPressed;
	}

	private void OnStartButtonPressed()
	{
		GetTree().ChangeSceneToFile(FirstLevelPath);
	}

	private void OnQuitButtonPressed()
	{
		GetTree().Quit();
	}
}
