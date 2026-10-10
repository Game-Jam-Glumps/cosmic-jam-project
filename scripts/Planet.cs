using Godot;
using System;

public partial class Planet : Node2D
{
	[Export] public float RotationSpeed = 1.5f;
	
	public override void _PhysicsProcess(double delta)
	{
		float direction = Input.GetAxis("Left", "Right");

		Rotation -= direction * RotationSpeed * (float)delta;
	}
}

	
	
