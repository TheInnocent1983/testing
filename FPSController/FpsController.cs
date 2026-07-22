using Godot;
using System;

public partial class FpsController : CharacterBody3D 
{
	[ExportGroup("Sensitivity")]
	[Export]
	public float LookSensitivity { get; set; } = 0.006f;
	
	[ExportGroup("Jump")]
	[Export]
	public float JumpVelocity { get; set; } = 6.0f;

	[Export]
	public bool AutoBunnyHop { get; set; } = true;

	[ExportGroup("Movement Speed")]
	[Export]
	public float WalkSpeed { get; set; } = 7.0f;

	[Export]
	public float SprintSpeed { get; set; } = 11f;

	Vector3 WishDir = Vector3.Zero;

	public float _GetMoveSpeed()
	{
		if (Input.IsActionPressed("sprint"))
		{
			return SprintSpeed;
		}
		else 
		{
			return WalkSpeed;
		}
	}

	public override void _Ready()
	{
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

				// Get the Camera3D node from Hierarchy
				Camera3D camera = GetNode<Camera3D>("%Camera3D");

				// Rotate the camera vertically
				camera.RotateX(-eventMouseMotion.Relative.Y * LookSensitivity);

				Vector3 cameraRotation = camera.Rotation;
				cameraRotation.X = Mathf.Clamp(cameraRotation.X, Mathf.DegToRad(-89f), Mathf.DegToRad(89f));
				camera.Rotation = cameraRotation;
			}
		}
	}

	public override void _Process(double delta)
	{
		// Called every frame. Delta is time since the last frame.
		// Update game logic here.
	} 

	public void _HandleAirPhysics(double delta)
	{
		float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
		Vector3 currentVelocity = Velocity;
		currentVelocity.Y -= gravity * (float)delta;
		Velocity = currentVelocity;
	}

	public void _HandleGroundPhysics(double delta) 
	{
		float speed = _GetMoveSpeed();

		Vector3 currentVelocity = Velocity;

		currentVelocity.X = WishDir.X * speed;
		currentVelocity.Z = WishDir.Z * speed;

		Velocity = currentVelocity;
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 inputDirection = Input.GetVector("move_left", "move_right", "move_forward", "move_backwards").Normalized();
		WishDir = this.GlobalTransform.Basis * new Vector3(inputDirection.X, 0.0f, inputDirection.Y);

		if (IsOnFloor())
		{
			_HandleGroundPhysics(delta);

			if (Input.IsActionJustPressed("jump"))
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
