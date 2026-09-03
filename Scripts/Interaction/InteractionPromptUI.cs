using Godot;

namespace Parkour.Interaction;

/// <summary>
/// HUD hint for the currently focused interactable ("[E] Grab key").
/// Put this on a Control that holds a Label, point it at the player's InteractionComponent.
/// </summary>
[GlobalClass]
public partial class InteractionPromptUI : Control
{
	[ExportGroup("Nodes")]
	[Export] public InteractionComponent Interaction { get; set; }
	[Export] public Label PromptLabel { get; set; }

	[ExportGroup("Format")]
	[Export] public string Format { get; set; } = "[{0}] {1}";   // {0} = key, {1} = prompt text

	private string _keyHint = "E";

	public override void _Ready()
	{
		PromptLabel ??= GetNodeOrNull<Label>("PromptLabel");

		if (Interaction != null)
		{
			Interaction.FocusChanged += OnFocusChanged;
			_keyHint = GetKeyHint(Interaction.InteractAction);
		}

		Visible = false;
	}

	public override void _ExitTree()
	{
		if (Interaction != null && GodotObject.IsInstanceValid(Interaction))
			Interaction.FocusChanged -= OnFocusChanged;
	}

	// Rebinding happens at runtime, so refresh the hint whenever the key may have changed.
	public void RefreshKeyHint()
	{
		if (Interaction != null)
			_keyHint = GetKeyHint(Interaction.InteractAction);
	}

	private void OnFocusChanged(Node interactable, string prompt)
	{
		bool hasFocus = interactable != null && !string.IsNullOrEmpty(prompt);
		Visible = hasFocus;

		if (hasFocus && PromptLabel != null)
			PromptLabel.Text = string.Format(Format, _keyHint, prompt);
	}

	private static string GetKeyHint(string action)
	{
		if (!InputMap.HasAction(action)) return "?";

		foreach (InputEvent evt in InputMap.ActionGetEvents(action))
		{
			if (evt is InputEventKey key)
				return key.AsTextPhysicalKeycode();
		}

		return "?";
	}
}
