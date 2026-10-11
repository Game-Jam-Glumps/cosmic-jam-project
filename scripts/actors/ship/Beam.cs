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

    private readonly Dictionary<Node2D, Capture> _targets = new();
    private CollisionPolygon2D _collision;
    private float _lastTopWidth, _lastBottomWidth;

    // Initializes target detection and the cone shape, and starts hidden.
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
        // Polling includes targets already inside when the beam is switched on.
        var candidates = new List<Node2D>();
        foreach (Node2D body in GetOverlappingBodies())
        {
            if (body is not Critter && body is not Obstacle ||
                body.IsQueuedForDeletion() || _targets.ContainsKey(body)) continue;

            Planet world = body is Critter critter
                ? critter.World
                : ((Obstacle)body).World;
            if (!GodotObject.IsInstanceValid(world)) continue;

            candidates.Add(body);
        }
        candidates.Sort((a, b) => a.GlobalPosition.DistanceSquaredTo(GlobalPosition)
            .CompareTo(b.GlobalPosition.DistanceSquaredTo(GlobalPosition)));
        foreach (Node2D target in candidates)
        {
            if (Mode == BeamMode.Single && _targets.Count >= 1) break;

            Planet world = target is Critter critter
                ? critter.World
                : ((Obstacle)target).World;
            _targets.Add(target, new Capture
            {
                Planet = world,
                OriginalScale = target.GlobalScale,
                InitialDistance = Mathf.Max(1f, target.GlobalPosition.DistanceTo(GlobalPosition))
            });
            DetachTarget(target);
            EmitSignal(SignalName.Hit);
        }
    }

    // Updates latched creatures and discards targets removed by another system.
    private void UpdateTargets(float dt)
    {
        // Captured targets stay latched during ship movement until collected or released.
        foreach (var entry in new List<KeyValuePair<Node2D, Capture>>(_targets))
        {
            Node2D target = entry.Key;
            Capture capture = entry.Value;

            // A mothership can remove a carried target between beam physics ticks.
            if (!GodotObject.IsInstanceValid(target) || target.IsQueuedForDeletion())
            {
                _targets.Remove(target);
                continue;
            }

            capture.Speed = Mathf.MoveToward(capture.Speed, Mathf.Max(1f, PullSpeed),
                Mathf.Max(0f, PullAcceleration) * dt);
            UpdateHeldTarget(target, capture, dt);
        }
    }


    // Moves one target toward the safe hover point without shrinking or consuming it.
    private void UpdateHeldTarget(Node2D target, Capture capture, float dt)
    {
        float targetHeight = target is Critter critter
            ? critter.Profile.SpriteHeight
            : ((Obstacle)target).SpriteHeight;
        float halfHeight = targetHeight * Mathf.Abs(capture.OriginalScale.Y) / 2f;
        float hoverY = Mathf.Min(Mathf.Max(24f, HoldDistance),
            Mathf.Max(24f, CurrentLength - halfHeight - 6f));
        Vector2 hoverPoint = ToGlobal(new Vector2(0f, hoverY));
        target.GlobalScale = capture.OriginalScale;
        target.GlobalPosition = target.GlobalPosition.MoveToward(hoverPoint, capture.Speed * dt);
        target.GlobalRotation = Mathf.LerpAngle(target.GlobalRotation, 0f, Mathf.Min(1f, dt * 8f));
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
            Node2D target = entry.Key;
            if (!GodotObject.IsInstanceValid(target) || target.IsQueuedForDeletion()) continue;
            target.GlobalScale = entry.Value.OriginalScale;
            Planet planet = entry.Value.Planet;
            if (GodotObject.IsInstanceValid(planet) && planet.IsInsideTree())
                LandTarget(target, planet);
        }
        _targets.Clear();
    }

    private static void DetachTarget(Node2D target)
    {
        if (target is Critter critter)
            critter.DetachFromPlanet();
        else if (target is Obstacle obstacle)
            obstacle.DetachFromPlanet();
    }

    private static void LandTarget(Node2D target, Planet planet)
    {
        if (target is Critter critter)
            critter.LandOnPlanet(planet);
        else if (target is Obstacle obstacle)
            obstacle.LandOnPlanet(planet);
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
        foreach (Node2D target in _targets.Keys)
            if (GodotObject.IsInstanceValid(target))
            {
                float targetHeight = target is Critter critter
                    ? critter.Profile.SpriteHeight
                    : ((Obstacle)target).SpriteHeight;
                Visuals.SetHoldHalo(Visuals.ToLocal(target.GlobalPosition), targetHeight * 0.65f);
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
