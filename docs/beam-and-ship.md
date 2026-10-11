# Beam and Ship function guide

This guide describes the current implementation for collaborators changing the
abduction mechanic. Source files are
`scripts/actors/ship/Beam.cs`, `scripts/actors/ship/Ship.cs`, and the beam
helpers in `scripts/actors/ship/beam/`.
See [architecture.md](architecture.md) for state ownership and scene wiring.

## Beam

Beam is an `Area2D` that detects creatures on collision layer 2. It receives
commands, owns capture mechanics, and emits collection events. Drawing is
handled by its optional BeamVisuals child. It has no HUD references, input
polling, global flag, or rescue total.

| Function | Called by | What it does |
| --- | --- | --- |
| `_Ready()` | Godot when the node becomes ready | Sets physics priority, creature-only collision mask, and the exported polygon reference. Initializes the cone and hides it. Reports a missing collision reference and disables processing in that case. |
| `_PhysicsProcess(delta)` | Godot each physics tick | Updates the cone length even while off. When active, advances BeamVisuals effects, acquires targets, updates captures and sends presentation values. `delta` is seconds. It does not choose the mode. |
| `AcquireTargets()` | Active physics tick | Reads overlapping bodies, filters to living, attached Critters that are not already captured, and sorts them by distance. Hold latches at most one; Absorb can latch many. Saves original planet/scale/distance, detaches the creature and emits `Hit`. |
| `UpdateTargets(dt)` | Active physics tick | Iterates a copy of the capture dictionary so collection/removal can safely change it. Drops invalid targets, accelerates pull speed, then dispatches each capture to hold or absorb behavior. A latched target stays captured even if it leaves the cone during movement. |
| `UpdateHeldTarget(critter, capture, dt)` | Target update in Hold mode | Finds a hover point below the intake, bounded by surface clearance. Moves the creature toward it, restores its full size, and rotates it upright. It neither frees the creature nor emits collection. |
| `UpdateAbsorbedTarget(critter, capture, dt)` | Target update in Absorb mode | Pulls toward the intake, shrinks according to progress, and spins the creature. Within `CaptureRadius`, removes the target, asks BeamVisuals to flash, emits `CritterCollected(creature)` once, and queues the creature for deletion. Spawner cleanup removes its wrapper. |
| `SetActive(active)` | Existing callers / compatibility API | Converts `true` to Absorb and `false` to Off. Use `SetMode` when you need Hold. |
| `SetMode(mode)` | PlayerController or another controller | Updates mode and visibility. Off releases every capture. Switching a captured group to Hold releases the group so Hold can select one. A single held target can transfer directly into Absorb. |
| `ReleaseTargets()` | Mode change, focus loss, or scene teardown | Restores original scales and calls `LandOnPlanet` on surviving creatures. Landing uses the release location; displaced creatures walk home rather than teleporting. Clears the capture dictionary. |
| `UpdateGeometry()` | Ready and every physics tick | Uses BeamGeometry to calculate terrain-clipped reach in local units. Updates the collision polygon only when length or width changes. |
| `UpdateVisuals()` | Active physics tick and mode changes | Sends cone dimensions, color and mode to BeamVisuals. Converts the held creature's position into visual-local coordinates and supplies a halo radius. Does nothing when presentation is absent. |
| `_Notification(what)` | Godot window/application notifications | Switches off and releases targets on focus loss. PlayerController separately clears held input actions. |
| `_ExitTree()` | Godot when this node leaves the tree | Releases any surviving captures so removing a beam does not strand creatures in a detached state. |

The private `Capture` record stores the creature's original planet and global
scale, initial distance, and current pull speed. It is temporary mechanic state,
not cargo or score. `Mode`, `IsActive`, `TargetCount`, `CurrentLength` and
`ActiveColor` expose the current mechanic state. `PlanetInstance`, `Collision`,
and optional `Visuals` are explicit Inspector references.

`Hit` reports a newly latched target. `CritterCollected(creature)` reports a
completed absorption before the creature is freed. A listener must copy any
needed species data during that callback rather than retain the freed node.
The main scene connects collection to GameSession's rescue rule, discarding
the creature parameter for the current one-point-per-creature rule.

## BeamVisuals and BeamGeometry

`BeamVisuals` owns presentation state: pulse time, flash intensity, cone
appearance and halo coordinates. It has no references to captured creatures or
game rules. Its Node2D sits at the beam's local origin with the same transform;
parent visibility hides all effects when the beam is off. Existing width/color
settings are still edited on Beam, which passes their values to the renderer.

| Function | Called by | What it does |
| --- | --- | --- |
| `SetAppearance(topWidth, bottomWidth, length, color, holding)` | Beam.UpdateVisuals | Stores the current cone and mode appearance and requests a redraw. |
| `SetHoldHalo(center, radius)` | Beam.UpdateVisuals | Stores a local halo center/radius. Zero radius clears the halo. |
| `AdvanceEffects(delta)` | Active Beam physics tick | Advances pulse/streak time and fades the intake flash. Time pauses while the beam is off, as before. |
| `PlayCollectionFlash()` | Completed absorption | Resets the intake flash intensity. It does not count or free a creature. |
| `_Draw()` | Godot after a redraw request | Composes cone, streaks, optional hold halo and intake glow. |
| `DrawCone(pulse)` | Visual draw | Draws the translucent trapezoid and its lit edges. Uses BeamGeometry's shared corners. |
| `DrawStreaks(pulse)` | Visual draw | Draws stationary oscillating streaks for Hold or upward-moving streaks for Absorb. |

`BeamGeometry` is a stateless C# helper shared by physics and rendering.

| Function | Called by | What it does |
| --- | --- | --- |
| `LengthAt(planet, position, scale, maximum)` | Beam.UpdateGeometry | Limits reach to the surface beneath the intake and converts world distance to beam-local units. Without a planet, uses the configured maximum. |
| `ConePoints(topWidth, bottomWidth, length)` | Beam collision update and BeamVisuals.DrawCone | Returns the same four local trapezoid corners for both systems. |

## Ship

Ship is a `CharacterBody2D` actor responsible for its movement and appearance.
It receives a vertical command and reads terrain height. It does not know the
beam's capture behavior, cargo total, mission timer, labels or player actions.

| Function | Called by | What it does |
| --- | --- | --- |
| `_Ready()` | Godot when the node becomes ready | Sets physics priority and configures the ship's appearance and attachment point. |
| `SetVerticalInput(direction)` | PlayerController or another controller | Clamps and stores a command from -1 (up) through 0 (hover) to +1 (down). Fractional values work for analog or AI control. |
| `_PhysicsProcess(delta)` | Godot each physics tick | Runs movement, altitude limiting and animation updates in that order. `delta` is seconds. |
| `ConfigureAppearance()` | Ready | Uses SpriteWidth to size the exported AnimatedSprite2D, hull collision and BeamMount. Starts the existing hover animation. BeamMount is a generic attachment node; Ship does not call Beam methods. |
| `UpdateVerticalMovement(dt)` | Physics tick | Accelerates toward commanded speed. With zero command, decelerates toward stationary hover. Applies world-space movement using velocity times elapsed seconds. |
| `ClampAltitude()` | After movement | Queries the planet's visible upper surface beneath the ship. Clamps altitude between the configured minimum/maximum, with clearance for hull size. Hitting a limit zeroes velocity. Without a planet reference, movement remains unrestricted. |
| `UpdateAnimation()` | After altitude limiting | Sets hover playback speed between 1 and 1.5 from actual velocity, so a stopped or altitude-limited ship returns to normal hover. |

The original Inspector name `speed` is retained to preserve existing overrides.
`Sprite`, `HullCollision`, `BeamMount` and `PlanetInstance` are explicit scene
references. Direct position updates preserve the current hovering behavior;
the hull's collision mask remains zero. Introducing physical ship collisions
would require a separate change to movement and collision policy.

## One frame and one rescue

1. PlayerController reads actions and commands Ship, Planet and Beam.
2. Planet rotates, then Ship moves and clamps altitude.
3. Beam updates its cone against the updated terrain, detects creatures, and
   advances its captures. Hold preserves a creature; Absorb consumes it only at
   the intake.
4. A completed capture emits `CritterCollected`; scene wiring calls
   `GameSession.RecordRescue()`.
5. GameSession increments `RescuedCount` and emits `RescueCountChanged`.
   GameHud formats the new total into its cargo label.

Edit capture mechanics in Beam, effects in BeamVisuals, shared cone calculations
in BeamGeometry, movement in Ship, bindings in PlayerController,
scoring/countdown rules in GameSession, and label formatting in GameHud.
