using Godot;
using System;

public partial class Planet : Node2D
{
	[Export] public float RotationSpeed = 1.5f;

	public void RotatePlanet(float direction, double delta)
	{
		Rotation -= direction * RotationSpeed * (float)delta;
	}
}
