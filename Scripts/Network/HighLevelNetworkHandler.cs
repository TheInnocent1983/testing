using Godot;
using System;

namespace Parkour.Network;

/// <summary>
/// Owns the lifetime of the network connection. Registered as an autoload so it
/// lives on /root and survives scene reloads (see RestartLevelComponent).
/// </summary>
public partial class HighLevelNetworkHandler : Node
{
	/// The autoload instance. Available to any script from _Ready onwards.
	public static HighLevelNetworkHandler Instance { get; private set; }

	const string IP_ADDRESS = "localhost";
	const int PORT = 42069;
	const int MAX_PLAYERS = 4;

	ENetMultiplayerPeer peer;

	/// True once StartServer/StartClient has run and the peer is still alive.
	/// Checked against our own peer field rather than Multiplayer.MultiplayerPeer,
	/// because Godot installs an OfflineMultiplayerPeer by default that reports
	/// itself as connected.
	public bool IsNetworkActive =>
		peer is not null &&
		peer.GetConnectionStatus() != MultiplayerPeer.ConnectionStatus.Disconnected;

	/// Raised on the host right after it becomes a server. Multiplayer.PeerConnected
	/// never fires for peer 1, so anything that needs to react to "the host joined"
	/// has to listen here instead.
	public event Action ServerStarted;

	public override void _EnterTree()
	{
		Instance = this;
	}

	public void StartServer()
	{
		peer = new ENetMultiplayerPeer();
		peer.CreateServer(PORT, MAX_PLAYERS);
		Multiplayer.MultiplayerPeer = peer;

		ServerStarted?.Invoke();
	}

	public void StartClient()
	{
		peer = new ENetMultiplayerPeer();
		peer.CreateClient(IP_ADDRESS, PORT);
		Multiplayer.MultiplayerPeer = peer;
	}
}
