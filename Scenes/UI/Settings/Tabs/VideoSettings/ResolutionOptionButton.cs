using Godot;
using System.Collections.Generic;

namespace Parkour.UI.Settings.Video;

public partial class ResolutionOptionButton : OptionButton
{
	private List<Vector2I> _currentResolutions = new();

	public override void _Ready()
	{
		ItemSelected += OnResolutionSelected;
	}

	public void UpdateResolutionList(List<Vector2I> newResolutions)
	{
		_currentResolutions = newResolutions;
		Clear();

		if (_currentResolutions == null || _currentResolutions.Count == 0) return;

		foreach (var res in _currentResolutions)
		{
			AddItem($"{res.X} × {res.Y}");
		}

		// Auto-select resolution matching monitor size, or pick highest available
		Vector2I screenSize = DisplayServer.ScreenGetSize();
		int bestIndex = 0;

		for (int i = 0; i < _currentResolutions.Count; i++)
		{
			if (_currentResolutions[i] == screenSize)
			{
				bestIndex = i;
				break;
			}
			// Fallback: Pick the closest resolution that doesn't exceed screen height
			if (_currentResolutions[i].Y <= screenSize.Y)
			{
				bestIndex = i;
			}
		}

		Select(bestIndex);
		OnResolutionSelected(bestIndex);
	}

	private void OnResolutionSelected(long index)
	{
		if (index < 0 || index >= _currentResolutions.Count) return;

		Vector2I targetResolution = _currentResolutions[(int)index];

		// Apply rendering scale & expansion dynamically
		GetTree().Root.ContentScaleSize = targetResolution;
		GetTree().Root.ContentScaleMode = Window.ContentScaleModeEnum.CanvasItems;
		GetTree().Root.ContentScaleAspect = Window.ContentScaleAspectEnum.Expand;

		// Resize actual window
		DisplayServer.WindowSetSize(targetResolution);
		GetWindow().Size = targetResolution;

		if (DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Windowed)
		{
			Vector2I screenSize = DisplayServer.ScreenGetSize();
			DisplayServer.WindowSetPosition((screenSize - targetResolution) / 2);
		}
	}
}
