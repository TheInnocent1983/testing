using Godot;
using Parkour.Network;

namespace Parkour.UI;

public partial class HighLevelUi : Control
{
	private HighLevelNetworkHandler networkHandler;

	public override void _Ready()
	{
		// The handler is an autoload; it outlives this menu and any scene reload.
		networkHandler = HighLevelNetworkHandler.Instance;

		// A scene reload rebuilds this menu even though the connection survived,
		// so don't ask the player to pick a role they already picked.
		if (networkHandler.IsNetworkActive)
		{
			Hide();
			return;
		}

		GetNode<Button>("VBoxContainer/Server").Pressed += OnServerPressed;
		GetNode<Button>("VBoxContainer/Client").Pressed += OnClientPressed;
	}

	private void OnServerPressed()
	{
		networkHandler.StartServer();
		Hide();
	}

	private void OnClientPressed()
	{
		networkHandler.StartClient();
		Hide();
	}
}
