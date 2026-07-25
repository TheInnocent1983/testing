using Godot;
using System;

public partial class NoclipComponent : Node
{
	[ExportGroup("Noclip Settings")]
	[Export] public float NoclipSpeedMultiplier = 3.0f;
	[Export] public bool AutoNoclip = false;

	private FpsController _player;
	
	// Store original collision settings to restore later
	private uint _originalCollisionLayer;
	private uint _originalCollisionMask;

	public override void _Ready()
	{
		_player = GetOwner<FpsController>() ?? GetParent()?.GetParent() as FpsController;

		if (_player != null)
		{
			// Store initial Inspector collision settings
			_originalCollisionLayer = _player.CollisionLayer;
			_originalCollisionMask = _player.CollisionMask;
		}
	}

	public bool _HandleNoclip(double delta)
	{
		if (Input.IsActionJustPressed("noclip"))
		{
			AutoNoclip = !AutoNoclip;
			GD.Print($"Noclip state toggled to: {AutoNoclip}");

			if (_player != null)
			{
				if (AutoNoclip)
				{
					// Cache layer/mask in case they changed during gameplay
					_originalCollisionLayer = _player.CollisionLayer;
					_originalCollisionMask = _player.CollisionMask;

					// Wipe layers to bypass all physics, Raycasts, and Area3D triggers
					_player.CollisionLayer = 0;
					_player.CollisionMask = 0;
				}
				else
				{
					// Restore original collision layer/mask
					_player.CollisionLayer = _originalCollisionLayer;
					_player.CollisionMask = _originalCollisionMask;
				}
			}
		}

		if (_player == null) return false;

		var collisionShape = _player.GetNodeOrNull<CollisionShape3D>("%CollisionShape3D");
		if (collisionShape != null)
			collisionShape.Disabled = AutoNoclip;

		if (!AutoNoclip)
		{
			return false;
		}

		Vector3 wishDir = Vector3.Zero;
		Camera3D cam = _player.CameraComp?.Camera ?? _player.GetNodeOrNull<Camera3D>("%Camera3D");

		if (cam != null)
		{
			wishDir = cam.GlobalTransform.Basis * new Vector3(_player.InputDir.X, 0.0f, _player.InputDir.Y);
		}
		else
		{
			wishDir = _player.GlobalTransform.Basis * new Vector3(_player.InputDir.X, 0.0f, _player.InputDir.Y);
		}

		float baseSpeed = _player.GroundComp != null ? _player.GroundComp.GetTargetSpeed() : 7.0f;
		float speed = baseSpeed * NoclipSpeedMultiplier;

		if (Input.IsActionPressed("sprint"))
			speed *= 3.0f;

		_player.GlobalPosition += wishDir * speed * (float)delta;

		return true;
	}
}
