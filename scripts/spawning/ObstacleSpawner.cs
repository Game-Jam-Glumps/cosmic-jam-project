using Godot;
using System;

namespace Orbitals;

public partial class ObstacleSpawner : Node2D
{
    [Export] public PackedScene ObstacleScene { get; set; }
    [Export] public Planet World { get; set; }

    [Export(PropertyHint.Range, "0,100,1")]
    public int ObstacleCount { get; set; } = 30;

    [Export] public bool SpawnOnReady { get; set; } = true;
    [Export] public bool RespawnMissing { get; set; } = true;
    [Export] public bool RandomizeAngles { get; set; } = true;
    [Export] public float StartAngleDegrees { get; set; } = -180f;
    [Export] public float EndAngleDegrees { get; set; } = 180f;

    private readonly RandomNumberGenerator _random = new();

    public override void _Ready()
    {
        _random.Randomize();
        if (SpawnOnReady)
            CallDeferred(nameof(SpawnObstacles));
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!RespawnMissing || !GodotObject.IsInstanceValid(World))
            return;

        int missing = Mathf.Max(0, ObstacleCount - GetActiveObstacleCount());
        for (int index = 0; index < missing; index++)
            SpawnObstacle();
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

        for (int index = 0; index < ObstacleCount; index++)
            SpawnObstacle();
    }

    public Obstacle SpawnObstacle()
    {
        if (ObstacleScene == null)
            return null;

        Obstacle obstacle = ObstacleScene.Instantiate<Obstacle>();
        if (obstacle == null)
        {
            GD.PushError($"{Name}: ObstacleScene is not an Obstacle.");
            return null;
        }

        GetParent().AddChild(obstacle);
        obstacle.AddToGroup("obstacles");
        obstacle.ZIndex = 0;

        float angle = GetSpawnAngle();
        obstacle.GlobalPosition = World.SurfacePoint(angle, 0f);
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
        return Mathf.DegToRad(_random.RandfRange(start, end));
    }

    public void ClearObstacles()
    {
        foreach (Node child in GetParent().GetChildren())
            if (child is Obstacle obstacle)
                obstacle.QueueFree();
    }

    private int GetActiveObstacleCount()
    {
        int count = 0;
        foreach (Node node in GetTree().GetNodesInGroup("obstacles"))
            if (GodotObject.IsInstanceValid(node) && !node.IsQueuedForDeletion())
                count++;
        return count;
    }
}
