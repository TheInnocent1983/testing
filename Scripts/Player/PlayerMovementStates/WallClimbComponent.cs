using Godot;

namespace Parkour.Movement;
public partial class WallClimbComponent : Node
{
	[ExportGroup("Wall Detection")]
	[Export] public RayCast3D WallRayForward { get; private set; }

	[ExportGroup("Wall Climb")]
	[Export] public float ClimbTime { get; set; } = 1.6f;         // How long your grip lasts before it gives out
	[Export] public float ClimbSpeed { get; set; } = 4.5f;        // Baseline upward speed
	[Export] public float MaxClimbSpeed { get; set; } = 6.0f;     // Speed ceiling
	[Export] public float StrafeSpeed { get; set; } = 3.0f;       // Sideways movement while climbing
	[Export] public float ClimbGravity { get; set; } = 1.0f;      // Downward accel while attached (small = slow slip)
	[Export] public float WallStickForce { get; set; } = 3.0f;    // Pull into the wall so you don't peel off
	[Export] public float MinApproachAngle { get; set; } = 0.5f;  // Dot(forward, -wallNormal) must exceed this to grab

	[ExportGroup("Wall Jump")]
	[Export] public float WallJumpPush { get; set; } = 7.0f;      // Shove away from the wall
	[Export] public float WallJumpUp { get; set; } = 7.0f;        // Upward kick
	[Export] public float ReattachCooldown { get; set; } = 0.25f; // Blocks re-sticking right after a jump

	public bool IsWallClimbing => _wallClimbing;

	private bool _wallClimbing;
	private float _timer;
	private Vector3 _wallNormal = Vector3.Zero;
	private float _reattachTimer;
	private Vector3 _blockedNormal = Vector3.Zero; // wall we just left/exhausted — don't re-grab until we leave it

	// Returns true if wall-climb handled movement this frame (so the caller skips air physics).
	public bool TryWallClimb(FpsController player, float delta)
	{
		if (_reattachTimer > 0.0f)
			_reattachTimer -= delta;

		Vector3 velocity = player.Velocity;
		Vector3 forward = -player.GlobalTransform.Basis.Z;

		bool hasWall = TryGetWall(out Vector3 wallNormal);

		
		if (!hasWall && !_wallClimbing)
			_blockedNormal = Vector3.Zero;

		bool holdingClimb = Input.IsActionPressed("move_forward");

		// --- Not climbing yet: decide whether to start ---
		if (!_wallClimbing)
		{
			bool facingWall = hasWall && forward.Dot(-wallNormal) >= MinApproachAngle;
			bool differentWall = _blockedNormal == Vector3.Zero || !wallNormal.IsEqualApprox(_blockedNormal);
			bool cooldownOk = _reattachTimer <= 0.0f;

			if (hasWall && facingWall && holdingClimb && differentWall && cooldownOk)
				StartWallClimb(wallNormal);
			else
				return false; // let air movement run
		}

		
		if (!hasWall || player.IsOnFloor() || _timer >= ClimbTime || !holdingClimb)
		{
			StopWallClimb();
			return false;
		}

		_wallNormal = wallNormal;
		_timer += delta;

		
		float fatigue = _timer / ClimbTime;
		float effectiveSpeed = Mathf.Clamp(Mathf.Lerp(ClimbSpeed, 0.0f, fatigue), 0.0f, MaxClimbSpeed);
		float gravity = Mathf.Lerp(ClimbGravity, ClimbGravity * 5.0f, fatigue);

		Vector3 right = _wallNormal.Cross(Vector3.Up).Normalized();
		float strafeInput = Input.GetAxis("move_left", "move_right");

		// Horizontal = sideways shimmy along the wall, plus a small pull into the wall to stay attached.
		Vector3 newHorizontal = right * strafeInput * StrafeSpeed - _wallNormal * WallStickForce * delta;
		float newY = effectiveSpeed - gravity * delta;

		velocity = new Vector3(newHorizontal.X, newY, newHorizontal.Z);

		// Wall jump: kick off the wall's normal, add height, let go.
		if (Input.IsActionJustPressed("jump"))
		{
			velocity = _wallNormal * WallJumpPush + Vector3.Up * WallJumpUp;
			_blockedNormal = _wallNormal;
			_reattachTimer = ReattachCooldown;
			StopWallClimb();
			player.Velocity = velocity;
			return true;
		}

		player.Velocity = velocity;
		return true;
	}

	private void StartWallClimb(Vector3 wallNormal)
	{
		_wallClimbing = true;
		_timer = 0.0f;
		_wallNormal = wallNormal;
	}

	private void StopWallClimb()
	{
		if (_wallClimbing)
			_blockedNormal = _wallNormal; // don't immediately re-grab the wall we just finished
		_wallClimbing = false;
		_timer = 0.0f;
	}

	private bool TryGetWall(out Vector3 wallNormal)
	{
		if (WallRayForward != null && WallRayForward.IsColliding())
		{
			wallNormal = WallRayForward.GetCollisionNormal();
			return true;
		}
		wallNormal = Vector3.Zero;
		return false;
	}
}
