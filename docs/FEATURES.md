# Feature parameters

Generated from `Rebuilt2026.fs`. Units are inches and degrees unless noted. Every value can be typed as an expression or a `#variable`.

## RB Robot

REBUILT frame perimeter, bumpers, swerve modules and battery. Put this first: every other RB feature reads the frame and the game limits from it.

| Field | Parameter | Unit | Default | Range / options |
| --- | --- | --- | --- | --- |
| Frame length (front to back) | length | in | 28 | 10 to 60 |
| Frame width (side to side) | width | in | 26 | 10 to 60 |
| Rail height | railHeight | in | 2 | 0.5 to 6 |
| Rail width | railWidth | in | 1 | 0.5 to 4 |
| Frame bottom above floor | frameBottom | in | 1.5 | 0 to 6 |
| Bumper bottom above floor | bumperBottom | in | 1.5 | 0 to 6 |
| Bumper height | bumperHeight | in | 5 | 1 to 10 |
| Bumper depth (backing + padding) | bumperDepth | in | 3.25 | 0.5 to 6 |
| Bumper color | bumperColor | choice | Red | Red, Blue |
| Bumper gap (one large gap allowed) | gapSide | choice | No gap | No gap, Front, Back, Left, Right |
| Gap width (centered) | gapWidth | in | 12 | 1 to 60 |
| Swerve module | modulePreset | choice | SDS MK4i / MK4n (approx.) | SDS MK4i / MK4n (approx.), WCP X2i (approx.), Custom |
| Wheel center inset from frame edge | wheelInset | in | 2.625 | 0.5 to 6 |
| Wheel diameter | wheelDia | in | 4 | 2 to 8 |
| Module block size (square) | moduleSize | in | 5.25 | 2 to 10 |
| Module top above floor | moduleTop | in | 6 | 1 to 12 |
| Show battery | showBattery | boolean | true |  |
| Battery center X (forward) | batteryX | in | -9 | -200 to 200 |
| Battery center Y (left) | batteryY | in | 0 | -200 to 200 |
| Frame + drivetrain + electronics (lb) | baseMassLb | number | 70 | 0 to 200 |
| Bumpers (lb) | bumperMassLb | number | 15 | 0 to 40 |
| Battery (lb) | batteryMassLb | number | 13 | 0 to 20 |
| Override REBUILT limits | overrideLimits | boolean | false |  |
| Max frame perimeter | maxPerimeter | in | 110 | 20 to 300 |
| Max height (start and match) | maxHeight | in | 30 | 5 to 120 |
| Horizontal extension limit | extensionLimit | in | 12 | 0 to 60 |
| TRENCH clearance height | trenchHeight | in | 22.25 | 5 to 60 |

## RB Intake

Over-bumper pivot, four-bar or slide-out FUEL intake on one side of the robot, with stowed and deployed poses and a swept envelope.

| Field | Parameter | Unit | Default | Range / options |
| --- | --- | --- | --- | --- |
| Name | mechName | string | Intake |  |
| Intake type | intakeType | choice | Over bumper (pivot) | Over bumper (pivot), Four-bar, Linear slide-out |
| Side | side | choice | Front | Front, Back, Left, Right |
| Full width of the frame side | fullWidth | boolean | true |  |
| Margin at each end | sideMargin | in | 1 | 0 to 10 |
| Intake width | intakeWidth | in | 20 | 2 to 60 |
| Roller diameter | rollerDia | in | 2 | 0.5 to 6 |
| Number of rollers | rollerCount | count | 2 | 1 to 4 |
| Roller spacing (center to center) | rollerSpacing | in | 3 | 0.5 to 12 |
| Side plate height | plateHeight | in | 2.5 | 0.25 to 8 |
| Box front stowed, inside the frame edge | setback | in | 2 | -12 to 24 |
| Box length (in the slide direction) | boxLength | in | 6 | 1 to 24 |
| Box bottom above floor | boxBottom | in | 1.5 | 0 to 20 |
| Slide travel | travel | in | 10 | 0 to 30 |
| Pivot inside the frame edge | setback | in | 2 | -12 to 24 |
| Pivot height above floor | pivotHeight | in | 10 | 1 to 40 |
| Pivot to elbow (four-bar: link length) | armLength | in | 7.5 | 1 to 40 |
| Elbow to front roller | arm2Length | in | 6.5 | 0.5 to 30 |
| Bend down at the elbow (0 = straight arm) | bend | deg | 60 | 0 to 150 |
| Front roller ahead of link tip | couplerReach | in | 4 | -10 to 20 |
| Roller drop below link tip | couplerDrop | in | 2 | -10 to 20 |
| Stowed angle (90 = straight up) | stowAngle | deg | 160 | -360 to 360 |
| Deployed angle (negative = below level) | deployAngle | deg | -5 | -360 to 360 |
| Pose to draw | pose | choice | Stowed | Stowed, Deployed, In between |
| Deployed fraction | partial | number | 0.5 | 0 to 1 |
| Deploys | group | choice | Out at the same time as other mechanisms | Out at the same time as other mechanisms, Only out when the others are stowed |
| Mass estimate (lb) | massLb | number | 6 | 0 to 100 |
| Include in collision check | collide | boolean | true |  |
| Show motion envelope | showEnvelope | boolean | true |  |
| Envelope samples | samples | count | 7 | 2 to 25 |

## RB Hopper

Fixed open-top hopper with a FUEL capacity estimate from the FUEL diameter and a packing factor.

| Field | Parameter | Unit | Default | Range / options |
| --- | --- | --- | --- | --- |
| Name | mechName | string | Hopper |  |
| Center X (forward) | centerX | in | 0 | -200 to 200 |
| Center Y (left) | centerY | in | 0 | -200 to 200 |
| Floor height above ground | floorZ | in | 4 | 0 to 40 |
| Inside length (front to back) | hopperLength | in | 18 | 3 to 60 |
| Inside width (side to side) | hopperWidth | in | 20 | 3 to 60 |
| Inside height | hopperHeight | in | 12 | 2 to 40 |
| Packing factor | packing | number | 0.5 | 0.05 to 0.74 |
| Volume taken by other parts (cubic in) | lostVolume | number | 0 | 0 to 100000 |
| Show FUEL preview | showFuel | boolean | false |  |
| Mass estimate (lb) | massLb | number | 6 | 0 to 100 |
| Include in collision check | collide | boolean | true |  |
| Show motion envelope | showEnvelope | boolean | true |  |
| Envelope samples | samples | count | 7 | 2 to 25 |

## RB Expanding Hopper

Hopper whose walls telescope out one side or up. Reports FUEL capacity stowed and expanded; the expansion counts toward the extension and one-direction rules.

| Field | Parameter | Unit | Default | Range / options |
| --- | --- | --- | --- | --- |
| Name | mechName | string | Expanding |  |
| Center X (forward) | centerX | in | 0 | -200 to 200 |
| Center Y (left) | centerY | in | 0 | -200 to 200 |
| Floor height above ground | floorZ | in | 4 | 0 to 40 |
| Inside length (front to back) | hopperLength | in | 18 | 3 to 60 |
| Inside width (side to side) | hopperWidth | in | 20 | 3 to 60 |
| Inside height | hopperHeight | in | 12 | 2 to 40 |
| Packing factor | packing | number | 0.5 | 0.05 to 0.74 |
| Volume taken by other parts (cubic in) | lostVolume | number | 0 | 0 to 100000 |
| Show FUEL preview | showFuel | boolean | false |  |
| Expands toward | expandDir | choice | Front | Front, Back, Left, Right, Up |
| Expansion distance | expandDist | in | 10 | 0 to 40 |
| Telescoping sections | sections | count | 1 | 1 to 3 |
| Pose to draw | pose | choice | Stowed | Stowed, Deployed, In between |
| Expanded fraction | partial | number | 0.5 | 0 to 1 |
| Deploys | group | choice | Out at the same time as other mechanisms | Out at the same time as other mechanisms, Only out when the others are stowed |
| Mass estimate (lb) | massLb | number | 6 | 0 to 100 |
| Include in collision check | collide | boolean | true |  |
| Show motion envelope | showEnvelope | boolean | true |  |
| Envelope samples | samples | count | 7 | 2 to 25 |

## RB Indexer

Path from hopper to shooter: a straight conveyor or tower channel, or a spindexer (rotating floor).

| Field | Parameter | Unit | Default | Range / options |
| --- | --- | --- | --- | --- |
| Name | mechName | string | Indexer |  |
| Indexer type | indexerType | choice | Conveyor / tower (straight channel) | Conveyor / tower (straight channel), Spindexer (rotating floor) |
| Start X | startX | in | 0 | -200 to 200 |
| Start Y | startY | in | 0 | -200 to 200 |
| Start Z | startZ | in | 4 | 0 to 60 |
| End X | endX | in | 4 | -200 to 200 |
| End Y | endY | in | 0 | -200 to 200 |
| End Z | endZ | in | 16 | 0 to 60 |
| Channel width (inside) | channelWidth | in | 7 | 1 to 30 |
| Center X (forward) | centerX | in | 0 | -200 to 200 |
| Center Y (left) | centerY | in | 0 | -200 to 200 |
| Floor height above ground | floorZ | in | 4 | 0 to 40 |
| Disc diameter | discDia | in | 18 | 4 to 40 |
| Number of paddles | paddles | count | 6 | 2 to 12 |
| Exit direction (0 = front, 90 = left) | exitAngle | deg | 0 | -360 to 360 |
| Wall height | wallHeight | in | 6 | 0.5 to 30 |
| Counts as FUEL storage | storage | boolean | true |  |
| Mass estimate (lb) | massLb | number | 6 | 0 to 100 |
| Include in collision check | collide | boolean | false |  |

## RB Turret

Turntable with a rotation range and a swept body through that range. An RB Shooter can mount on it.

| Field | Parameter | Unit | Default | Range / options |
| --- | --- | --- | --- | --- |
| Name | mechName | string | Turret |  |
| Center X (forward) | centerX | in | 0 | -200 to 200 |
| Center Y (left) | centerY | in | 0 | -200 to 200 |
| Ring bottom above floor | ringZ | in | 8 | 0 to 40 |
| Ring diameter | ringDia | in | 12 | 2 to 30 |
| Ring height | ringHeight | in | 1.5 | 0.25 to 6 |
| Payload length (along the aim) | payloadLength | in | 12 | 1 to 40 |
| Payload width | payloadWidth | in | 12 | 1 to 40 |
| Payload height | payloadHeight | in | 3 | 0.5 to 30 |
| Payload offset forward of center | payloadOffset | in | 0 | -20 to 20 |
| Zero direction (0 = front, 90 = left) | zeroDir | deg | 0 | -360 to 360 |
| Minimum angle | angleMin | deg | -180 | -720 to 720 |
| Maximum angle | angleMax | deg | 180 | -720 to 720 |
| Current angle | angleNow | deg | 0 | -720 to 720 |
| Mass estimate (lb) | massLb | number | 6 | 0 to 100 |
| Include in collision check | collide | boolean | true |  |
| Show motion envelope | showEnvelope | boolean | true |  |
| Envelope samples | samples | count | 7 | 2 to 25 |

## RB Shooter

Flywheel shooter with an adjustable hood (or tilting dual flywheels), exit point and direction, and a simple trajectory to the HUB opening. Mounts on the robot or an RB Turret.

| Field | Parameter | Unit | Default | Range / options |
| --- | --- | --- | --- | --- |
| Name | mechName | string | Shooter |  |
| Mount on | mountOn | choice | Robot | Robot, Turret |
| Turret name | turretName | string | Turret |  |
| Facing (0 = front, 180 = back) | facing | deg | 0 | -360 to 360 |
| Mount X (forward; from turret center if on a turret) | mountX | in | 0 | -200 to 200 |
| Mount Y (left) | mountY | in | 0 | -200 to 200 |
| Mount Z (up; from ring top if on a turret) | mountZ | in | 0 | -20 to 40 |
| Shooter type | shooterType | choice | Flywheel + adjustable hood | Flywheel + adjustable hood, Dual flywheel (tilting) |
| Flywheel diameter | wheelDia | in | 3 | 1 to 10 |
| Flywheel width | wheelWidth | in | 10 | 1 to 30 |
| Flywheel axle above mount | axleHeight | in | 2 | 0 to 20 |
| FUEL compression | compression | in | 0.5 | 0 to 3 |
| Hood wrap around the flywheel | hoodWrap | deg | 90 | 10 to 270 |
| Minimum exit angle | exitMin | deg | 40 | 0 to 90 |
| Maximum exit angle | exitMax | deg | 75 | 0 to 90 |
| Current exit angle | exitNow | deg | 55 | 0 to 90 |
| Show trajectory | showTrajectory | boolean | true |  |
| Distance from robot center to HUB center | targetDist | in | 120 | 12 to 600 |
| Exit speed for the drawn curve (ft/s) | exitSpeed | number | 30 | 1 to 100 |
| Ball speed / wheel surface speed | speedRatio | number | 0.5 | 0.05 to 1 |
| Mass estimate (lb) | massLb | number | 6 | 0 to 100 |
| Include in collision check | collide | boolean | true |  |
| Show motion envelope | showEnvelope | boolean | true |  |
| Envelope samples | samples | count | 7 | 2 to 25 |

## RB Climber

Telescoping hooks for the REBUILT TOWER rungs with level selection. Checks reach, the 30 in height limit, hook size and the rung-to-rung geometry for LEVEL 2 and 3.

| Field | Parameter | Unit | Default | Range / options |
| --- | --- | --- | --- | --- |
| Name | mechName | string | Climber |  |
| Target level | level | choice | Level 1 | Level 1, Level 2, Level 3 |
| Center X (forward) | centerX | in | 0 | -200 to 200 |
| Center Y (left) | centerY | in | 0 | -200 to 200 |
| Hooks open toward | facing | choice | Front | Front, Back, Left, Right |
| Number of hooks | hookCount | count | 2 | 1 to 4 |
| Hook spacing (side to side) | hookSpacing | in | 20 | 0 to 60 |
| Tube bottom above floor | baseZ | in | 4 | 0 to 30 |
| Tube size (square) | tubeSize | in | 1.5 | 0.5 to 4 |
| Hook seat height, retracted | seatHeight | in | 11 | 4 to 30 |
| Stroke (extension) | stroke | in | 18.5 | 0 to 40 |
| Hook throat (opening) | throat | in | 2.25 | 0.5 to 6 |
| Hook lip depth | lipDepth | in | 1.5 | 0.25 to 6 |
| Hook thickness (top of hook above seat) | hookThick | in | 0.5 | 0.1 to 3 |
| Clearance over a rung when reaching | clearance | in | 0.5 | 0 to 3 |
| Lift needed off the floor (TOWER BASE height not verified) | liftNeeded | in | 2 | 0 to 12 |
| Has static hooks to hold a rung while reaching | staticHooks | boolean | false |  |
| Pose to draw | pose | choice | Retracted | Retracted, Extended |
| Deploys | group | choice | Out at the same time as other mechanisms | Out at the same time as other mechanisms, Only out when the others are stowed |
| Mass estimate (lb) | massLb | number | 6 | 0 to 100 |
| Include in collision check | collide | boolean | true |  |
| Show motion envelope | showEnvelope | boolean | true |  |
| Envelope samples | samples | count | 7 | 2 to 25 |

## RB Field Reference

See-through REBUILT TRENCH, BUMP, HUB or TOWER placed near the robot for checking clearance by eye.

| Field | Parameter | Unit | Default | Range / options |
| --- | --- | --- | --- | --- |
| Field element | element | choice | TRENCH | TRENCH, BUMP, HUB, TOWER |
| Center X (forward of robot center) | placeX | in | 0 | -600 to 600 |
| Center Y (left of robot center) | placeY | in | 0 | -600 to 600 |
| Rotation (about vertical) | rotation | deg | 0 | -360 to 360 |

## RB Robot Check

Put this LAST. Checks starting size, height, the 12 in one-direction extension rule, TRENCH clearance, collisions, FUEL capacity, mass and CG for the whole REBUILT robot.

| Field | Parameter | Unit | Default | Range / options |
| --- | --- | --- | --- | --- |
| TRENCH safety margin | trenchMargin | in | 0.5 | 0 to 6 |
| Check collisions between mechanisms | checkCollisions | boolean | true |  |
| Check collisions with frame, bumpers and modules | checkRobot | boolean | true |  |
| Include a full FUEL load in the CG | fuelInCg | boolean | true |  |
| Show red problem bodies | showProblems | boolean | true |  |
| Show starting box and TRENCH plane | showLimits | boolean | true |  |
| Show CG ball | showCg | boolean | true |  |
