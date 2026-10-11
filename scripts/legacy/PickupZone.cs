using Godot;
namespace Orbitals;

public partial class PickupZone : Area2D
{
	[Signal]
	public delegate void CritterDeliveredEventHandler(Critter critter);

	private void OnBodyEntered(Node2D body)
	{
		GD.Print($"Body entered pickup zone: {body.Name}");
		if (body is Critter critter && critter.WasCarried)
		{
			GD.Print("Critter delivered!");
			EmitSignal(SignalName.CritterDelivered, critter);
			body.QueueFree();
		}
	}
}
