using Godot;

namespace Orbitals;


[GlobalClass]
public partial class RunResult : Resource
{
    [Export] public int RescuedCount { get; set; }
    [Export] public int Score { get; set; }
    [Export(PropertyHint.Range, "0,5,1")] public int Stars { get; set; }
}
