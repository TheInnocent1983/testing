using Godot;
using Parkour.Movement; // Adjust if your CameraController is in a different namespace

namespace Parkour.UI.Settings.Video;

public partial class FovSlider : HSlider
{
	private CameraController _cameraController;

	public override void _Ready()
	{
		// 1. Set your custom FOV bounds
		MinValue = 60;
		MaxValue = 140;
		Step = 1;
		Value = 90; // Default baseline FOV

		ValueChanged += OnFovValueChanged;
	}

	private void OnFovValueChanged(double value)
	{
		// 2. Find the camera controller dynamically in the scene tree
		if (_cameraController == null || !GodotObject.IsInstanceValid(_cameraController))
		{
			_cameraController = GetTree().Root.FindChild("CameraController", true, false) as CameraController;
		}

		// 3. Apply the FOV directly
		if (_cameraController != null)
		{
			_cameraController.FieldOfView = (float)value;
		}
	}
}
