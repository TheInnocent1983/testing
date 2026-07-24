using System;
using Godot;

public partial class CameraController : Node
{
	[Export] public CharacterBody3D Player { get; set; }
	[Export] public Camera3D Camera { get; set; }

	[ExportGroup("Sensitivity")]
	[Export] public float LookSensitivity { get; set; } = 0.006f;
	[Export] public float ControllerLookSensitivity { get; set; } = 0.06f;

	[ExportGroup("Headbob")]
	[Export] public float HeadbobMoveAmount { get; set; } = 0.0275f;
	[Export] public float HeadbobFrequency { get; set; } = 2.4f;

	[ExportGroup("View Settings")]
	[Export] public Camera3D TargetCamera { get; set; }
	[Export] public float FieldOfView { get; set; } = 75.0f;

	private Vector2 _currentControllerLook;
	private float _headbobTime = 0.0f;
	private float _defaultCameraY;

	public override void _Ready()
	{
		if (Camera != null)
			_defaultCameraY = Camera.Position.Y;

		ApplyFov();
	}

	public override void _Process(double delta)
	{
		HandleControllerLook((float)delta);
	}

	private void ApplyFov()
	{
		Camera3D activeCam = TargetCamera ?? Camera;

		if (GodotObject.IsInstanceValid(activeCam))
		{
			activeCam.Fov = FieldOfView;
		}
	}

	public void HandleMouseLook(InputEventMouseMotion mouseMotion)
	{
		if (Player == null || Camera == null) return;

		Player.RotateY(-mouseMotion.Relative.X * LookSensitivity);

		Camera.RotateX(-mouseMotion.Relative.Y * LookSensitivity);
		Vector3 rot = Camera.Rotation;
		rot.X = Mathf.Clamp(rot.X, Mathf.DegToRad(-89f), Mathf.DegToRad(89f));
		Camera.Rotation = rot;
	}

	private void HandleControllerLook(float delta)
	{
		if (Player == null || Camera == null) return;

		Vector2 targetLook = Input.GetVector("look_left", "look_right", "look_up", "look_down").Normalized();
		_currentControllerLook = _currentControllerLook.Lerp(targetLook, 15.0f * delta);

		if (_currentControllerLook.LengthSquared() > 0.001f)
		{
			Player.RotateY(-_currentControllerLook.X * ControllerLookSensitivity);

			Camera.RotateX(-_currentControllerLook.Y * ControllerLookSensitivity);
			Vector3 rot = Camera.Rotation;
			rot.X = Mathf.Clamp(rot.X, Mathf.DegToRad(-89f), Mathf.DegToRad(89f));
			Camera.Rotation = rot;
		}
	}

	public void ApplyHeadbob(Vector3 velocity, float delta)
	{
		if (Camera == null) return;

		_headbobTime += delta * velocity.Length();
		Transform3D camTransform = Camera.Transform;

		camTransform.Origin = new Vector3(
			Mathf.Cos(_headbobTime * HeadbobFrequency * 0.75f) * HeadbobMoveAmount,
			_defaultCameraY + Mathf.Sin(_headbobTime * HeadbobFrequency) * HeadbobMoveAmount,
			0f
		);

		Camera.Transform = camTransform;
	}

	public void ApplyRoll(float targetRollRadians, float delta, float lerpSpeed = 10.0f)
	{
		if (Camera == null) return;

		Vector3 rot = Camera.Rotation;
		float blend = 1.0f - Mathf.Pow(0.5f, delta * Mathf.Max(1.0f, lerpSpeed));
		rot.Z = Mathf.Lerp(rot.Z, targetRollRadians, blend);
		Camera.Rotation = rot;
	}
}
