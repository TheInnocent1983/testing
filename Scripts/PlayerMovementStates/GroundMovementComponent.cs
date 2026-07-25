using Godot;

public enum PlayerState
{
	Stand,
	Crouch,
	Slide
}

public partial class GroundMovementComponent : Node
{
	private PlayerState _state = PlayerState.Stand;
	private float _slideTimer;
	private float _momentum;
	private Vector3 _slideDir = Vector3.Zero;

	private bool _isCrouchToggled = false;

	[ExportGroup("Movement Speed")]
	[Export] public float WalkSpeed { get; set; } = 7.0f;
	[Export] public float SprintSpeed { get; set; } = 11.0f;
	[Export] public float GroundAcceleration { get; set; } = 14.0f;
	[Export] public float GroundDeceleration { get; set; } = 10.0f;
	[Export] public float GroundFriction { get; set; } = 6.0f;

	[ExportCategory("Crouch")]
	[Export] public float CrouchSpeed {private set; get;} = 3.0f;
	[Export] public float CrouchDepth {private set; get;} = 0.45f;
	[Export] public float CrouchLerpSpeed {private set; get;} = 10.0f;
	[Export] public RayCast3D CeilingCheck {private set; get;}
	[Export] public float StandCapsuleHeight {private set; get;} = 2.0f;

	[ExportCategory("Slide")]
	[Export] public float SlideTimerMax {private set; get;} = 0.9f;      // Max slide duration on flat ground
	[Export] public float SlideSpeed {private set; get;} = 10.0f;        // Baseline boost — a slide is always at least this fast
	[Export] public float MinSlideSpeed {private set; get;} = 4.0f;      // Momentum floor while sliding
	[Export] public float MaxSlideSpeed {private set; get;} = 20.0f;     // Momentum ceiling (keeps downhill controllable)
	[Export] public float MinSlideStartSpeed {private set; get;} = 8.0f; // Speed needed to trigger a slide
	[Export] public float SlideSlopeAccel {private set; get;} = 10.0f;   // Speed gained per second going downhill
	[Export] public float UphillPenalty {private set; get;} = 4.0f;      // Extra speed lost per second going uphill
	[Export] public float SlideFriction {private set; get;} = 4.0f;      // Speed lost per second on flat ground
	[Export] public float SlideCameraTilt {private set; get;} = 8.0f;    // Camera roll while sliding (feel)

	public PlayerState CurrentState => _state;

	public float GetTargetSpeed() => Input.IsActionPressed("sprint") ? SprintSpeed : WalkSpeed;

	public void UpdateGroundPhysics(FpsController player, float delta)
	{
		UpdateStateTransitions(player);

		switch (_state)
		{
			case PlayerState.Stand:
				UpdateStand(player, delta);
				break;
			case PlayerState.Crouch:
				UpdateCrouch(player, delta);
				break;
			case PlayerState.Slide:
				UpdateSlide(player, delta);
				break;
		}

		// Headbob
		player.CameraComp?.ApplyHeadbob(player.Velocity, delta);
	}

	private void UpdateStateTransitions(FpsController player)
	{
		bool slideHold = InputMap.HasAction("slide") && Input.IsActionPressed("slide");
		bool slideJustPressed = InputMap.HasAction("slide") && Input.IsActionJustPressed("slide");
		bool toggleJustPressed = InputMap.HasAction("crouch_toggle") && Input.IsActionJustPressed("crouch_toggle");

		// Переключение режима Toggle при нажатии 'C'
		if (toggleJustPressed)
		{
			_isCrouchToggled = !_isCrouchToggled;
		}

		// Если зажат Ctrl ИЛИ активен Toggle приседания через C
		bool crouchRequested = slideHold || _isCrouchToggled;

		switch (_state)
		{
			case PlayerState.Stand:
				// Если нажали Ctrl или C
				if (slideJustPressed || toggleJustPressed)
				{
					// Проверяем возможность подката (только если бежали и нажали действие)
					if (ShouldStartSlide(player))
					{
						EnterSlide(player); //[cite: 1]
					}
					else if (crouchRequested)
					{
						_state = PlayerState.Crouch; //[cite: 1]
					}
				}
				break;

			case PlayerState.Crouch:
				// Выходим из приседа, только если отжали Ctrl, не взведен Toggle и сверху нет потолка
				if (!crouchRequested && !IsCeilingBlocked()) //[cite: 1]
				{
					_state = PlayerState.Stand; //[cite: 1]
					_isCrouchToggled = false;
				}
				break;

			case PlayerState.Slide:
				// Когда подкат закончился по таймеру
				if (_slideTimer <= 0.0f) //[cite: 1]
				{
					if (crouchRequested || IsCeilingBlocked()) //[cite: 1]
					{
						_state = PlayerState.Crouch; //[cite: 1]
					}
					else
					{
						_state = PlayerState.Stand; //[cite: 1]
						_isCrouchToggled = false;
					}
				}
				break;
		}
	}

	private void UpdateStand(FpsController player, float delta)
	{
		ApplyStance(player, 0.0f, 0.0f, delta);
		player.CameraComp?.ApplyRoll(0.0f, delta);

		Vector3 velocity = player.Velocity;
		ApplyGroundFriction(ref velocity, delta, GroundFriction);
		Accelerate(ref velocity, player.WishDir, GetTargetSpeed(), delta);
		player.Velocity = velocity;
	}

	private void UpdateCrouch(FpsController player, float delta)
	{
		ApplyStance(player, -Mathf.Abs(CrouchDepth), GetCrouchCapsuleHeight(player), delta);
		player.CameraComp?.ApplyRoll(0.0f, delta);

		Vector3 velocity = player.Velocity;
		ApplyGroundFriction(ref velocity, delta, GroundFriction);

		float crouchTargetSpeed = CrouchSpeed > 0.0f ? CrouchSpeed : WalkSpeed * 0.5f;
		Accelerate(ref velocity, player.WishDir, crouchTargetSpeed, delta);
		player.Velocity = velocity;
	}

	private void UpdateSlide(FpsController player, float delta)
	{
		ApplyStance(player, -Mathf.Abs(CrouchDepth), GetCrouchCapsuleHeight(player), delta);
		if (SlideCameraTilt > 0.0f)
			player.CameraComp?.ApplyRoll(-Mathf.DegToRad(SlideCameraTilt), delta);

		Vector3 velocity = player.Velocity;
		float y = velocity.Y;

		if (_slideDir.LengthSquared() < 0.001f)
		{
			Vector3 fallback = player.WishDir;
			fallback.Y = 0.0f;
			_slideDir = fallback.LengthSquared() > 0.001f ? fallback.Normalized() : -player.GlobalTransform.Basis.Z;
		}

		Vector3 floorDir = _slideDir.Slide(player.GetFloorNormal()).Normalized();
		if (floorDir.LengthSquared() < 0.001f)
			floorDir = _slideDir;

		float floorAngle = Mathf.RadToDeg(player.GetFloorAngle());
		bool runningUpSlope = IsRunningUpSlope(player);

		// --- Momentum model: your speed carries; slopes shape it ---
		if (floorAngle > 8.0f && !runningUpSlope)
		{
			// Downhill: build speed and keep the slide alive (long ramp = long slide)
			_momentum += delta * Mathf.Max(0.0f, SlideSlopeAccel);
			_slideTimer = Mathf.Min(SlideTimerMax, _slideTimer + delta);
		}
		else
		{
			// Flat/uphill: bleed speed and run down the timer
			float drop = SlideFriction + (runningUpSlope ? Mathf.Max(0.0f, UphillPenalty) : 0.0f);
			_momentum -= delta * drop;
			_slideTimer -= delta;
		}

		_momentum = Mathf.Clamp(_momentum, MinSlideSpeed, MaxSlideSpeed);

		// Drive velocity from momentum along the floor; gravity (Y) is preserved
		Vector3 horizontal = floorDir * _momentum;
		velocity = new Vector3(horizontal.X, y, horizontal.Z);
		player.Velocity = velocity;
	}

	private void ApplyGroundFriction(ref Vector3 velocity, float delta, float friction)
	{
		float currentSpeed = velocity.Length();
		if (currentSpeed <= 0.0f)
			return;

		float control = Mathf.Max(currentSpeed, GroundDeceleration);
		float drop = control * friction * delta;
		float newSpeed = Mathf.Max(currentSpeed - drop, 0.0f) / currentSpeed;
		velocity *= newSpeed;
	}

	private void Accelerate(ref Vector3 velocity, Vector3 wishDir, float targetSpeed, float delta)
	{
		if (wishDir.LengthSquared() <= 0.001f)
			return;

		float currentSpeedInWishDir = velocity.Dot(wishDir);
		float addSpeedTillCap = targetSpeed - currentSpeedInWishDir;
		if (addSpeedTillCap <= 0.0f)
			return;

		float accelSpeed = GroundAcceleration * targetSpeed * delta;
		accelSpeed = Mathf.Min(accelSpeed, addSpeedTillCap);
		velocity += accelSpeed * wishDir;
	}

	private bool IsCrouchPressed()
	{
		return InputMap.HasAction("crouch") && Input.IsActionPressed("crouch");
	}

	private bool ShouldStartSlide(FpsController player)
	{
		if (player.WishDir.LengthSquared() <= 0.001f)
			return false;

		float speed = new Vector3(player.Velocity.X, 0.0f, player.Velocity.Z).Length();
		return speed >= MinSlideStartSpeed;
	}

	private void EnterSlide(FpsController player)
	{
		_state = PlayerState.Slide;
		_slideTimer = Mathf.Max(0.1f, SlideTimerMax);

		Vector3 horizontalVel = new Vector3(player.Velocity.X, 0.0f, player.Velocity.Z);
		float entrySpeed = horizontalVel.Length();

		if (horizontalVel.LengthSquared() > 0.001f)
		{
			_slideDir = horizontalVel.Normalized();
		}
		else
		{
			_slideDir = player.WishDir.LengthSquared() > 0.001f ? player.WishDir : -player.GlobalTransform.Basis.Z;
			_slideDir.Y = 0.0f;
			if (_slideDir.LengthSquared() > 0.001f)
				_slideDir = _slideDir.Normalized();
		}

		// Keep the speed you ran in with (always at least SlideSpeed), capped so it stays controllable.
		// This is what makes "faster run = faster slide" and lets a slide-jump carry your momentum.
		_momentum = Mathf.Clamp(Mathf.Max(entrySpeed, SlideSpeed), MinSlideSpeed, MaxSlideSpeed);
	}

	private bool IsCeilingBlocked()
	{
		return CeilingCheck != null && CeilingCheck.IsColliding();
	}

	private bool IsRunningUpSlope(FpsController player)
	{
		float dot = player.GetFloorNormal().Dot(-player.Transform.Basis.Z);
		return dot < 0.0f;
	}

	private void ApplyStance(FpsController player, float headYOffset, float capsuleHeightOverride, float delta)
	{
		if (player == null)
			return;

		float standHeight = StandCapsuleHeight > 0.0f
			? StandCapsuleHeight
			: (player.DefaultCapsuleHeight > 0.0f ? player.DefaultCapsuleHeight : 2.0f);

		float targetHeight = capsuleHeightOverride > 0.0f ? capsuleHeightOverride : standHeight;
		player.ApplyStance(headYOffset, targetHeight, CrouchLerpSpeed, delta);
	}

	private float GetCrouchCapsuleHeight(FpsController player)
	{
		float standHeight = StandCapsuleHeight > 0.0f
			? StandCapsuleHeight
			: (player.DefaultCapsuleHeight > 0.0f ? player.DefaultCapsuleHeight : 2.0f);

		return Mathf.Max(0.2f, standHeight - Mathf.Abs(CrouchDepth));
	}
}
