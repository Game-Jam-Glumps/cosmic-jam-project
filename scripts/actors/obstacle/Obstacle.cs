using Godot;
using System;

namespace Orbitals;

public partial class Obstacle : CharacterBody2D
{
    [Export] public Texture2D StaticTexture { get; set; }
    [Export] public float SpriteHeight { get; set; } = 32f;
    [Export] public float BodyOffset { get; private set; } = 16f;
    [Export] public float HomeCenterDegrees { get; set; } = -90f;

    [Export(PropertyHint.Range, "1,180,1")]
    public float HomeHalfWidthDegrees { get; set; } = 60f;

    public Planet World { get; private set; }
    public bool WasCarried { get; private set; }
    public bool IsCarried => WasCarried && !GodotObject.IsInstanceValid(World);
    public float Angle { get; private set; }
    public float SurfaceSpeed { get; private set; }
    public float HomeCenter => Mathf.DegToRad(HomeCenterDegrees);
    public float HomeHalfWidth => Mathf.DegToRad(HomeHalfWidthDegrees);
    public bool HomeCoversWholePlanet => HomeHalfWidthDegrees >= 180f;

    private Sprite2D _sprite;
    private float _bodyWidth = 24f;
    private bool _initialized;

    public float SurfaceOffset
    {
        get
        {
            if (!GodotObject.IsInstanceValid(World))
                return 0f;
            return (BodyOffset * Mathf.Abs(GlobalScale.Y) + 2f) /
                Mathf.Max(0.0001f, Mathf.Abs(World.GlobalScale.X)) +
                World.SupportRadiusAt(Angle, _bodyWidth * Mathf.Abs(GlobalScale.X) / 2f) -
                World.SurfaceRadiusAt(Angle);
        }
    }

    public float TrackRadius => !GodotObject.IsInstanceValid(World)
        ? 0f
        : (World.SurfaceRadiusAt(Angle) + SurfaceOffset) * Mathf.Abs(World.GlobalScale.X);

    public override void _Ready()
    {
        _sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
        if (_sprite == null)
            GD.PushError($"{Name}: missing Sprite2D child.");
        else
        {
            if (StaticTexture != null)
                _sprite.Texture = StaticTexture;
            ConfigureSprite();
        }

        _initialized = true;
        if (GodotObject.IsInstanceValid(World))
            PlaceOnSurface();
    }

    private void ConfigureSprite()
    {
        if (_sprite == null || _sprite.Texture == null)
            return;

        float textureHeight = Mathf.Max(1f, _sprite.Texture.GetHeight());
        float textureWidth = Mathf.Max(1f, _sprite.Texture.GetWidth());
        float height = Mathf.Max(8f, SpriteHeight);
        _sprite.Scale = Vector2.One * height / textureHeight;
        BodyOffset = height / 2f;
        _bodyWidth = height * textureWidth / textureHeight;

        CollisionShape2D collision = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
        if (collision != null)
            collision.Shape = new RectangleShape2D { Size = new Vector2(height * 0.65f, height) };
    }

    public void AttachToPlanet(Planet planet) => SetPlanet(planet, true);
    public void LandOnPlanet(Planet planet) => SetPlanet(planet, false);

    public void DetachFromPlanet()
    {
        World = null;
        StopMoving();
    }

    public void MarkCarried() => WasCarried = true;

    private void SetPlanet(Planet planet, bool clampToHome)
    {
        if (!GodotObject.IsInstanceValid(planet) || planet.Radius <= 0f)
        {
            GD.PushError($"{Name}: invalid planet.");
            return;
        }

        World = planet;
        if (!_initialized)
            return;

        Angle = World.AngleAt(GlobalPosition);
        if (clampToHome && !InHome(Angle))
            Angle = HomeCenter;
        StopMoving();
        PlaceOnSurface();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!GodotObject.IsInstanceValid(World))
            return;

        float dt = (float)delta;
        if (dt <= 0f)
            return;

        Angle = Planet.Wrap(Angle + SurfaceSpeed / Mathf.Max(0.001f, TrackRadius) * dt);
        if (_sprite != null && SurfaceSpeed != 0f)
            _sprite.FlipH = SurfaceSpeed < 0f;
        PlaceOnSurface();
    }

    public void StopMoving() => SurfaceSpeed = 0f;
    public void SetSurfaceSpeed(float speed) => SurfaceSpeed = speed;

    private void PlaceOnSurface()
    {
        if (!GodotObject.IsInstanceValid(World))
            return;
        GlobalPosition = World.SurfacePoint(Angle, SurfaceOffset);
        Vector2 outward = GlobalPosition - World.CenterPosition;
        GlobalRotation = outward.Angle() + Mathf.Pi / 2f;
    }

    public bool InHome(float angle)
        => HomeCoversWholePlanet ||
           Mathf.Abs(Planet.Wrap(angle - HomeCenter)) <= HomeHalfWidth + 0.0001f;

    public float RouteDelta(float targetAngle)
        => HomeCoversWholePlanet || !InHome(Angle)
            ? Planet.Wrap(targetAngle - Angle)
            : Planet.Wrap(targetAngle - HomeCenter) - Planet.Wrap(Angle - HomeCenter);

    public float ArcDistance(float targetAngle) => Mathf.Abs(RouteDelta(targetAngle)) * TrackRadius;

    public bool MoveTowardAngle(float targetAngle, float speed, float delta, float stopDistance)
    {
        float difference = RouteDelta(targetAngle);
        float distance = Mathf.Abs(difference) * TrackRadius;
        if (distance <= Mathf.Max(0.01f, stopDistance))
        {
            StopMoving();
            return true;
        }

        if (delta <= 0f)
        {
            StopMoving();
            return false;
        }

        SurfaceSpeed = Mathf.Sign(difference) * Mathf.Min(Mathf.Max(0f, speed), distance / delta);
        return false;
    }
}
