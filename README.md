# FRC CAD: REBUILT 2026 Block CAD library

An Onshape FeatureScript custom feature library for block CADing a 2026 FRC robot for **REBUILT presented by Haas** in minutes, with the game rules checked as you go. It is also the template for the 2027 game: every 2026 number lives in one `GAME` map at the top of the file.

- [`Rebuilt2026.fs`](Rebuilt2026.fs): the whole library, one paste into a Feature Studio
- [`docs/FEATURES.md`](docs/FEATURES.md): every parameter of every feature (units, defaults, limits)
- [`docs/RESEARCH.md`](docs/RESEARCH.md): the rules and field dimensions, with rule numbers, Team Updates and sources
- `src/`: the same code split by section, for editing (see "Editing the code")

> **Not tested in Onshape yet.** The code was checked against Onshape's published FeatureScript standard library (version 2960): every function it calls exists there, and the brackets balance. Onshape itself could not be reached from the environment it was written in, so expect a few errors on the first paste. See "If something breaks".

## The features

| Feature | Makes | Checks it feeds | Attaches by |
| --- | --- | --- | --- |
| **RB Robot** | frame rails, bumpers (optional gap), 4 swerve modules, battery | perimeter (R104), bumper zone (R405), bumper depth (R403), bumper height (R402), bumper gap corners (R401); weight and CG | origin on the floor; mate connectors `FRAME TOP` and `EDGE - front/back/left/right` |
| **RB Intake** | over-bumper pivot (with an elbow bend), four-bar, or slide-out; stowed and deployed poses, swept envelope, a FUEL at the pickup | reach past frame and bumper, floor strike, roller height vs FUEL; extension, one direction, TRENCH, collisions | a frame side (front/back/left/right), measured from that edge; `HANDOFF - <name>` |
| **RB Hopper** | fixed open-top hopper, see-through FUEL volume, optional FUEL preview | FUEL capacity, mass of a full load | center X/Y and floor height; `HOPPER - <name>` |
| **RB Expanding Hopper** | hopper with 1 to 3 telescoping sections out one side or up | capacity stowed and expanded; extension and one direction (counted even when full, per G413/TU13) | same as RB Hopper |
| **RB Indexer** | straight conveyor/tower channel, or a spindexer | FUEL held in the path (optional) | start and end points, or a center; `FEED - <name>` |
| **RB Turret** | turntable ring and payload at the current angle, swept through the whole range | height, TRENCH (the whole sweep must fit, since it turns while driving), collisions | center X/Y; `TURRET - <name>` (+X = aim) |
| **RB Shooter** | flywheel + adjustable hood, or tilting dual flywheels; exit line, trajectory curve, HUB marker | exit point and direction, speed needed to reach the HUB opening, RPM estimate; height, TRENCH, collisions | the robot, or an RB Turret by name; `EXIT - <name>` |
| **RB Climber** | telescoping hooks, retracted and extended | reach to the LOW RUNG, 30 in height when extended, hook throat, LEVEL 1 lift, rung-to-rung stroke and bumper height for LEVEL 2/3 | center X/Y and facing; `HOOK - <name>` |
| **RB Field Reference** | see-through TRENCH, BUMP, HUB or TOWER | none (visual) | place X/Y and rotation from the robot center |
| **RB Robot Check** | yellow starting box, orange TRENCH plane, red problem boxes, red collision bodies, CG ball | everything below | put it **last** |

### What RB Robot Check checks

1. Frame perimeter at most 110 in (R104).
2. Starting configuration: every mechanism stowed stays inside the frame perimeter (R102) and under 30 in (R104).
3. Height under 30 in through every mechanism's whole motion (R107).
4. Horizontal extension: nothing more than 12 in past the frame (R105), no single mechanism over more than one side, and all mechanisms marked **"Out at the same time as other mechanisms"** extend toward the same side (R106). An intake out the front with an expanding hopper out the back is flagged. Mark a mechanism **"Only out when the others are stowed"** (an endgame climber) to check it on its own.
5. TRENCH: everything stowed, plus the turret and shooter through their whole range, under 22.25 in minus a safety margin; the robot's narrow side with bumpers under 50.34 in wide.
6. Collisions between the swept envelopes of every mechanism with **Include in collision check** on (intake, turret, shooter, climber and expanding hopper by default), and with the frame, bumpers and modules. A shooter riding on its turret isn't checked against that turret.
7. FUEL capacity, stowed and expanded, summed over hoppers and indexers.
8. Mass and CG, empty and with a full FUEL load; weight estimate against 115 lb (R103) and 135 lb with bumpers (R408).

The feature shows a one-line summary. The full report, pass and fail, prints in the FeatureScript console (the `{✓}` icon at the bottom right of Onshape).

## Install (once, about 5 minutes)

1. In Onshape, create a document called **REBUILT Block CAD Library**.
2. Click **+** at the bottom left, then **Create Feature Studio**, and name it `Rebuilt2026`.
3. Delete everything in it and paste the whole of [`Rebuilt2026.fs`](Rebuilt2026.fs) (on GitHub, click **Raw**, then select all and copy).
4. If Onshape offers to update the version on the first line (`FeatureScript 2960;`), accept. Wait for it to save, and check for red errors.
5. Create a **version** of the document (the clock icon, then **Create version**). Custom features can only be shared from a version.
6. In any Part Studio, click **Custom features** (the puzzle piece on the toolbar), find the library document, and add all ten RB features.

When you change the code later, make a new version, then update the features from the toolbar (Onshape shows an update badge).

## Block CAD a REBUILT robot (about 10 minutes)

Add features in this order. Each one reads the ones above it.

1. **RB Robot**: 28 x 26 in frame (108 in perimeter), bumpers 1.5 in off the floor. The info message shows the perimeter and the size with bumpers.
2. **RB Intake**: over-bumper pivot on the front, full width. Defaults: pivot 2 in inside the edge and 10 in up, 7.5 in to the elbow, 6.5 in to the front roller, 60 degree bend, stowed at 160 degrees (folded back over the robot), deployed at -5 degrees. The message says how far it reaches past the frame and the bumper.
3. **RB Expanding Hopper** (or **RB Hopper**): center X 0, floor 4 in, 18 x 20 x 12 in, expanding 10 in. Pick the same side as the intake to stay legal under R106. Try **Back** to see the one-direction problem.
4. **RB Indexer**: conveyor from the hopper up to the shooter.
5. **RB Turret**: center X -7 in, ring 8 in up, full +/-180 degrees.
6. **RB Shooter**: mount on **Turret**, turret name `Turret`. Scrub **Current exit angle** and **Distance from robot center to HUB center** to see the exit line, the trajectory and the speed needed.
7. **RB Climber**: target level, seat height and stroke. Read its message: the 30 in limit makes LEVEL 2 and 3 a rung-to-rung climb.
8. **RB Field Reference**: a TRENCH at X 0 straddles the robot so you can see the clearance.
9. **RB Robot Check**, last.

Then fix whatever turns red. The defaults are reasonable starting points, not a legal robot: expect the check to find something (for example the intake sweeping through the hopper or shooter) on the first try. That's the point.

To compare archetypes, duplicate the Part Studio tab (right-click it, **Duplicate**) and change features in the copy. To share numbers between archetypes, type `#variables` from a Variable Studio into any field.

## Coordinates and conventions

- Origin on the floor, centered under the frame. **+X = front, +Y = left, +Z = up.**
- Intakes are measured from the frame edge they face: "inside the edge" is positive toward the robot center.
- Angles for the intake: 0 = level and pointing out, 90 = straight up, negative = below level.
- Directions for the turret, shooter facing and spindexer exit: 0 = front, 90 = left, 180 = back.
- Exit angle for the shooter: degrees above horizontal.
- Pose 0 = stowed (the starting configuration), 1 = deployed. The pose you pick is drawn solid, the other one see-through, and `ENV - <name>` shows the whole motion.

## Library design

### File structure

One file, so it installs with one paste. Top to bottom:

1. **`GAME` map: the only place 2026 numbers live.** Robot limits, bumper rules, FUEL size and mass, TRENCH, BUMP, HUB and TOWER dimensions, each with its rule number.
2. Library constants (colors, tolerances), then generated dialog bounds (`RB_..._BOUNDS`: min, default, max).
3. Enums (the dropdowns).
4. Helpers: box and cylinder makers, styling, mate connectors, bounding boxes, FUEL capacity, frame side transforms.
5. Lookups: `getRobot` and `getMechRecords` read what earlier features stored.
6. `buildMech`: the shared mechanism builder. A feature describes its parts as simple boxes and cylinders plus a function giving each part's transform for a pose value. `buildMech` draws the parts, the see-through other pose and the swept envelope, and computes the stowed, deployed and swept bounding boxes and the CG analytically.
7. One section per feature, in toolbar order, with a comment block saying what it makes, checks and feeds.
8. RB Robot Check.

### How the features talk

RB Robot stores the frame size, bumper heights, game limits and its mass items on a point body. Each mechanism stores a **record** on its own point body: kind, stowed box, deployed box, swept box, TRENCH box, deploy group, collision flag, mass, CG, FUEL capacity and mass, and (turret, shooter) the extra data children need. RB Robot Check reads all of them, so no feature needs a selection except the shooter's turret name.

### Adapting it for 2027

1. Copy the document. Edit the `GAME` map from the 2027 manual: robot limits, bumper zone, game piece, field elements.
2. Rename the `RB ...` features (Feature Type Name) and the prefix in the console report.
3. Search for `GAME-SPECIFIC`: the field reference feature, FUEL-specific capacity and trajectory code, and the climber's rung logic. Replace or delete what doesn't fit the new game.
4. The robot, intake, hopper, indexer, turret and shooter mechanics don't depend on the game and should carry over.

## Editing the code

`Rebuilt2026.fs` is built from `src/`: `part1_core.fs` (with `bounds.fs` inserted after the colors), then `part2_robot.fs` to `part6_climber_field_check.fs` in order. Small fixes can go straight into `Rebuilt2026.fs` in Onshape; if you keep `src/`, make the same change there.

## If something breaks

- **Red error in the Feature Studio after pasting:** click it to jump to the line. The likely causes are a standard library function renamed since version 2960, or a syntax detail. Fix it there, or send the error text.
- **A feature fails to regenerate:** hover over it. `Add RB Robot before ...` means the feature order is wrong. `No RB Turret named ...` means the shooter's turret name doesn't match.
- **Slow regeneration:** lower **Envelope samples**, or turn off **Show motion envelope** on mechanisms you aren't working on. Collisions then use the parts in their drawn pose only.

## Limits of this version

- Envelopes are sampled poses (default 7). A thin collision between samples can be missed, so raise the samples before trusting a near miss.
- Extension, height and TRENCH checks use bounding boxes, which are slightly conservative for rotated parts.
- Masses are your estimates; the CG uses each mechanism's volume-weighted block center.
- Trajectories ignore air drag and spin. Treat the speeds as rough.
- The BUMP is drawn as a ridge (15 degree ramps up to 6.5 in don't fit the 44.4 in depth the manual gives). The TOWER BASE thickness and the hex opening's "across flats" reading are placeholders.
- Section 8 values (perimeter, bumper rules, weight) came from an unofficial copy of the manual. Check them against the official PDF; they're marked "unverified" in the `GAME` map.
