
// ===========================================================================
// RB Climber: telescoping hooks for the TOWER rungs
// ===========================================================================
//
// REBUILT TOWER (5.8): three RUNGS (1.66 in OD pipe) with centers at 27, 45
// and 63 in, 18 in apart. LEVEL 1 = off the carpet and TOWER BASE; LEVEL 2 =
// BUMPERS completely above the LOW RUNG; LEVEL 3 = BUMPERS completely above
// the MID RUNG (6.5.2).
//
// The catch: the 30 in height limit (R107) applies while climbing. From the
// floor a hook can only reach the LOW RUNG (top at 27.83 in). To go higher the
// robot must climb rung to rung: hang on one rung with static hooks, reach the
// next rung 18 in up, and pull up. This feature checks that geometry. These
// are derived checks, not rule text.
//
// Makes:   outer tubes (fixed), inner tubes and hooks (moving), ENV - <name>,
//          and HOOK - <name> at the hook seat.
// Checks:  reach to the LOW RUNG, height when extended, hook throat vs rung,
//          lift off the floor (LEVEL 1), stroke and bumper height for
//          LEVEL 2 and 3.

annotation { "Feature Type Name" : "RB Climber",
             "Feature Type Description" : "Telescoping hooks for the REBUILT TOWER rungs with level selection. Checks reach, the 30 in height limit, hook size and the rung-to-rung geometry for LEVEL 2 and 3." }
export const rbClimber = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Name", "Default" : "Climber" }
        definition.mechName is string;

        annotation { "Name" : "Target level", "UIHint" : UIHint.HORIZONTAL_ENUM }
        definition.level is RBClimbLevel;

        annotation { "Name" : "Center X (forward)" }
        isLength(definition.centerX, RB_POS_BOUNDS);

        annotation { "Name" : "Center Y (left)" }
        isLength(definition.centerY, RB_POS_BOUNDS);

        annotation { "Name" : "Hooks open toward", "UIHint" : UIHint.HORIZONTAL_ENUM }
        definition.facing is RBSide;

        annotation { "Name" : "Number of hooks" }
        isInteger(definition.hookCount, RB_HOOK_COUNT_BOUNDS);

        annotation { "Name" : "Hook spacing (side to side)" }
        isLength(definition.hookSpacing, RB_HOOK_SPACING_BOUNDS);

        annotation { "Name" : "Tube bottom above floor" }
        isLength(definition.baseZ, RB_CLIMB_BASE_BOUNDS);

        annotation { "Name" : "Tube size (square)" }
        isLength(definition.tubeSize, RB_CLIMB_TUBE_BOUNDS);

        annotation { "Name" : "Hook seat height, retracted" }
        isLength(definition.seatHeight, RB_SEAT_HEIGHT_BOUNDS);

        annotation { "Name" : "Stroke (extension)" }
        isLength(definition.stroke, RB_STROKE_BOUNDS);

        annotation { "Name" : "Hook throat (opening)" }
        isLength(definition.throat, RB_THROAT_BOUNDS);

        annotation { "Name" : "Hook lip depth" }
        isLength(definition.lipDepth, RB_LIP_BOUNDS);

        annotation { "Name" : "Hook thickness (top of hook above seat)" }
        isLength(definition.hookThick, RB_HOOK_THICK_BOUNDS);

        annotation { "Name" : "Clearance over a rung when reaching" }
        isLength(definition.clearance, RB_CLIMB_CLEAR_BOUNDS);

        annotation { "Name" : "Lift needed off the floor (TOWER BASE height not verified)" }
        isLength(definition.liftNeeded, RB_LIFT_BOUNDS);

        annotation { "Name" : "Has static hooks to hold a rung while reaching", "Default" : false }
        definition.staticHooks is boolean;

        annotation { "Name" : "Pose to draw", "UIHint" : UIHint.HORIZONTAL_ENUM }
        definition.pose is RBClimberPose;

        annotation { "Name" : "Deploys" }
        definition.group is RBDeployGroup;

        rbMechCommonPredicate(definition);
    }
    {
        const robot = requireRobot(context, "RB Climber");
        const ts = definition.tubeSize;
        const sr = definition.seatHeight;
        const stroke = definition.stroke;
        const t = definition.hookThick;
        const th = definition.throat;
        const z0 = definition.baseZ;
        if (sr - 2 * inch <= z0)
            throw regenError("Hook seat must be at least 2 in above the tube bottom.", ["seatHeight"]);

        var yaw = 0 * degree;
        if (definition.facing == RBSide.BACK)
            yaw = 180 * degree;
        else if (definition.facing == RBSide.LEFT)
            yaw = 90 * degree;
        else if (definition.facing == RBSide.RIGHT)
            yaw = -90 * degree;
        const toRobot = transform(vector(definition.centerX, definition.centerY, 0 * inch)) * yawAbout(WORLD_ORIGIN, yaw);

        var elements = [];
        const innerLen = min(stroke + 4 * inch, sr - z0);
        for (var k = 0; k < definition.hookCount; k += 1)
        {
            const y = definition.hookCount == 1 ? 0 * inch : -definition.hookSpacing / 2 + definition.hookSpacing * k / (definition.hookCount - 1);
            elements = append(elements, { "label" : "outer tube " ~ (k + 1), "kind" : "box", "fixed" : true,
                        "c1" : vector(-ts / 2, y - ts / 2, z0), "c2" : vector(ts / 2, y + ts / 2, sr - 2 * inch) });
            elements = append(elements, { "label" : "inner tube " ~ (k + 1), "kind" : "box",
                        "c1" : vector(-ts / 2 + 0.125 * inch, y - ts / 2 + 0.125 * inch, sr - innerLen), "c2" : vector(ts / 2 - 0.125 * inch, y + ts / 2 - 0.125 * inch, sr) });
            elements = append(elements, { "label" : "hook top " ~ (k + 1), "kind" : "box",
                        "c1" : vector(-ts / 2, y - 0.5 * inch, sr), "c2" : vector(ts / 2 + th + t, y + 0.5 * inch, sr + t) });
            elements = append(elements, { "label" : "hook lip " ~ (k + 1), "kind" : "box",
                        "c1" : vector(ts / 2 + th, y - 0.5 * inch, sr - definition.lipDepth), "c2" : vector(ts / 2 + th + t, y + 0.5 * inch, sr) });
        }
        const poseAt = function(p)
            {
                var out = [];
                for (var el in elements)
                    out = append(out, el.fixed == true ? toRobot : toRobot * transform(vector(0 * inch, 0 * inch, stroke * p)));
                return out;
            };
        const pShow = definition.pose == RBClimberPose.EXTENDED ? 1 : 0;
        const mech = buildMech(context, id, {
                    "name" : definition.mechName,
                    "color" : COLOR_CLIMBER,
                    "elements" : elements,
                    "poseAt" : poseAt,
                    "samples" : linearSamples(0, 1, min(definition.samples, MAX_SAMPLES)),
                    "pStow" : 0,
                    "pDeploy" : 1,
                    "pShow" : pShow,
                    "pGhost" : pShow == 1 ? 0 : 1,
                    "showEnvelope" : definition.showEnvelope
                });
        addConnector(context, id + "hook", toRobot * transform(vector(ts / 2 + th / 2, 0 * inch, sr + stroke * pShow)), mech.parts, "HOOK - " ~ definition.mechName);

        // ---- climb geometry checks ----
        const rr = GAME.rungOD / 2;
        const rungTop = [GAME.rungHeights[0] + rr, GAME.rungHeights[1] + rr, GAME.rungHeights[2] + rr];
        const spacing = rungTop[1] - rungTop[0];
        const c = definition.clearance;
        const seatExt = sr + stroke;
        const topExt = seatExt + t;
        var problems = [];
        var notes = [];

        if (th < GAME.rungOD + c - TOL)
            problems = append(problems, "Hook throat " ~ fmtIn(th) ~ " is too tight for the " ~ fmtIn(GAME.rungOD) ~ " RUNG plus clearance");
        if (seatExt < rungTop[0] + c - TOL)
            problems = append(problems, "Extended hooks reach " ~ fmtIn(seatExt) ~ "; they need " ~ fmtIn(rungTop[0] + c) ~ " to get over the LOW RUNG");
        if (topExt > robot.limits.maxHeight + TOL)
            problems = append(problems, "Extended climber is " ~ fmtIn(topExt) ~ " tall, over the " ~ fmtIn(robot.limits.maxHeight) ~ " limit (R107)");
        const lift = rungTop[0] - sr;
        if (lift < definition.liftNeeded - TOL)
            problems = append(problems, "Pulled up on the LOW RUNG the robot only lifts " ~ fmtIn(lift) ~ " (need " ~ fmtIn(definition.liftNeeded) ~ " for LEVEL 1)");
        notes = append(notes, "LEVEL 1 lift " ~ fmtIn(lift));

        if (definition.level != RBClimbLevel.L1)
        {
            // rung to rung: hang on rung N with static hooks, reach rung N+1, pull up
            if (!definition.staticHooks)
                problems = append(problems, "LEVEL 2 and 3 need static hooks to hold a RUNG while the climber reaches the next one (the 30 in limit stops a direct reach)");
            if (stroke < spacing + c - TOL)
                problems = append(problems, "Stroke " ~ fmtIn(stroke) ~ " can't reach the next RUNG " ~ fmtIn(spacing) ~ " up (need " ~ fmtIn(spacing + c) ~ ")");
            // hanging from the next rung up, the bumper bottom must clear the rung below
            const bumperClear = (spacing - (sr - robot.bumperBottom));
            if (bumperClear <= TOL)
                problems = append(problems, "Hanging from the next RUNG, the bumpers sit " ~ fmtIn(-bumperClear) ~ " too low to be above the RUNG below (seat is " ~
                        fmtIn(sr - robot.bumperBottom) ~ " above the bumper bottom; must be under " ~ fmtIn(spacing) ~ ")");
            else
                notes = append(notes, "bumpers clear the lower RUNG by " ~ fmtIn(bumperClear));
            const maxSeat = robot.limits.maxHeight - t - spacing - c;
            notes = append(notes, "rung to rung needs the retracted seat at or under " ~ fmtIn(maxSeat) ~ " so the reach stays under " ~ fmtIn(robot.limits.maxHeight));
        }

        storeRecord(context, id, toRobot * vector(0 * inch, 0 * inch, z0), {
                    "name" : definition.mechName,
                    "kind" : "climber",
                    "parent" : "",
                    "stow" : mech.stow,
                    "deploy" : mech.deploy,
                    "swept" : mech.swept,
                    "trench" : mech.stow,
                    "group" : definition.group == RBDeployGroup.MAIN ? "main" : "alone",
                    "collide" : definition.collide,
                    "massLb" : definition.massLb,
                    "cg" : mech.cg,
                    "capStow" : 0,
                    "capDeploy" : 0
                });

        const levelName = definition.level == RBClimbLevel.L1 ? "LEVEL 1" : (definition.level == RBClimbLevel.L2 ? "LEVEL 2" : "LEVEL 3");
        if (size(problems) > 0)
            reportFeatureWarning(context, id, levelName ~ ": " ~ joinStrings(problems, ". ") ~ ".");
        else
            reportFeatureInfo(context, id, levelName ~ " geometry works: " ~ joinStrings(notes, ", ") ~ ". Extended height " ~ fmtIn(topExt) ~ ".");
    });

// ===========================================================================
// RB Field Reference: TRENCH, BUMP, HUB or TOWER (GAME-SPECIFIC)
// ===========================================================================
//
// Lightweight see-through reference bodies, built from the dimensions in the
// GAME map (manual text, not the official field drawings). Place one next to
// the robot to eyeball clearance. The real checks live in RB Robot Check.
// The TRENCH defaults to straddling the robot so you can see the clearance.

/** Extruded closed profile in the robot's XZ plane, extruded along -Y from y = +depth/2. */
function extrudeXZProfile(context is Context, id is Id, points2d is array, depth is ValueWithUnits)
{
    const sketch = newSketchOnPlane(context, id + "sketch", {
                "sketchPlane" : plane(vector(0 * inch, depth / 2, 0 * inch), -Y_DIRECTION, X_DIRECTION)
            });
    skPolyline(sketch, "profile", { "points" : points2d });
    skSolve(sketch);
    opExtrude(context, id + "extrude", {
                "entities" : qSketchRegion(id + "sketch"),
                "direction" : -Y_DIRECTION,
                "endBound" : BoundingType.BLIND,
                "endDepth" : depth
            });
    opDeleteBodies(context, id + "deleteSketch", { "entities" : qCreatedBy(id + "sketch", EntityType.BODY) });
}

annotation { "Feature Type Name" : "RB Field Reference",
             "Feature Type Description" : "See-through REBUILT TRENCH, BUMP, HUB or TOWER placed near the robot for checking clearance by eye." }
export const rbFieldReference = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Field element", "UIHint" : UIHint.HORIZONTAL_ENUM }
        definition.element is RBFieldElement;

        annotation { "Name" : "Center X (forward of robot center)" }
        isLength(definition.placeX, RB_FIELD_POS_BOUNDS);

        annotation { "Name" : "Center Y (left of robot center)" }
        isLength(definition.placeY, RB_FIELD_POS_BOUNDS);

        annotation { "Name" : "Rotation (about vertical)" }
        isAngle(definition.rotation, RB_DIRECTION_BOUNDS);
    }
    {
        var label = "";
        if (definition.element == RBFieldElement.TRENCH)
        {
            // two legs and a top beam; the robot drives along X under the beam
            label = "TRENCH";
            const legW = (GAME.trenchWidth - GAME.trenchClearWidth) / 2;
            const hd = GAME.trenchDepth / 2;
            const cw = GAME.trenchClearWidth / 2;
            makeBox(context, id + "legL", vector(-hd, cw, 0 * inch), vector(hd, cw + legW, GAME.trenchHeight));
            makeBox(context, id + "legR", vector(-hd, -cw - legW, 0 * inch), vector(hd, -cw, GAME.trenchHeight));
            makeBox(context, id + "beam", vector(-hd, -cw, GAME.trenchClearHeight), vector(hd, cw, GAME.trenchHeight));
        }
        else if (definition.element == RBFieldElement.BUMP)
        {
            // Drawn as a ridge. The manual gives 15 degree ramps, but 15 degree ramps up to
            // 6.5 in don't fit in 44.4 in, so this is an approximation.
            label = "BUMP";
            const hd = GAME.bumpDepth / 2;
            extrudeXZProfile(context, id + "bump", [vector(-hd, 0 * inch), vector(0 * inch, GAME.bumpHeight), vector(hd, 0 * inch), vector(-hd, 0 * inch)], GAME.bumpWidth);
        }
        else if (definition.element == RBFieldElement.HUB)
        {
            label = "HUB";
            const hs = GAME.hubSize / 2;
            makeBox(context, id + "body", vector(-hs, -hs, 0 * inch), vector(hs, hs, GAME.hubOpeningHeight - 0.5 * inch));
            const hexR = GAME.hubOpeningAcross / 2 / cos(30 * degree);
            var hex = [];
            for (var k = 0; k <= 6; k += 1)
                hex = append(hex, vector(0 * inch, 0 * inch, GAME.hubOpeningHeight) + hexR * vector(cos(k * 60 * degree), sin(k * 60 * degree), 0));
            opPolyline(context, id + "opening", { "points" : hex });
        }
        else
        {
            label = "TOWER";
            const sp = GAME.uprightSpacing / 2;
            makeBox(context, id + "base", vector(-GAME.towerBaseDepth / 2, -GAME.towerBaseWidth / 2, 0 * inch),
                vector(GAME.towerBaseDepth / 2, GAME.towerBaseWidth / 2, GAME.towerBaseThick));
            for (var s in [-1, 1])
            {
                makeBox(context, id + ("upright" ~ (s > 0 ? "L" : "R")),
                    vector(-GAME.uprightDepth / 2, s * sp - GAME.uprightThick / 2, 0 * inch),
                    vector(GAME.uprightDepth / 2, s * sp + GAME.uprightThick / 2, GAME.uprightHeight));
            }
            const halfLen = sp + GAME.uprightThick / 2 + GAME.rungOverhang;
            const rungNames = ["LOW", "MID", "HIGH"];
            for (var k = 0; k < 3; k += 1)
            {
                const z = GAME.rungHeights[k];
                makeCylinder(context, id + ("rung" ~ rungNames[k]), vector(0 * inch, -halfLen, z), vector(0 * inch, halfLen, z), GAME.rungOD / 2);
            }
        }

        const bodies = qCreatedBy(id, EntityType.BODY);
        opTransform(context, id + "place", {
                    "bodies" : bodies,
                    "transform" : transform(vector(definition.placeX, definition.placeY, 0 * inch)) * yawAbout(WORLD_ORIGIN, definition.rotation)
                });
        styleReference(context, bodies, "FIELD - " ~ label, COLOR_FIELD);
        tag(context, bodies, { "type" : "field", "element" : label });

        if (definition.element == RBFieldElement.TRENCH)
            reportFeatureInfo(context, id, "TRENCH opening " ~ fmtIn(GAME.trenchClearWidth) ~ " wide x " ~ fmtIn(GAME.trenchClearHeight) ~ " tall (manual 5.6).");
        else if (definition.element == RBFieldElement.BUMP)
            reportFeatureInfo(context, id, "BUMP " ~ fmtIn(GAME.bumpHeight) ~ " tall, drawn as a ridge (manual 5.5 says 15 deg ramps).");
        else if (definition.element == RBFieldElement.HUB)
            reportFeatureInfo(context, id, "HUB opening front edge at " ~ fmtIn(GAME.hubOpeningHeight) ~ " (manual 5.4).");
        else
            reportFeatureInfo(context, id, "RUNG centers at 27, 45 and 63 in (manual 5.8). Base thickness is a placeholder.");
    });

// ===========================================================================
// RB Robot Check (put LAST)
// ===========================================================================
//
// Reads RB Robot and every mechanism record, then checks:
//   1. Frame perimeter (R104)
//   2. Starting configuration: everything stowed stays inside the frame
//      perimeter (R102) and under the starting height (R104)
//   3. Height through every mechanism state (R107)
//   4. Horizontal extension: no mechanism past the 12 in limit (R105), no
//      mechanism over more than one side, and the "out at the same time"
//      mechanisms all on ONE side (R106)
//   5. TRENCH: everything stowed (turret and shooter through their whole
//      range) under the TRENCH clearance, minus a margin, and narrow enough
//   6. Collisions between envelopes of turret, hood, intake, climber and any
//      other mechanism marked for the collision check, and with the frame and
//      bumpers
//   7. FUEL capacity, stowed and expanded
//   8. Mass and CG (with and without a full FUEL load) and the weight limits
//      (R103, R408)
// Shows: a yellow starting box, an orange TRENCH clearance plane, red boxes
// where something breaks a limit, red overlap bodies for collisions, and a CG
// ball. The full report prints in the FeatureScript console.

function overhangs(b is map, L is ValueWithUnits, W is ValueWithUnits) returns map
{
    return {
            "front" : b.hi[0] - L / 2,
            "back" : -L / 2 - b.lo[0],
            "left" : b.hi[1] - W / 2,
            "right" : -W / 2 - b.lo[1]
        };
}

/** Sides (in a fixed order) where an overhang map is past the tolerance. */
function sidesOut(o is map) returns array
{
    var out = [];
    for (var s in ["front", "back", "left", "right"])
        if (o[s] > TOL)
            out = append(out, s);
    return out;
}

/** The part of a box beyond a frame side (plus an allowance), or undefined. */
function beyondSide(b is map, side is string, L is ValueWithUnits, W is ValueWithUnits, allow is ValueWithUnits)
{
    var lo = b.lo;
    var hi = b.hi;
    if (side == "front")
        lo[0] = max(lo[0], L / 2 + allow);
    else if (side == "back")
        hi[0] = min(hi[0], -L / 2 - allow);
    else if (side == "left")
        lo[1] = max(lo[1], W / 2 + allow);
    else
        hi[1] = min(hi[1], -W / 2 - allow);
    for (var d = 0; d < 3; d += 1)
        if (hi[d] - lo[d] < 0.05 * inch)
            return undefined;
    return { "lo" : lo, "hi" : hi };
}

function aboveHeight(b is map, z is ValueWithUnits)
{
    if (b.hi[2] - z < 0.05 * inch)
        return undefined;
    return { "lo" : vector(b.lo[0], b.lo[1], max(b.lo[2], z)), "hi" : b.hi };
}

annotation { "Feature Type Name" : "RB Robot Check",
             "Feature Type Description" : "Put this LAST. Checks starting size, height, the 12 in one-direction extension rule, TRENCH clearance, collisions, FUEL capacity, mass and CG for the whole REBUILT robot." }
export const rbRobotCheck = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "TRENCH safety margin" }
        isLength(definition.trenchMargin, RB_TRENCH_MARGIN_BOUNDS);

        annotation { "Name" : "Check collisions between mechanisms", "Default" : true }
        definition.checkCollisions is boolean;

        annotation { "Name" : "Check collisions with frame, bumpers and modules", "Default" : true }
        definition.checkRobot is boolean;

        annotation { "Name" : "Include a full FUEL load in the CG", "Default" : true }
        definition.fuelInCg is boolean;

        annotation { "Name" : "Show red problem bodies", "Default" : true }
        definition.showProblems is boolean;

        annotation { "Name" : "Show starting box and TRENCH plane", "Default" : true }
        definition.showLimits is boolean;

        annotation { "Name" : "Show CG ball", "Default" : true }
        definition.showCg is boolean;
    }
    {
        const robot = requireRobot(context, "RB Robot Check");
        const recs = getMechRecords(context);
        const L = robot.length;
        const W = robot.width;
        const lim = robot.limits;
        var problems = [];
        var report = [];          // every line, pass or fail, for the console
        var red = [];             // boxes to draw in red

        report = append(report, "=== " ~ GAME.name ~ " robot check: " ~ size(recs) ~ " mechanism(s) ===");

        // ---- 1. perimeter ----
        const perimeter = 2 * (L + W);
        if (perimeter > lim.maxPerimeter + TOL)
            problems = append(problems, "Frame perimeter " ~ fmtIn(perimeter) ~ " is over " ~ fmtIn(lim.maxPerimeter) ~ " (R104)");
        report = append(report, "Perimeter: " ~ fmtIn(perimeter) ~ " of " ~ fmtIn(lim.maxPerimeter));

        // ---- 2. starting configuration ----
        var startTop = robot.fixedTop;
        for (var r in recs)
        {
            const o = overhangs(r.stow, L, W);
            for (var s in sidesOut(o))
            {
                problems = append(problems, r.name ~ " sticks out " ~ fmtIn(o[s]) ~ " past the " ~ s ~ " at the start (R102)");
                const rb = beyondSide(r.stow, s, L, W, 0 * inch);
                if (rb != undefined)
                    red = append(red, rb);
            }
            startTop = max(startTop, r.stow.hi[2]);
        }
        if (startTop > lim.maxStartHeight + TOL)
        {
            problems = append(problems, "Starting height " ~ fmtIn(startTop) ~ " is over " ~ fmtIn(lim.maxStartHeight) ~ " (R104)");
            for (var r in recs)
            {
                const ab = aboveHeight(r.stow, lim.maxStartHeight);
                if (ab != undefined)
                    red = append(red, ab);
            }
        }
        report = append(report, "Starting height: " ~ fmtIn(startTop) ~ " of " ~ fmtIn(lim.maxStartHeight));

        // ---- 3. height through every state ----
        var matchTop = robot.fixedTop;
        var tallest = "frame";
        for (var r in recs)
        {
            if (r.swept.hi[2] > matchTop)
            {
                matchTop = r.swept.hi[2];
                tallest = r.name;
            }
            if (r.swept.hi[2] > lim.maxHeight + TOL)
            {
                problems = append(problems, r.name ~ " reaches " ~ fmtIn(r.swept.hi[2]) ~ ", over the " ~ fmtIn(lim.maxHeight) ~ " height limit (R107)");
                const ab = aboveHeight(r.swept, lim.maxHeight);
                if (ab != undefined)
                    red = append(red, ab);
            }
        }
        report = append(report, "Max height in any state: " ~ fmtIn(matchTop) ~ " (" ~ tallest ~ ") of " ~ fmtIn(lim.maxHeight));

        // ---- 4. horizontal extension ----
        var mainSides = {};       // side -> array of mechanism names
        for (var r in recs)
        {
            if (r.group == "none")
                continue;
            const o = overhangs(r.swept, L, W);
            const sides = sidesOut(o);
            for (var s in sides)
            {
                report = append(report, "Extension: " ~ r.name ~ " goes " ~ fmtIn(o[s]) ~ " past the " ~ s ~ " (" ~ r.group ~ ")");
                if (o[s] > lim.extensionLimit + TOL)
                {
                    problems = append(problems, r.name ~ " extends " ~ fmtIn(o[s]) ~ " past the " ~ s ~ ", over the " ~ fmtIn(lim.extensionLimit) ~ " limit (R105)");
                    const rb = beyondSide(r.swept, s, L, W, lim.extensionLimit);
                    if (rb != undefined)
                        red = append(red, rb);
                }
                if (r.group == "main")
                {
                    var names = mainSides[s] == undefined ? [] : mainSides[s];
                    mainSides[s] = append(names, r.name);
                }
            }
            if (size(sides) > GAME.extensionSidesAtOnce)
                problems = append(problems, r.name ~ " by itself extends past " ~ size(sides) ~ " sides (" ~ joinStrings(sides, ", ") ~ "); only one at a time is allowed (R106)");
        }
        var usedSides = [];
        for (var entry in mainSides)
            usedSides = append(usedSides, entry.key ~ ": " ~ joinStrings(entry.value, " + "));
        if (size(usedSides) > GAME.extensionSidesAtOnce)
            problems = append(problems, "Mechanisms that are out at the same time extend in " ~ size(usedSides) ~ " directions (" ~ joinStrings(usedSides, "; ") ~
                    "). R106 allows one direction at a time. Move one to the same side or mark it \"Only out when the others are stowed\"");
        report = append(report, "Directions used by mechanisms out together: " ~ (size(usedSides) == 0 ? "none" : joinStrings(usedSides, "; ")));

        // ---- 5. TRENCH ----
        const trenchLimit = lim.trenchHeight - definition.trenchMargin;
        var trenchTop = robot.fixedTop;
        var trenchName = "frame / bumpers / battery";
        for (var r in recs)
        {
            if (r.trench.hi[2] > trenchTop)
            {
                trenchTop = r.trench.hi[2];
                trenchName = r.name;
            }
            if (r.trench.hi[2] > trenchLimit + TOL)
            {
                problems = append(problems, r.name ~ " is " ~ fmtIn(r.trench.hi[2]) ~ " tall stowed; it must be under " ~ fmtIn(trenchLimit) ~ " to fit under the TRENCH");
                const ab = aboveHeight(r.trench, trenchLimit);
                if (ab != undefined)
                    red = append(red, ab);
            }
        }
        if (robot.fixedTop > trenchLimit + TOL)
            problems = append(problems, "The frame, bumpers or battery alone are taller than the TRENCH limit");
        const narrow = min(L, W) + 2 * robot.bumperDepth;
        if (narrow > GAME.trenchClearWidth + TOL)
            problems = append(problems, "Robot is " ~ fmtIn(narrow) ~ " across its narrow side with bumpers, wider than the " ~ fmtIn(GAME.trenchClearWidth) ~ " TRENCH opening");
        report = append(report, "TRENCH: tallest stowed point " ~ fmtIn(trenchTop) ~ " (" ~ trenchName ~ "); clearance " ~ fmtIn(lim.trenchHeight) ~
                " minus " ~ fmtIn(definition.trenchMargin) ~ " margin leaves " ~ fmtIn(trenchLimit - trenchTop));

        // ---- 6. collisions ----
        var hitPairs = [];
        const solidTypes = [ClashType.INTERFERE, ClashType.TARGET_IN_TOOL, ClashType.TOOL_IN_TARGET];
        var collideRecs = [];
        for (var r in recs)
            if (r.collide)
                collideRecs = append(collideRecs, r);
        const bodiesFor = function(name)
            {
                const env = qHasAttributeWithValueMatching(RB, { "type" : "envelope", "mech" : name });
                if (!isQueryEmpty(context, env))
                    return env;
                return qHasAttributeWithValueMatching(RB, { "type" : "part", "mech" : name });
            };
        if (definition.checkCollisions)
        {
            for (var a = 0; a < size(collideRecs); a += 1)
            {
                for (var b = a + 1; b < size(collideRecs); b += 1)
                {
                    const ra = collideRecs[a];
                    const rb = collideRecs[b];
                    if (ra.parent == rb.name || rb.parent == ra.name)
                        continue;       // a shooter riding on its turret
                    const hits = evCollision(context, { "tools" : bodiesFor(ra.name), "targets" : bodiesFor(rb.name) });
                    var found = false;
                    for (var h in hits)
                    {
                        if (isIn(h["type"], solidTypes))
                        {
                            found = true;
                            hitPairs = append(hitPairs, [h.toolBody, h.targetBody]);
                        }
                    }
                    if (found)
                        problems = append(problems, ra.name ~ " and " ~ rb.name ~ " collide somewhere in their motion");
                }
            }
        }
        if (definition.checkRobot)
        {
            var robotBodies = [];
            for (var part in ["frame", "bumper", "module"])
                robotBodies = append(robotBodies, qHasAttributeWithValueMatching(RB, { "type" : "robotPart", "part" : part }));
            for (var r in collideRecs)
            {
                const hits = evCollision(context, { "tools" : bodiesFor(r.name), "targets" : qUnion(robotBodies) });
                var hitParts = [];
                for (var h in hits)
                {
                    if (isIn(h["type"], solidTypes))
                    {
                        hitPairs = append(hitPairs, [h.toolBody, h.targetBody]);
                        const pd = getAttribute(context, { "entity" : h.targetBody, "name" : RB });
                        if (pd != undefined && !isIn(pd.part, hitParts))
                            hitParts = append(hitParts, pd.part);
                    }
                }
                if (size(hitParts) > 0)
                    problems = append(problems, r.name ~ " hits the robot " ~ joinStrings(hitParts, "/") ~ " somewhere in its motion");
            }
        }
        report = append(report, "Collision check: " ~ size(collideRecs) ~ " mechanism(s) checked, " ~ size(hitPairs) ~ " overlapping body pair(s)");

        // ---- 7. FUEL capacity ----
        var capStow = 0;
        var capDeploy = 0;
        for (var r in recs)
        {
            capStow += r.capStow;
            capDeploy += r.capDeploy;
        }
        report = append(report, GAME.pieceName ~ " capacity: about " ~ capStow ~ " stowed, " ~ capDeploy ~ " expanded (preload limit " ~ GAME.preloadMax ~ ")");

        // ---- 8. mass and CG ----
        var weightLb = 0;          // counts toward R103 (no bumpers, no battery)
        var withBumpersLb = 0;     // R408 (no battery)
        var totalLb = 0;           // everything on the robot, for the CG
        var moment = vector(0, 0, 0) * inch;
        for (var item in robot.massItems)
        {
            totalLb += item.lb;
            moment = moment + item.lb * item.cg;
            if (item.counts)
                weightLb += item.lb;
            if (item.name != "Battery")
                withBumpersLb += item.lb;
        }
        var fuelLb = 0;
        var fuelMoment = vector(0, 0, 0) * inch;
        for (var r in recs)
        {
            totalLb += r.massLb;
            weightLb += r.massLb;
            withBumpersLb += r.massLb;
            moment = moment + r.massLb * r.cg;
            if (r.fuelLb != undefined && r.fuelLb > 0)
            {
                fuelLb += r.fuelLb;
                fuelMoment = fuelMoment + r.fuelLb * r.fuelCg;
            }
        }
        if (weightLb > GAME.maxWeightLb + 1e-6)
            problems = append(problems, "Estimated weight " ~ fmtNum(weightLb, 1) ~ " lb is over " ~ GAME.maxWeightLb ~ " lb (R103)");
        if (withBumpersLb > GAME.maxWeightWithBumpersLb + 1e-6)
            problems = append(problems, "Estimated weight with bumpers " ~ fmtNum(withBumpersLb, 1) ~ " lb is over " ~ GAME.maxWeightWithBumpersLb ~ " lb (R408)");
        var cg = totalLb > 0 ? moment / totalLb : vector(0, 0, 0) * inch;
        var cgLabel = "empty";
        if (definition.fuelInCg && fuelLb > 0)
        {
            cg = (moment + fuelMoment) / (totalLb + fuelLb);
            cgLabel = "with " ~ fmtNum(fuelLb, 1) ~ " lb of " ~ GAME.pieceName;
        }
        const cgEmpty = totalLb > 0 ? moment / totalLb : vector(0, 0, 0) * inch;
        report = append(report, "Weight estimate: " ~ fmtNum(weightLb, 1) ~ " lb (limit " ~ GAME.maxWeightLb ~ "), with bumpers " ~ fmtNum(withBumpersLb, 1) ~
                " lb (limit " ~ GAME.maxWeightWithBumpersLb ~ "), with battery " ~ fmtNum(totalLb, 1) ~ " lb");
        report = append(report, "CG empty: X " ~ fmtIn(cgEmpty[0]) ~ ", Y " ~ fmtIn(cgEmpty[1]) ~ ", Z " ~ fmtIn(cgEmpty[2]));
        report = append(report, "CG " ~ cgLabel ~ ": X " ~ fmtIn(cg[0]) ~ ", Y " ~ fmtIn(cg[1]) ~ ", Z " ~ fmtIn(cg[2]));

        // ---- visuals ----
        if (definition.showLimits)
        {
            const startBody = makeBox(context, id + "startBox", vector(-L / 2, -W / 2, 0 * inch), vector(L / 2, W / 2, lim.maxStartHeight));
            styleReference(context, startBody, "LIMIT - starting configuration", COLOR_START_BOX);
            tag(context, startBody, { "type" : "limit" });
            const tw = max(L, W) / 2 + lim.extensionLimit + 4 * inch;
            const trenchBody = makeBox(context, id + "trenchPlane", vector(-tw, -tw, lim.trenchHeight - 0.05 * inch), vector(tw, tw, lim.trenchHeight));
            styleReference(context, trenchBody, "LIMIT - TRENCH clearance", COLOR_TRENCH_PLANE);
            tag(context, trenchBody, { "type" : "limit" });
        }
        if (definition.showProblems)
        {
            for (var i = 0; i < size(red); i += 1)
            {
                const body = makeBox(context, id + ("red" ~ i), red[i].lo, red[i].hi);
                styleReference(context, body, "PROBLEM - outside a limit", COLOR_VIOLATION);
                tag(context, body, { "type" : "violation" });
            }
            for (var i = 0; i < size(hitPairs); i += 1)
            {
                const copyId = id + ("hitCopy" ~ i);
                opPattern(context, copyId, {
                            "entities" : qUnion(hitPairs[i][0], hitPairs[i][1]),
                            "transforms" : [identityTransform()],
                            "instanceNames" : ["c"],
                            "copyPropertiesAndAttributes" : false
                        });
                const copies = qCreatedBy(copyId, EntityType.BODY);
                try silent
                {
                    opBoolean(context, id + ("hitCut" ~ i), { "tools" : copies, "operationType" : BooleanOperationType.INTERSECTION });
                    const result = qUnion(qCreatedBy(copyId, EntityType.BODY), qCreatedBy(id + ("hitCut" ~ i), EntityType.BODY));
                    styleReference(context, result, "PROBLEM - collision", COLOR_VIOLATION);
                    tag(context, result, { "type" : "violation" });
                }
                catch
                {
                    opDeleteBodies(context, id + ("hitClean" ~ i), { "entities" : copies });
                }
            }
        }
        if (definition.showCg && totalLb > 0)
        {
            opSphere(context, id + "cg", { "center" : cg, "radius" : 1 * inch });
            styleReference(context, qCreatedBy(id + "cg", EntityType.BODY), "CG - " ~ cgLabel, COLOR_CG);
            tag(context, qCreatedBy(id + "cg", EntityType.BODY), { "type" : "cg" });
        }

        // ---- report ----
        for (var p in problems)
            report = append(report, "PROBLEM: " ~ p);
        for (var reportLine in report)
            println(reportLine);
        const summary = "Capacity " ~ capStow ~ "/" ~ capDeploy ~ " " ~ GAME.pieceName ~ ", " ~ fmtNum(weightLb, 0) ~ " lb, CG height " ~ fmtIn(cg[2]) ~
            ", TRENCH margin " ~ fmtIn(trenchLimit - trenchTop) ~ ".";
        if (size(problems) > 0)
            reportFeatureWarning(context, id, size(problems) ~ " problem(s): " ~ joinStrings(problems, "; ") ~ ". " ~ summary);
        else
            reportFeatureInfo(context, id, "All REBUILT checks pass. " ~ summary);
    });
