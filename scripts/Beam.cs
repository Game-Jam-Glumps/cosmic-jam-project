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

	public override void _Process(double delta)
	{

		if (Input.IsActionPressed("Right"))
		{
			var x = this.Position.X +1; 
			var y = this.Position.Y;
			this.Position = new Vector2(x,y);
		}
	}

	// private void OnAreaShapeEntered(Rid area_rid, Area2D area, int area_shape_index, int local_shape_index)
	// {
	// 	GD.Print("collision detected 1");
	// 	// GD.Print(nodeBody.Name);
	// }
	// private void OnBodyEntered(Node2D nodeBody)
	// {
	// 	GD.Print("collision detected 2");
	// }

	// this one works
	private void _on_body_entered(Node2D nodeBody)
	{
		GD.Print("collision detected 3");
	}
	// private void _on_area_shape_entered(Rid area_rid, Area2D area, int area_shape_index, int local_shape_index)
	// {
	// 	GD.Print("collision detected 4");
	// 	// GD.Print(nodeBody.Name);
	// }
}
