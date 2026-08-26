using Godot;
using Parkour.Movement;

public partial class RestartLevelComponent : Node
{
	/// Emitted on the local player whenever their run resets. A run timer should
	/// listen to this rather than watching the "restart" action itself, so that
	/// future reset sources (falling out of bounds, hitting a reset volume)
	/// restart the clock too.
	[Signal] public delegate void RunResetEventHandler();

	private FpsController _player;
	private Transform3D _spawnTransform;

	public override void _Ready()
	{
		_player = GetOwner<FpsController>() ?? GetParent()?.GetParent() as FpsController;

		if (_player is not null)
			_spawnTransform = _player.GlobalTransform;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		// Every peer's copy of every player runs this component, so only the
		// owning peer may act on local input.
		if (_player is null || !_player.IsMultiplayerAuthority()) return;

		if (@event.IsActionPressed("restart"))
			ResetRun();
	}

	/// Returns this player to their spawn. Replaces a full scene reload, which
	/// would destroy every other player's synced state as well as our own.
	public void ResetRun()
	{
		_player.Velocity = Vector3.Zero;
		_player.GlobalTransform = _spawnTransform;

		EmitSignal(SignalName.RunReset);
	}
}
