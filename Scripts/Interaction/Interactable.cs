using Godot;

namespace Parkour.Interaction;

/// <summary>
/// Marks a prop as interactable. Add it as a child of any CollisionObject3D
/// (StaticBody3D / Area3D / RigidBody3D) — InteractionComponent resolves it from the collider
/// its ray hits, the same way the movement components hang off the player body.
///
/// Behaviour that belongs to the object itself goes in OnInteract (subclass); reactions in
/// other nodes (a door opening) are wired through the Interacted signal in the editor.
/// </summary>
[GlobalClass]
public partial class Interactable : Node, IInteractable
{
	[Signal]
	public delegate void InteractedEventHandler(Node3D interactor);

	[Signal]
	public delegate void AvailabilityChangedEventHandler(bool available);

	[ExportGroup("Prompt")]
	[Export] public string Prompt { get; set; } = "Interact";

	[ExportGroup("Rules")]
	[Export] public bool Enabled { get; set; } = true;
	[Export] public bool OneShot { get; set; }        // Usable once — keys, levers that stay pulled
	[Export] public float Cooldown { get; set; }      // Seconds before re-use. 0 = none

	public bool Used { get; private set; }

	private float _cooldownLeft;

	public override void _Ready()
	{
		SetProcess(Cooldown > 0.0f);
	}

	public override void _Process(double delta)
	{
		if (_cooldownLeft > 0.0f)
			_cooldownLeft -= (float)delta;
	}

	public virtual bool CanInteract(Node3D interactor)
	{
		if (!Enabled) return false;
		if (OneShot && Used) return false;

		return _cooldownLeft <= 0.0f;
	}

	public void Interact(Node3D interactor)
	{
		if (!CanInteract(interactor)) return;

		Used = true;
		_cooldownLeft = Cooldown;

		OnInteract(interactor);
		EmitSignal(SignalName.Interacted, interactor);
	}

	// Base does nothing on purpose — the signal is the hook for level scripting.
	protected virtual void OnInteract(Node3D interactor) { }

	public void SetEnabled(bool enabled)
	{
		if (Enabled == enabled) return;

		Enabled = enabled;
		EmitSignal(SignalName.AvailabilityChanged, enabled);
	}
}
