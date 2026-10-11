using Godot;
using System.Collections.Generic;

namespace Orbitals;

public partial class CritterSpawner : Node2D
{
	[Export] public Planet PlanetInstance { get; set; }
	[Export] public PackedScene CritterScene { get; set; }
	[Export] public Godot.Collections.Array<CritterSpawnProfile> SpawnProfiles { get; set; } = new();
	[Export(PropertyHint.Range, "0,1000,1")]
	public int MaxPopulation { get; set; } = 15;
	[Export] public bool SpawnOnReady { get; set; } = true;
	[Export(PropertyHint.Range, "0.1,4,0.1")]
	public float CritterScale { get; set; } = 1f;
	[Export] public bool ContinuousSpawning { get; set; }
	[Export(PropertyHint.Range, "0.1,60,0.1,suffix:s")]
	public float SpawnInterval { get; set; } = 2f;
	private float _spawnTimer;

	private readonly RandomNumberGenerator _rng = new();
	private readonly List<(CritterSpawnProfile Rule, int Slot, Node2D Root, Critter Body)> _spawned = new();
	private Node2D _population;
	// Planet-local angles for the three authored dropoff areas:
	// (-273, 161), (265, 169), and (0, -297).
	private static readonly float[] DropoffAngles =
	{
		Mathf.DegToRad(150f),
		Mathf.DegToRad(32f),
		Mathf.DegToRad(-90f)
	};
	private const float DropoffExclusionHalfWidth = 0.35f;
	private const float DropoffNudge = 0.5f;

	public Godot.Collections.Array<Critter> Critters
	{
		get
		{
			RemoveFreedCritters();
			var critters = new Godot.Collections.Array<Critter>();
			foreach (var entry in _spawned) critters.Add(entry.Body);
			return critters;
		}
	}

	public override void _Ready()
	{
		_rng.Randomize();
		_population = GetNode<Node2D>("Population");
		_spawnTimer = Mathf.Max(0.1f, SpawnInterval);
		if (SpawnProfiles.Count == 0 && (SpawnOnReady || ContinuousSpawning))
			GD.PushWarning($"{Name}: SpawnProfiles is empty. Assign spawn resources in the Inspector to create creatures.");
		if (SpawnOnReady) SpawnPopulation();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!ContinuousSpawning) return;
		_spawnTimer -= (float)delta;
		if (_spawnTimer > 0f) return;
		_spawnTimer = Mathf.Max(0.1f, SpawnInterval);
		SpawnPopulation();
	}

	// Fill configured counts, keeping existing creatures and respecting the total cap.
	// Call again after collection/removal to replenish the population when desired.
	public void SpawnPopulation()
	{
		if (!GodotObject.IsInstanceValid(PlanetInstance) || CritterScene == null || _population == null)
		{
			GD.PushError($"{Name}: assign a planet and a critter scene before spawning.");
			return;
		}

		RemoveFreedCritters();
		foreach (CritterSpawnProfile rule in SpawnProfiles)
		{
			if (rule?.Habitat == null || rule.Species == null || !PlanetInstance.Habitats.Contains(rule.Habitat))
			{
				GD.PushWarning($"{Name}: each spawn profile needs a species and one of this planet's habitats.");
				continue;
			}

			int count = Mathf.Clamp(rule.Count, 0, 1000);
			for (int index = 0; index < count && _spawned.Count < Mathf.Max(0, MaxPopulation); index++)
			{
				if (_spawned.Exists(entry => entry.Rule == rule && entry.Slot == index)) continue;
				Node2D instance = CritterScene.Instantiate<Node2D>();
				Critter critter = instance as Critter ?? instance.GetNodeOrNull<Critter>("Critter");
				if (critter == null)
				{
					instance.Free();
					GD.PushError($"{Name}: the critter scene needs a Critter root or a child named Critter.");
					return;
				}

				critter.Profile = (SpeciesProfile)rule.Species.Duplicate();
				critter.Profile.Rarity = _rng.Randf() < 0.05f ? "Shiny" : "Normal";
				critter.Habitat = rule.Habitat;
				instance.Name = $"{rule.Habitat.HabitatName}Critter";
				_population.AddChild(instance, true);
				instance.GlobalScale = Vector2.One * Mathf.Max(0.1f, CritterScale);
				critter.TreeExited += () =>
				{
					_spawned.RemoveAll(entry => entry.Body == critter);
					if (GodotObject.IsInstanceValid(instance) && !instance.IsQueuedForDeletion())
						instance.QueueFree();
				};

				// Spread starts across the region, leaving space at its boundaries.
				float margin = Mathf.Min(Mathf.DegToRad(10f), rule.Habitat.HalfWidth * 0.25f);
				float reach = rule.Habitat.HalfWidth - margin;
				float fraction = (index + _rng.RandfRange(0.25f, 0.75f)) / count;
				float angle = rule.Habitat.Center + Mathf.Lerp(-reach, reach, fraction);
				angle = MoveOutsideDropoff(angle);
				// BodyOffset is measured in creature units, independently of planet scale.
				float offset = critter.BodyOffset * Mathf.Abs(critter.GlobalScale.Y) /
					Mathf.Max(0.0001f, Mathf.Abs(PlanetInstance.GlobalScale.X));
				critter.GlobalPosition = PlanetInstance.SurfacePoint(angle, offset);
				critter.AttachToPlanet(PlanetInstance);
				_spawned.Add((rule, index, instance, critter));
			}
		}
	}

	private static float MoveOutsideDropoff(float angle)
	{
		foreach (float dropoffAngle in DropoffAngles)
		{
			float difference = Planet.Wrap(angle - dropoffAngle);
			if (Mathf.Abs(difference) <= DropoffExclusionHalfWidth)
				return Planet.Wrap(dropoffAngle + Mathf.Sign(difference == 0f ? 1f : difference) *
					(DropoffExclusionHalfWidth + DropoffNudge));
		}
		return angle;
	}

	private void RemoveFreedCritters()
		=> _spawned.RemoveAll(entry =>
		{
			if (!GodotObject.IsInstanceValid(entry.Root) || entry.Root.IsQueuedForDeletion()) return true;
			if (GodotObject.IsInstanceValid(entry.Body) && !entry.Body.IsQueuedForDeletion()) return false;
			entry.Root.QueueFree();
			return true;
		});
}
