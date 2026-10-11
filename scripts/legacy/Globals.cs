// Legacy prototype: preserved for reference; not used by the main game scene.
using Godot;
using System;

namespace Orbitals;

[GlobalClass]
public partial class Globals : Node
{
	public static Globals Instance {get; private set;}
	[Export]
	public Boolean IsBeamActive {get; set;} = false;

	
	public override void _Ready()
	{
		Instance = this;
		GD.Print("Globals ready");
	}

	public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _ExitTree()
    {
        if (Instance == this)
            Instance = null;
    }

}
