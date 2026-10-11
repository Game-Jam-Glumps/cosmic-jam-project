using Godot;
using System;

public partial class ShowPlanet : Node2D
{
	public override void _Ready()
	{
		Hide();
	}

	public void OnPlanetTimeout()
	{
		Show();
	}
}
