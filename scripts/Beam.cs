using Godot;
using System;

namespace Orbitals;
public partial class Beam : Area2D
{
	[Signal]
	public delegate void HitEventHandler();
	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseButton eventMouseButton)
		{
			if (eventMouseButton.Pressed)
			{
				Show();
				Globals.Instance.IsBeamActive = true;
				GD.Print($"beam is {Globals.Instance.IsBeamActive}");
				GD.Print("mouse pressed");
				GetNode("BeamCollisionBox").SetDeferred("disabled", false);
			}else {
				Hide();
				Globals.Instance.IsBeamActive = false;
				GD.Print($"beam is {Globals.Instance.IsBeamActive}");
				GD.Print("mouse released");
				GetNode("BeamCollisionBox").SetDeferred("disabled", true);
			}
		}
	}
	public void triggerBeam()
	{
		GD.Print("meep");
	}
    public override void _Ready()
    {
        base._Ready();
		Hide();
    }
	private void OnBodyEntered(Node2D nodeBody)
	{
		GD.Print("collision detected 2");
		if(nodeBody is Critter critter)
		{
			GD.Print($"Critter pos: {nodeBody.GlobalPosition}");
			GD.Print($"Ship pos: {GlobalPosition}");
			critter.DetachFromPlanet();
			critter.GlobalPosition = GlobalPosition;
			GD.Print($"Critter pos after: {critter.GlobalPosition}");
		}
	}


}
