using Godot;

namespace Parkour.UI.Settings;

public partial class RestoreDefaultsSection : VBoxContainer
{
	[Export] private MouseSettingsSection _mouseSettingsSection;
	[Export] private Button _restoreDefaultsButton;
	[Export] private PanelContainer _restoreDefaultPanel;
	[Export] private DescriptionPanel _descriptionPanel;

	public override void _Ready()
	{
		if (_restoreDefaultsButton != null)
		{
			_restoreDefaultsButton.Pressed += () => _mouseSettingsSection?.ResetToDefaults();
		}

		if (_restoreDefaultPanel != null && _descriptionPanel != null)
		{
			_restoreDefaultPanel.MouseEntered += () => _descriptionPanel.ShowDescription("restore_defaults");
			_restoreDefaultPanel.MouseExited += () => _descriptionPanel.ClearDescription();
		}
	}
}
