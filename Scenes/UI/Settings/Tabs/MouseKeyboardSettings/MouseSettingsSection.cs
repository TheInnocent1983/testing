using Godot;
using Parkour.Movement;

namespace Parkour.UI.Settings;

public partial class MouseSettingsSection : VBoxContainer
{
    // These will now be supplied at runtime via Initialize()
    private DescriptionPanel _descriptionPanel;
    private CameraController _cameraController;

    [ExportGroup("Other Controls")]
    [Export] private PanelContainer _restoreDefaultsPanel; 

    [ExportGroup("Row Containers")]
    [Export] private PanelContainer _mouseSensitivityPanel;
    [Export] private PanelContainer _adsModePanel;
    [Export] private PanelContainer _sprintModePanel;

    [ExportGroup("Mouse Sensitivity Controls")]
    [Export] private HSlider _mouseSlider;
    [Export] private SpinBox _mouseSpinBox;

    [ExportGroup("ADS Mode Controls")]
    [Export] private Button _adsHoldButton;
    [Export] private Button _adsToggleButton;

    [ExportGroup("Sprint Mode Controls")]
    [Export] private Button _sprintHoldButton;
    [Export] private Button _sprintToggleButton;

    private const float DefaultSensitivity = 50.0f;
    private bool _isSprintToggle = false;
    private bool _isAdsToggle = false;

    public override void _Ready()
    {
        SetupHoverDescriptions();
        SetupSensitivityLogic();
        SetupModeButtons();
    }

    /// <summary>
    /// Called by KeybindManager at runtime to inject scene dependencies.
    /// </summary>
    public void Initialize(DescriptionPanel descriptionPanel, CameraController cameraController = null)
    {
        _descriptionPanel = descriptionPanel;
        _cameraController = cameraController;
    }

    // --- Hover Descriptions ---
    private void SetupHoverDescriptions()
    {
        RegisterHover(_mouseSensitivityPanel, "mouse_sensitivity");
        RegisterHover(_adsModePanel, "ads_mode");
        RegisterHover(_sprintModePanel, "sprint_mode");
        RegisterHover(_restoreDefaultsPanel, "restore_defaults");
    }

    private void RegisterHover(Control control, string key)
    {
        if (control == null) return;

        control.MouseEntered += () =>
        {
            if (_descriptionPanel != null)
                _descriptionPanel.ShowDescription(key);
            else
                GD.PrintErr($"[MouseSettingsSection] _descriptionPanel is NULL when hovering '{key}'.");
        };

        control.MouseExited += () =>
        {
            if (_descriptionPanel != null)
                _descriptionPanel.ClearDescription();
        };
    }

    // --- Sensitivity Control ---
    private void SetupSensitivityLogic()
    {
        if (_mouseSlider != null && _mouseSpinBox != null)
        {
            _mouseSlider.ValueChanged += OnSensitivityChanged;
            _mouseSpinBox.ValueChanged += OnSensitivityChanged;
        }
    }

    private void OnSensitivityChanged(double value)
    {
        if (_mouseSlider != null && !Mathf.IsEqualApprox((float)_mouseSlider.Value, (float)value))
            _mouseSlider.Value = value;

        if (_mouseSpinBox != null && !Mathf.IsEqualApprox((float)_mouseSpinBox.Value, (float)value))
            _mouseSpinBox.Value = value;

        if (_cameraController != null)
        {
            _cameraController.SetMouseSensitivityFromUI((float)value);
        }
    }

    // --- Sprint & ADS Mode Toggles ---
    private void SetupModeButtons()
    {
        if (_sprintHoldButton != null)
            _sprintHoldButton.Pressed += () => SetSprintMode(isToggle: false);

        if (_sprintToggleButton != null)
            _sprintToggleButton.Pressed += () => SetSprintMode(isToggle: true);

        if (_adsHoldButton != null)
            _adsHoldButton.Pressed += () => SetAdsMode(isToggle: false);

        if (_adsToggleButton != null)
            _adsToggleButton.Pressed += () => SetAdsMode(isToggle: true);

        SetSprintMode(false);
        SetAdsMode(false);
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

    public void ResetToDefaults()
    {
        if (_mouseSlider != null)
            _mouseSlider.Value = DefaultSensitivity;

        SetSprintMode(isToggle: false);
        SetAdsMode(isToggle: false);
    }
}