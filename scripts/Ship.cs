using Godot;
using System;

public partial class Ship : CharacterBody2D
{
	[Export] public Planet Planet { get; set; }

	public override void _PhysicsProcess(double delta)
	{
		float direction = Input.GetAxis("Left", "Right");

		Planet.RotatePlanet(direction, delta);
	}
}
