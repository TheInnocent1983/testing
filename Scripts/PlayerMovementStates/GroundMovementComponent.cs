using Godot;

public partial class GroundMovementComponent : Node
{
	[ExportGroup("Movement Speed")]
	[Export] public float WalkSpeed { get; set; } = 7.0f;
	[Export] public float SprintSpeed { get; set; } = 11.0f;
	[Export] public float GroundAcceleration { get; set; } = 14.0f;
	[Export] public float GroundDeceleration { get; set; } = 10.0f;
	[Export] public float GroundFriction { get; set; } = 6.0f;

	public float GetTargetSpeed() => Input.IsActionPressed("sprint") ? SprintSpeed : WalkSpeed;

	public void UpdateGroundPhysics(FpsController player, float delta)
	{
		Vector3 velocity = player.Velocity;
		float currentSpeed = velocity.Length();

		// 1. Friction
		if (currentSpeed > 0.0f)
		{
			float control = Mathf.Max(currentSpeed, GroundDeceleration);
			float drop = control * GroundFriction * delta;
			float newSpeed = Mathf.Max(currentSpeed - drop, 0.0f) / currentSpeed;
			velocity *= newSpeed;
		}

		// 2. Acceleration
		if (player.WishDir.LengthSquared() > 0.001f)
		{
			float targetSpeed = GetTargetSpeed();
			float currentSpeedInWishDir = velocity.Dot(player.WishDir);
			float addSpeedTillCap = targetSpeed - currentSpeedInWishDir;

			if (addSpeedTillCap > 0.0f)
			{
				float accelSpeed = GroundAcceleration * targetSpeed * delta;
				accelSpeed = Mathf.Min(accelSpeed, addSpeedTillCap);
				velocity += accelSpeed * player.WishDir;
			}
		}

		player.Velocity = velocity;

		// Headbob
		player.CameraComp?.ApplyHeadbob(velocity, delta);
	}
}
