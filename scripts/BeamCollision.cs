using Godot;
using Orbitals;
using System;

public partial class BeamCollision : CollisionShape2D
{
    public override void _Process(double delta)
    {
        beamCollide(Globals.Instance.IsBeamActive);
    }

    public void beamCollide(Boolean beamStatus)
    {
        if(beamStatus == true)
        {
            GD.Print("Collision is active");
            CollisionShape2D.
        }
        else
        {
            GD.Print("NOO Collision");
        }
    }

}
