using Godot;
namespace Orbitals;

[GlobalClass]
public partial class SpeciesProfile : Resource
{
    [Export] public float WalkSpeed { get; set; } = 35f;
    [Export] public float IdleTimeMin { get; set; } = 1f;
    [Export] public float IdleTimeMax { get; set; } = 3f;
    [Export] public float WanderDistance { get; set; } = 80f;
    [Export] public float ArrivalDistance { get; set; } = 3f;
}
