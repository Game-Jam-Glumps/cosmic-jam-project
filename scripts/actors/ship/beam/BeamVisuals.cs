using Godot;

namespace Orbitals;

// Draws beam effects from supplied values; has no creature, planet, input, or session references.
public partial class BeamVisuals : Node2D
{
    private float _topWidth, _bottomWidth, _length;
    private Color _color;
    private bool _holding;
    private Vector2 _haloCenter;
    private float _haloRadius;
    private float _time, _collectionFlash;

    // Receives current cone dimensions, color, and mode from the capture component.
    public void SetAppearance(float topWidth, float bottomWidth, float length, Color color, bool holding)
    {
        _topWidth = topWidth;
        _bottomWidth = bottomWidth;
        _length = length;
        _color = color;
        _holding = holding;
        QueueRedraw();
    }

    // Places the held-creature halo in local coordinates; a zero radius removes it.
    public void SetHoldHalo(Vector2 center, float radius)
    {
        _haloCenter = center;
        _haloRadius = radius;
        QueueRedraw();
    }

    // Advances pulse/streak time and fades the collection flash while the beam is active.
    public void AdvanceEffects(float delta)
    {
        _time += delta;
        _collectionFlash = Mathf.MoveToward(_collectionFlash, 0f, delta * 4f);
        QueueRedraw();
    }

    // Starts an intake flash after a completed capture, without awarding a rescue.
    public void PlayCollectionFlash() => _collectionFlash = 1f;

    // Composes the cone, streaks, hold halo, and intake glow from presentation state.
    public override void _Draw()
    {
        if (_length <= 0f) return;
        float pulse = 0.9f + Mathf.Sin(_time * 7f) * 0.1f;
        DrawCone(pulse);
        DrawStreaks(pulse);
        if (_holding && _haloRadius > 0f)
            DrawArc(_haloCenter, _haloRadius, 0f, Mathf.Tau, 32,
                new Color(_color, 0.85f * pulse), 2f, true);
        DrawCircle(Vector2.Zero, 5f + _collectionFlash * 10f,
            new Color(_color, 0.6f + _collectionFlash * 0.3f));
    }

    // Draws the translucent cone and both illuminated edges using the shared collision shape.
    private void DrawCone(float pulse)
    {
        DrawPolygon(BeamGeometry.ConePoints(_topWidth, _bottomWidth, _length), new[]
        {
            new Color(_color, 0.34f * pulse), new Color(_color, 0.34f * pulse),
            new Color(_color, 0.12f * pulse), new Color(_color, 0.12f * pulse)
        });
        DrawLine(new Vector2(-_topWidth / 2f, 0f), new Vector2(-_bottomWidth / 2f, _length),
            new Color(_color, 0.65f * pulse), 2f, true);
        DrawLine(new Vector2(_topWidth / 2f, 0f), new Vector2(_bottomWidth / 2f, _length),
            new Color(_color, 0.65f * pulse), 2f, true);
    }

    // Draws hovering streaks for Hold and upward-moving streaks for Absorb.
    private void DrawStreaks(float pulse)
    {
        for (int index = 0; index < 12; index++)
        {
            float y = _holding
                ? (index + 0.5f) * _length / 12f + Mathf.Sin(_time * 3f + index) * 3f
                : Mathf.PosMod(index * _length / 12f - _time * 150f, _length);
            float width = Mathf.Lerp(_topWidth, _bottomWidth, y / _length);
            float x = Mathf.Sin(index * 7.2f) * width * 0.3f;
            DrawLine(new Vector2(x, y), new Vector2(x, Mathf.Max(0f, y - 10f)),
                new Color(_color, 0.6f * pulse), 2f, true);
        }
    }
}
