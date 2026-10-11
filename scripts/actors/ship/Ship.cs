using Godot;

namespace Orbitals;


public partial class Ship : CharacterBody2D
{

    [Export] public int speed { get; set; } = 200;
    [Export] public float Acceleration { get; set; } = 800f;
    [Export] public float Deceleration { get; set; } = 1100f;
    [Export] public Planet PlanetInstance { get; set; }
    [Export(PropertyHint.Range, "32,512,1,or_greater,suffix:px")]
    public float SpriteWidth { get; set; } = 128f;
    [Export(PropertyHint.Range, "0,3000,10,or_greater,suffix:px")]
    public float MinimumAltitude { get; set; } = 120f;
    [Export(PropertyHint.Range, "0,3000,10,or_greater,suffix:px")]
    public float MaximumAltitude { get; set; } = 1400f;
    [Export] public AnimatedSprite2D Sprite { get; set; }
    [Export] public CollisionShape2D HullCollision { get; set; }
    [Export] public Node2D BeamMount { get; set; }

    private float _verticalInput;

    // Orders ship physics before the beam and applies the configured appearance.
    public override void _Ready()
    {
        ProcessPhysicsPriority = -10;
        ConfigureAppearance();
    }

    // Accepts a movement command from any controller: -1 up, 0 hover, +1 down.
    public void SetVerticalInput(float direction)
        => _verticalInput = Mathf.Clamp(direction, -1f, 1f);

    // Moves the ship, enforces terrain clearance, and updates its hover animation.
    public override void _PhysicsProcess(double delta)
    {
        UpdateVerticalMovement((float)delta);
        ClampAltitude();
        UpdateAnimation();
    }

    // Sizes the sprite, hull, and attachment point from the readable SpriteWidth setting.
    private void ConfigureAppearance()
    {
        float width = Mathf.Max(32f, SpriteWidth);
        if (GodotObject.IsInstanceValid(Sprite))
        {
            Sprite.Play("hover");
            Sprite.Scale = Vector2.One * width / Sprite.SpriteFrames.GetFrameTexture("hover", 0).GetWidth();
        }
        if (GodotObject.IsInstanceValid(HullCollision))
            HullCollision.Shape = new CircleShape2D { Radius = width * 0.375f };
        if (GodotObject.IsInstanceValid(BeamMount))
            BeamMount.Position = new Vector2(0f, width * 0.375f);
    }

    // Accelerates toward commanded speed or brakes toward a stationary hover.
    private void UpdateVerticalMovement(float dt)
    {
        float target = _verticalInput * Mathf.Max(0, speed);
        float rate = _verticalInput == 0f ? Deceleration : Acceleration;
        Velocity = new Vector2(0f, Mathf.MoveToward(Velocity.Y, target, Mathf.Max(0f, rate) * dt));
        GlobalPosition += Velocity * dt;
    }

    // Clamps altitude above the visible surface and cancels velocity at either limit.
    private void ClampAltitude()
    {
        if (!GodotObject.IsInstanceValid(PlanetInstance)) return;
        Vector2 center = PlanetInstance.CenterPosition;
        float radius = PlanetInstance.Radius * Mathf.Abs(PlanetInstance.GlobalScale.X);
        if (Mathf.Abs(GlobalPosition.X - center.X) >= radius) return;
        float groundY = PlanetInstance.SurfaceYAt(GlobalPosition.X);
        float minimum = Mathf.Max(SpriteWidth * 0.6f, MinimumAltitude);
        float maximum = Mathf.Max(minimum, MaximumAltitude);
        float y = Mathf.Clamp(GlobalPosition.Y, groundY - maximum, groundY - minimum);
        if (!Mathf.IsEqualApprox(y, GlobalPosition.Y))
        {
            GlobalPosition = new Vector2(GlobalPosition.X, y);
            Velocity = Vector2.Zero;
        }
    }

    // Plays the existing hover frames faster in proportion to actual vertical speed.
    private void UpdateAnimation()
    {
        if (GodotObject.IsInstanceValid(Sprite))
            Sprite.SpeedScale = Mathf.Lerp(1f, 1.5f,
                Mathf.Clamp(Mathf.Abs(Velocity.Y) / Mathf.Max(1, speed), 0f, 1f));
    }
}
