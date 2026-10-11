using Godot;
using System;

namespace Orbitals;
public partial class Beam : Area2D
{
	[Signal]
	public delegate void HitEventHandler();

	private Node2D _activeTarget = null;
	private Planet _activePlanet = null;
	[Export] public float PullSpeed { get; set; } = 200f;

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
				ReleaseActiveTarget();
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

	public override void _PhysicsProcess(double delta)
	{
        if (!Globals.Instance.IsBeamActive ||
        	_activeTarget == null ||
        	!GodotObject.IsInstanceValid(_activeTarget))
        {
        	return;
        }

        float distance = PullSpeed * (float)delta;
        _activeTarget.GlobalPosition =
        	_activeTarget.GlobalPosition.MoveToward(GlobalPosition, distance);
	}

	private void OnBodyEntered(Node2D nodeBody)
	{
        GD.Print("collision detected 2");

        if (!Globals.Instance.IsBeamActive || _activeTarget != null)
        {
        	return;
        }

        _activeTarget = nodeBody;
        if(nodeBody is Critter critter)
        {
        	GD.Print($"Critter pos: {nodeBody.GlobalPosition}");
        	GD.Print($"Ship pos: {GlobalPosition}");
        	_activePlanet = critter.World;
        	critter.DetachFromPlanet();
        }
	}

	private void OnBodyExited(Node2D nodeBody)
	{
        if (nodeBody == _activeTarget)
        {
        	ReleaseActiveTarget();
        }
	}

	private void ReleaseActiveTarget()
	{
        if (_activeTarget is Critter critter &&
        	GodotObject.IsInstanceValid(_activePlanet))
        {
        	critter.AttachToPlanet(_activePlanet);
        }

        _activeTarget = null;
        _activePlanet = null;
	}
}
