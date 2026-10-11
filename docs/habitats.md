# Planet habitats

The planet has three habitat resources in `resources/habitats`: grassland,
snowfield, and desert. Their arcs match the authored artwork: grassland occupies
150 degrees, and snowfield and desert each occupy 105 degrees.
The region angles are relative to the planet, so the creatures' movement bounds
rotate with it. Habitats control creature movement only. The planet keeps its
hand-authored terrain, including the oasis, withered grass, and icy patches.

| Habitat | Center | Surface arc |
| --- | --- | --- |
| Grassland | -52.5 degrees | -127.5 to 22.5 degrees |
| Snowfield | 75 degrees | 22.5 to 127.5 degrees (white snow/ice only) |
| Desert | 180 degrees | 127.5 through the angle seam to -127.5 degrees |

The green oasis remains inside the desert's arc. Snow creatures do not treat
grass or withered grass as snow. Normal wander destinations leave enough room
for the creature's body at habitat edges.

To change a region, open its `.tres` resource in the Godot Inspector:

- **HabitatName** identifies the habitat.
- **CenterDegrees** sets its position around the planet.
- **SurfaceDegrees** sets how much circumference it occupies, from 1 to 360 degrees.

Creature bounds use the exact configured angles. Changing a habitat's position
or size does not change any terrain textures or environmental details.
For a radius `r`, the habitat's surface length is `r * SurfaceDegrees * PI / 180`
in planet-local units (`HabitatProfile.SurfaceLength(r)`).

To add a habitat, create a **HabitatProfile** resource and add it to the planet's
**Habitats** array. Set its center and size so it occupies a separate region;
the default three regions cover the complete planet without overlapping.
If regions overlap, an unassigned creature adopts the first matching entry.

Assign the same resource to the **Habitat** field on a creature's inner
`CharacterBody2D` node (the node with `Critter.cs`). Several creatures can share
a habitat, or each can have its own. Assigned creatures start inside their region
and wander within its boundaries. An unassigned creature adopts the habitat at
its spawn position; on a planet without habitat resources, its existing
**HomeCenterDegrees** and **HomeHalfWidthDegrees** fields still apply.

Both `GameManager.tscn` and `critter_planet.tscn` use a single instance of
`critter_spawner.tscn`. Its **PlanetInstance** points to the scene's planet.
Individual creatures are created at runtime under the spawner's `Population`
node, keeping the main scene small.

The spawner's **SpawnProfiles** array pairs a habitat with a **SpeciesProfile**
and a **Count**. The main game includes all fourteen species from the supplied
folders, with **MaxPopulation** set to 144:

The main game's eighteen spawn rules are saved separately in
`resources/spawning/`. Drag these **CritterSpawnProfile** resources into the
spawner's **SpawnProfiles** array. Open a resource to edit its habitat, species,
or count. An empty array creates no creatures and reports a startup warning.

| Folder | Species | Population |
| --- | --- | --- |
| Grassland | Moss creature, Lorax, mouse-snail | 40 |
| Snow | Penguin, many-eyes, slug-dog, walker | 40 |
| Desert | Bombastick, Cocokyu, Metromome, Soroff | 40 |
| Sky | Mothalist, Screechure (three of each per habitat) | 18 |
| Special | CUG (desert) | 6 |

The reusable spawner scene uses three per habitat and a cap of nine for
small tests. Set counts or the cap to zero to disable those spawns, or turn off
**SpawnOnReady** to control when they appear. Creatures start spread across their
regions. **CritterScale** controls their size independently of the planet; at
the default `1`, the supplied species are 48 pixels tall and the ship is 128 pixels wide.

`CritterSpawner.SpawnPopulation()` fills any missing creatures up to the configured
counts and cap. Calling it again does not duplicate a full population. Removed
creatures and their empty wrapper nodes are cleaned up as they leave the scene.
**ContinuousSpawning** enables timed replenishment; the main scene sets it on
with **SpawnInterval** at two seconds. The timer replaces missing creatures up
to the configured counts and cap, including ones abducted by the ship. Turn it
off for a fixed population. Reducing counts does not remove existing creatures.

Species resources in `resources/species` configure movement and visuals:

- **Animations** points to a `SpriteFrames` resource with looping `idle` and `walk` animations.
- **SpriteHeight** sets the frame height in world pixels before the spawner's
  **CritterScale** multiplier. The supplied species all use `48`; the script
  calculates the source texture scale automatically.
- **FlightHeight** adds a world-pixel offset above the terrain. Mothalist uses
  `650`, Screechure uses `950`, and CUG hovers at `60`; ground species use zero.
  These offsets remain fixed above the terrain while the planet rotates.
- **FacesRight** describes the artwork's original facing direction so walking
  creatures flip correctly. The penguin faces left in its original sheet.

The ship can climb to **MaximumAltitude = 1400** world pixels above the terrain.
The beam reaches at most 500 pixels below its intake, so collecting high-flying
creatures requires ascending above their layer. Near the surface, the sky species
remain out of reach. Change the ceiling on Ship and flight height on each species
resource in the Inspector.

Carried creatures land at the release location, even outside their assigned
habitat. They then enter **ReturnHome** and walk back gradually. Normal spawning
still starts each creature inside its own habitat.

The frame resources in `resources/animations` use atlas regions of the supplied
sprite sheets, leaving the original image files intact. Idle animations play
more slowly; walking animations play at eight frames per second.

The ship uses the sixteen 32-by-32 frames in `PO_Spaceship_Empty.png`, in row order,
for its looping `hover` animation. It plays at ten frames per second at rest and
fifteen while ascending or descending. Its beam starts below the new ship sprite.
See [abduction.md](abduction.md) for controls and beam settings.
See [architecture.md](architecture.md) for script folders and component ownership.
