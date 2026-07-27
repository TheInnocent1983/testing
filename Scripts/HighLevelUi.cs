using Godot;
using Parkour.Network;

namespace Parkour.UI;

public partial class HighLevelUi : Control
{
	private HighLevelNetworkHandler networkHandler;

	public override void _Ready()
	{
		networkHandler = new HighLevelNetworkHandler { Name = nameof(HighLevelNetworkHandler) };
		AddChild(networkHandler);

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
