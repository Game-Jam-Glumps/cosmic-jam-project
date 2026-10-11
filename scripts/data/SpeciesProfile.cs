using Godot;
namespace Orbitals;

[GlobalClass]
public partial class SpeciesProfile : Resource
{
    [Export] public SpriteFrames Animations { get; set; }
    [Export(PropertyHint.Range, "8,256,1,or_greater,suffix:px")]
    public float SpriteHeight { get; set; } = 48f;
    [Export(PropertyHint.Range, "0,2000,10,or_greater,suffix:px")]
    public float FlightHeight { get; set; }
    [Export] public bool FacesRight { get; set; } = true;
    [Export] public float WalkSpeed { get; set; } = 35f;
    [Export] public float IdleTimeMin { get; set; } = 1f;
    [Export] public float IdleTimeMax { get; set; } = 3f;
    [Export] public float WanderDistance { get; set; } = 80f;
    [Export] public float ArrivalDistance { get; set; } = 3f;
<<<<<<< HEAD

    [Export] public float Rarity = 1;
=======
    [Export] public string Rarity { get; set; } = "Normal";
>>>>>>> 2007f16 (Add random chance for Shiny critter)
}
