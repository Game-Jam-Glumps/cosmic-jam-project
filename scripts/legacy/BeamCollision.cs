// Legacy prototype: preserved for reference; not used by the main game scene.
using Godot;
using Orbitals;
using System;

namespace Orbitals;

public partial class BeamCollision : CollisionShape2D
{
    // public override void _Process(double delta)
    // {
    //     beamCollide(Globals.Instance.IsBeamActive);
    // }

    // public void beamCollide(Boolean beamStatus)
    // {
    //     if(beamStatus == true)
    //     {
    //         GD.Print($"Collision is active {this.Disabled}");  
    //     }
    //     else
    //     {
    //         GD.Print($"NOO Collision {this.Disabled}");
    //     }
    // }

    public override void _Ready()
    {
        this.SetDeferred("disabled", true);
    }


}
