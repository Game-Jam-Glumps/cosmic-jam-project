using Godot;
using System;

public partial class Critter : CharacterBody2D
{

	[Export]
	public Node2D Center;


	[Export]
	public float OrbitalSpeed = 1.0f;

	private Vector2 orbit;

	public override void _Ready()
	{
		orbit = GlobalPosition - Center.GlobalPosition;

	}

	public override void _PhysicsProcess(double delta)
	{
		orbit = orbit.Rotated(OrbitalSpeed * (float)delta);

		GlobalPosition = Center.GlobalPosition + orbit;

	}
}
