using Godot;
using Orbitals;

public partial class PickupZone : Area2D
{
	[Signal]
	public delegate void CritterCollectedEventHandler();

	private void OnBodyEntered(Node2D body)
	{
		GD.Print($"Body entered pickup zone: {body.Name}");
		if (body is Critter)
		{
			GD.Print("Critter collected!");
			EmitSignal(SignalName.CritterCollected);
			Globals.Instance.PlayerScore += 50;
			GD.Print($"Player score: {Globals.Instance.PlayerScore}");
		}
	}
}
