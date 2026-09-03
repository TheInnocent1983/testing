using Godot;

namespace Parkour.Interaction;

/// <summary>
/// An interactable that takes itself out of the world when grabbed — keys, orbs, collectibles.
/// Hides rather than frees by default, so the Interacted signal and any future grab animation
/// still have a live node to run on.
/// </summary>
[GlobalClass]
public partial class PickupInteractable : Interactable
{
	[Signal]
	public delegate void PickedUpEventHandler(Node3D interactor);

	[ExportGroup("Pickup")]
	[Export] public Node3D PickupRoot { get; set; }        // What disappears. Defaults to the scene root
	[Export] public float RemoveDelay { get; set; }        // Delay before it vanishes — room for a grab anim
	[Export] public bool FreeAfterPickup { get; set; }     // Free instead of hide (freed nodes can't animate)

	public PickupInteractable()
	{
		// Sensible default for a collectible; still overridable in the inspector.
		OneShot = true;
	}

	public override void _Ready()
	{
		base._Ready();

		PickupRoot ??= ResolvePropRoot();
	}

	// The prop's own scene root when this lives in a sub-scene. Dropped straight into a level
	// the owner would be the level root, so fall back to the collision body we hang off — never
	// hide the whole map because someone skipped making a scene for the prop.
	private Node3D ResolvePropRoot()
	{
		Node3D owner = GetOwner<Node3D>();
		if (owner != null && owner != GetTree().CurrentScene)
			return owner;

		for (Node node = GetParent(); node != null; node = node.GetParent())
		{
			if (node is CollisionObject3D body)
				return body;
		}

		return GetParent<Node3D>();
	}

	protected override void OnInteract(Node3D interactor)
	{
		if (RemoveDelay > 0.0f)
		{
			GetTree().CreateTimer(RemoveDelay).Timeout += () => RemoveProp(interactor);
			return;
		}

		RemoveProp(interactor);
	}

	private void RemoveProp(Node3D interactor)
	{
		if (PickupRoot == null || !GodotObject.IsInstanceValid(PickupRoot)) return;

		InteractionUtil.SetCollisionEnabled(PickupRoot, false);
		PickupRoot.Visible = false;

		EmitSignal(SignalName.PickedUp, interactor);

		if (FreeAfterPickup)
			PickupRoot.QueueFree();
	}
}
