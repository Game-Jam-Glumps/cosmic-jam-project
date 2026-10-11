using Godot;
using System.Collections.Generic;
namespace Orbitals;

public partial class Planet : Node2D
{
	[Export] public Node2D Center { get; set; }

	[Export] public float Radius { get; set; } = 200f;
	// Zero preserves the authored transform; a positive value sizes the whole planet in world pixels.
	[Export(PropertyHint.Range, "0,20000,100,or_greater,suffix:px")]
	public float SurfaceDiameter { get; set; }
	[Export] public float RotationSpeed { get; set; } = 0.7f;
	[Export] public Godot.Collections.Array<HabitatProfile> Habitats { get; set; } = new();
	[Export] public Node2D SurfaceArt { get; set; }
	private const int SurfaceSamples = 7200;
	private float[] _surfaceRadii;
	private float _orbitInput;

	private sealed class SurfaceTile
	{
		public Transform2D PlanetToSprite;
		public Vector2 PixelOrigin;
		public float Angle;
		public int Width, Height;
		public byte[] Pixels;
	}

	public override void _Ready()
	{
		ProcessPhysicsPriority = -20;
		if (SurfaceDiameter > 0f && Radius > 0f)
			GlobalScale = Vector2.One * SurfaceDiameter / (Radius * 2f);
		CacheSurfaceRadii();
	}

	// Read the existing ground silhouettes once; no textures or scenery are changed.
	private void CacheSurfaceRadii()
	{
		if (!GodotObject.IsInstanceValid(SurfaceArt)) return;
		var tiles = new List<SurfaceTile>();
		foreach (Node node in SurfaceArt.GetChildren())
		{
			if (node is not Sprite2D sprite || sprite.Texture == null || !sprite.Visible) continue;
			using Image image = sprite.Texture.GetImage();
			if (image == null || image.IsEmpty()) continue;
			if (image.IsCompressed()) image.Decompress();
			image.Convert(Image.Format.Rgba8);
			Transform2D spriteToPlanet = GlobalTransform.AffineInverse() * sprite.GlobalTransform;
			tiles.Add(new SurfaceTile
			{
				PlanetToSprite = spriteToPlanet.AffineInverse(),
				PixelOrigin = (sprite.Centered ? (Vector2)image.GetSize() / 2f : Vector2.Zero) - sprite.Offset,
				Angle = AngleAt(sprite.GlobalPosition),
				Width = image.GetWidth(), Height = image.GetHeight(), Pixels = image.GetData()
			});
		}
		if (tiles.Count == 0) return;
		Vector2 localCenter = LocalCenter;
		_surfaceRadii = new float[SurfaceSamples];
		for (int index = 0; index < SurfaceSamples; index++)
		{
			float angle = index * Mathf.Tau / SurfaceSamples;
			Vector2 direction = Vector2.Right.Rotated(angle);
			var candidates = tiles.FindAll(tile => Mathf.Abs(Wrap(angle - tile.Angle)) < 0.35f);
			float result = Radius;
			bool found = false;
			for (float radius = Radius + 30f; radius >= Mathf.Max(1f, Radius - 30f) && !found; radius -= 0.1f)
			{
				Vector2 point = localCenter + direction * radius;
				foreach (SurfaceTile tile in candidates)
				{
					Vector2 pixel = tile.PlanetToSprite * point + tile.PixelOrigin;
					int x = Mathf.FloorToInt(pixel.X), y = Mathf.FloorToInt(pixel.Y);
					if (x < 0 || y < 0 || x >= tile.Width || y >= tile.Height) continue;
					if (tile.Pixels[(y * tile.Width + x) * 4 + 3] <= 127) continue;
					result = radius;
					found = true;
					break;
				}
			}
			_surfaceRadii[index] = result;
		}
	}

	public float SurfaceRadiusAt(float angle)
	{
		if (_surfaceRadii == null) return Radius;
		float sample = Mathf.PosMod(angle, Mathf.Tau) * SurfaceSamples / Mathf.Tau;
		int index = Mathf.FloorToInt(sample) % SurfaceSamples;
		// Interpolation can cut underneath abrupt sprite seams; use the higher endpoint.
		return Mathf.Max(_surfaceRadii[index], _surfaceRadii[(index + 1) % SurfaceSamples]);
	}

	public float SurfaceYAt(float worldX)
	{
		Vector2 center = CenterPosition;
		float scale = Mathf.Abs(GlobalScale.X);
		float x = worldX - center.X;
		float radius = Radius * scale;
		float y = center.Y;
		for (int pass = 0; pass < 3; pass++)
		{
			y = center.Y - Mathf.Sqrt(Mathf.Max(0f, radius * radius - x * x));
			radius = SurfaceRadiusAt(AngleAt(new Vector2(worldX, y))) * scale;
		}
		return center.Y - Mathf.Sqrt(Mathf.Max(0f, radius * radius - x * x));
	}

	// Support both feet across uneven tile edges, not just the radial center point.
	public float SupportRadiusAt(float angle, float worldHalfWidth)
	{
		float scale = Mathf.Max(0.0001f, Mathf.Abs(GlobalScale.X));
		float radius = SurfaceRadiusAt(angle);
		float support = radius;
		for (int sample = -8; sample <= 8; sample++)
		{
			float offset = Mathf.Atan2(worldHalfWidth * sample / (8f * scale), radius);
			support = Mathf.Max(support, SurfaceRadiusAt(angle + offset) * Mathf.Cos(offset));
		}
		return support;
	}

	public HabitatProfile HabitatAt(float angle)
	{
		foreach (HabitatProfile habitat in Habitats)
			if (habitat != null && habitat.Contains(angle)) return habitat;
		return null;
	}

	// Accepts a normalized orbit command from a controller.
	public void SetOrbitInput(float direction)
		=> _orbitInput = Mathf.Clamp(direction, -1f, 1f);

	public override void _PhysicsProcess(double delta)
	{
		Rotation -= _orbitInput * RotationSpeed * (float)delta;
	}

	public Vector2 CenterPosition =>
		GodotObject.IsInstanceValid(Center)
			? Center.GlobalPosition
			: GlobalPosition;

	private Vector2 LocalCenter => ToLocal(CenterPosition);

	public static float Wrap(float angle)
	{
		return Mathf.Wrap(angle, -Mathf.Pi, Mathf.Pi);
	}

	public float AngleAt(Vector2 worldPosition)
	{
		return (ToLocal(worldPosition) - LocalCenter).Angle();
	}

	public Vector2 SurfacePoint(float angle, float height = 0f)
	{
		Vector2 direction = Vector2.Right.Rotated(angle);

		return ToGlobal(
			LocalCenter + direction * (SurfaceRadiusAt(angle) + height)
		);
	}
}
	
