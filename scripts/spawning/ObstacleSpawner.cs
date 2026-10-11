using Godot;
using System;

namespace Orbitals;

public partial class ObstacleSpawner : Node2D
{
    [Export] public PackedScene ObstacleScene { get; set; }
    [Export] public Planet World { get; set; }

    [Export(PropertyHint.Range, "0,100,1")]
    public int ObstacleCount { get; set; } = 144;

    [Export] public bool SpawnOnReady { get; set; } = true;
    [Export] public bool RandomizeAngles { get; set; } = true;

    [Export]
    public float StartAngleDegrees { get; set; } = -180f;

    [Export]
    public float EndAngleDegrees { get; set; } = 180f;

    private readonly RandomNumberGenerator _random =
        new RandomNumberGenerator();

    public override void _Ready()
    {
        _random.Randomize();

        if (SpawnOnReady)
            CallDeferred(nameof(SpawnObstacles));
    }

    public void SpawnObstacles()
    {
        if (!GodotObject.IsInstanceValid(World))
        {
            GD.PushError($"{Name}: World planet is not assigned.");
            return;
        }

        if (ObstacleScene == null)
        {
            GD.PushError($"{Name}: ObstacleScene is not assigned.");
            return;
        }

        for (int i = 0; i < ObstacleCount; i++)
            SpawnObstacle();
    }

    public Obstacle SpawnObstacle()
    {
        Obstacle obstacle = ObstacleScene.Instantiate<Obstacle>();

        if (obstacle == null)
        {
            GD.PushError($"{Name}: ObstacleScene is not an Obstacle.");
            return null;
        }

        Node parent = GetParent();
        parent.AddChild(obstacle);

        float angle = GetSpawnAngle();

        // Place it near the selected point before attaching it.
        obstacle.GlobalPosition = World.SurfacePoint(angle, 0f);

        // This initializes orbiting and places the obstacle exactly on the surface.
        obstacle.AttachToPlanet(World);

        obstacle.Scale *= 5f;

        return obstacle;
    }

    private float GetSpawnAngle()
    {
        if (!RandomizeAngles)
            return Mathf.DegToRad(StartAngleDegrees);

        float start = Mathf.Min(StartAngleDegrees, EndAngleDegrees);
        float end = Mathf.Max(StartAngleDegrees, EndAngleDegrees);

        return Mathf.DegToRad(
            _random.RandfRange(start, end)
        );
    }

    public void ClearObstacles()
    {
        foreach (Node child in GetParent().GetChildren())
        {
            if (child is Obstacle obstacle)
                obstacle.QueueFree();
        }
    }
}