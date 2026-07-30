using Godot;
using Parkour.Movement;

namespace Parkour.UI.Settings;

public partial class ControllerSettingsSection : VBoxContainer
{
	private DescriptionPanel _descriptionPanel;
	private CameraController _cameraController;

	[ExportGroup("Row Containers (for Hover Descriptions)")]
	[Export] private PanelContainer _lookSensitivityPanel;
	[Export] private PanelContainer _adsModePanel;
	[Export] private PanelContainer _sprintModePanel;
	[Export] private PanelContainer _crouchModePanel;
	[Export] private PanelContainer _restoreDefaultPanel;

	[ExportGroup("Look Sensitivity Controls")]
	[Export] private HSlider _lookSlider;
	[Export] private SpinBox _lookSpinBox;

	[ExportGroup("ADS Mode Controls")]
	[Export] private Button _adsHoldButton;
	[Export] private Button _adsToggleButton;

	[ExportGroup("Sprint Mode Controls")]
	[Export] private Button _sprintHoldButton;
	[Export] private Button _sprintToggleButton;

	[ExportGroup("Crouch Mode Controls")]
	[Export] private Button _crouchHoldButton;
	[Export] private Button _crouchToggleButton;

	[ExportGroup("Restore Defaults")]
	[Export] private Button _restoreDefaultsButton;

	private const float DefaultSensitivity = 50.0f;
	private bool _isSprintToggle = false;
	private bool _isAdsToggle = false;
	private bool _isCrouchToggle = false;

	public override void _Ready()
	{
		SetupHoverDescriptions();
		SetupSensitivityLogic();
		SetupModeButtons();
	}

	/// <summary>
	/// Runtime dependency injection from parent manager scene.
	/// </summary>
	public void Initialize(DescriptionPanel descriptionPanel, CameraController cameraController = null)
	{
		_descriptionPanel = descriptionPanel;
		_cameraController = cameraController;
	}

	// --- Hover Descriptions ---
	private void SetupHoverDescriptions()
	{
		RegisterHover(_lookSensitivityPanel, "controller_look_sensitivity");
		RegisterHover(_adsModePanel, "ads_mode");
		RegisterHover(_sprintModePanel, "sprint_mode");
		RegisterHover(_crouchModePanel, "crouch_mode");
		RegisterHover(_restoreDefaultPanel, "restore_defaults");
	}

	private void RegisterHover(Control control, string key)
	{
		if (control == null) return;

		control.MouseEntered += () =>
		{
			if (_descriptionPanel != null)
				_descriptionPanel.ShowDescription(key);
		};

		control.MouseExited += () =>
		{
			if (_descriptionPanel != null)
				_descriptionPanel.ClearDescription();
		};
	}

	// --- Sensitivity ---
	private void SetupSensitivityLogic()
	{
		if (_lookSlider != null && _lookSpinBox != null)
		{
			_lookSlider.ValueChanged += OnSensitivityChanged;
			_lookSpinBox.ValueChanged += OnSensitivityChanged;
		}
	}

	private void OnSensitivityChanged(double value)
	{
		if (_lookSlider != null && !Mathf.IsEqualApprox((float)_lookSlider.Value, (float)value))
			_lookSlider.Value = value;

		if (_lookSpinBox != null && !Mathf.IsEqualApprox((float)_lookSpinBox.Value, (float)value))
			_lookSpinBox.Value = value;

		if (_cameraController != null)
		{
			// Update controller sensitivity on camera
			_cameraController.SetControllerSensitivityFromUI((float)value);
		}
	}

	// --- Mode Buttons ---
	private void SetupModeButtons()
	{
		if (_sprintHoldButton != null) _sprintHoldButton.Pressed += () => SetSprintMode(false);
		if (_sprintToggleButton != null) _sprintToggleButton.Pressed += () => SetSprintMode(true);

		if (_adsHoldButton != null) _adsHoldButton.Pressed += () => SetAdsMode(false);
		if (_adsToggleButton != null) _adsToggleButton.Pressed += () => SetAdsMode(true);

		if (_crouchHoldButton != null) _crouchHoldButton.Pressed += () => SetCrouchMode(false);
		if (_crouchToggleButton != null) _crouchToggleButton.Pressed += () => SetCrouchMode(true);

		if (_restoreDefaultsButton != null) _restoreDefaultsButton.Pressed += ResetToDefaults;

		SetSprintMode(false);
		SetAdsMode(false);
		SetCrouchMode(false);
	}

	private void SetSprintMode(bool isToggle)
	{
		_isSprintToggle = isToggle;
		if (_sprintHoldButton != null) _sprintHoldButton.Disabled = !isToggle;
		if (_sprintToggleButton != null) _sprintToggleButton.Disabled = isToggle;
	}

	private void SetAdsMode(bool isToggle)
	{
		_isAdsToggle = isToggle;
		if (_adsHoldButton != null) _adsHoldButton.Disabled = !isToggle;
		if (_adsToggleButton != null) _adsToggleButton.Disabled = isToggle;
	}

	private void SetCrouchMode(bool isToggle)
	{
		_isCrouchToggle = isToggle;
		if (_crouchHoldButton != null) _crouchHoldButton.Disabled = !isToggle;
		if (_crouchToggleButton != null) _crouchToggleButton.Disabled = isToggle;
	}

	public void ResetToDefaults()
	{
		if (_lookSlider != null) _lookSlider.Value = DefaultSensitivity;
		SetSprintMode(false);
		SetAdsMode(false);
		SetCrouchMode(false);
	}
}
