using Godot;

public partial class Critter : CharacterBody2D
{

	[Export] public SpeciesProfile Profile { get; set; } = new();
	[Export] public float BodyOffset { get; set; } = 12f;
	[Export] public float HomeCenterDegrees { get; set; } = -90f;
	[Export(PropertyHint.Range, "1,180,1")]
	public float HomeHalfWidthDegrees { get; set; } = 60f;

	public Planet World { get; private set; }
	public float Angle { get; private set; }
	public float SurfaceSpeed { get; private set; }
	public float TrackRadius => World.Radius + BodyOffset;
	public float HomeCenter => Mathf.DegToRad(HomeCenterDegrees);
	public float HomeHalfWidth => Mathf.DegToRad(Mathf.Clamp(HomeHalfWidthDegrees, 1f, 180f));

	private CritterBrain _brain;
	private Sprite2D _sprite;
	private bool _initialized;

	public override void _Ready()
	{
		Profile ??= new SpeciesProfile();
		_brain = GetNodeOrNull<CritterBrain>("Brain");
		_sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
		_initialized = true;
		if (GodotObject.IsInstanceValid(World)) AttachToPlanet(World);
	}

	public void AttachToPlanet(Planet planet)
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
		StopMoving();
		PlaceOnSurface();
		_brain?.OnEnvironmentChanged();
	}

	public void DetachFromPlanet()
	{
		World = null;
		StopMoving();
		_brain?.OnEnvironmentChanged();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!GodotObject.IsInstanceValid(World)) return;
		float dt = (float)delta;
		if (dt <= 0f) return;
		StopMoving();
		_brain?.Tick(dt);
		Angle = Planet.Wrap(Angle + SurfaceSpeed / TrackRadius * dt);
		if (_sprite != null && SurfaceSpeed != 0f)
			_sprite.FlipH = SurfaceSpeed < 0f;
		PlaceOnSurface();
	}

	public void StopMoving() => SurfaceSpeed = 0f;

	private void PlaceOnSurface()
	{
		GlobalPosition = World.SurfacePoint(Angle, BodyOffset);
		Vector2 outward = GlobalPosition - World.CenterPosition;
		GlobalRotation = outward.Angle() + Mathf.Pi / 2f;
	}

	public bool InHome(float angle)
		=> HomeHalfWidthDegrees >= 180f ||
		   Mathf.Abs(Planet.Wrap(angle - HomeCenter)) <= HomeHalfWidth + 0.0001f;

	public float RouteDelta(float targetAngle)
	{
		if (HomeHalfWidthDegrees >= 180f || !InHome(Angle))
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
		if (HomeHalfWidthDegrees < 180f)
		{
			minimum = Mathf.Max(minimum, -HomeHalfWidth);
			maximum = Mathf.Min(maximum, HomeHalfWidth);
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
