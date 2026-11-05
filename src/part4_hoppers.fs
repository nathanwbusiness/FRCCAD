
// ===========================================================================
// RB Hopper (fixed) and RB Expanding Hopper
// ===========================================================================
//
// FUEL capacity estimate:
//   capacity = usable volume x packing factor / volume of one FUEL
// One FUEL (5.91 in sphere) is about 108 cubic inches. Randomly poured balls
// pack at roughly 0.55 to 0.64; 0.5 is a safer default for a real hopper with
// corners, rollers and wires. Subtract the volume taken by anything inside
// (an indexer, a turret base) with "Volume taken by other parts".
//
// RB Hopper makes a floor, four walls, a see-through FUEL VOLUME body, an
// optional preview of FUEL balls on a simple grid, and HOPPER - <name>.
// RB Expanding Hopper adds telescoping sections that slide out one side (or
// up) and reports capacity stowed and expanded. Its expansion counts toward
// the 12 in limit and the one-direction rule, including when it is full of
// FUEL (G413, Team Update 13).

const WALL = 0.25 * inch;   // drawn wall thickness

/** Floor + walls of an open-top box, as elements. `skip` = which walls to leave out ("+x", "-x", "+y", "-y"). */
function hopperShell(lo is Vector, hi is Vector, skip is array, label is string) returns array
{
    var els = [];
    els = append(els, { "label" : label ~ " floor", "kind" : "box", "fixed" : true,
                "c1" : vector(lo[0], lo[1], lo[2] - WALL), "c2" : vector(hi[0], hi[1], lo[2]) });
    if (!isIn("-x", skip))
        els = append(els, { "label" : label ~ " back wall", "kind" : "box", "fixed" : true,
                    "c1" : vector(lo[0] - WALL, lo[1] - WALL, lo[2] - WALL), "c2" : vector(lo[0], hi[1] + WALL, hi[2]) });
    if (!isIn("+x", skip))
        els = append(els, { "label" : label ~ " front wall", "kind" : "box", "fixed" : true,
                    "c1" : vector(hi[0], lo[1] - WALL, lo[2] - WALL), "c2" : vector(hi[0] + WALL, hi[1] + WALL, hi[2]) });
    if (!isIn("-y", skip))
        els = append(els, { "label" : label ~ " right wall", "kind" : "box", "fixed" : true,
                    "c1" : vector(lo[0], lo[1] - WALL, lo[2] - WALL), "c2" : vector(hi[0], lo[1], hi[2]) });
    if (!isIn("+y", skip))
        els = append(els, { "label" : label ~ " left wall", "kind" : "box", "fixed" : true,
                    "c1" : vector(lo[0], hi[1], lo[2] - WALL), "c2" : vector(hi[0], hi[1] + WALL, hi[2]) });
    return els;
}

/** See-through volume body showing where FUEL can go (robot coordinates). */
function makeFuelVolume(context is Context, id is Id, lo is Vector, hi is Vector, name is string)
{
    const body = makeBox(context, id, lo, hi);
    styleReference(context, body, "FUEL VOLUME - " ~ name, withAlpha(COLOR_FUEL, 0.12));
    tag(context, body, { "type" : "volume", "mech" : name });
}

/** FUEL balls on a simple grid inside a box (robot coordinates). Visual only. */
function makeFuelPreview(context is Context, id is Id, lo is Vector, hi is Vector, name is string, maxCount is number) returns number
{
    const d = GAME.pieceDia;
    const nx = floor((hi[0] - lo[0]) / d);
    const ny = floor((hi[1] - lo[1]) / d);
    const nz = floor((hi[2] - lo[2]) / d);
    var count = 0;
    for (var k = 0; k < nz; k += 1)
        for (var j = 0; j < ny; j += 1)
            for (var i = 0; i < nx; i += 1)
            {
                if (count >= maxCount)
                    continue;
                const c = lo + vector((i + 0.5) * d, (j + 0.5) * d, (k + 0.5) * d);
                opSphere(context, id + ("ball" ~ count), { "center" : c, "radius" : d / 2 });
                count += 1;
            }
    if (count > 0)
    {
        const balls = qCreatedBy(id, EntityType.BODY);
        styleReference(context, balls, GAME.pieceName ~ " - " ~ name ~ " preview", COLOR_FUEL);
        tag(context, balls, { "type" : "piece" });
    }
    return nx * ny * nz;
}

export predicate rbHopperBoxPredicate(definition)
{
    annotation { "Name" : "Center X (forward)" }
    isLength(definition.centerX, RB_POS_BOUNDS);

    annotation { "Name" : "Center Y (left)" }
    isLength(definition.centerY, RB_POS_BOUNDS);

    annotation { "Name" : "Floor height above ground" }
    isLength(definition.floorZ, RB_HOPPER_FLOOR_BOUNDS);

    annotation { "Name" : "Inside length (front to back)" }
    isLength(definition.hopperLength, RB_HOPPER_LENGTH_BOUNDS);

    annotation { "Name" : "Inside width (side to side)" }
    isLength(definition.hopperWidth, RB_HOPPER_WIDTH_BOUNDS);

    annotation { "Name" : "Inside height" }
    isLength(definition.hopperHeight, RB_HOPPER_HEIGHT_BOUNDS);

    annotation { "Name" : "Packing factor" }
    isReal(definition.packing, RB_PACKING_BOUNDS);

    annotation { "Name" : "Volume taken by other parts (cubic in)" }
    isReal(definition.lostVolume, RB_LOST_VOLUME_BOUNDS);

    annotation { "Name" : "Show FUEL preview", "Default" : false }
    definition.showFuel is boolean;
}

annotation { "Feature Type Name" : "RB Hopper",
             "Feature Type Description" : "Fixed open-top hopper with a FUEL capacity estimate from the FUEL diameter and a packing factor." }
export const rbHopper = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Name", "Default" : "Hopper" }
        definition.mechName is string;

        rbHopperBoxPredicate(definition);

        rbMechCommonPredicate(definition);
    }
    {
        const robot = requireRobot(context, "RB Hopper");
        const c = vector(definition.centerX, definition.centerY, definition.floorZ);
        const lo = c + vector(-definition.hopperLength / 2, -definition.hopperWidth / 2, 0 * inch);
        const hi = c + vector(definition.hopperLength / 2, definition.hopperWidth / 2, definition.hopperHeight);
        const elements = hopperShell(lo, hi, [], "");

        const mech = buildMech(context, id, {
                    "name" : definition.mechName,
                    "color" : COLOR_HOPPER,
                    "elements" : elements,
                    "poseAt" : function(p)
                        {
                            var out = [];
                            for (var e = 0; e < size(elements); e += 1)
                                out = append(out, identityTransform());
                            return out;
                        },
                    "samples" : [0],
                    "pStow" : 0,
                    "pDeploy" : 0,
                    "pShow" : 0,
                    "pGhost" : undefined,
                    "showEnvelope" : false
                });
        makeFuelVolume(context, id + "volume", lo, hi, definition.mechName);

        const vol = (hi[0] - lo[0]) / inch * (hi[1] - lo[1]) / inch * (hi[2] - lo[2]) / inch - definition.lostVolume;
        const cap = fuelCapacity(vol, definition.packing);
        var gridCount = 0;
        if (definition.showFuel)
            gridCount = makeFuelPreview(context, id + "preview", lo, hi, definition.mechName, 60);

        addConnector(context, id + "mc", transform(c), mech.parts, "HOPPER - " ~ definition.mechName);

        // empty-hopper CG plus a full load of FUEL at the volume center (mass counts full)
        const fuelLb = cap * GAME.pieceMassLb;
        storeRecord(context, id, c, {
                    "name" : definition.mechName,
                    "kind" : "hopper",
                    "parent" : "",
                    "stow" : mech.stow,
                    "deploy" : mech.deploy,
                    "swept" : mech.swept,
                    "trench" : mech.stow,
                    "group" : "none",
                    "collide" : definition.collide,
                    "massLb" : definition.massLb,
                    "cg" : mech.cg,
                    "fuelLb" : fuelLb,
                    "fuelCg" : (lo + hi) / 2,
                    "capStow" : cap,
                    "capDeploy" : cap
                });

        var msg = "Holds about " ~ cap ~ " " ~ GAME.pieceName ~ " (" ~ fmtNum(vol, 0) ~ " cu in at packing " ~ definition.packing ~
            ", " ~ fmtNum(fuelLb, 1) ~ " lb when full).";
        if (definition.showFuel)
            msg = msg ~ " The preview grid fits " ~ gridCount ~ " in a simple stack.";
        if (cap < GAME.preloadMax)
            msg = msg ~ " That is less than the " ~ GAME.preloadMax ~ " " ~ GAME.pieceName ~ " preload.";
        reportFeatureInfo(context, id, msg);
    });

annotation { "Feature Type Name" : "RB Expanding Hopper",
             "Feature Type Description" : "Hopper whose walls telescope out one side or up. Reports FUEL capacity stowed and expanded; the expansion counts toward the extension and one-direction rules." }
export const rbExpandingHopper = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Name", "Default" : "Expanding Hopper" }
        definition.mechName is string;

        rbHopperBoxPredicate(definition);

        annotation { "Name" : "Expands toward" }
        definition.expandDir is RBExpandDir;

        annotation { "Name" : "Expansion distance" }
        isLength(definition.expandDist, RB_EXPAND_BOUNDS);

        annotation { "Name" : "Telescoping sections" }
        isInteger(definition.sections, RB_SECTION_BOUNDS);

        annotation { "Name" : "Pose to draw", "UIHint" : UIHint.HORIZONTAL_ENUM }
        definition.pose is RBPose;

        if (definition.pose == RBPose.PARTIAL)
        {
            annotation { "Name" : "Expanded fraction" }
            isReal(definition.partial, RB_FRACTION_BOUNDS);
        }

        annotation { "Name" : "Deploys" }
        definition.group is RBDeployGroup;

        rbMechCommonPredicate(definition);
    }
    {
        const robot = requireRobot(context, "RB Expanding Hopper");
        const center = vector(definition.centerX, definition.centerY, definition.floorZ);
        const H = definition.hopperHeight;
        const d = definition.expandDist;
        const n = definition.sections;
        const inset = 0.3 * inch;           // nesting gap per section
        const up = definition.expandDir == RBExpandDir.UP;

        // Local frame: origin at the floor center, expansion toward +X (or +Z for UP).
        var yaw = 0 * degree;
        var along = definition.hopperLength;
        var across = definition.hopperWidth;
        if (definition.expandDir == RBExpandDir.BACK)
            yaw = 180 * degree;
        else if (definition.expandDir == RBExpandDir.LEFT)
        {
            yaw = 90 * degree;
            along = definition.hopperWidth;
            across = definition.hopperLength;
        }
        else if (definition.expandDir == RBExpandDir.RIGHT)
        {
            yaw = -90 * degree;
            along = definition.hopperWidth;
            across = definition.hopperLength;
        }
        const toRobot = transform(center) * yawAbout(WORLD_ORIGIN, yaw);

        const lo = vector(-along / 2, -across / 2, 0 * inch);
        const hi = vector(along / 2, across / 2, H);

        // base: open on the expanding side (or full walls for UP)
        var elements = hopperShell(lo, hi, up ? [] : ["+x"], "base");
        const baseCount = size(elements);

        // telescoping sections, each one nested inside the last
        for (var i = 1; i <= n; i += 1)
        {
            const s = i * inset;
            if (up)
            {
                elements = append(elements, { "label" : "section " ~ i ~ " back", "kind" : "box", "section" : i,
                            "c1" : vector(lo[0] + s, lo[1] + s, 0 * inch), "c2" : vector(lo[0] + s + WALL, hi[1] - s, H) });
                elements = append(elements, { "label" : "section " ~ i ~ " front", "kind" : "box", "section" : i,
                            "c1" : vector(hi[0] - s - WALL, lo[1] + s, 0 * inch), "c2" : vector(hi[0] - s, hi[1] - s, H) });
                elements = append(elements, { "label" : "section " ~ i ~ " right", "kind" : "box", "section" : i,
                            "c1" : vector(lo[0] + s + WALL, lo[1] + s, 0 * inch), "c2" : vector(hi[0] - s - WALL, lo[1] + s + WALL, H) });
                elements = append(elements, { "label" : "section " ~ i ~ " left", "kind" : "box", "section" : i,
                            "c1" : vector(lo[0] + s + WALL, hi[1] - s - WALL, 0 * inch), "c2" : vector(hi[0] - s - WALL, hi[1] - s, H) });
            }
            else
            {
                // end wall at the open side, two side walls running back over the base, and a floor plate
                const zt = H - s;
                elements = append(elements, { "label" : "section " ~ i ~ " end wall", "kind" : "box", "section" : i,
                            "c1" : vector(hi[0] - WALL, lo[1] + s, s), "c2" : vector(hi[0], hi[1] - s, zt) });
                elements = append(elements, { "label" : "section " ~ i ~ " right wall", "kind" : "box", "section" : i,
                            "c1" : vector(lo[0] + s + WALL, lo[1] + s, s), "c2" : vector(hi[0] - WALL, lo[1] + s + WALL, zt) });
                elements = append(elements, { "label" : "section " ~ i ~ " left wall", "kind" : "box", "section" : i,
                            "c1" : vector(lo[0] + s + WALL, hi[1] - s - WALL, s), "c2" : vector(hi[0] - WALL, hi[1] - s, zt) });
                elements = append(elements, { "label" : "section " ~ i ~ " floor", "kind" : "box", "section" : i,
                            "c1" : vector(lo[0] + s + WALL, lo[1] + s + WALL, s - WALL * 0.5), "c2" : vector(hi[0] - WALL, hi[1] - s - WALL, s) });
            }
        }

        const moveDir = up ? Z_DIRECTION : X_DIRECTION;
        const poseAt = function(p)
            {
                var out = [];
                for (var e = 0; e < size(elements); e += 1)
                {
                    if (e < baseCount)
                        out = append(out, toRobot);
                    else
                        out = append(out, toRobot * transform(moveDir * d * p * elements[e].section / n));
                }
                return out;
            };

        const pShow = poseValue(definition.pose, definition.partial);
        const mech = buildMech(context, id, {
                    "name" : definition.mechName,
                    "color" : COLOR_HOPPER,
                    "elements" : elements,
                    "poseAt" : poseAt,
                    "samples" : linearSamples(0, 1, min(definition.samples, MAX_SAMPLES)),
                    "pStow" : 0,
                    "pDeploy" : 1,
                    "pShow" : pShow,
                    "pGhost" : pShow < 0.5 ? 1 : 0,
                    "showEnvelope" : definition.showEnvelope
                });

        // FUEL volume at the shown pose (robot coordinates)
        const grow = d * pShow;
        const hiShown = up ? hi + vector(0 * inch, 0 * inch, grow) : hi + vector(grow, 0 * inch, 0 * inch);
        const volCorners = bxFromPoints([toRobot * lo, toRobot * hiShown]);
        makeFuelVolume(context, id + "volume", volCorners.lo, volCorners.hi, definition.mechName);
        if (definition.showFuel)
            makeFuelPreview(context, id + "preview", volCorners.lo, volCorners.hi, definition.mechName, 80);

        const volStow = along / inch * across / inch * H / inch - definition.lostVolume;
        const volExp = up ? along / inch * across / inch * (H + d) / inch - definition.lostVolume
                          : (along + d) / inch * across / inch * H / inch - definition.lostVolume;
        const capStow = fuelCapacity(volStow, definition.packing);
        const capExp = fuelCapacity(volExp, definition.packing);

        addConnector(context, id + "mc", toRobot, mech.parts, "HOPPER - " ~ definition.mechName);

        const fuelLb = capExp * GAME.pieceMassLb;
        storeRecord(context, id, center, {
                    "name" : definition.mechName,
                    "kind" : "expandingHopper",
                    "parent" : "",
                    "stow" : mech.stow,
                    "deploy" : mech.deploy,
                    "swept" : mech.swept,
                    "trench" : mech.stow,
                    "group" : up ? "none" : (definition.group == RBDeployGroup.MAIN ? "main" : "alone"),
                    "collide" : definition.collide,
                    "massLb" : definition.massLb,
                    "cg" : mech.cg,
                    "fuelLb" : fuelLb,
                    "fuelCg" : (volCorners.lo + volCorners.hi) / 2,
                    "capStow" : capStow,
                    "capDeploy" : capExp
                });

        reportFeatureInfo(context, id, "Holds about " ~ capStow ~ " " ~ GAME.pieceName ~ " stowed and " ~ capExp ~ " expanded (packing " ~
                definition.packing ~ "). Full and expanded: about " ~ fmtNum(fuelLb, 1) ~ " lb of " ~ GAME.pieceName ~ ".");
    });
