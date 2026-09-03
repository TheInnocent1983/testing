using Godot;

namespace Parkour.Interaction;

/// <summary>
/// Anything the player can aim at and trigger with the "interact" action.
/// InteractionComponent looks for this on whatever its ray hits.
/// </summary>
public interface IInteractable
{
	// Text the HUD shows while this object is focused ("Grab key", "Open door", ...)
	string Prompt { get; }

	// False while disabled, on cooldown, or already used up
	bool CanInteract(Node3D interactor);

	// Runs the interaction. Implementations re-check CanInteract themselves.
	void Interact(Node3D interactor);
}
