using Godot;
using Parkour.Movement;
using System;

public partial class SlideMovementComponent : Node
{
	[ExportGroup("Slide Settings")]
	[Export] public float SlideTimerMax { get; set; } = 0.9f;
	[Export] public float SlideSpeed {private set; get;} = 10.0f;       
	[Export] public float MinSlideSpeed {private set; get;} = 4.0f;      
	[Export] public float MaxSlideSpeed {private set; get;} = 20.0f;     
	[Export] public float MinSlideStartSpeed {private set; get;} = 8.0f; 
	[Export] public float SlideSlopeAccel {private set; get;} = 12.0f;   
	[Export] public float UphillPenalty {private set; get;} = 4.0f;   
	[Export] public float SlideFriction {private set; get;} = 4.0f;    
	[Export] public float SlideCameraTilt {private set; get;} = 8.0f;    

	public float SlideTimer { get; private set; }
	public float Momentum { get; private set; }
	public Vector3 SlideDir { get; private set; } = Vector3.Zero;

	public bool ShouldStartSlide (FpsController _player)
	{
		if (_player == null || _player.WishDir.LengthSquared() <= 0.001f)
			return false;

		float horizontalSpeed = new Vector3(_player.Velocity.X, 0.0f, _player.Velocity.Z).Length();
		return horizontalSpeed >= MinSlideStartSpeed;
	}

	public void EnterSlide(FpsController _player)
	{
		SlideTimer = Mathf.Max(0.1f, SlideTimerMax);

		Vector3 horizontalVel = new Vector3(_player.Velocity.X, 0.0f, _player.Velocity.Z);
		float entrySpeed = horizontalVel.Length();

		if (horizontalVel.LengthSquared() > 0.001f)
		{
			SlideDir = horizontalVel.Normalized();
		}
		else
		{
			Vector3 fallback = _player.WishDir.LengthSquared() > 0.001f ? _player.WishDir : -_player.GlobalTransform.Basis.Z;
			fallback.Y = 0.0f;
			SlideDir = fallback.LengthSquared() > 0.001f ? fallback.Normalized() : -_player.GlobalTransform.Basis.Z;
		}

		Momentum = Mathf.Clamp(Mathf.Max(entrySpeed, SlideSpeed), MinSlideSpeed, MaxSlideSpeed);
	}

	public void UpdateSlidePhysics(FpsController _player, double delta)
	{
		if (_player == null) return;

		// Apply camera tilt
		if (SlideCameraTilt > 0.0f)
			_player.CameraComp?.ApplyRoll(-Mathf.DegToRad(SlideCameraTilt), (float)delta);

		Vector3 velocity = _player.Velocity;
		float y = velocity.Y;

		Vector3 currentSlideDir = SlideDir;
		if (currentSlideDir.LengthSquared() < 0.001f)
		{
			Vector3 fallback = _player.WishDir;
			fallback.Y = 0.0f;
			currentSlideDir = fallback.LengthSquared() > 0.001f ? fallback.Normalized() : -_player.GlobalTransform.Basis.Z;
		}

		Vector3 floorDir = currentSlideDir.Slide(_player.GetFloorNormal()).Normalized();
		if (floorDir.LengthSquared() < 0.001f)
			floorDir = currentSlideDir;

		float floorAngle = Mathf.RadToDeg(_player.GetFloorAngle());
		bool runningUpSlope = IsRunningUpSlope(_player);

		// Momentum calculations
		if (floorAngle > 8.0f && !runningUpSlope)
		{
			// Downhill: build speed and sustain slide duration
			Momentum += (float)delta * Mathf.Max(0.0f, SlideSlopeAccel);
			SlideTimer = Mathf.Min(SlideTimerMax, SlideTimer + (float)delta);
		}
		else
		{
			// Flat/Uphill: decrease momentum and timer
			float drop = SlideFriction + (runningUpSlope ? Mathf.Max(0.0f, UphillPenalty) : 0.0f);
			Momentum -= (float)delta * drop;
			SlideTimer -= (float)delta;
		}

		Momentum = Mathf.Clamp(Momentum, MinSlideSpeed, MaxSlideSpeed);

		// Apply calculated slide velocity
		Vector3 horizontal = floorDir * Momentum;
		_player.Velocity = new Vector3(horizontal.X, y, horizontal.Z);
	}

	private bool IsRunningUpSlope(FpsController _player)
	{
		float dot = _player.GetFloorNormal().Dot(-_player.Transform.Basis.Z);
		return dot < 0.0f;
	}
}
