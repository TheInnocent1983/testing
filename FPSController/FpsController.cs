using Godot;
using System;
using System.Collections.Specialized;

public partial class FpsController : CharacterBody3D 
{
	[ExportGroup("Sensitivity")]
	[Export] public float LookSensitivity { get; set; } = 0.006f;
	[Export] public float ControllerLookSensitivity { get; set; } = 0.06f;
	
	[ExportGroup("Jump")]
	[Export] public float JumpVelocity { get; set; } = 6.0f;
	[Export] public bool AutoBunnyHop { get; set; } = true;

	[ExportGroup("Movement Speed")]
	[Export] public float WalkSpeed { get; set; } = 7.0f;
	[Export] public float SprintSpeed { get; set; } = 11f;

	Vector2 CurrentControllerLook;

	private float HeadbobMoveAmount = 0.0275f;
	private float HeadbobFrequency = 2.4f;
	private float HeadbobTime = 0.0f;
	private float DefaultCameraY;

	private Camera3D _camera;
	Vector3 _wishDir = Vector3.Zero;
	float _gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

	public override void _Ready()
	{
		_camera = GetNode<Camera3D>("%Camera3D");
		DefaultCameraY = _camera.Position.Y;

		foreach (Node child in GetNode("%WorldModel").FindChildren("*", "VisualInstance3D"))
		{
			if (child is VisualInstance3D visualChild)
			{
				visualChild.SetLayerMaskValue(1, false);
				visualChild.SetLayerMaskValue(2, true);
			}
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		// Hiting "Esc" will bring back cursor
		if (@event is InputEventMouseButton eventMouseButton && eventMouseButton.Pressed)
		{
			Input.MouseMode = Input.MouseModeEnum.Captured;
		}
		else if (@event.IsActionPressed("ui_cancel"))
		{
			Input.MouseMode = Input.MouseModeEnum.Visible;
		}

		// Use Mouse to look around
		if (Input.MouseMode == Input.MouseModeEnum.Captured)
		{
			if (@event is InputEventMouseMotion eventMouseMotion)
			{
				// Rotate the body horizontally
				RotateY(-eventMouseMotion.Relative.X * LookSensitivity);

				// Rotate the camera vertically
				_camera.RotateX(-eventMouseMotion.Relative.Y * LookSensitivity);

				Vector3 cameraRotation = _camera.Rotation;
				cameraRotation.X = Mathf.Clamp(cameraRotation.X, Mathf.DegToRad(-89f), Mathf.DegToRad(89f));
				_camera.Rotation = cameraRotation;
			}
		}
	}

	public void _HeadbobEffect(double delta)
	{
		Vector3 currentVelocity = Velocity;
		HeadbobTime += (float)delta * currentVelocity.Length();	

		Transform3D camTransform = _camera.Transform;

		camTransform.Origin = new Vector3(
			Mathf.Cos(HeadbobTime * HeadbobFrequency * 0.75f) * HeadbobMoveAmount,
			DefaultCameraY + Mathf.Sin(HeadbobTime * HeadbobFrequency) * HeadbobMoveAmount, 
			0f);

		_camera.Transform = camTransform;
	}

	public void _HandleControllerLookInput(double delta)
	{
		Vector2 targetLook = Input.GetVector("look_left", "look_right", "look_down", "look_up").Normalized();
		CurrentControllerLook = targetLook;

		RotateY(-CurrentControllerLook.X * ControllerLookSensitivity);
		_camera.RotateX(-CurrentControllerLook.Y * ControllerLookSensitivity);
		
		Vector3 cameraRotation = _camera.Rotation;
		cameraRotation.X = Mathf.Clamp(cameraRotation.X, Mathf.DegToRad(-89f), Mathf.DegToRad(89f));
		_camera.Rotation = cameraRotation;
	}

	public float _GetMoveSpeed()
	{
		return Input.IsActionPressed("sprint") ? SprintSpeed : WalkSpeed;
	}

	public override void _Process(double delta)
	{
		_HandleControllerLookInput(delta);
	} 

	public void _HandleAirPhysics(double delta)
	{
		Vector3 currentVelocity = Velocity;
		currentVelocity.Y -= _gravity * (float)delta;
		Velocity = currentVelocity;
	}

	public void _HandleGroundPhysics(double delta) 
	{
		float speed = _GetMoveSpeed();

		Vector3 currentVelocity = Velocity;

		currentVelocity.X = _wishDir.X * speed;
		currentVelocity.Z = _wishDir.Z * speed;

		Velocity = currentVelocity;

		_HeadbobEffect(delta);
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 inputDirection = Input.GetVector("move_left", "move_right", "move_forward", "move_backwards").Normalized();
		_wishDir = GlobalTransform.Basis * new Vector3(inputDirection.X, 0.0f, inputDirection.Y);

		if (IsOnFloor())
		{
			_HandleGroundPhysics(delta);

			if (Input.IsActionJustPressed("jump") || (AutoBunnyHop && Input.IsActionPressed("jump")))
			{
				Vector3 currentVelocity = Velocity;
				currentVelocity.Y = JumpVelocity;
				Velocity = currentVelocity;
			}
		}
		else
		{
			_HandleAirPhysics(delta);
		}

		MoveAndSlide();
	}
}
