using Godot;

namespace Parkour.Interaction;

/// <summary>
/// Shared helpers for turning interactable props solid / non-solid.
/// </summary>
public static class InteractionUtil
{
	// Toggles every CollisionShape3D under root (root included). Deferred, because shapes
	// cannot be enabled or disabled from inside a physics callback.
	public static void SetCollisionEnabled(Node root, bool enabled)
	{
		if (root == null || !GodotObject.IsInstanceValid(root)) return;

		if (root is CollisionShape3D rootShape)
			rootShape.SetDeferred(CollisionShape3D.PropertyName.Disabled, !enabled);

		// owned: false so shapes inside instanced sub-scenes (.glb imports) are found too
		foreach (Node child in root.FindChildren("*", nameof(CollisionShape3D), true, false))
		{
			if (child is CollisionShape3D shape)
				shape.SetDeferred(CollisionShape3D.PropertyName.Disabled, !enabled);
		}
	}
}
