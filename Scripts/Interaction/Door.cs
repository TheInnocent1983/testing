using Godot;

namespace Parkour.Interaction;

/// <summary>
/// A door that can be opened by anything — an Interactable's signal, a trigger, level script.
/// Right now "open" just means the visual disappears and the collision stops blocking.
/// Assign an AnimationPlayer later and it plays that instead, with no other change needed.
/// </summary>
[GlobalClass]
public partial class Door : Node3D
{
	[Signal]
	public delegate void OpenedEventHandler();

	[Signal]
	public delegate void ClosedEventHandler();

	[ExportGroup("Nodes")]
	[Export] public Node3D VisualRoot { get; set; }        // What is hidden while open. Defaults to this
	[Export] public Node CollisionRoot { get; set; }       // Shapes to disable. Defaults to VisualRoot
	[Export] public AnimationPlayer Animator { get; set; } // Optional — placeholder hide is used when null

	[ExportGroup("Animation")]
	[Export] public StringName OpenAnimation { get; set; } = "open";
	[Export] public StringName CloseAnimation { get; set; } = "close";

	[ExportGroup("Behaviour")]
	[Export] public bool StartOpen { get; set; }
	[Export] public bool ToggleOnInteract { get; set; }    // Off = an interaction only ever opens it
	[Export] public bool BlockWhileOpen { get; set; }      // Keep collision on while open (sliding grate, etc.)

	public bool IsOpen { get; private set; }

	public override void _Ready()
	{
		VisualRoot ??= this;
		CollisionRoot ??= VisualRoot;

		ApplyState(StartOpen, instant: true);
	}

	// Signal target: Interactable.Interacted passes the interactor, so the arity has to match.
	public void OnInteracted(Node3D interactor)
	{
		if (ToggleOnInteract)
			Toggle();
		else
			Open();
	}

	public void Open() => ApplyState(true, instant: false);

	public void Close() => ApplyState(false, instant: false);

	public void Toggle() => ApplyState(!IsOpen, instant: false);

	private void ApplyState(bool open, bool instant)
	{
		IsOpen = open;

		StringName anim = open ? OpenAnimation : CloseAnimation;
		bool animated = !instant
			&& Animator != null
			&& GodotObject.IsInstanceValid(Animator)
			&& !string.IsNullOrEmpty(anim)
			&& Animator.HasAnimation(anim);

		if (animated)
			Animator.Play(anim);
		else if (VisualRoot != null)
			VisualRoot.Visible = !open; // Placeholder until the animation exists

		InteractionUtil.SetCollisionEnabled(CollisionRoot, !open || BlockWhileOpen);

		EmitSignal(open ? SignalName.Opened : SignalName.Closed);
	}
}
