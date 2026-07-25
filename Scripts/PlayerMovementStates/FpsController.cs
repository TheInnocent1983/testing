using Godot;

public partial class FpsController : CharacterBody3D
{
	[ExportGroup("Components")]
	[Export] public CameraController CameraComp { get; private set; }
	[Export] public GroundMovementComponent GroundComp { get; private set; }
	[Export] public AirMovementComponent AirComp { get; private set; }
	[Export] public WallRunComponent WallRunComp { get; private set; }
	[Export] public NoclipComponent NoclipComp { get; private set; }

	[ExportGroup("Jump")]
	[Export] public float JumpVelocity { get; set; } = 6.0f;
	[Export] public bool AutoBunnyHop { get; set; } = true;

	[ExportGroup("Noclip")]
	[Export] public bool AutoNoclip { get; set; } = false;

	public Vector3 WishDir { get; private set; } = Vector3.Zero;
	
	public Vector2 InputDir { get; private set; } = Vector2.Zero;

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
		InputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_backwards").Normalized();
		WishDir = GlobalTransform.Basis * new Vector3(InputDir.X, 0.0f, InputDir.Y);

		bool isNoclipActive = NoclipComp != null && NoclipComp._HandleNoclip(delta);

		if (!isNoclipActive)
		{
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
				// Wall-run takes priority while airborne; fall back to normal air control.
				bool wallRunning = WallRunComp != null && WallRunComp.TryWallRun(this, (float)delta);
				if (!wallRunning)
					AirComp?.UpdateAirPhysics(this, (float)delta);
			}
		}
		else
		{
			// HERE! When noclip IS active, kill normal physics velocity
			Velocity = Vector3.Zero;
		}

		MoveAndSlide();
	}
}
