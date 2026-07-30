using Godot;

namespace Parkour.Network;

public partial class HighLevelNetworkHandler : Node
{
	const string IP_ADDRESS = "localhost";
	const int PORT = 42069;

	ENetMultiplayerPeer peer;

	public void StartServer()
	{
		peer = new ENetMultiplayerPeer();
		peer.CreateServer(PORT, 4);
		Multiplayer.MultiplayerPeer = peer;
	}

	public void StartClient()
	{
		peer = new ENetMultiplayerPeer();
		peer.CreateClient(IP_ADDRESS, PORT);
		Multiplayer.MultiplayerPeer = peer;
	}
}
