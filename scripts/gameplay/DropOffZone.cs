using Godot;

namespace Orbitals;


public partial class DropOffZone : Area2D
{
    [Signal] public delegate void CritterDeliveredEventHandler(Critter critter);
    [Signal] public delegate void ObstacleDeliveredEventHandler(Obstacle obstacle);
    [Export] public Planet PlanetInstance { get; set; }
    [Export] public Sprite2D Sprite { get; set; }
    [Export] public CollisionShape2D IntakeCollision { get; set; }
    [Export] public float SurfaceAngleDegrees { get; set; } = -90f;
    [Export] public float Altitude { get; set; } = 480f;
    [Export] public float SpriteWidth { get; set; } = 480f;
    [Export] public Vector2 IntakeSize { get; set; } = new(280f, 220f);
    [Export] public float IntakeOffset { get; set; } = 140f;

    
    public override void _Ready()
    {
        ProcessPhysicsPriority = -15;
        CollisionLayer = 0;
        CollisionMask = 2;
        float width = Mathf.Max(128f, SpriteWidth);
        if (GodotObject.IsInstanceValid(Sprite) && Sprite.Texture != null)
            Sprite.Scale = Vector2.One * width / Sprite.Texture.GetWidth();
        if (GodotObject.IsInstanceValid(IntakeCollision))
        {
            IntakeCollision.Shape = new RectangleShape2D
            {
                Size = new Vector2(Mathf.Max(1f, IntakeSize.X), Mathf.Max(1f, IntakeSize.Y))
            };
            IntakeCollision.Position = new Vector2(0f, IntakeOffset);
        }
        UpdatePlacement();
    }

    
    public override void _PhysicsProcess(double delta)
    {
        UpdatePlacement();
        foreach (Node2D body in GetOverlappingBodies())
        {
            if (body is Critter critter)
                TryDeliver(critter);
            else if (body is Obstacle obstacle)
                TryDeliver(obstacle);
        }
    }

    
    private void UpdatePlacement()
    {
        if (!GodotObject.IsInstanceValid(PlanetInstance)) return;
        float angle = Mathf.DegToRad(SurfaceAngleDegrees);
        float planetScale = Mathf.Max(0.0001f, Mathf.Abs(PlanetInstance.GlobalScale.X));
        GlobalPosition = PlanetInstance.SurfacePoint(angle, Mathf.Max(0f, Altitude) / planetScale);
        GlobalRotation = PlanetInstance.GlobalRotation + angle + Mathf.Pi / 2f;
        GlobalScale = Vector2.One;
    }

    
    private void TryDeliver(Critter critter)
    {
        if (!GodotObject.IsInstanceValid(critter) || critter.IsQueuedForDeletion() || !critter.WasCarried)
            return;
        EmitSignal(SignalName.CritterDelivered, critter);
        critter.QueueFree();
    }

    private void TryDeliver(Obstacle obstacle)
    {
        if (!GodotObject.IsInstanceValid(obstacle) || obstacle.IsQueuedForDeletion() || !obstacle.IsCarried)
            return;
        EmitSignal(SignalName.ObstacleDelivered, obstacle);
        obstacle.QueueFree();
    }
}
