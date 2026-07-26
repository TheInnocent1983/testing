using Godot;
using Parkour.Movement;
using System;

public partial class RestartLevelComponent : Node
{
	private FpsController _player;

	public override void _Ready()
	{
		_player = GetOwner<FpsController>() ?? GetParent()?.GetParent() as FpsController;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("restart"))
		{
			GetTree().ReloadCurrentScene();
			return;
		}
	}
}
