using Godot;

public partial class FpsController : CharacterBody3D
{
	[ExportGroup("Components")]
	[Export] public CameraController CameraComp { get; private set; }
	[Export] public GroundMovementComponent GroundComp { get; private set; }
	[Export] public AirMovementComponent AirComp { get; private set; }

	[ExportGroup("Jump")]
	[Export] public float JumpVelocity { get; set; } = 6.0f;
	[Export] public bool AutoBunnyHop { get; set; } = true;

	public Vector3 WishDir { get; private set; } = Vector3.Zero;

	public override void _Ready()
	{
		// Setup visual layers for body/world model
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
		// Toggle Mouse Mode
		if (@event is InputEventMouseButton eventMouseButton && eventMouseButton.Pressed)
		{
			Input.MouseMode = Input.MouseModeEnum.Captured;
		}
		else if (@event.IsActionPressed("ui_cancel"))
		{
			Input.MouseMode = Input.MouseModeEnum.Visible;
		}

		// Pass mouse motion to Camera Component
		if (Input.MouseMode == Input.MouseModeEnum.Captured && @event is InputEventMouseMotion mouseMotion)
		{
			CameraComp?.HandleMouseLook(mouseMotion);
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		// Calculate Movement Direction
		Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_backwards").Normalized();
		WishDir = GlobalTransform.Basis * new Vector3(inputDir.X, 0.0f, inputDir.Y);

		if (IsOnFloor())
		{
			GroundComp?.UpdateGroundPhysics(this, (float)delta);

			if (Input.IsActionJustPressed("jump") || (AutoBunnyHop && Input.IsActionPressed("jump")))
			{
				Vector3 vel = Velocity;
				vel.Y = JumpVelocity;
				Velocity = vel;
			}
		}
		else
		{
			AirComp?.UpdateAirPhysics(this, (float)delta);
		}

		MoveAndSlide();
	}
}
