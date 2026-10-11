using Godot;

namespace Orbitals;
public partial class Critter : CharacterBody2D
{

	[Export] public SpeciesProfile Profile { get; set; } = new();
	[Export] public HabitatProfile Habitat { get; set; }
	[Export] public float BodyOffset { get; set; } = 12f;
	[Export] public float HomeCenterDegrees { get; set; } = -90f;
	[Export(PropertyHint.Range, "1,180,1")]
	public float HomeHalfWidthDegrees { get; set; } = 60f;

	public Planet World { get; private set; }
	public bool WasCarried { get; private set; }
	public float Angle { get; private set; }
	public float SurfaceSpeed { get; private set; }
	public float SurfaceOffset =>
		(BodyOffset * Mathf.Abs(GlobalScale.Y) + 2f + Profile.FlightHeight) /
		Mathf.Max(0.0001f, Mathf.Abs(World.GlobalScale.X)) +
		World.SupportRadiusAt(Angle, _bodyWidth * Mathf.Abs(GlobalScale.X) / 2f) - World.SurfaceRadiusAt(Angle);
	public float TrackRadius => (World.SurfaceRadiusAt(Angle) + SurfaceOffset) * Mathf.Abs(World.GlobalScale.X);
	public float HomeCenter => Habitat?.Center ?? Mathf.DegToRad(HomeCenterDegrees);
	public float HomeHalfWidth => Habitat?.HalfWidth ?? Mathf.DegToRad(Mathf.Clamp(HomeHalfWidthDegrees, 1f, 180f));
	public bool HomeCoversWholePlanet => Habitat?.CoversWholePlanet ?? HomeHalfWidthDegrees >= 180f;
	// Keep the entire body inside the region when selecting normal wander destinations.
	private float WanderHalfWidth => Mathf.Max(0f, HomeHalfWidth -
		Mathf.Atan2(_bodyWidth * Mathf.Abs(GlobalScale.X) / 2f + 2f, TrackRadius));

	private CritterBrain _brain;
	private AnimatedSprite2D _sprite;
	private bool _initialized;
	private float _bodyWidth = 24f;

	public override void _Ready()
	{
		Profile ??= new SpeciesProfile();
		_brain = GetNodeOrNull<CritterBrain>("Brain");
		_sprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
		if (_sprite != null && Profile.Animations != null)
		{
			_sprite.SpriteFrames = Profile.Animations;
			StringName animation = _sprite.SpriteFrames.HasAnimation("idle") ? "idle" : _sprite.Animation;
			Texture2D frame = _sprite.SpriteFrames.GetFrameTexture(animation, 0);
			float height = Mathf.Max(8f, Profile.SpriteHeight);
			_sprite.Scale = Vector2.One * height / Mathf.Max(1f, frame.GetHeight());
			BodyOffset = height / 2f;
			_bodyWidth = height * frame.GetWidth() / Mathf.Max(1f, frame.GetHeight());
			CollisionShape2D collision = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
			if (collision != null)
				collision.Shape = new RectangleShape2D { Size = new Vector2(height * 0.65f, height) };
		}
		UpdateAnimation();
		_initialized = true;
		if (GodotObject.IsInstanceValid(World)) AttachToPlanet(World);
	}

	public void AttachToPlanet(Planet planet)
		=> SetPlanet(planet, true);

	// A released creature lands where it was carried, then walks home instead of teleporting.
	public void LandOnPlanet(Planet planet)
		=> SetPlanet(planet, false);

	private void SetPlanet(Planet planet, bool clampToHome)
	{
		if (!GodotObject.IsInstanceValid(planet) || planet.Radius <= 0f ||
			planet.Radius + BodyOffset <= 0f)
		{
			GD.PushError($"{Name}: assign a planet with a positive surface radius.");
			return;
		}
		World = planet;
		if (!_initialized) return;
		Angle = World.AngleAt(GlobalPosition);
		Habitat ??= World.HabitatAt(Angle);
		// Start in the assigned region instead of walking across other habitats at spawn.
		if (clampToHome && !InHome(Angle)) Angle = HomeCenter;
		StopMoving();
		PlaceOnSurface();
		_brain?.OnEnvironmentChanged();
	}

	public void DetachFromPlanet()
	{
		World = null;
		StopMoving();
		_brain?.OnEnvironmentChanged();
		UpdateAnimation();
	}

	public void MarkCarried() => WasCarried = true;

	public override void _PhysicsProcess(double delta)
	{
		if (!GodotObject.IsInstanceValid(World)) return;
		float dt = (float)delta;
		if (dt <= 0f) return;
		StopMoving();
		_brain?.Tick(dt);
		Angle = Planet.Wrap(Angle + SurfaceSpeed / TrackRadius * dt);
		if (_sprite != null && SurfaceSpeed != 0f)
			_sprite.FlipH = Profile.FacesRight ? SurfaceSpeed < 0f : SurfaceSpeed > 0f;
		UpdateAnimation();
		PlaceOnSurface();
	}

	private void UpdateAnimation()
	{
		if (_sprite?.SpriteFrames == null) return;
		StringName animation = SurfaceSpeed == 0f ? "idle" : "walk";
		if (_sprite.SpriteFrames.HasAnimation(animation) &&
			(_sprite.Animation != animation || !_sprite.IsPlaying()))
			_sprite.Play(animation);
	}

	public void StopMoving() => SurfaceSpeed = 0f;

	private void PlaceOnSurface()
	{
		GlobalPosition = World.SurfacePoint(Angle, SurfaceOffset);
		Vector2 outward = GlobalPosition - World.CenterPosition;
		GlobalRotation = outward.Angle() + Mathf.Pi / 2f;
	}

	public bool InHome(float angle)
		=> HomeCoversWholePlanet ||
		   Mathf.Abs(Planet.Wrap(angle - HomeCenter)) <= HomeHalfWidth + 0.0001f;

	public float RouteDelta(float targetAngle)
	{
		if (HomeCoversWholePlanet || !InHome(Angle))
			return Planet.Wrap(targetAngle - Angle);
	
		return Planet.Wrap(targetAngle - HomeCenter) - Planet.Wrap(Angle - HomeCenter);
	}

	public float ArcDistance(float targetAngle)
		=> Mathf.Abs(RouteDelta(targetAngle)) * TrackRadius;

	public float RandomWanderAngle(RandomNumberGenerator rng)
	{
		float reach = Mathf.Max(0f, Profile.WanderDistance) / TrackRadius;
		float current = Planet.Wrap(Angle - HomeCenter);
		float minimum = current - reach;
		float maximum = current + reach;
		if (!HomeCoversWholePlanet)
		{
			// A carried creature near an edge may already be outside this inset.
			current = Mathf.Clamp(current, -WanderHalfWidth, WanderHalfWidth);
			minimum = Mathf.Max(current - reach, -WanderHalfWidth);
			maximum = Mathf.Min(current + reach, WanderHalfWidth);
		}
	
		return Planet.Wrap(HomeCenter + rng.RandfRange(minimum, maximum));
	}

	public bool MoveTowardAngle(float targetAngle, float speed, float dt, float stop)
	{
		float difference = RouteDelta(targetAngle);
		float distance = Mathf.Abs(difference) * TrackRadius;
		if (distance <= Mathf.Max(0.01f, stop)) { StopMoving(); return true; }
		if (dt <= 0f) { StopMoving(); return false; }
		SurfaceSpeed = Mathf.Sign(difference) * Mathf.Min(Mathf.Max(0f, speed), distance / dt);
		return false;
	}

}
