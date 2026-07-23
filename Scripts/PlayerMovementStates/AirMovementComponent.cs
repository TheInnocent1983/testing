using Godot;

public partial class AirMovementComponent : Node
{
	[ExportGroup("Air Movement")]
	[Export] public float AirCap { get; set; } = 1.0f;
	[Export] public float AirAcceleration { get; set; } = 150.0f;

	private readonly float _gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

	public void UpdateAirPhysics(FpsController player, float delta)
	{
		Vector3 velocity = player.Velocity;

		// Apply Gravity
		velocity.Y -= _gravity * delta;

		// Air Strafe (Horizontal Only)
		if (player.WishDir.LengthSquared() > 0.001f)
		{
			Vector3 horizontalVel = new Vector3(velocity.X, 0.0f, velocity.Z);
			float currentSpeedInWishDir = horizontalVel.Dot(player.WishDir);
			float addSpeedTillCap = AirCap - currentSpeedInWishDir;

			if (addSpeedTillCap > 0.0f)
			{
				float accelSpeed = AirAcceleration * delta;
				accelSpeed = Mathf.Min(accelSpeed, addSpeedTillCap);

				velocity.X += player.WishDir.X * accelSpeed;
				velocity.Z += player.WishDir.Z * accelSpeed;
			}
		}

		player.Velocity = velocity;
	}
}
