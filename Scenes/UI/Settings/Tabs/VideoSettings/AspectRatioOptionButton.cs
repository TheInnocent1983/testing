using Godot;
using System;
using System.Collections.Generic;

namespace Parkour.UI.Settings.Video;

public partial class AspectRatioOptionButton : OptionButton
{
	[Export] private ResolutionOptionButton _resolutionSelector;

	private readonly Dictionary<string, List<Vector2I>> _resolutionsByAspect = new()
	{
		{ "16:9",  new List<Vector2I> { new(1280, 720), new(1600, 900), new(1920, 1080), new(2560, 1440), new(3840, 2160) } },
		{ "16:10", new List<Vector2I> { new(1280, 800), new(1440, 900), new(1680, 1050), new(1920, 1200), new(2560, 1600) } },
		{ "4:3",   new List<Vector2I> { new(1024, 768), new(1280, 960), new(1440, 1080), new(1600, 1200) } },
		{ "21:9",  new List<Vector2I> { new(2560, 1080), new(3440, 1440), new(5120, 2160) } },
		{ "32:9",  new List<Vector2I> { new(3840, 1080), new(5120, 1440) } }
	};

	public override void _Ready()
	{
		Clear();
		foreach (var aspectKey in _resolutionsByAspect.Keys)
		{
			AddItem(aspectKey);
		}

		ItemSelected += OnAspectRatioSelected;
		VisibilityChanged += OnVisibilityChanged;

		CallDeferred(MethodName.InitializeSelection);
	}

	private void OnVisibilityChanged()
	{
		if (IsVisibleInTree())
		{
			InitializeSelection();
		}
	}

	private void InitializeSelection()
	{
		if (_resolutionSelector == null)
		{
			_resolutionSelector = GetNodeOrNull<ResolutionOptionButton>("../../../ResolutionPanelContainer/ResolutionRow/ResolutionOptionButton");
		}

		// Detect player's primary monitor aspect ratio
		string detectedAspect = DetectScreenAspectRatio();
		int targetIndex = 0;

		for (int i = 0; i < ItemCount; i++)
		{
			if (GetItemText(i) == detectedAspect)
			{
				targetIndex = i;
				break;
			}
		}

		Select(targetIndex);
		OnAspectRatioSelected(targetIndex);
	}

	private string DetectScreenAspectRatio()
	{
		Vector2I screenSize = DisplayServer.ScreenGetSize();
		float ratio = (float)screenSize.X / screenSize.Y;

		// Find the closest matching aspect ratio string
		if (MathF.Abs(ratio - (4f / 3f)) < 0.05f) return "4:3";
		if (MathF.Abs(ratio - (16f / 10f)) < 0.05f) return "16:10";
		if (MathF.Abs(ratio - (21f / 9f)) < 0.1f || MathF.Abs(ratio - 2.370f) < 0.1f) return "21:9";
		if (MathF.Abs(ratio - (32f / 9f)) < 0.1f || MathF.Abs(ratio - 3.555f) < 0.1f) return "32:9";

		// Default fallback for standard widescreen monitors
		return "16:9"; 
	}

	private void OnAspectRatioSelected(long index)
	{
		string selectedAspect = GetItemText((int)index);

		if (_resolutionsByAspect.TryGetValue(selectedAspect, out var resolutions))
		{
			_resolutionSelector?.UpdateResolutionList(resolutions);
		}
	}
}
