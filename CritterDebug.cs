using Godot;

public partial class CritterDebug : Node
{
    [Export]
    public Critter Creature { get; set; }

    private float _timer;

    public override void _Ready()
    {
        ProcessPhysicsPriority = 10;

        GD.Print("CritterDebug started");
    }

    public override void _PhysicsProcess(double delta)
    {
        _timer += (float)delta;

        if (_timer < 1f)
            return;

        _timer = 0f;

        if (!GodotObject.IsInstanceValid(Creature))
        {
            GD.Print("DEBUG: No creature assigned.");
            return;
        }

        CritterBrain brain =
            Creature.GetNodeOrNull<CritterBrain>("Brain");

        string planetName =
            GodotObject.IsInstanceValid(Creature.World)
                ? Creature.World.GetPath().ToString()
                : "NONE";

        string state =
            brain != null
                ? brain.State.ToString()
                : "BRAIN MISSING";

        GD.Print(
            $"Planet: {planetName} | " +
            $"State: {state} | " +
            $"Angle: {Mathf.RadToDeg(Creature.Angle):F1} | " +
            $"Speed: {Creature.SurfaceSpeed:F1} | " +
            $"WalkSpeed: {Creature.Profile?.WalkSpeed} | " +
            $"Position: {Creature.GlobalPosition} | " +
            $"Physics: {Creature.IsPhysicsProcessing()}"
        );
    }
}