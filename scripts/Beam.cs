using Godot;
using System;

namespace Orbitals;
public partial class Beam : StaticBody2D
{
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
			}else {
				Hide();
				Globals.Instance.IsBeamActive = false;
				GD.Print($"beam is {Globals.Instance.IsBeamActive}");
				GD.Print("mouse released");
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

}
