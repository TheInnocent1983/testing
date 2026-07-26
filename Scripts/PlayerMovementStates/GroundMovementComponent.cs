using Godot;

namespace Parkour.Movement;

public enum PlayerState
{
    Stand,
    Crouch,
    Slide
}

public partial class GroundMovementComponent : Node
{
    private PlayerState _state = PlayerState.Stand;
    private bool _isCrouchToggled = false;

    [ExportGroup("Movement Speed")]
    [Export] public float WalkSpeed { get; set; } = 7.0f;
    [Export] public float SprintSpeed { get; set; } = 11.0f;
    [Export] public float GroundAcceleration { get; set; } = 14.0f;
    [Export] public float GroundDeceleration { get; set; } = 10.0f;
    [Export] public float GroundFriction { get; set; } = 6.0f;

	bool slideHold = InputMap.HasAction("slide") && Input.IsActionPressed("slide");
	bool slideJustPressed = InputMap.HasAction("slide") && Input.IsActionJustPressed("slide");
	bool toggleJustPressed = InputMap.HasAction("crouch_toggle") && Input.IsActionJustPressed("crouch_toggle");

    public PlayerState CurrentState => _state;

    public float GetTargetSpeed() => Input.IsActionPressed("sprint") ? SprintSpeed : WalkSpeed;

    public void UpdateGroundPhysics(FpsController player, double delta)
    {
        UpdateStateTransitions(player);

        switch (_state)
        {
            case PlayerState.Stand:
                UpdateStand(player, (float)delta);
                break;
            case PlayerState.Crouch:
                UpdateCrouch(player, (float)delta);
                break;
            case PlayerState.Slide:
                UpdateSlide(player, (float)delta);
                break;
        }

        // Headbob
        player.CameraComp?.ApplyHeadbob(player.Velocity, (float)delta);
    }

    private void UpdateStateTransitions(FpsController player)
{
    bool slideHold = InputMap.HasAction("slide") && Input.IsActionPressed("slide");
    bool slideJustPressed = InputMap.HasAction("slide") && Input.IsActionJustPressed("slide");
    bool toggleJustPressed = InputMap.HasAction("crouch_toggle") && Input.IsActionJustPressed("crouch_toggle");

    if (toggleJustPressed)
    {
        _isCrouchToggled = !_isCrouchToggled;
    }

    bool crouchRequested = slideHold || _isCrouchToggled;
    bool ceilingBlocked = player.CrouchComp != null && player.CrouchComp.IsCeilingBlocked();

    switch (_state)
    {
        case PlayerState.Stand:
            // Check if we should slide OR crouch
            if (slideJustPressed || toggleJustPressed)
            {
                // Only slide if we have room/speed; otherwise, fallback directly to crouch if crouch was requested
                if (player.SlideComp != null && player.SlideComp.ShouldStartSlide(player))
                {
                    _state = PlayerState.Slide;
                    player.SlideComp.EnterSlide(player);
                }
                else if (crouchRequested)
                {
                    _state = PlayerState.Crouch;
                }
            }
            // Catch case: if toggle is active or hold is pressed but we weren't in slide speed, ensure we enter crouch
            else if (crouchRequested)
            {
                _state = PlayerState.Crouch;
            }
            break;

        case PlayerState.Crouch:
            if (!crouchRequested && !ceilingBlocked)
            {
                _state = PlayerState.Stand;
                _isCrouchToggled = false;
            }
            break;

        case PlayerState.Slide:
            if (player.SlideComp == null || player.SlideComp.SlideTimer <= 0.0f)
            {
                if (crouchRequested || ceilingBlocked)
                {
                    _state = PlayerState.Crouch;
                }
                else
                {
                    _state = PlayerState.Stand;
                    _isCrouchToggled = false;
                }
            }
            break;
    }
}

    private void UpdateStand(FpsController _player, float delta)
    {
        float standHeight = _player.DefaultCapsuleHeight > 0.0f ? _player.DefaultCapsuleHeight : 2.0f;
        _player.ApplyStance(0.0f, standHeight, 10.0f, delta);
        _player.CameraComp?.ApplyRoll(0.0f, delta);

        Vector3 velocity = _player.Velocity;
        ApplyGroundFriction(ref velocity, delta, GroundFriction);
        Accelerate(ref velocity, _player.WishDir, GetTargetSpeed(), delta);
        _player.Velocity = velocity;
    }

    private void UpdateCrouch(FpsController _player, float delta)
    {
        _player.CameraComp?.ApplyRoll(0.0f, delta);

        if (_player.CrouchComp != null)
        {
            _player.CrouchComp.UpdateCrouch(_player, delta);
        }

        Vector3 velocity = _player.Velocity;
        ApplyGroundFriction(ref velocity, delta, GroundFriction);

        float crouchSpeed = _player.CrouchComp != null ? _player.CrouchComp.CrouchSpeed : WalkSpeed * 0.5f;
        Accelerate(ref velocity, _player.WishDir, crouchSpeed, delta);
        _player.Velocity = velocity;
    }

    private void UpdateSlide(FpsController _player, float delta)
    {
        if (_player.CrouchComp != null)
        {
            float crouchDepth = _player.CrouchComp.CrouchDepth;
            float crouchHeight = _player.CrouchComp.GetCrouchCapsuleHeight(_player);
            _player.ApplyStance(-Mathf.Abs(crouchDepth), crouchHeight, _player.CrouchComp.CrouchLerpSpeed, delta);
        }

        _player.SlideComp?.UpdateSlidePhysics(_player, delta);
    }

    private void ApplyGroundFriction(ref Vector3 velocity, float delta, float friction)
    {
        float currentSpeed = velocity.Length();
        if (currentSpeed <= 0.0f) return;

        float control = Mathf.Max(currentSpeed, GroundDeceleration);
        float drop = control * friction * delta;
        float newSpeed = Mathf.Max(currentSpeed - drop, 0.0f) / currentSpeed;
        velocity *= newSpeed;
    }

    private void Accelerate(ref Vector3 velocity, Vector3 wishDir, float targetSpeed, float delta)
    {
        if (wishDir.LengthSquared() <= 0.001f) return;

        float currentSpeedInWishDir = velocity.Dot(wishDir);
        float addSpeedTillCap = targetSpeed - currentSpeedInWishDir;
        if (addSpeedTillCap <= 0.0f) return;

        float accelSpeed = GroundAcceleration * targetSpeed * delta;
        accelSpeed = Mathf.Min(accelSpeed, addSpeedTillCap);
        velocity += accelSpeed * wishDir;
    }
}