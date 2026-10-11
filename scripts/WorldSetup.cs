using Godot;

namespace Orbitals;
public partial class WorldSetup : Node2D
{
	[Export]
	public Planet PlanetInstance { get; set; }

	[Export]
	public Godot.Collections.Array<Critter> Critters { get; set; }
		= new();

	[Export]
	public Camera2D SceneCamera { get; set; }

	public override void _Ready()
	{
		if (GodotObject.IsInstanceValid(SceneCamera))
			SceneCamera.MakeCurrent();

		if (!GodotObject.IsInstanceValid(PlanetInstance))
		{
			GD.PushError(
				"WorldSetup: assign the inner Planet node " +
                "with Planet.cs."
			);

			return;
		}

		foreach (Critter critter in Critters)
		{
			if (GodotObject.IsInstanceValid(critter))
				critter.AttachToPlanet(PlanetInstance);
		}

	}
}
