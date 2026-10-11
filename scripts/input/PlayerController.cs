using Godot;

namespace Orbitals;


public partial class PlayerController : Node
{
    [Export] public Ship ShipActor { get; set; }
    [Export] public Beam BeamActor { get; set; }
    [Export] public Planet PlanetActor { get; set; }

    // Runs before world and actor physics so commands apply on the same frame.
    public override void _Ready()
        => ProcessPhysicsPriority = -30;

    // Reads movement and beam actions; absorption takes priority over holding.
    public override void _PhysicsProcess(double delta)
    {
        if (GodotObject.IsInstanceValid(ShipActor))
            ShipActor.SetVerticalInput(Input.GetAxis("Up", "Down"));
        if (GodotObject.IsInstanceValid(PlanetActor))
            PlanetActor.SetOrbitInput(Input.GetAxis("Left", "Right"));
        if (GodotObject.IsInstanceValid(BeamActor))
            BeamActor.SetMode(Input.IsActionPressed("Abduct") ? Beam.BeamMode.Multiple :
                Input.IsActionPressed("HoldBeam") ? Beam.BeamMode.Single : Beam.BeamMode.Off);
    }

    // Clears held controls and actor commands when the application loses focus.
    public override void _Notification(int what)
    {
        if (what != NotificationApplicationFocusOut && what != NotificationWMWindowFocusOut) return;
        ReleaseControls();
    }

    // Enables round controls or releases them immediately when the round ends.
    public void SetEnabled(bool enabled)
    {
        if (!enabled) ReleaseControls();
        SetPhysicsProcess(enabled);
    }

    // Clears action state so a menu click or held key cannot carry into another round.
    public void ReleaseControls()
    {
        foreach (string action in new[] { "Abduct", "HoldBeam", "Up", "Down", "Left", "Right" })
            Input.ActionRelease(action);
        ClearCommands();
    }

    // Stops commands when the controller is removed from the scene.
    public override void _ExitTree()
        => ClearCommands();

    // Leaves the ship hovering, stops orbit inpt, and releases captured creatures.
    private void ClearCommands()
    {
        if (GodotObject.IsInstanceValid(ShipActor)) ShipActor.SetVerticalInput(0f);
        if (GodotObject.IsInstanceValid(PlanetActor)) PlanetActor.SetOrbitInput(0f);
        if (GodotObject.IsInstanceValid(BeamActor)) BeamActor.SetMode(Beam.BeamMode.Off);
    }
}
