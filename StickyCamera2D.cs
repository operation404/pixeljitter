using Godot;
using System;

namespace DungeonSynn.CustomNode;

[GlobalClass]
public partial class StickyCamera2D : Camera2D
{
    #region Class members
    #region ========== Public Data ==========

    public bool ForceCentered = false;

    [Export]
    public Node2D Target { get; private set; } = null;

    #endregion
    #region ========== Restricted Data ==========

    private Viewport _viewport;
    private Vector2 _viewportCenter;
    private Vector2 _axisScaling;
    private Vector2 _zoom = Vector2.One;

    #endregion
    #endregion
    #region ============================== Setup ==============================

    private void CalculateCameraAxisScaling()
    {
        _viewport = GetViewport();
        Vector2 viewportSize = _viewport.GetVisibleRect().Size;

        _viewportCenter = viewportSize / 2;

        if (viewportSize.X > viewportSize.Y)
        {
            _axisScaling = new(1, viewportSize.X / viewportSize.Y);
        }
        else
        {
            _axisScaling = new(viewportSize.Y / viewportSize.X, 1);
        }
    }

    #endregion
    #region ============================== Functionality ==============================

    public void SetTarget(Node2D target)
    {
        Target = target;
        SetProcess(Target != null);
    }

    private Vector2 CalcCameraPosition()
    {
        const float SnapToCenterThreshold = 200f;
        const float MaxTravelDistance = 100f;

        Vector2 mouseOffsetFromCenter = (_viewport.GetMousePosition() - _viewportCenter) * _axisScaling;
        float lengthSquared = mouseOffsetFromCenter.LengthSquared();

        // Snap to center of screen if mouse is close enough to the center
        if (lengthSquared <= (SnapToCenterThreshold * SnapToCenterThreshold))
            return Target.GlobalPosition;

        // Move camera towards screen edges
        float distancePastThreshold = MathF.Sqrt(lengthSquared) - SnapToCenterThreshold;
        float moveCamDist = MathF.Min(distancePastThreshold, MaxTravelDistance);
        return Target.GlobalPosition + mouseOffsetFromCenter.Normalized() * moveCamDist;
    }

    #endregion
    #region ============================== Processing ==============================

    public override void _Process(double delta)
    {
        const float FollowSpeed = 4f;
        const float ZoomSpeed = 4f;

        Vector2 nextCamPos = ForceCentered ? Target.GlobalPosition : CalcCameraPosition();

        if (!nextCamPos.IsEqualApprox(GlobalPosition))
            GlobalPosition = GlobalPosition.Lerp(nextCamPos, (float)delta * FollowSpeed);

        if (!Zoom.IsEqualApprox(_zoom))
            Zoom = Zoom.Lerp(_zoom, (float)delta * ZoomSpeed);
    }

    #endregion
    #region ============================== Event Handlers ==============================

    public override void _UnhandledInput(InputEvent @event)
    {
        const float MinZoom = 0.5f;
        const float MaxZoom = 4;

        if (!@event.IsPressed())
            return;

        if (@event.IsAction("ZoomIn", exactMatch: false))
        {
            float zoom = Clamp(_zoom.X + 0.1f, MinZoom, MaxZoom);
            _zoom = new(zoom, zoom);
        }
        else if (@event.IsAction("ZoomOut", exactMatch: false))
        {
            float zoom = Clamp(_zoom.X - 0.1f, MinZoom, MaxZoom);
            _zoom = new(zoom, zoom);
        }
        else if (@event.IsAction("ZoomReset", exactMatch: false))
        {
            _zoom = Vector2.One;
        }
    }

    private static T Clamp<T>(T val, T min, T max) where T : System.Numerics.INumber<T>
    {
        return val < min ? min : (val > max ? max : val);
    }

    public override void _Notification(int what)
    {
        switch ((long)what)
        {
            case NotificationReady:
                SetProcess(Target != null);
                break;

            case NotificationPostEnterTree:
                CalculateCameraAxisScaling();
                break;
        }
    }

    #endregion
    #region ============================== Disposal ==============================

    protected override void Dispose(bool safeToDisposeManagedObjects)
    {
        if (safeToDisposeManagedObjects)
        {
            SetProcess(false);
            Target = null;
            _viewport = null;
        }
        base.Dispose(safeToDisposeManagedObjects);
    }

    #endregion
}