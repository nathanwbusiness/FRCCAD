FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

/*
 * REBUILT 2026 Block CAD library
 * ===========================================================================
 * A game-specific Onshape custom feature library for the 2026 FRC game
 * REBUILT presented by Haas. Build a REBUILT robot out of blocks in minutes,
 * then let RB Robot Check tell you what breaks the rules.
 *
 * Features (add them to a Part Studio in this order):
 *
 *   RB Robot              frame, bumpers, swerve modules, battery, game limits
 *   RB Intake             over-bumper pivot, four-bar or slide-out intake
 *   RB Hopper             fixed hopper with a FUEL capacity estimate
 *   RB Expanding Hopper   hopper whose walls telescope out one side or up
 *   RB Indexer            conveyor channel or spindexer from hopper to shooter
 *   RB Turret             turntable with a rotation range
 *   RB Shooter            flywheel(s), adjustable hood, exit vector, trajectory
 *   RB Climber            telescoping hooks for the TOWER rungs, level checks
 *   RB Field Reference    TRENCH, BUMP, HUB or TOWER reference body
 *   RB Robot Check        LAST: every rule check, CG, mass and FUEL capacity
 *
 * Robot coordinates (every feature uses them):
 *   origin = floor, centered under the frame
 *   +X = robot front, +Y = robot left, +Z = up
 *
 * How the features talk to each other:
 *   RB Robot tags its frame with an attribute holding the frame size and the
 *   game limits. Every mechanism feature reads it, and stores its own record
 *   (stowed box, deployed box, swept box, mass, CG, capacity) on a point
 *   body. RB Robot Check reads all of the records. No selections needed.
 *
 * TO ADAPT THIS FOR 2027: edit the GAME map just below (limits, game piece,
 * field elements), rename the features, and check every place marked
 * "GAME-SPECIFIC". The mechanism code does not care which game it is.
 *
 * Rule sources: 2026 Game Manual, version TU22 (Apr 21, 2026). Values marked
 * "unverified" came from an unofficial copy of Section 8 and should be
 * checked against the official PDF.
 */

// ===========================================================================
// GAME CONSTANTS (GAME-SPECIFIC): the only place 2026 numbers live
// ===========================================================================

export const GAME = {
        "name" : "REBUILT 2026",

        // ---- ROBOT size (Section 8) ----
        "maxPerimeter" : 110 * inch,        // R104, unverified
        "maxStartHeight" : 30 * inch,       // R104, unverified
        "maxHeight" : 30 * inch,            // R107 (30 in confirmed in TU06)
        "extensionLimit" : 12 * inch,       // R105 (12 in confirmed in TU06)
        "extensionSidesAtOnce" : 1,         // R106: one side of the ROBOT at a time
        "maxWeightLb" : 115,                // R103, without BUMPERS and battery, unverified
        "maxWeightWithBumpersLb" : 135,     // R408, unverified

        // ---- BUMPERS (Section 8.4) ----
        "bumperZoneBottom" : 2.5 * inch,    // R405, unverified
        "bumperZoneTop" : 5.75 * inch,      // R405, unverified
        "bumperMaxDepth" : 4 * inch,        // R403, unverified
        "bumperMinHeight" : 4.5 * inch,     // R402 backing height, unverified
        "bumperCornerCover" : 5 * inch,     // R401: covered perimeter each side of each corner when there is a big gap

        // ---- Game piece (Section 5.10.1) ----
        "pieceName" : "FUEL",
        "pieceDia" : 5.91 * inch,           // official
        "pieceMassLb" : 0.474,              // official range 0.448 to 0.500 lb, midpoint
        "preloadMax" : 8,                   // 6.3.4

        // ---- TRENCH (5.6) ----
        "trenchClearHeight" : 22.25 * inch,
        "trenchClearWidth" : 50.34 * inch,
        "trenchWidth" : 65.65 * inch,
        "trenchDepth" : 47.0 * inch,
        "trenchHeight" : 40.25 * inch,

        // ---- BUMP (5.5) ----
        "bumpWidth" : 73.0 * inch,
        "bumpDepth" : 44.4 * inch,
        "bumpHeight" : 6.513 * inch,

        // ---- HUB (5.4) ----
        "hubSize" : 47 * inch,
        "hubOpeningHeight" : 72 * inch,     // front edge of the opening
        "hubOpeningAcross" : 41.7 * inch,   // hexagon; assumed across flats (unverified)

        // ---- TOWER (5.8) ----
        "towerWidth" : 49.25 * inch,
        "towerDepth" : 45.0 * inch,
        "towerBaseWidth" : 39.0 * inch,
        "towerBaseDepth" : 45.18 * inch,
        "towerBaseThick" : 1.0 * inch,      // not in the manual text: placeholder
        "uprightHeight" : 72.1 * inch,
        "uprightDepth" : 3.5 * inch,
        "uprightThick" : 1.5 * inch,
        "uprightSpacing" : 32.25 * inch,    // assumed center to center
        "rungOD" : 1.66 * inch,             // 1-1/4 in Sch 40 pipe
        "rungHeights" : [27.0 * inch, 45.0 * inch, 63.0 * inch],   // LOW, MID, HIGH centers
        "rungOverhang" : 5.875 * inch       // past each UPRIGHT face
    };

// ===========================================================================
// Library constants
// ===========================================================================

const RB = "rebuilt2026";            // attribute name on every body this library makes
const TOL = 0.01 * inch;             // tolerance for limit checks
const MAX_SAMPLES = 25;              // cap on sampled poses per mechanism
const PIECE_VOLUME = (4 / 3) * PI * (GAME.pieceDia / 2) * (GAME.pieceDia / 2) * (GAME.pieceDia / 2);

const COLOR_FRAME = color(0.55, 0.57, 0.60);
const COLOR_DRIVE = color(0.25, 0.25, 0.28);
const COLOR_WHEEL = color(0.10, 0.10, 0.10);
const COLOR_BATTERY = color(0.15, 0.15, 0.15);
const COLOR_BUMPER_RED = color(0.80, 0.12, 0.12);
const COLOR_BUMPER_BLUE = color(0.12, 0.30, 0.80);
const COLOR_INTAKE = color(0.15, 0.70, 0.55);
const COLOR_HOPPER = color(0.85, 0.75, 0.55);
const COLOR_INDEXER = color(0.60, 0.45, 0.85);
const COLOR_TURRET = color(0.95, 0.55, 0.10);
const COLOR_SHOOTER = color(0.20, 0.55, 0.85);
const COLOR_CLIMBER = color(0.75, 0.20, 0.55);
const COLOR_FUEL = color(1.0, 0.85, 0.10, 0.55);
const COLOR_FIELD = color(0.55, 0.55, 0.55, 0.25);
const COLOR_START_BOX = color(0.95, 0.85, 0.20, 0.08);
const COLOR_TRENCH_PLANE = color(1.0, 0.50, 0.0, 0.25);
const COLOR_VIOLATION = color(1.0, 0.0, 0.0, 0.6);
const COLOR_CG = color(0.0, 0.0, 0.0);
const ENVELOPE_ALPHA = 0.15;
const GHOST_ALPHA = 0.30;

// ===========================================================================
// Enums (dropdowns in the feature dialogs)
// ===========================================================================

export enum RBSide
{
    annotation { "Name" : "Front" }
    FRONT,
    annotation { "Name" : "Back" }
    BACK,
    annotation { "Name" : "Left" }
    LEFT,
    annotation { "Name" : "Right" }
    RIGHT
}

export enum RBGapSide
{
    annotation { "Name" : "No gap" }
    NONE,
    annotation { "Name" : "Front" }
    FRONT,
    annotation { "Name" : "Back" }
    BACK,
    annotation { "Name" : "Left" }
    LEFT,
    annotation { "Name" : "Right" }
    RIGHT
}

export enum RBBumperColor
{
    annotation { "Name" : "Red" }
    RED,
    annotation { "Name" : "Blue" }
    BLUE
}

export enum RBModulePreset
{
    annotation { "Name" : "SDS MK4i / MK4n (approx.)" }
    SDS_MK4I,
    annotation { "Name" : "WCP X2i (approx.)" }
    WCP_X2I,
    annotation { "Name" : "Custom" }
    CUSTOM
}

/** Which pose a mechanism is drawn in. The other pose is shown see-through. */
export enum RBPose
{
    annotation { "Name" : "Stowed" }
    STOWED,
    annotation { "Name" : "Deployed" }
    DEPLOYED,
    annotation { "Name" : "In between" }
    PARTIAL
}

/**
 * How a mechanism's extension is counted for the one-direction rule (R106).
 * MAIN mechanisms can all be out at the same time (intake + expanding hopper);
 * ALONE mechanisms are only out when the others are stowed (an endgame climber).
 */
export enum RBDeployGroup
{
    annotation { "Name" : "Out at the same time as other mechanisms" }
    MAIN,
    annotation { "Name" : "Only out when the others are stowed" }
    ALONE
}

export enum RBIntakeType
{
    annotation { "Name" : "Over bumper (pivot)" }
    PIVOT,
    annotation { "Name" : "Four-bar" }
    FOUR_BAR,
    annotation { "Name" : "Linear slide-out" }
    SLIDE
}

export enum RBExpandDir
{
    annotation { "Name" : "Front" }
    FRONT,
    annotation { "Name" : "Back" }
    BACK,
    annotation { "Name" : "Left" }
    LEFT,
    annotation { "Name" : "Right" }
    RIGHT,
    annotation { "Name" : "Up" }
    UP
}

export enum RBIndexerType
{
    annotation { "Name" : "Conveyor / tower (straight channel)" }
    CONVEYOR,
    annotation { "Name" : "Spindexer (rotating floor)" }
    SPINDEXER
}

export enum RBShooterMount
{
    annotation { "Name" : "Robot" }
    ROBOT,
    annotation { "Name" : "Turret" }
    TURRET
}

export enum RBShooterType
{
    annotation { "Name" : "Flywheel + adjustable hood" }
    HOODED,
    annotation { "Name" : "Dual flywheel (tilting)" }
    DUAL
}

export enum RBClimbLevel
{
    annotation { "Name" : "Level 1" }
    L1,
    annotation { "Name" : "Level 2" }
    L2,
    annotation { "Name" : "Level 3" }
    L3
}

export enum RBClimberPose
{
    annotation { "Name" : "Retracted" }
    RETRACTED,
    annotation { "Name" : "Extended" }
    EXTENDED
}

export enum RBFieldElement
{
    annotation { "Name" : "TRENCH" }
    TRENCH,
    annotation { "Name" : "BUMP" }
    BUMP,
    annotation { "Name" : "HUB" }
    HUB,
    annotation { "Name" : "TOWER" }
    TOWER
}

// ===========================================================================
// Small helpers
// ===========================================================================

function fmtIn(v is ValueWithUnits) returns string
{
    return "" ~ roundToPrecision(v / inch, 1) ~ " in";
}

function fmtDeg(v is ValueWithUnits) returns string
{
    return "" ~ roundToPrecision(v / degree, 0) ~ " deg";
}

function fmtNum(v is number, digits is number) returns string
{
    return "" ~ roundToPrecision(v, digits);
}

function joinStrings(parts is array, separator is string) returns string
{
    var out = "";
    for (var i = 0; i < size(parts); i += 1)
    {
        if (i > 0)
            out = out ~ separator;
        out = out ~ parts[i];
    }
    return out;
}

function sortedCorners(c1 is Vector, c2 is Vector) returns map
{
    return {
            "lo" : vector(min(c1[0], c2[0]), min(c1[1], c2[1]), min(c1[2], c2[2])),
            "hi" : vector(max(c1[0], c2[0]), max(c1[1], c2[1]), max(c1[2], c2[2]))
        };
}

/** Axis-aligned box between two corners. Returns a query for the new body. */
function makeBox(context is Context, id is Id, c1 is Vector, c2 is Vector) returns Query
{
    const b = sortedCorners(c1, c2);
    for (var d = 0; d < 3; d += 1)
    {
        if (b.hi[d] - b.lo[d] < 0.001 * inch)
            throw regenError("A block has zero size in one direction. Check the dimensions.");
    }
    fCuboid(context, id, { "corner1" : b.lo, "corner2" : b.hi });
    return qCreatedBy(id, EntityType.BODY);
}

/** Cylinder from bottom to top center. Returns a query for the new body. */
function makeCylinder(context is Context, id is Id, bottom is Vector, top is Vector, radius is ValueWithUnits) returns Query
{
    fCylinder(context, id, { "bottomCenter" : bottom, "topCenter" : top, "radius" : radius });
    return qCreatedBy(id, EntityType.BODY);
}

function style(context is Context, bodies is Query, name is string, col is Color)
{
    setProperty(context, { "entities" : bodies, "propertyType" : PropertyType.NAME, "value" : name });
    setProperty(context, { "entities" : bodies, "propertyType" : PropertyType.APPEARANCE, "value" : col });
}

/** Reference bodies (envelopes, ghosts, field, limits) stay out of the BOM. */
function styleReference(context is Context, bodies is Query, name is string, col is Color)
{
    style(context, bodies, name, col);
    try silent
    {
        setProperty(context, { "entities" : bodies, "propertyType" : PropertyType.EXCLUDE_FROM_BOM, "value" : true });
    }
}

function tag(context is Context, bodies is Query, data is map)
{
    setAttribute(context, { "entities" : bodies, "name" : RB, "attribute" : data });
}

function withAlpha(c is Color, alpha is number) returns Color
{
    return color(c.red, c.green, c.blue, alpha);
}

function transformToCSys(t is Transform) returns CoordSystem
{
    return coordSystem(t * WORLD_ORIGIN, t.linear * X_DIRECTION, t.linear * Z_DIRECTION);
}

/** Rotation about a horizontal Y-direction axis through `point`. Positive angle tips +X up toward +Z. */
function pitchUp(point is Vector, angle is ValueWithUnits) returns Transform
{
    return rotationAround(line(point, Y_DIRECTION), -angle);
}

/** Rotation about a vertical axis through `point`. Positive angle turns +X toward +Y (counterclockwise from above). */
function yawAbout(point is Vector, angle is ValueWithUnits) returns Transform
{
    return rotationAround(line(point, Z_DIRECTION), angle);
}

/** A mate connector owned by `owner`, with a name when Onshape allows it. */
function addConnector(context is Context, id is Id, t is Transform, owner is Query, name is string)
{
    opMateConnector(context, id, { "coordSystem" : transformToCSys(t), "owner" : owner });
    try silent
    {
        setProperty(context, { "entities" : qCreatedBy(id, EntityType.BODY), "propertyType" : PropertyType.NAME, "value" : name });
    }
}

// ---- simple boxes stored as { "lo" : Vector, "hi" : Vector } ----

function bxFromPoints(points is array) returns map
{
    const b = box3d(points);
    return { "lo" : b.minCorner, "hi" : b.maxCorner };
}

function bxUnion(a, b)
{
    if (a == undefined)
        return b;
    if (b == undefined)
        return a;
    return {
            "lo" : vector(min(a.lo[0], b.lo[0]), min(a.lo[1], b.lo[1]), min(a.lo[2], b.lo[2])),
            "hi" : vector(max(a.hi[0], b.hi[0]), max(a.hi[1], b.hi[1]), max(a.hi[2], b.hi[2]))
        };
}

/** Corner points of a box or cylinder element, used for fast bounding boxes. */
function elementCorners(el is map) returns array
{
    var pts = [];
    if (el.kind == "box")
    {
        const b = sortedCorners(el.c1, el.c2);
        for (var i = 0; i < 2; i += 1)
            for (var j = 0; j < 2; j += 1)
                for (var k = 0; k < 2; k += 1)
                    pts = append(pts, vector(i == 0 ? b.lo[0] : b.hi[0],
                                             j == 0 ? b.lo[1] : b.hi[1],
                                             k == 0 ? b.lo[2] : b.hi[2]));
    }
    else
    {
        // square prism around the cylinder: slightly conservative
        const axis = normalize(el.top - el.bottom);
        const u = normalize(perpendicularVector(axis));
        const v = cross(axis, u);
        for (var endPt in [el.bottom, el.top])
            for (var su in [-1, 1])
                for (var sv in [-1, 1])
                    pts = append(pts, endPt + el.radius * (su * u + sv * v));
    }
    return pts;
}

function elementCenter(el is map) returns Vector
{
    if (el.kind == "box")
        return (el.c1 + el.c2) / 2;
    return (el.bottom + el.top) / 2;
}

/** Volume in cubic inches (a plain number), used to weight the CG. */
function elementVolume(el is map) returns number
{
    if (el.kind == "box")
    {
        const d = el.c2 - el.c1;
        return abs(d[0] / inch) * abs(d[1] / inch) * abs(d[2] / inch);
    }
    const r = el.radius / inch;
    return PI * r * r * (norm(el.top - el.bottom) / inch);
}

function linearSamples(lo, hi, n is number) returns array
{
    var out = [];
    for (var k = 0; k < n; k += 1)
        out = append(out, lo + (hi - lo) * k / (n - 1));
    return out;
}

/** Pose value (0 = stowed, 1 = deployed) from a pose choice. */
function poseValue(pose is RBPose, partial) returns number
{
    if (pose == RBPose.STOWED)
        return 0;
    if (pose == RBPose.DEPLOYED)
        return 1;
    return partial;
}

/** FUEL that fit in a volume (cubic inches) at a packing factor. */
function fuelCapacity(volumeIn3 is number, packing is number) returns number
{
    if (volumeIn3 <= 0)
        return 0;
    return floor(volumeIn3 * packing / (PIECE_VOLUME / (inch * inch * inch)));
}

/** Frame edge midpoint for a side, and the yaw that points local +X outward. */
function sideFrame(robot is map, side is RBSide) returns map
{
    if (side == RBSide.FRONT)
        return { "origin" : vector(robot.length / 2, 0 * inch, 0 * inch), "yaw" : 0 * degree, "span" : robot.width, "name" : "front" };
    if (side == RBSide.BACK)
        return { "origin" : vector(-robot.length / 2, 0 * inch, 0 * inch), "yaw" : 180 * degree, "span" : robot.width, "name" : "back" };
    if (side == RBSide.LEFT)
        return { "origin" : vector(0 * inch, robot.width / 2, 0 * inch), "yaw" : 90 * degree, "span" : robot.length, "name" : "left" };
    return { "origin" : vector(0 * inch, -robot.width / 2, 0 * inch), "yaw" : -90 * degree, "span" : robot.length, "name" : "right" };
}

/** Transform from a side's local frame (origin at frame edge on the floor, +X outward) to robot coordinates. */
function sideTransform(robot is map, side is RBSide) returns Transform
{
    const f = sideFrame(robot, side);
    return transform(f.origin) * yawAbout(WORLD_ORIGIN, f.yaw);
}

// ===========================================================================
// Lookups of earlier features
// ===========================================================================

function getRobot(context is Context)
{
    const hits = evaluateQuery(context, qHasAttributeWithValueMatching(RB, { "type" : "robot" }));
    if (size(hits) == 0)
        return undefined;
    return getAttribute(context, { "entity" : hits[size(hits) - 1], "name" : RB });
}

function requireRobot(context is Context, featureName is string) returns map
{
    const robot = getRobot(context);
    if (robot == undefined)
        throw regenError("Add RB Robot before " ~ featureName ~ ".");
    return robot;
}

/** Every mechanism record, in feature order. */
function getMechRecords(context is Context) returns array
{
    var out = [];
    for (var hit in evaluateQuery(context, qHasAttributeWithValueMatching(RB, { "type" : "record" })))
        out = append(out, getAttribute(context, { "entity" : hit, "name" : RB }));
    return out;
}

function findMech(context is Context, kind is string, name is string)
{
    var found = undefined;
    for (var r in getMechRecords(context))
    {
        if (r.kind == kind && (name == "" || r.name == name))
            found = r;
    }
    return found;
}

/** Stores a mechanism's record on a point body at `where`. Records are read by RB Robot Check. */
function storeRecord(context is Context, id is Id, where is Vector, data is map)
{
    opPoint(context, id + "record", { "point" : where });
    var rec = data;
    rec["type"] = "record";
    const point = qCreatedBy(id + "record", EntityType.BODY);
    tag(context, point, rec);
    try silent
    {
        setProperty(context, { "entities" : point, "propertyType" : PropertyType.NAME, "value" : "RECORD - " ~ data.name });
    }
}

// ===========================================================================
// Mechanism builder (shared by every moving feature)
// ===========================================================================

/**
 * Builds a mechanism from a description and returns its boxes and CG.
 *
 *   spec.name         display name ("Intake")
 *   spec.color        Color for the solid parts
 *   spec.elements     array of { label, kind : "box" | "cyl", c1, c2 | bottom, top, radius, fixed }
 *                     in the mechanism's own coordinates
 *   spec.poseAt       function(p) -> array of Transforms (to robot coordinates), one per element
 *   spec.samples      array of p values swept for the envelope
 *   spec.pStow        p for the stowed / starting pose
 *   spec.pDeploy      p for the deployed pose
 *   spec.pShow        p for the pose drawn solid
 *   spec.pGhost       p for a see-through copy (or undefined for none)
 *   spec.showEnvelope draw the translucent swept envelope
 *
 * Parts are tagged { type : "part", mech }, envelope bodies { type : "envelope", mech }.
 */
function buildMech(context is Context, id is Id, spec is map) returns map
{
    const showPose = spec.poseAt(spec.pShow);
    var parts = [];
    var envIds = [];

    for (var e = 0; e < size(spec.elements); e += 1)
    {
        const el = spec.elements[e];
        const elId = id + ("el" ~ e);
        var body;
        if (el.kind == "box")
            body = makeBox(context, elId, el.c1, el.c2);
        else
            body = makeCylinder(context, elId, el.bottom, el.top, el.radius);

        // envelope copies, made before the original moves to its shown pose
        if (spec.showEnvelope)
        {
            var transforms = [];
            var names = [];
            if (el.fixed == true)
            {
                transforms = [showPose[e]];
                names = ["s0"];
            }
            else
            {
                for (var k = 0; k < size(spec.samples); k += 1)
                {
                    transforms = append(transforms, spec.poseAt(spec.samples[k])[e]);
                    names = append(names, "s" ~ k);
                }
            }
            const envId = id + ("env" ~ e);
            opPattern(context, envId, {
                        "entities" : body,
                        "transforms" : transforms,
                        "instanceNames" : names,
                        "copyPropertiesAndAttributes" : false
                    });
            envIds = append(envIds, envId);
        }

        // see-through copy of the other pose (stowed vs deployed)
        if (spec.pGhost != undefined && el.fixed != true)
        {
            opPattern(context, id + "ghost" + ("g" ~ e), {
                        "entities" : body,
                        "transforms" : [spec.poseAt(spec.pGhost)[e]],
                        "instanceNames" : ["g"],
                        "copyPropertiesAndAttributes" : false
                    });
        }

        opTransform(context, id + ("move" ~ e), { "bodies" : body, "transform" : showPose[e] });
        style(context, body, spec.name ~ " - " ~ el.label, spec.color);
        tag(context, body, { "type" : "part", "mech" : spec.name });
        parts = append(parts, body);
    }

    const partQuery = qUnion(parts);
    const ghostQuery = qCreatedBy(id + "ghost", EntityType.BODY);
    if (spec.pGhost != undefined)
    {
        styleReference(context, ghostQuery, spec.name ~ " - other pose", withAlpha(spec.color, GHOST_ALPHA));
        tag(context, ghostQuery, { "type" : "ghost", "mech" : spec.name });
    }

    // merge the envelope copies into one translucent body
    if (spec.showEnvelope)
    {
        var envQueries = [];
        for (var envId in envIds)
            envQueries = append(envQueries, qCreatedBy(envId, EntityType.BODY));
        const envAll = qUnion(envQueries);
        if (evaluateQueryCount(context, envAll) > 1)
        {
            try silent
            {
                opBoolean(context, id + "envUnion", { "tools" : envAll, "operationType" : BooleanOperationType.UNION });
            }
        }
        var envBodies = qBodyType(qCreatedBy(id, EntityType.BODY), BodyType.SOLID);
        envBodies = qSubtraction(qSubtraction(envBodies, partQuery), ghostQuery);
        styleReference(context, envBodies, "ENV - " ~ spec.name, withAlpha(spec.color, ENVELOPE_ALPHA));
        tag(context, envBodies, { "type" : "envelope", "mech" : spec.name });
    }

    // ---- analytic boxes and CG (no geometry queries needed) ----
    const boxAt = function(p)
        {
            const pose = spec.poseAt(p);
            var pts = [];
            for (var e = 0; e < size(spec.elements); e += 1)
                for (var pt in elementCorners(spec.elements[e]))
                    pts = append(pts, pose[e] * pt);
            return bxFromPoints(pts);
        };
    const cgAt = function(p)
        {
            const pose = spec.poseAt(p);
            var sum = vector(0, 0, 0) * inch;
            var total = 0;
            for (var e = 0; e < size(spec.elements); e += 1)
            {
                const vol = elementVolume(spec.elements[e]);
                sum = sum + vol * (pose[e] * elementCenter(spec.elements[e]));
                total += vol;
            }
            return total > 0 ? sum / total : pose[0] * WORLD_ORIGIN;
        };

    var swept = bxUnion(boxAt(spec.pStow), boxAt(spec.pDeploy));
    for (var s in spec.samples)
        swept = bxUnion(swept, boxAt(s));

    return {
            "parts" : partQuery,
            "stow" : boxAt(spec.pStow),
            "deploy" : boxAt(spec.pDeploy),
            "show" : boxAt(spec.pShow),
            "swept" : swept,
            "cg" : cgAt(spec.pShow),
            "boxAt" : boxAt
        };
}

/** Shared dialog fields at the end of every mechanism feature. */
export predicate rbMechCommonPredicate(definition)
{
    annotation { "Name" : "Mass estimate (lb)" }
    isReal(definition.massLb, RB_MASS_BOUNDS);

    annotation { "Name" : "Include in collision check", "Default" : true }
    definition.collide is boolean;

    annotation { "Name" : "Show motion envelope", "Default" : true }
    definition.showEnvelope is boolean;

    annotation { "Name" : "Envelope samples" }
    isInteger(definition.samples, RB_SAMPLE_BOUNDS);
}
