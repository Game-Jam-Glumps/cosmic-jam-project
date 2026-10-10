using Godot;
using System;

namespace Orbitals;

[GlobalClass]
public partial class Globals : Node
{
	public static Globals Instance {get; private set;}
	[Export]
	public Boolean IsBeamActive {get; set;} = false;

	public int PlayerScore {get; set;} = 0;
	
	public override void _Ready()
	{
		Instance = this;
		GD.Print("Globals ready");
	}

}
