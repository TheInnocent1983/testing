using Godot;

namespace Parkour.UI.Settings.Video;

public partial class BrightnessSlider : HSlider
{
	private WorldEnvironment _worldEnvironment;

	public override void _Ready()
	{
		// 1. Match your UI settings: 1 to 100, default 50
		MinValue = 1;
		MaxValue = 100;
		Step = 1;
		Value = 50; 

		ValueChanged += OnBrightnessValueChanged;
	}

	private void OnBrightnessValueChanged(double value)
	{
		// 2. Map 1..100 to Godot exposure range 0.5..1.5 (where 50 = 1.0 default)
		float exposure = (float)(0.5 + (value - 1) * (1.0 / 99.0));

		// 3. Find WorldEnvironment and apply exposure
		if (_worldEnvironment == null || !GodotObject.IsInstanceValid(_worldEnvironment))
		{
			_worldEnvironment = GetTree().Root.FindChild("WorldEnvironment", true, false) as WorldEnvironment;
		}

		if (_worldEnvironment?.Environment != null)
		{
			_worldEnvironment.Environment.TonemapExposure = exposure;
		}
	}
}
