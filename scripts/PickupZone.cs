using Godot;
public partial class PickupZone : Area2D
{
	[Signal]
	public delegate void CritterCollectedEventHandler();

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
	}

	private void OnBodyEntered(Node2D body)
	{
		GD.Print($"Body entered pickup zone: {body.Name}");
		if (body is Critter)
		{
			GD.Print("Critter collected!");
			EmitSignal(SignalName.CritterCollected);
		}
	}
}
