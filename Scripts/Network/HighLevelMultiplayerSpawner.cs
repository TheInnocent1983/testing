using Godot;

namespace Parkour.Network;

public partial class HighLevelMultiplayerSpawner : MultiplayerSpawner
{
    [Export] public PackedScene NetworkPlayer { get; set; }

    public override void _Ready()
    {
        Multiplayer.PeerConnected += SpawnPlayer;
    }

    public void SpawnPlayer(long id)
    {
        if (!Multiplayer.IsServer()) return;

        Node player = NetworkPlayer.Instantiate();
        player.Name = id.ToString();
        GetNode(SpawnPath).CallDeferred(Node.MethodName.AddChild, player);
    }
}
