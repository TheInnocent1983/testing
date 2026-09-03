using Godot;

namespace Parkour.Interaction;

/// <summary>
/// Player-side half of the interaction system. Casts a short ray out of the camera every
/// physics frame, tracks what is focused, and fires the focused Interactable on "interact".
///
/// Lives under MovementComponents alongside the other player components.
/// </summary>
[GlobalClass]
public partial class InteractionComponent : Node
{
	// interactable is null when focus is lost; prompt is empty in that case.
	[Signal]
	public delegate void FocusChangedEventHandler(Node interactable, string prompt);

	[Signal]
	public delegate void InteractionPerformedEventHandler(Node interactable);

	[ExportGroup("Nodes")]
	[Export] public Camera3D Camera { get; set; }
	[Export] public Node3D Interactor { get; set; }   // The player body — excluded from the ray

	[ExportGroup("Ray")]
	[Export] public float Range { get; set; } = 3.0f;
	// Layer 1 keeps walls blocking the ray; add the layer your interactables live on.
	[Export(PropertyHint.Layers3DPhysics)] public uint CollisionMask { get; set; } = 0b101;
	[Export] public bool DetectAreas { get; set; } = true;
	[Export] public int SearchDepth { get; set; } = 2;   // Parents to walk up from the hit collider

	[ExportGroup("Input")]
	[Export] public string InteractAction { get; set; } = "interact";
	[Export] public bool RequireCapturedMouse { get; set; } = true;

	public IInteractable Focused { get; private set; }
	public Node FocusedNode { get; private set; }

	private Rid _interactorRid;

	public override void _Ready()
	{
		Camera ??= GetNodeOrNull<Camera3D>("%Camera3D");

		if (Interactor is CollisionObject3D body)
			_interactorRid = body.GetRid();
	}

	public override void _PhysicsProcess(double delta)
	{
		UpdateFocus(FindInteractable());
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!InputMap.HasAction(InteractAction)) return;
		if (!@event.IsActionPressed(InteractAction)) return;
		if (RequireCapturedMouse && Input.MouseMode != Input.MouseModeEnum.Captured) return;

		TryInteract();
	}

	public bool TryInteract()
	{
		if (Focused == null || !Focused.CanInteract(Interactor)) return false;

		Focused.Interact(Interactor);
		EmitSignal(SignalName.InteractionPerformed, FocusedNode);

		// The interaction may have consumed the object — refresh so the prompt clears.
		UpdateFocus(FindInteractable());
		return true;
	}

	private void UpdateFocus(IInteractable found)
	{
		Node foundNode = found as Node;
		if (foundNode == FocusedNode) return;

		Focused = found;
		FocusedNode = foundNode;

		EmitSignal(SignalName.FocusChanged, foundNode, found?.Prompt ?? string.Empty);
	}

	private IInteractable FindInteractable()
	{
		if (Camera == null || !GodotObject.IsInstanceValid(Camera)) return null;

		Vector3 from = Camera.GlobalPosition;
		Vector3 to = from + -Camera.GlobalTransform.Basis.Z * Range;

		var query = PhysicsRayQueryParameters3D.Create(from, to, CollisionMask);
		query.CollideWithAreas = DetectAreas;
		query.CollideWithBodies = true;

		if (_interactorRid.IsValid)
			query.Exclude = new Godot.Collections.Array<Rid> { _interactorRid };

		Godot.Collections.Dictionary hit = Camera.GetWorld3D().DirectSpaceState.IntersectRay(query);
		if (hit.Count == 0) return null;

		Node collider = hit["collider"].As<GodotObject>() as Node;
		IInteractable interactable = Resolve(collider);

		// Only offer it while it is actually usable, so the prompt tells the truth.
		return interactable != null && interactable.CanInteract(Interactor) ? interactable : null;
	}

	// The ray hits a collider, but the Interactable is usually a child component of it — and
	// sometimes the collider is a child of the prop root. Check both directions, shallowly.
	private IInteractable Resolve(Node collider)
	{
		Node node = collider;

		for (int depth = 0; node != null && depth <= SearchDepth; depth++, node = node.GetParent())
		{
			if (node is IInteractable self) return self;

			foreach (Node child in node.GetChildren())
			{
				if (child is IInteractable component) return component;
			}
		}

		return null;
	}
}
