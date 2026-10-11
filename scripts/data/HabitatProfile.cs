using Godot;

namespace Orbitals;

[GlobalClass]
public partial class HabitatProfile : Resource
{
    [Export] public string HabitatName { get; set; } = "Habitat";
    [Export(PropertyHint.Range, "-180,180,0.5")]
    public float CenterDegrees { get; set; } = -90f;
    [Export(PropertyHint.Range, "1,360,0.5")]
    public float SurfaceDegrees { get; set; } = 120f;

    public float Center => Mathf.DegToRad(CenterDegrees);
    public float HalfWidth => Mathf.DegToRad(Mathf.Clamp(SurfaceDegrees, 1f, 360f)) / 2f;
    public bool CoversWholePlanet => SurfaceDegrees >= 360f;

    public float SurfaceLength(float radius) => 2f * HalfWidth * Mathf.Max(0f, radius);

    public bool Contains(float angle)
        => CoversWholePlanet || Mathf.Abs(Planet.Wrap(angle - Center)) <= HalfWidth + 0.0001f;

    public float ClampAngle(float angle)
        => CoversWholePlanet ? Planet.Wrap(angle)
            : Planet.Wrap(Center + Mathf.Clamp(Planet.Wrap(angle - Center), -HalfWidth, HalfWidth));
}
