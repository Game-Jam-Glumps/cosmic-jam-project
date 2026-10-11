// Legacy prototype: preserved for reference; not used by the main game scene.
using Godot;
using System;

namespace Orbitals;
public partial class ShipInitalizer : Node2D
{
	[Export]
	public Planet PlanetInstance {get; set;}

	[Export]
	public Critter CritterInstance {get; set;}
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		if(GodotObject.IsInstanceValid(PlanetInstance))
		{
			CritterInstance.AttachToPlanet(PlanetInstance);
		}
		else
		{
			GD.Print("Planet instance is not valid.");
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
