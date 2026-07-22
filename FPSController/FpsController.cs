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
	[Export] public float GroundAcceleration { get; set; } = 14.0f;
	[Export] public float GroundDeceleration { get; set; } = 10.0f;
	[Export] public float GroundFriction { get; set; } = 6.0f;

	[ExportGroup("Air Movement")]
	[Export] public float AirCap = 0.85f;
	[Export] public float AirAcceleration = 800.0f;
	[Export] public float AirMoveSpeed = 500.0f;

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
		Vector2 targetLook = Input.GetVector("look_left", "look_right", "look_up", "look_down").Normalized();
		CurrentControllerLook = CurrentControllerLook.Lerp(targetLook, 15.0f * (float)delta);
		

		if (CurrentControllerLook.LengthSquared() > 0.001f)
		{
			RotateY(-CurrentControllerLook.X * ControllerLookSensitivity);

			_camera.RotateX(-CurrentControllerLook.Y * ControllerLookSensitivity);

			Vector3 cameraRotation = _camera.Rotation;
			cameraRotation.X = Mathf.Clamp(cameraRotation.X, Mathf.DegToRad(-89f), Mathf.DegToRad(89f));
			_camera.Rotation = cameraRotation;
		}
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

		var currentSpeedInWishDir = currentVelocity.Dot(_wishDir);
		var cappedSpeed = Mathf.Min((AirMoveSpeed * _wishDir).Length(), AirCap);
		var addSpeedTillCap = cappedSpeed - currentSpeedInWishDir;

		if (addSpeedTillCap > 0)
		{
			var accelerationSpeed = AirAcceleration * AirMoveSpeed * (float)delta;
			accelerationSpeed = Mathf.Min(accelerationSpeed, addSpeedTillCap);
			currentVelocity += accelerationSpeed * _wishDir;
			Velocity = currentVelocity;
		}
	}

	public void _HandleGroundPhysics(double delta) 
	{
		Vector3 currentVelocity = Velocity;
		float currentSpeed = currentVelocity.Length();

		if (currentSpeed > 0.0f)
		{
			float control = Mathf.Max(currentSpeed, GroundDeceleration);
			float drop = control * GroundFriction * (float)delta;
			float newSpeed = Mathf.Max(currentSpeed - drop, 0.0f) / currentSpeed;

			currentVelocity *= newSpeed;
		}

		if (_wishDir.LengthSquared() > 0.001f)
		{
			float maxSpeed = _GetMoveSpeed();
			float currentSpeedInWishDir = currentVelocity.Dot(_wishDir);
			float addSpeedTillCap = maxSpeed - currentSpeedInWishDir;

			if (addSpeedTillCap > 0.0f)
			{
				float accelSpeed = GroundAcceleration * maxSpeed * (float)delta;
				accelSpeed = Mathf.Min(accelSpeed, addSpeedTillCap);
				currentVelocity += accelSpeed * _wishDir;
			}
		}

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
	public void Wallrun()
	{
		// Implement wall running logic here
	}
}
