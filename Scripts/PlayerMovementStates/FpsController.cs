using Godot;

public partial class FpsController : CharacterBody3D
{
	[ExportGroup("Components")]
	[Export] public CameraController CameraComp { get; private set; }
	[Export] public GroundMovementComponent GroundComp { get; private set; }
	[Export] public AirMovementComponent AirComp { get; private set; }

	[ExportGroup("Crouch/Slide Setup")]
	[Export] public CollisionShape3D BodyCollision { get; private set; }
	[Export] public Node3D HeadNode { get; private set; }

	[ExportGroup("Jump")]
	[Export] public float JumpVelocity { get; set; } = 6.0f;
	[Export] public bool AutoBunnyHop { get; set; } = true;

	public Vector3 WishDir { get; private set; } = Vector3.Zero;
	public float DefaultCapsuleHeight { get; private set; }

	private float _defaultHeadY;
	private CapsuleShape3D _capsuleShape;

	public override void _Ready()
	{
		if (BodyCollision == null)
			BodyCollision = GetNodeOrNull<CollisionShape3D>("CollisionShape3D");

		if (HeadNode == null)
			HeadNode = GetNodeOrNull<Node3D>("%Head");

		if (BodyCollision?.Shape is CapsuleShape3D capsule)
		{
			_capsuleShape = capsule;
			DefaultCapsuleHeight = capsule.Height;
		}

		if (HeadNode != null)
			_defaultHeadY = HeadNode.Position.Y;

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

	public void ApplyStance(float headYOffset, float capsuleHeight, float lerpSpeed, float delta)
	{
		float blend = 1.0f - Mathf.Pow(0.5f, delta * Mathf.Max(1.0f, lerpSpeed));

		if (HeadNode != null)
		{
			Vector3 headPos = HeadNode.Position;
			headPos.Y = Mathf.Lerp(headPos.Y, _defaultHeadY + headYOffset, blend);
			HeadNode.Position = headPos;
		}

		if (_capsuleShape != null)
		{
			float safeHeight = Mathf.Max(0.2f, capsuleHeight);
			_capsuleShape.Height = Mathf.Lerp(_capsuleShape.Height, safeHeight, blend);
		}
	}
}
