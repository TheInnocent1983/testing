using Godot;

namespace Parkour.Movement;

public partial class WallRunComponent : Node
{
    [ExportGroup("Wall Detection")]
    [Export] public RayCast3D WallRayLeft { get; private set; }
    [Export] public RayCast3D WallRayRight { get; private set; }

	// Layer 10 bitvalue is 512 (1 << 9)
    [Export(PropertyHint.Layers3DPhysics)]
    public uint WallCollisionMask { get; set; } = 512;

    [ExportGroup("Wall Run")]
    [Export] public float WallRunTime { get; set; } = 1.4f;      // How long one wall holds you before it lets go
    [Export] public float WallRunSpeed { get; set; } = 11.0f;    // Baseline along-wall speed
    [Export] public float MaxWallRunSpeed { get; set; } = 16.0f; // Momentum ceiling
    [Export] public float MinWallRunSpeed { get; set; } = 3.0f;  // Need at least this much speed to attach
    [Export] public float WallRunGravity { get; set; } = 2.5f;   // Downward accel while attached (small = slow sink)
    [Export] public float WallStickForce { get; set; } = 3.0f;   // Pull into the wall so you don't peel off

    [ExportGroup("Wall Jump")]
    [Export] public float WallJumpPush { get; set; } = 8.0f;      // Shove away from the wall (toward the next one)
    [Export] public float WallJumpUp { get; set; } = 6.0f;        // Upward kick
    [Export] public float ReattachCooldown { get; set; } = 0.25f; // Blocks re-sticking right after a jump

    [ExportGroup("Camera Tilt")]
    [Export] public CameraController CameraCtrl { get; set; }
    [Export] public float WallRunTiltDegrees { get; set; } = 15.0f;

    public bool IsWallRunning => _wallRunning;

    private bool _wallRunning;
    private float _timer;
    private float _speed;
    private int _wallSide;               // -1 = left, +1 = right
    private Vector3 _wallNormal = Vector3.Zero;
    private Vector3 _alongWall = Vector3.Zero;
    private float _reattachTimer;
    private Vector3 _blockedNormal = Vector3.Zero; // wall we just left/exhausted — don't re-grab until we leave it

    public override void _Ready()
    {
        ApplyCollisionMask();
    }   

	private void ApplyCollisionMask()
	{
		if (WallRayLeft != null)
			WallRayLeft.CollisionMask = WallCollisionMask;

		if (WallRayRight != null)
			WallRayRight.CollisionMask = WallCollisionMask;
	}

    // Returns true if wall-run handled movement this frame (so the caller skips air physics).
    public bool TryWallRun(FpsController player, float delta)
    {
        if (_reattachTimer > 0.0f)
            _reattachTimer -= delta;

        Vector3 velocity = player.Velocity;
        Vector3 horizontal = new Vector3(velocity.X, 0.0f, velocity.Z);
        float horizontalSpeed = horizontal.Length();

        bool hasWall = TryGetWall(out Vector3 wallNormal);

        // Reset blocked wall if player lands on floor or gets clear of walls while not running
        if (player.IsOnFloor() || (!hasWall && !_wallRunning))
        {
            _blockedNormal = Vector3.Zero;
        }

        // --- Not wall-running yet: decide whether to start ---
        if (!_wallRunning)
        {
            bool inAir = !player.IsOnFloor();
            bool fastEnough = horizontalSpeed >= MinWallRunSpeed;
            bool notRisingFast = velocity.Y < 4.0f;
            bool differentWall = _blockedNormal == Vector3.Zero || !wallNormal.IsEqualApprox(_blockedNormal);
            bool cooldownOk = _reattachTimer <= 0.0f;

            if (hasWall && inAir && fastEnough && notRisingFast && differentWall && cooldownOk)
            {
                StartWallRun(wallNormal, horizontalSpeed);
            }
            else
            {
                UpdateCameraTilt(delta);
                return false; // let air/ground movement run
            }
        }

        // --- We are wall-running: end conditions ---
        if (!hasWall || player.IsOnFloor() || _timer >= WallRunTime)
        {
            StopWallRun();
            UpdateCameraTilt(delta);
            return false;
        }

        _wallNormal = wallNormal;
        _timer += delta;

        // Direction along the wall, flipped to match where the player is looking.
        _alongWall = _wallNormal.Cross(Vector3.Up).Normalized();
        Vector3 forward = -player.GlobalTransform.Basis.Z;
        if (_alongWall.Dot(forward) < 0.0f)
            _alongWall = -_alongWall;

        _speed = Mathf.Clamp(_speed, MinWallRunSpeed, MaxWallRunSpeed);

        // Horizontal = momentum along the wall, plus a small pull into the wall to stay attached.
        Vector3 newHorizontal = _alongWall * _speed - _wallNormal * WallStickForce * delta;

        // Reduced gravity that ramps back toward full over the run, so you sink slowly then let go.
        float gravity = Mathf.Lerp(WallRunGravity, WallRunGravity * 4.0f, _timer / WallRunTime);
        float newY = velocity.Y - gravity * delta;

        velocity = new Vector3(newHorizontal.X, newY, newHorizontal.Z);

        UpdateCameraTilt(delta);

        // Wall jump: push off the wall's normal, keep some forward speed, add height.
        if (Input.IsActionJustPressed("jump"))
        {
            velocity = _alongWall * _speed * 0.6f + _wallNormal * WallJumpPush + Vector3.Up * WallJumpUp;
            _blockedNormal = _wallNormal;
            _reattachTimer = ReattachCooldown;
            StopWallRun();
            player.Velocity = velocity;
            return true;
        }

        player.Velocity = velocity;
        return true;
    }

    private void StartWallRun(Vector3 wallNormal, float entrySpeed)
    {
        _wallRunning = true;
        _timer = 0.0f;
        _wallNormal = wallNormal;
        _speed = Mathf.Clamp(Mathf.Max(entrySpeed, WallRunSpeed), MinWallRunSpeed, MaxWallRunSpeed);
    }

    private void StopWallRun()
    {
        if (_wallRunning)
            _blockedNormal = _wallNormal; // don't immediately re-grab the wall we just finished
        _wallRunning = false;
        _timer = 0.0f;
    }

    private bool TryGetWall(out Vector3 wallNormal)
    {
        if (WallRayRight != null && WallRayRight.IsColliding())
        {
            wallNormal = WallRayRight.GetCollisionNormal();
            _wallSide = -1;
            return true;
        }
        if (WallRayLeft != null && WallRayLeft.IsColliding())
        {
            wallNormal = WallRayLeft.GetCollisionNormal();
            _wallSide = 1;
            return true;
        }
        wallNormal = Vector3.Zero;
        return false;
    }

    private void UpdateCameraTilt(float delta)
    {
        if (CameraCtrl == null) return;

        float targetRoll = 0.0f;

        if (_wallRunning)
        {
            // _wallSide is -1 for left wall (tilts right / negative roll)
            // or +1 for right wall (tilts left / positive roll)
            targetRoll = -_wallSide * Mathf.DegToRad(WallRunTiltDegrees);
        }

        CameraCtrl.ApplyRoll(targetRoll, delta, 8.0f);
    }
}