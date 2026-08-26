using Godot;

namespace Parkour.Network;

public partial class HighLevelMultiplayerSpawner : MultiplayerSpawner
{
    /// The host is always peer 1 in Godot's high-level API.
    private const long HostPeerId = 1;

    [Export] public PackedScene NetworkPlayer { get; set; }

    public override void _Ready()
    {
        Multiplayer.PeerConnected += SpawnPlayer;
        Multiplayer.PeerDisconnected += DespawnPlayer;

        // PeerConnected never fires for the host itself, so without this the
        // server would have no player of its own.
        HighLevelNetworkHandler.Instance.ServerStarted += OnServerStarted;
    }

    public override void _ExitTree()
    {
        Multiplayer.PeerConnected -= SpawnPlayer;
        Multiplayer.PeerDisconnected -= DespawnPlayer;

        // The handler outlives this scene, so a subscription left behind here
        // would keep pointing at a freed node.
        if (HighLevelNetworkHandler.Instance is not null)
            HighLevelNetworkHandler.Instance.ServerStarted -= OnServerStarted;
    }

    private void OnServerStarted() => SpawnPlayer(HostPeerId);

    public void SpawnPlayer(long id)
    {
        if (!Multiplayer.IsServer()) return;

        Node player = NetworkPlayer.Instantiate();
        player.Name = id.ToString();
        GetNode(SpawnPath).CallDeferred(Node.MethodName.AddChild, player);
    }

    public void DespawnPlayer(long id)
    {
        if (!Multiplayer.IsServer()) return;

        // Freeing on the server propagates the removal to every client.
        GetNode(SpawnPath).GetNodeOrNull(id.ToString())?.QueueFree();
    }
}
