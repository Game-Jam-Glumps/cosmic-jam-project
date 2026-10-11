using Godot;

namespace Orbitals;

// Shared cone calculations used by beam collision and presentation.
internal static class BeamGeometry
{
    // Clips reach at the terrain and converts the distance into beam-local units.
    public static float LengthAt(Planet planet, Vector2 position, Vector2 scale, float maximum)
    {
        float length = Mathf.Max(1f, maximum);
        if (!GodotObject.IsInstanceValid(planet)) return length;

        float radius = planet.Radius * Mathf.Abs(planet.GlobalScale.X);
        if (Mathf.Abs(position.X - planet.CenterPosition.X) < radius)
            length = Mathf.Min(length, Mathf.Max(1f,
                (planet.SurfaceYAt(position.X) - position.Y) / Mathf.Max(0.0001f, Mathf.Abs(scale.Y))));
        return length;
    }

    // Returns matching trapezoid corners for the visible cone and detection polygon.
    public static Vector2[] ConePoints(float topWidth, float bottomWidth, float length) => new[]
    {
        new Vector2(-Mathf.Max(1f, topWidth) / 2f, 0f),
        new Vector2(Mathf.Max(1f, topWidth) / 2f, 0f),
        new Vector2(Mathf.Max(1f, bottomWidth) / 2f, length),
        new Vector2(-Mathf.Max(1f, bottomWidth) / 2f, length)
    };
}
