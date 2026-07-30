using Godot;

namespace Parkour.UI.Settings;

public partial class VideoSettingsTab : VBoxContainer
{
    [ExportGroup("Description Reference")]
    [Export] private DescriptionPanel _descriptionPanel;

	[ExportGroup("Panel Containers")]
    [Export] private PanelContainer _fovPanel;
    [Export] private PanelContainer _aspectRatioPanel;
    [Export] private PanelContainer _resolutionPanel;
    [Export] private PanelContainer _unlimitedFpsPanel;
    [Export] private PanelContainer _fpsPanel;
    [Export] private PanelContainer _brightnessPanel;
    [Export] private PanelContainer _restoreDefaultPanel;

    [ExportGroup("UI Controls")]
    [Export] private HSlider _fovSlider;
    [Export] private HSlider _brightnessSlider;
    [Export] private OptionButton _aspectRatioOption;
    [Export] private Button _restoreDefaultsButton;

    [ExportGroup("FPS Controls")]
    [Export] private CheckBox _unlimitedFpsCheckBox;
    [Export] private HSlider _fpsSlider;
    [Export] private SpinBox _fpsSpinBox;

    // Standard Defaults
    private const float DefaultFov = 90.0f;
    private const float DefaultBrightness = 50.0f;
    private const int DefaultFps = 144;
    private const bool DefaultUnlimitedFps = false;

    public override void _Ready()
    {
        SetupFpsLogic();
        SetupHoverDescriptions(); // NEW: Hook up hover events

        if (_restoreDefaultsButton != null)
        {
            _restoreDefaultsButton.Pressed += OnRestoreDefaultsPressed;
        }
    }

    private void SetupHoverDescriptions()
    {
        RegisterHover(_fovPanel, "fov");
        RegisterHover(_aspectRatioPanel, "aspect_ratio");
        RegisterHover(_resolutionPanel, "resolution");
        RegisterHover(_unlimitedFpsPanel, "unlimited_fps");
        RegisterHover(_fpsPanel, "max_fps");
        RegisterHover(_brightnessPanel, "brightness");
        RegisterHover(_restoreDefaultPanel, "restore_defaults");
    }

    private void RegisterHover(Control control, string key)
    {
        if (control == null || _descriptionPanel == null) return;

        control.MouseEntered += () => _descriptionPanel.ShowDescription(key);
        control.MouseExited += () => _descriptionPanel.ClearDescription();
    }

    private void SetupFpsLogic()
    {
        if (_unlimitedFpsCheckBox != null)
        {
            // Listen to checkbox toggles
            _unlimitedFpsCheckBox.Toggled += OnUnlimitedFpsToggled;

            // Apply initial state based on default editor setting
            OnUnlimitedFpsToggled(_unlimitedFpsCheckBox.ButtonPressed);
        }

        // Keep slider and spinbox synced
        if (_fpsSlider != null && _fpsSpinBox != null)
        {
            _fpsSlider.ValueChanged += OnFpsValueChanged;
            _fpsSpinBox.ValueChanged += OnFpsValueChanged;
        }
    }

    private void OnUnlimitedFpsToggled(bool isUnlimited)
    {
        // 1. Lock/unlock input fields
        if (_fpsSlider != null) _fpsSlider.Editable = !isUnlimited;
        if (_fpsSpinBox != null) _fpsSpinBox.Editable = !isUnlimited;

        // 2. Apply max FPS (0 in Godot = Unlimited)
        if (isUnlimited)
        {
            Engine.MaxFps = 0;
        }
        else if (_fpsSlider != null)
        {
            Engine.MaxFps = (int)_fpsSlider.Value;
        }
    }

    private void OnFpsValueChanged(double value)
    {
        // Ensure both UI elements mirror each other
        if (_fpsSlider != null && _fpsSlider.Value != value)
            _fpsSlider.Value = value;

        if (_fpsSpinBox != null && _fpsSpinBox.Value != value)
            _fpsSpinBox.Value = value;

        // Apply new limit if uncapped mode is off
        if (_unlimitedFpsCheckBox != null && !_unlimitedFpsCheckBox.ButtonPressed)
        {
            Engine.MaxFps = (int)value;
        }
    }

    private void OnRestoreDefaultsPressed()
    {
        // 1. Reset FOV (Triggers FovSlider script -> updates Camera3D)
        if (_fovSlider != null)
        {
            _fovSlider.Value = DefaultFov;
        }

        // 2. Reset Brightness (Triggers BrightnessSlider script -> updates WorldEnvironment)
        if (_brightnessSlider != null)
        {
            _brightnessSlider.Value = DefaultBrightness;
        }

        // 3. Reset Aspect Ratio & Resolution (Triggers AspectRatioOptionButton logic)
        if (_aspectRatioOption != null && _aspectRatioOption.ItemCount > 0)
        {
            _aspectRatioOption.Select(0);
            _aspectRatioOption.EmitSignal(OptionButton.SignalName.ItemSelected, 0);
        }

        // 4. Reset Unlimited FPS Checkbox
        if (_unlimitedFpsCheckBox != null)
        {
            _unlimitedFpsCheckBox.ButtonPressed = DefaultUnlimitedFps;
            OnUnlimitedFpsToggled(DefaultUnlimitedFps);
        }

        // 5. Reset FPS Value
        if (_fpsSlider != null)
        {
            _fpsSlider.Value = DefaultFps;
        }
    }
}