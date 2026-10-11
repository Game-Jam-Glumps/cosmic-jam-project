using Godot;

namespace Orbitals;

[GlobalClass]
public partial class CritterSpawnProfile : Resource
{
    [Export] public HabitatProfile Habitat { get; set; }
    [Export] public SpeciesProfile Species { get; set; }
    [Export(PropertyHint.Range, "0,1000,1")]
    public int Count { get; set; } = 3;
}
