using Godot;
using Parkour.Movement;
using System;

namespace Parkour.Movement;

public partial class CrouchComponent : Node
{
    [ExportGroup("Crouch Settings")]
    [Export] public float CrouchSpeed { get; set; } = 3.0f;
    [Export] public float CrouchDepth { get; set; } = 0.45f;
    [Export] public float CrouchLerpSpeed { get; set; } = 10.0f;
    [Export] public float StandCapsuleHeight { get; set; } = 2.0f;

    [ExportGroup("Inputs & Nodes")]
    [Export] public RayCast3D CeilingCheck { get; set; }
    [Export] public string HoldAction { get; set; } = "slide";
    [Export] public string ToggleAction { get; set; } = "crouch_toggle";

    private bool _isCrouchToggled = false;

    public bool IsCrouching { get; private set; } = false;

    public override void _Ready()
    {
        if (CeilingCheck == null)
            CeilingCheck = GetNodeOrNull<RayCast3D>("%CeilingCheck");
    }

    public void UpdateCrouch(FpsController player, double delta)
    {
        if (player == null) return;

        // FIXED: Use IsActionPressed for holding input, not IsActionJustPressed
        bool holdPressed = InputMap.HasAction(HoldAction) && Input.IsActionPressed(HoldAction);
        bool toggleJustPressed = InputMap.HasAction(ToggleAction) && Input.IsActionJustPressed(ToggleAction);

        if (toggleJustPressed)
            _isCrouchToggled = !_isCrouchToggled;

        bool crouchRequested = holdPressed || _isCrouchToggled;

        // FIXED: Update IsCrouching property!
        IsCrouching = crouchRequested || IsCeilingBlocked();

        if (!crouchRequested && !IsCeilingBlocked())
            _isCrouchToggled = false;

        float targetHeight = GetCrouchCapsuleHeight(player);
        float headYOffset = IsCrouching ? -Mathf.Abs(CrouchDepth) : 0.0f;
        float heightToApply = IsCrouching ? targetHeight : GetStandHeight(player);

        player.ApplyStance(headYOffset, heightToApply, CrouchLerpSpeed, (float)delta);
    }

    public bool IsCeilingBlocked()
    {
        return CeilingCheck != null && CeilingCheck.IsColliding();
    }

    private float GetStandHeight(FpsController player)
    {
        return StandCapsuleHeight > 0.0f ? StandCapsuleHeight : (player.DefaultCapsuleHeight > 0.0f ? player.DefaultCapsuleHeight : 2.0f);
    }

    public float GetCrouchCapsuleHeight(FpsController player)
    {
        return Mathf.Max(0.2f, GetStandHeight(player) - Mathf.Abs(CrouchDepth));
    }
}