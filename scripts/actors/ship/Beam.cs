using Godot;
using System.Collections.Generic;

namespace Orbitals;


public partial class Beam : Area2D
{
    public enum BeamMode { Off, Single, Multiple }
    [Signal] public delegate void HitEventHandler();
    [Signal] public delegate void CritterCollectedEventHandler(Critter creature);

    [Export] public float PullSpeed { get; set; } = 400f;
    [Export] public float PullAcceleration { get; set; } = 700f;
    [Export] public float CaptureRadius { get; set; } = 10f;
    [Export] public float BeamLength { get; set; } = 500f;
    [Export] public float TopWidth { get; set; } = 80f;
    [Export] public float BottomWidth { get; set; } = 340f;
    [Export] public Color BeamColor { get; set; } = new(0.35f, 0.95f, 1f);
    [Export] public Color HoldColor { get; set; } = new(0.85f, 0.45f, 1f);
    [Export] public float HoldDistance { get; set; } = 90f;

    [Export] public Planet PlanetInstance { get; set; }
    [Export] public CollisionPolygon2D Collision { get; set; }
    [Export] public BeamVisuals Visuals { get; set; }
    public bool IsActive { get; private set; }
    public BeamMode Mode { get; private set; }
    public Color ActiveColor => Mode == BeamMode.Single ? HoldColor : BeamColor;
    public int TargetCount => _targets.Count;
    public float CurrentLength { get; private set; }

    private sealed class Capture
    {
        public Planet Planet;
        public Vector2 OriginalScale;
        public float InitialDistance;
        public float Speed = 100f;
    }

    private readonly Dictionary<Critter, Capture> _targets = new();
    private CollisionPolygon2D _collision;
    private float _lastTopWidth, _lastBottomWidth;

    // Initializes creature-only detection and the cone shape, and starts hidden.
    public override void _Ready()
    {
        ProcessPhysicsPriority = -5;
        CollisionLayer = 0;
        CollisionMask = 2;
        _collision = Collision;
        if (!GodotObject.IsInstanceValid(_collision))
        {
            GD.PushError($"{Name}: assign the cone collision polygon.");
            SetPhysicsProcess(false);
        }
        UpdateGeometry();
        Hide();
    }

    // Updates cone geometry and captures, then supplies presentation values to the visual component.
    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        UpdateGeometry();
        if (!IsActive) return;

        if (GodotObject.IsInstanceValid(Visuals)) Visuals.AdvanceEffects(dt);
        AcquireTargets();
        UpdateTargets(dt);
        UpdateVisuals();
    }

    // Finds overlapping creatures, preferring the nearest one for the single-target hold mode.
    private void AcquireTargets()
    {
        // Polling includes creatures already inside when the beam is switched on.
        var candidates = new List<Critter>();
        foreach (Node2D body in GetOverlappingBodies())
        {
            if (body is not Critter critter || critter.IsQueuedForDeletion() ||
                _targets.ContainsKey(critter) || !GodotObject.IsInstanceValid(critter.World)) continue;
            candidates.Add(critter);
        }
        candidates.Sort((a, b) => a.GlobalPosition.DistanceSquaredTo(GlobalPosition)
            .CompareTo(b.GlobalPosition.DistanceSquaredTo(GlobalPosition)));
        foreach (Critter critter in candidates)
        {
            if (Mode == BeamMode.Single && _targets.Count >= 1) break;

            _targets.Add(critter, new Capture
            {
                Planet = critter.World,
                OriginalScale = critter.GlobalScale,
                InitialDistance = Mathf.Max(1f, critter.GlobalPosition.DistanceTo(GlobalPosition))
            });
            critter.MarkCarried();
            critter.DetachFromPlanet();
            EmitSignal(SignalName.Hit);
        }
    }

    // Updates latched creatures and discards targets removed by another system.
    private void UpdateTargets(float dt)
    {
        // Captured creatures stay latched during ship movement until collected or released.
        foreach (var entry in new List<KeyValuePair<Critter, Capture>>(_targets))
        {
            Critter critter = entry.Key;
            Capture capture = entry.Value;

            // A mothership can remove a carried target between beam physics ticks.
            if (!GodotObject.IsInstanceValid(critter) || critter.IsQueuedForDeletion())
            {
                _targets.Remove(critter);
                continue;
            }

            capture.Speed = Mathf.MoveToward(capture.Speed, Mathf.Max(1f, PullSpeed),
                Mathf.Max(0f, PullAcceleration) * dt);
            UpdateHeldTarget(critter, capture, dt);
        }
    }


    // Moves one creature toward the safe hover point without shrinking or consuming it.
    private void UpdateHeldTarget(Critter critter, Capture capture, float dt)
    {
        float halfHeight = critter.Profile.SpriteHeight * Mathf.Abs(capture.OriginalScale.Y) / 2f;
        float hoverY = Mathf.Min(Mathf.Max(24f, HoldDistance),
            Mathf.Max(24f, CurrentLength - halfHeight - 6f));
        Vector2 hoverPoint = ToGlobal(new Vector2(0f, hoverY));
        critter.GlobalScale = capture.OriginalScale;
        critter.GlobalPosition = critter.GlobalPosition.MoveToward(hoverPoint, capture.Speed * dt);
        critter.GlobalRotation = Mathf.LerpAngle(critter.GlobalRotation, 0f, Mathf.Min(1f, dt * 8f));
    }

    // Compatibility helper: true selects absorption; false switches the beam off.
    public void SetActive(bool active)
        => SetMode(active ? BeamMode.Multiple : BeamMode.Off);

    // Changes mode; turning off releases all targets, while switching a group to hold releases the group.
    public void SetMode(BeamMode mode)
    {
        if (Mode == mode) return;
        // A tractor beam holds one creature; switching back from a group releases the group.
        if (mode == BeamMode.Off || (mode == BeamMode.Single && _targets.Count > 1)) ReleaseTargets();
        Mode = mode;
        IsActive = mode != BeamMode.Off;
        Visible = IsActive;
        UpdateVisuals();
    }

    // Restores original scales and lands surviving creatures at their release positions.
    private void ReleaseTargets()
    {
        foreach (var entry in _targets)
        {
            Critter critter = entry.Key;
            if (!GodotObject.IsInstanceValid(critter) || critter.IsQueuedForDeletion()) continue;
            critter.GlobalScale = entry.Value.OriginalScale;
            Planet planet = entry.Value.Planet;
            if (GodotObject.IsInstanceValid(planet) && planet.IsInsideTree())
                critter.LandOnPlanet(planet);
        }
        _targets.Clear();
    }

    // Synchronizes detection with the terrain-clipped cone calculated by BeamGeometry.
    private void UpdateGeometry()
    {
        float length = BeamGeometry.LengthAt(PlanetInstance, GlobalPosition, GlobalScale, BeamLength);
        if (_collision != null && (Mathf.Abs(length - CurrentLength) > 0.1f ||
            !Mathf.IsEqualApprox(TopWidth, _lastTopWidth) || !Mathf.IsEqualApprox(BottomWidth, _lastBottomWidth)))
        {
            CurrentLength = length;
            _lastTopWidth = TopWidth;
            _lastBottomWidth = BottomWidth;
            _collision.Polygon = BeamGeometry.ConePoints(TopWidth, BottomWidth, CurrentLength);
        }
    }

    // Supplies dimensions, mode color, and a local halo position without exposing captured creatures.
    private void UpdateVisuals()
    {
        // Presentation is optional; removing it must not interrupt capture mechanics.
        if (!GodotObject.IsInstanceValid(Visuals)) return;
        Visuals.SetAppearance(TopWidth, BottomWidth, CurrentLength, ActiveColor, Mode == BeamMode.Single);
        Visuals.SetHoldHalo(Vector2.Zero, 0f);
        if (Mode != BeamMode.Single) return;
        foreach (Critter critter in _targets.Keys)
            if (GodotObject.IsInstanceValid(critter))
            {
                Visuals.SetHoldHalo(Visuals.ToLocal(critter.GlobalPosition), critter.Profile.SpriteHeight * 0.65f);
                break;
            }
    }

    // Releases targets on focus loss; PlayerController clears input actions separately.
    public override void _Notification(int what)
    {
        if (what == NotificationApplicationFocusOut || what == NotificationWMWindowFocusOut)
        {
            SetActive(false);
        }
    }

    // Releases surviving targets when this beam is removed from the scene.
    public override void _ExitTree()
    {
        ReleaseTargets();
    }
}
