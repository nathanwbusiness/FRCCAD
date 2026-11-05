
// ===========================================================================
// RB Indexer: conveyor / tower channel, or spindexer
// ===========================================================================
//
// Conveyor: a channel (floor belt + two side walls) from a start point to an
// end point. Use it for a flat conveyor, a ramp, or a vertical tower.
// Spindexer: a rotating floor disc with paddles and a wall, plus an exit.
//
// Makes:   the channel or spindexer, FEED - <name> at the exit (+X along the
//          FUEL's travel), and a FUEL ball at the exit.
// Feeds:   capacity (FUEL held in the channel or on the disc), mass and CG.
//          Not in the collision check by default, because it sits inside the
//          hopper on purpose.

annotation { "Feature Type Name" : "RB Indexer",
             "Feature Type Description" : "Path from hopper to shooter: a straight conveyor or tower channel, or a spindexer (rotating floor)." }
export const rbIndexer = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Name", "Default" : "Indexer" }
        definition.mechName is string;

        annotation { "Name" : "Indexer type" }
        definition.indexerType is RBIndexerType;

        if (definition.indexerType == RBIndexerType.CONVEYOR)
        {
            annotation { "Name" : "Start X" }
            isLength(definition.startX, RB_POS_BOUNDS);
            annotation { "Name" : "Start Y" }
            isLength(definition.startY, RB_POS_BOUNDS);
            annotation { "Name" : "Start Z" }
            isLength(definition.startZ, RB_POS_Z_BOUNDS);
            annotation { "Name" : "End X" }
            isLength(definition.endX, RB_END_X_BOUNDS);
            annotation { "Name" : "End Y" }
            isLength(definition.endY, RB_POS_BOUNDS);
            annotation { "Name" : "End Z" }
            isLength(definition.endZ, RB_END_Z_BOUNDS);
            annotation { "Name" : "Channel width (inside)" }
            isLength(definition.channelWidth, RB_CHANNEL_WIDTH_BOUNDS);
        }
        else
        {
            annotation { "Name" : "Center X (forward)" }
            isLength(definition.centerX, RB_POS_BOUNDS);
            annotation { "Name" : "Center Y (left)" }
            isLength(definition.centerY, RB_POS_BOUNDS);
            annotation { "Name" : "Floor height above ground" }
            isLength(definition.floorZ, RB_HOPPER_FLOOR_BOUNDS);
            annotation { "Name" : "Disc diameter" }
            isLength(definition.discDia, RB_DISC_DIA_BOUNDS);
            annotation { "Name" : "Number of paddles" }
            isInteger(definition.paddles, RB_PADDLE_BOUNDS);
            annotation { "Name" : "Exit direction (0 = front, 90 = left)" }
            isAngle(definition.exitAngle, RB_DIRECTION_BOUNDS);
        }

        annotation { "Name" : "Wall height" }
        isLength(definition.wallHeight, RB_WALL_HEIGHT_BOUNDS);

        annotation { "Name" : "Counts as FUEL storage", "Default" : true }
        definition.storage is boolean;

        annotation { "Name" : "Mass estimate (lb)" }
        isReal(definition.massLb, RB_MASS_BOUNDS);

        annotation { "Name" : "Include in collision check", "Default" : false }
        definition.collide is boolean;
    }
    {
        const robot = requireRobot(context, "RB Indexer");
        const fuelR = GAME.pieceDia / 2;
        const H = definition.wallHeight;
        var elements = [];
        var toRobot;
        var exitT;
        var cap = 0;

        if (definition.indexerType == RBIndexerType.CONVEYOR)
        {
            const s = vector(definition.startX, definition.startY, definition.startZ);
            const e = vector(definition.endX, definition.endY, definition.endZ);
            const len = norm(e - s);
            if (len < 1 * inch)
                throw regenError("Start and end points are too close.", ["endX"]);
            const dir = normalize(e - s);
            // local Z: the part of world up that is square to the channel (world X when the channel is vertical)
            var upRef = Z_DIRECTION;
            if (abs(dot(dir, Z_DIRECTION)) > 0.99)
                upRef = -X_DIRECTION;
            const zAxis = normalize(upRef - dot(upRef, dir) * dir);
            toRobot = toWorld(coordSystem(s, dir, zAxis));
            const cw = definition.channelWidth;
            elements = [
                    { "label" : "belt", "kind" : "box", "c1" : vector(0 * inch, -cw / 2, -0.5 * inch), "c2" : vector(len, cw / 2, 0 * inch) },
                    { "label" : "right wall", "kind" : "box", "c1" : vector(0 * inch, -cw / 2 - WALL, -0.5 * inch), "c2" : vector(len, -cw / 2, H) },
                    { "label" : "left wall", "kind" : "box", "c1" : vector(0 * inch, cw / 2, -0.5 * inch), "c2" : vector(len, cw / 2 + WALL, H) }
                ];
            exitT = toRobot * transform(vector(len, 0 * inch, fuelR));
            // FUEL in single file, plus extra lanes if the channel is wider
            const lanes = max(1, floor(cw / GAME.pieceDia));
            cap = floor(len / GAME.pieceDia) * lanes;
            if (cw < GAME.pieceDia)
                reportFeatureWarning(context, id, "Channel is narrower than a " ~ GAME.pieceName ~ " (" ~ fmtIn(GAME.pieceDia) ~ ").");
        }
        else
        {
            const c = vector(definition.centerX, definition.centerY, definition.floorZ);
            toRobot = transform(c);
            const R = definition.discDia / 2;
            elements = [{ "label" : "disc", "kind" : "cyl", "bottom" : vector(0, 0, -0.5) * inch, "top" : vector(0, 0, 0) * inch, "radius" : R }];
            for (var k = 0; k < definition.paddles; k += 1)
            {
                // paddles are drawn as boxes from the hub out, rotated into place by poseAt below
                elements = append(elements, { "label" : "paddle " ~ (k + 1), "kind" : "box", "paddle" : k,
                            "c1" : vector(1.5 * inch, -0.125 * inch, 0 * inch), "c2" : vector(R - 0.5 * inch, 0.125 * inch, min(H, fuelR)) });
            }
            elements = append(elements, { "label" : "hub", "kind" : "cyl", "bottom" : vector(0, 0, 0) * inch, "top" : vector(0 * inch, 0 * inch, H), "radius" : 1.5 * inch });
            exitT = toRobot * yawAbout(WORLD_ORIGIN, definition.exitAngle) * transform(vector(R + fuelR, 0 * inch, fuelR));
            const discArea = PI * (R / inch) * (R / inch);
            cap = fuelCapacity(discArea * min(H, GAME.pieceDia) / inch, 0.5);
        }

        const np = definition.paddles;
        const mech = buildMech(context, id, {
                    "name" : definition.mechName,
                    "color" : COLOR_INDEXER,
                    "elements" : elements,
                    "poseAt" : function(p)
                        {
                            var out = [];
                            for (var el in elements)
                            {
                                if (el.paddle != undefined)
                                    out = append(out, toRobot * yawAbout(WORLD_ORIGIN, 360 * degree * el.paddle / np));
                                else
                                    out = append(out, toRobot);
                            }
                            return out;
                        },
                    "samples" : [0],
                    "pStow" : 0,
                    "pDeploy" : 0,
                    "pShow" : 0,
                    "pGhost" : undefined,
                    "showEnvelope" : false
                });

        opSphere(context, id + "fuel", { "center" : exitT * WORLD_ORIGIN, "radius" : fuelR });
        styleReference(context, qCreatedBy(id + "fuel", EntityType.BODY), GAME.pieceName ~ " - " ~ definition.mechName ~ " exit", COLOR_FUEL);
        tag(context, qCreatedBy(id + "fuel", EntityType.BODY), { "type" : "piece" });
        addConnector(context, id + "feed", exitT, mech.parts, "FEED - " ~ definition.mechName);

        const storedCap = definition.storage ? cap : 0;
        storeRecord(context, id, toRobot * WORLD_ORIGIN, {
                    "name" : definition.mechName,
                    "kind" : "indexer",
                    "parent" : "",
                    "stow" : mech.stow,
                    "deploy" : mech.deploy,
                    "swept" : mech.swept,
                    "trench" : mech.stow,
                    "group" : "none",
                    "collide" : definition.collide,
                    "massLb" : definition.massLb,
                    "cg" : mech.cg,
                    "fuelLb" : storedCap * GAME.pieceMassLb,
                    "fuelCg" : mech.cg,
                    "capStow" : storedCap,
                    "capDeploy" : storedCap
                });
        reportFeatureInfo(context, id, "Holds about " ~ cap ~ " " ~ GAME.pieceName ~ (definition.storage ? " (counted as storage)." : " (not counted as storage)."));
    });

// ===========================================================================
// RB Turret
// ===========================================================================
//
// A turntable ring plus the "payload" box it carries (the shooter block or
// its frame). Angles are measured from the turret's zero direction, which is
// itself measured from the robot front (0 = front, 90 = left).
//
// Makes:   ring + payload at the current angle, ENV - <name> swept through the
//          whole range, and TURRET - <name> at the ring top (+X = aim).
// Feeds:   RB Shooter can mount on it; RB Robot Check uses the swept envelope
//          for height, TRENCH (the turret turns while driving, so its whole
//          sweep must fit), extension and collisions.

annotation { "Feature Type Name" : "RB Turret",
             "Feature Type Description" : "Turntable with a rotation range and a swept body through that range. An RB Shooter can mount on it." }
export const rbTurret = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Name", "Default" : "Turret" }
        definition.mechName is string;

        annotation { "Name" : "Center X (forward)" }
        isLength(definition.centerX, RB_POS_BOUNDS);

        annotation { "Name" : "Center Y (left)" }
        isLength(definition.centerY, RB_POS_BOUNDS);

        annotation { "Name" : "Ring bottom above floor" }
        isLength(definition.ringZ, RB_RING_Z_BOUNDS);

        annotation { "Name" : "Ring diameter" }
        isLength(definition.ringDia, RB_RING_DIA_BOUNDS);

        annotation { "Name" : "Ring height" }
        isLength(definition.ringHeight, RB_RING_HEIGHT_BOUNDS);

        annotation { "Name" : "Payload length (along the aim)" }
        isLength(definition.payloadLength, RB_PAYLOAD_LENGTH_BOUNDS);

        annotation { "Name" : "Payload width" }
        isLength(definition.payloadWidth, RB_PAYLOAD_WIDTH_BOUNDS);

        annotation { "Name" : "Payload height" }
        isLength(definition.payloadHeight, RB_PAYLOAD_HEIGHT_BOUNDS);

        annotation { "Name" : "Payload offset forward of center" }
        isLength(definition.payloadOffset, RB_PAYLOAD_OFFSET_BOUNDS);

        annotation { "Name" : "Zero direction (0 = front, 90 = left)" }
        isAngle(definition.zeroDir, RB_DIRECTION_BOUNDS);

        annotation { "Name" : "Minimum angle" }
        isAngle(definition.angleMin, RB_TURRET_MIN_BOUNDS);

        annotation { "Name" : "Maximum angle" }
        isAngle(definition.angleMax, RB_TURRET_MAX_BOUNDS);

        annotation { "Name" : "Current angle" }
        isAngle(definition.angleNow, RB_ANGLE_NOW_BOUNDS);

        rbMechCommonPredicate(definition);
    }
    {
        const robot = requireRobot(context, "RB Turret");
        if (definition.angleMax <= definition.angleMin)
            throw regenError("Maximum angle must be greater than minimum angle.", ["angleMax"]);
        if (definition.angleNow < definition.angleMin - 0.01 * degree || definition.angleNow > definition.angleMax + 0.01 * degree)
            throw regenError("Current angle is outside the range.", ["angleNow"]);

        const c = vector(definition.centerX, definition.centerY, definition.ringZ);
        const rh = definition.ringHeight;
        const pl = definition.payloadLength;
        const pw = definition.payloadWidth;
        const off = definition.payloadOffset;
        const elements = [
                { "label" : "ring", "kind" : "cyl", "bottom" : vector(0, 0, 0) * inch, "top" : vector(0 * inch, 0 * inch, rh), "radius" : definition.ringDia / 2 },
                { "label" : "payload", "kind" : "box", "c1" : vector(off - pl / 2, -pw / 2, rh), "c2" : vector(off + pl / 2, pw / 2, rh + definition.payloadHeight) }
            ];
        const zeroDir = definition.zeroDir;
        const turretT = function(a)
            {
                return transform(c) * yawAbout(WORLD_ORIGIN, zeroDir + a);
            };

        // sample the range at least every 30 degrees
        const range = definition.angleMax - definition.angleMin;
        const n = min(MAX_SAMPLES, max(definition.samples, ceil(range / (30 * degree)) + 1));
        const mech = buildMech(context, id, {
                    "name" : definition.mechName,
                    "color" : COLOR_TURRET,
                    "elements" : elements,
                    "poseAt" : function(a)
                        {
                            return [turretT(a), turretT(a)];
                        },
                    "samples" : linearSamples(definition.angleMin, definition.angleMax, n),
                    "pStow" : definition.angleNow,
                    "pDeploy" : definition.angleNow,
                    "pShow" : definition.angleNow,
                    "pGhost" : undefined,
                    "showEnvelope" : definition.showEnvelope
                });

        const topT = turretT(definition.angleNow) * transform(vector(0 * inch, 0 * inch, rh));
        addConnector(context, id + "mc", topT, mech.parts, "TURRET - " ~ definition.mechName);

        storeRecord(context, id, c, {
                    "name" : definition.mechName,
                    "kind" : "turret",
                    "parent" : "",
                    "stow" : mech.stow,           // the turret at its current angle
                    "deploy" : mech.swept,
                    "swept" : mech.swept,
                    "trench" : mech.swept,
                    "group" : "main",
                    "collide" : definition.collide,
                    "massLb" : definition.massLb,
                    "cg" : mech.cg,
                    "capStow" : 0,
                    "capDeploy" : 0,
                    "center" : c,
                    "ringTop" : rh,
                    "zeroDir" : zeroDir,
                    "angleMin" : definition.angleMin,
                    "angleMax" : definition.angleMax,
                    "angleNow" : definition.angleNow
                });

        var msg = "Range " ~ fmtDeg(range) ~ ". Top of payload at " ~ fmtIn(mech.swept.hi[2]) ~ ".";
        if (range < 360 * degree)
            msg = msg ~ " Blind zone of " ~ fmtDeg(360 * degree - range) ~ ".";
        reportFeatureInfo(context, id, msg);
    });

// ===========================================================================
// RB Shooter
// ===========================================================================
//
// Hooded: one flywheel, the FUEL rides around it under a hood. Moving the
//         hood changes the exit angle. The exit point is where the FUEL leaves
//         the flywheel at that angle.
// Dual:   two flywheels stacked with the FUEL between them; the whole shooter
//         tilts to set the exit angle.
// Exit angle is measured up from horizontal along the aim direction.
//
// Mounts on the robot (position in robot coordinates plus a facing
// direction) or on an RB Turret (position from the ring top center, rotating
// with the turret through its whole range).
//
// Makes:   flywheel(s), side plates, hood, ENV - <name> swept over the hood
//          range (and turret range), an EXIT line along the exit direction,
//          an optional trajectory curve, a HUB opening marker at the target
//          distance, and EXIT - <name> (+X = exit direction).
// Reports: exit point, exit height, the exit speed needed to reach the HUB
//          opening at the target distance (no air drag), and a flywheel RPM
//          estimate from a ball-to-wheel speed ratio. Treat these as rough.

/** Speed needed to pass through height dz at horizontal distance dx when launched at angle a. Returns undefined if impossible. */
function speedToHit(dx is ValueWithUnits, dz is ValueWithUnits, a is ValueWithUnits)
{
    const g = 386.09 * inch / (second * second);
    const denom = 2 * cos(a) * cos(a) * (dx * tan(a) - dz);
    if (denom <= 0 * inch)
        return undefined;
    return sqrt(g * dx * dx / denom);
}

annotation { "Feature Type Name" : "RB Shooter",
             "Feature Type Description" : "Flywheel shooter with an adjustable hood (or tilting dual flywheels), exit point and direction, and a simple trajectory to the HUB opening. Mounts on the robot or an RB Turret." }
export const rbShooter = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Name", "Default" : "Shooter" }
        definition.mechName is string;

        annotation { "Name" : "Mount on", "UIHint" : UIHint.HORIZONTAL_ENUM }
        definition.mountOn is RBShooterMount;

        if (definition.mountOn == RBShooterMount.TURRET)
        {
            annotation { "Name" : "Turret name", "Default" : "Turret" }
            definition.turretName is string;
        }
        else
        {
            annotation { "Name" : "Facing (0 = front, 180 = back)" }
            isAngle(definition.facing, RB_DIRECTION_BOUNDS);
        }

        annotation { "Name" : "Mount X (forward; from turret center if on a turret)" }
        isLength(definition.mountX, RB_POS_BOUNDS);

        annotation { "Name" : "Mount Y (left)" }
        isLength(definition.mountY, RB_POS_BOUNDS);

        annotation { "Name" : "Mount Z (up; from ring top if on a turret)" }
        isLength(definition.mountZ, RB_SHOOTER_Z_BOUNDS);

        annotation { "Name" : "Shooter type" }
        definition.shooterType is RBShooterType;

        annotation { "Name" : "Flywheel diameter" }
        isLength(definition.wheelDia, RB_FLYWHEEL_DIA_BOUNDS);

        annotation { "Name" : "Flywheel width" }
        isLength(definition.wheelWidth, RB_FLYWHEEL_WIDTH_BOUNDS);

        annotation { "Name" : "Flywheel axle above mount" }
        isLength(definition.axleHeight, RB_AXLE_HEIGHT_BOUNDS);

        annotation { "Name" : "FUEL compression" }
        isLength(definition.compression, RB_COMPRESSION_BOUNDS);

        if (definition.shooterType == RBShooterType.HOODED)
        {
            annotation { "Name" : "Hood wrap around the flywheel" }
            isAngle(definition.hoodWrap, RB_HOOD_WRAP_BOUNDS);
        }

        annotation { "Name" : "Minimum exit angle" }
        isAngle(definition.exitMin, RB_EXIT_MIN_BOUNDS);

        annotation { "Name" : "Maximum exit angle" }
        isAngle(definition.exitMax, RB_EXIT_MAX_BOUNDS);

        annotation { "Name" : "Current exit angle" }
        isAngle(definition.exitNow, RB_EXIT_NOW_BOUNDS);

        annotation { "Group Name" : "Trajectory", "Collapsed By Default" : true }
        {
            annotation { "Name" : "Show trajectory", "Default" : true }
            definition.showTrajectory is boolean;

            annotation { "Name" : "Distance from robot center to HUB center" }
            isLength(definition.targetDist, RB_TARGET_DIST_BOUNDS);

            annotation { "Name" : "Exit speed for the drawn curve (ft/s)" }
            isReal(definition.exitSpeed, RB_EXIT_SPEED_BOUNDS);

            annotation { "Name" : "Ball speed / wheel surface speed" }
            isReal(definition.speedRatio, RB_SPEED_RATIO_BOUNDS);
        }

        rbMechCommonPredicate(definition);
    }
    {
        const robot = requireRobot(context, "RB Shooter");
        if (definition.exitMax < definition.exitMin)
            throw regenError("Maximum exit angle must not be less than the minimum.", ["exitMax"]);
        if (definition.exitNow < definition.exitMin - 0.01 * degree || definition.exitNow > definition.exitMax + 0.01 * degree)
            throw regenError("Current exit angle is outside the range.", ["exitNow"]);

        // ---- mount: robot, or the turret's ring top ----
        var parentName = "";
        var mountAt;                 // function(turretAngle) -> Transform from shooter frame to robot
        var turretAngles = [0 * degree];
        var turretNow = 0 * degree;
        const local = transform(vector(definition.mountX, definition.mountY, definition.mountZ));
        if (definition.mountOn == RBShooterMount.TURRET)
        {
            const tr = findMech(context, "turret", definition.turretName);
            if (tr == undefined)
                throw regenError("No RB Turret named \"" ~ definition.turretName ~ "\" above this feature.", ["turretName"]);
            parentName = tr.name;
            mountAt = function(a)
                {
                    return transform(tr.center) * yawAbout(WORLD_ORIGIN, tr.zeroDir + a) * transform(vector(0 * inch, 0 * inch, tr.ringTop)) * local;
                };
            turretAngles = linearSamples(tr.angleMin, tr.angleMax, min(9, max(3, ceil((tr.angleMax - tr.angleMin) / (45 * degree)) + 1)));
            turretNow = tr.angleNow;
        }
        else
        {
            const facing = definition.facing;
            mountAt = function(a)
                {
                    return local * yawAbout(WORLD_ORIGIN, facing);
                };
        }

        // ---- geometry in the shooter frame (+X = aim, Z up, origin at the mount) ----
        const R = definition.wheelDia / 2;
        const ww = definition.wheelWidth;
        const fuelR = GAME.pieceDia / 2;
        const comp = definition.compression;
        const axle = vector(0 * inch, 0 * inch, definition.axleHeight);
        const hooded = definition.shooterType == RBShooterType.HOODED;
        const ballPath = R + fuelR - comp / 2;       // FUEL center distance from the axle (hooded)
        var elements = [];
        var exitLocal;                                // function(exitAngle) -> [point, direction] in the shooter frame
        var elementPose;                              // function(exitAngle) -> array of shooter-frame transforms

        if (hooded)
        {
            const rh = R + GAME.pieceDia - comp;      // hood inner radius
            const wrap = definition.hoodWrap;
            const seg = rh * (wrap / radian) / 3;     // length of each of the 3 hood plates
            elements = [
                    { "label" : "flywheel", "kind" : "cyl", "bottom" : axle - vector(0 * inch, ww / 2, 0 * inch), "top" : axle + vector(0 * inch, ww / 2, 0 * inch), "radius" : R },
                    { "label" : "right plate", "kind" : "box", "c1" : vector(-rh - 0.5 * inch, -ww / 2 - 0.75 * inch, 0 * inch), "c2" : vector(rh * 0.6, -ww / 2 - 0.5 * inch, definition.axleHeight + rh) },
                    { "label" : "left plate", "kind" : "box", "c1" : vector(-rh - 0.5 * inch, ww / 2 + 0.5 * inch, 0 * inch), "c2" : vector(rh * 0.6, ww / 2 + 0.75 * inch, definition.axleHeight + rh) }
                ];
            // each hood plate is built at the top of the wheel, then rotated back around the axle
            for (var k = 0; k < 3; k += 1)
            {
                elements = append(elements, { "label" : "hood " ~ (k + 1), "kind" : "box", "hood" : k,
                            "c1" : axle + vector(-seg / 2, -ww / 2 - 0.5 * inch, rh),
                            "c2" : axle + vector(seg / 2, ww / 2 + 0.5 * inch, rh + 0.25 * inch) });
            }
            elementPose = function(ea)
                {
                    var out = [];
                    for (var el in elements)
                    {
                        if (el.hood == undefined)
                            out = append(out, identityTransform());
                        else
                            out = append(out, rotationAround(line(axle, Y_DIRECTION), -(ea + wrap * (el.hood + 0.5) / 3)));
                    }
                    return out;
                };
            // The FUEL rides over the top of the wheel and leaves where its path points up at the exit angle.
            exitLocal = function(ea)
                {
                    return [axle + ballPath * vector(-sin(ea), 0, cos(ea)), vector(cos(ea), 0, sin(ea))];
                };
        }
        else
        {
            const gap = 2 * R + GAME.pieceDia - comp;     // axle to axle
            const top = axle + vector(0 * inch, 0 * inch, gap);
            const mid = axle + vector(0 * inch, 0 * inch, gap / 2);
            elements = [
                    { "label" : "bottom flywheel", "kind" : "cyl", "bottom" : axle - vector(0 * inch, ww / 2, 0 * inch), "top" : axle + vector(0 * inch, ww / 2, 0 * inch), "radius" : R },
                    { "label" : "top flywheel", "kind" : "cyl", "bottom" : top - vector(0 * inch, ww / 2, 0 * inch), "top" : top + vector(0 * inch, ww / 2, 0 * inch), "radius" : R },
                    { "label" : "right plate", "kind" : "box", "c1" : axle + vector(-R - 1 * inch, -ww / 2 - 0.75 * inch, -R - 1 * inch), "c2" : top + vector(R + 0.5 * inch, -ww / 2 - 0.5 * inch, R + 1 * inch) },
                    { "label" : "left plate", "kind" : "box", "c1" : axle + vector(-R - 1 * inch, ww / 2 + 0.5 * inch, -R - 1 * inch), "c2" : top + vector(R + 0.5 * inch, ww / 2 + 0.75 * inch, R + 1 * inch) }
                ];
            elementPose = function(ea)
                {
                    var out = [];
                    for (var el in elements)
                        out = append(out, pitchUp(mid, ea));
                    return out;
                };
            exitLocal = function(ea)
                {
                    return [pitchUp(mid, ea) * (mid + vector(R, 0 * inch, 0 * inch)), vector(cos(ea), 0, sin(ea))];
                };
        }

        // ---- poses: every turret sample x min / mid / max exit angle ----
        const exitMin = definition.exitMin;
        const exitMax = definition.exitMax;
        var samples = [];
        const hoodSamples = exitMax - exitMin < 0.5 * degree ? [exitMin] : [exitMin, (exitMin + exitMax) / 2, exitMax];
        for (var ta in turretAngles)
            for (var ea in hoodSamples)
                samples = append(samples, { "turret" : ta, "exit" : ea });
        const poseAt = function(p)
            {
                const m = mountAt(p.turret);
                var out = [];
                for (var t in elementPose(p.exit))
                    out = append(out, m * t);
                return out;
            };
        const nowPose = { "turret" : turretNow, "exit" : definition.exitNow };
        const mech = buildMech(context, id, {
                    "name" : definition.mechName,
                    "color" : COLOR_SHOOTER,
                    "elements" : elements,
                    "poseAt" : poseAt,
                    "samples" : samples,
                    "pStow" : nowPose,
                    "pDeploy" : nowPose,
                    "pShow" : nowPose,
                    "pGhost" : undefined,
                    "showEnvelope" : definition.showEnvelope
                });

        // ---- exit point and direction in robot coordinates ----
        const mNow = mountAt(turretNow);
        const ex = exitLocal(definition.exitNow);
        const exitPt = mNow * ex[0];
        const exitDir = normalize(mNow.linear * ex[1]);
        opPolyline(context, id + "exitLine", { "points" : [exitPt, exitPt + 18 * inch * exitDir] });
        styleReference(context, qCreatedBy(id + "exitLine", EntityType.BODY), "EXIT - " ~ definition.mechName ~ " direction", COLOR_SHOOTER);
        addConnector(context, id + "exit", toWorld(coordSystem(exitPt, exitDir, normalize(perpendicularVector(exitDir)))), mech.parts, "EXIT - " ~ definition.mechName);

        // ---- trajectory and HUB check ----
        var notes = ["Exit point " ~ fmtIn(exitPt[0]) ~ ", " ~ fmtIn(exitPt[1]) ~ ", " ~ fmtIn(exitPt[2]) ~ " at " ~ fmtDeg(definition.exitNow)];
        const flatDir = vector(exitDir[0], exitDir[1], 0);
        const horiz = norm(flatDir) > 1e-6 ? normalize(flatDir) : X_DIRECTION;
        const exitFlat = vector(exitPt[0], exitPt[1], 0 * inch);
        // HUB center on the aim line at the target distance from the robot center
        const hubCenter = horiz * definition.targetDist;
        const dx = norm(hubCenter - exitFlat);
        const dz = GAME.hubOpeningHeight - exitPt[2];
        const needNow = speedToHit(dx, dz, definition.exitNow);
        if (needNow == undefined)
            notes = append(notes, "can't reach the HUB opening at this angle");
        else
        {
            const fps = needNow / (12 * inch / second);
            const rpm = (needNow / definition.speedRatio) / (PI * definition.wheelDia) * 60 * second;
            notes = append(notes, "needs about " ~ fmtNum(fps, 1) ~ " ft/s (about " ~ fmtNum(round(rpm / 10) * 10, 0) ~ " RPM) to reach the HUB opening " ~ fmtIn(dx) ~ " away");
        }
        var bestRange = [];
        for (var ea in [exitMin, exitMax])
        {
            const v = speedToHit(dx, dz, ea);
            bestRange = append(bestRange, v == undefined ? "no shot" : fmtNum(v / (12 * inch / second), 1) ~ " ft/s");
        }
        notes = append(notes, "at min/max angle: " ~ bestRange[0] ~ " / " ~ bestRange[1]);

        if (definition.showTrajectory)
        {
            const g = 386.09 * inch / (second * second);
            const v = definition.exitSpeed * 12 * inch / second;
            const vx = v * cos(definition.exitNow);
            const vz = v * sin(definition.exitNow);
            var pts = [exitPt];
            var crossing = undefined;
            var prev = exitPt;
            for (var k = 1; k <= 60; k += 1)
            {
                const t = k * 0.04 * second;
                const p = exitPt + horiz * (vx * t) + vector(0 * inch, 0 * inch, vz * t - g * t * t / 2);
                if (crossing == undefined && prev[2] >= GAME.hubOpeningHeight && p[2] < GAME.hubOpeningHeight && vz - g * t < 0 * inch / second)
                    crossing = p;
                pts = append(pts, p);
                prev = p;
                if (p[2] < 0 * inch)
                    break;
            }
            opPolyline(context, id + "trajectory", { "points" : pts });
            styleReference(context, qCreatedBy(id + "trajectory", EntityType.BODY), "TRAJECTORY - " ~ definition.mechName, COLOR_SHOOTER);
            if (crossing == undefined)
                notes = append(notes, "at " ~ definition.exitSpeed ~ " ft/s the drawn curve never comes down through the HUB opening height");
            else
                notes = append(notes, "at " ~ definition.exitSpeed ~ " ft/s it comes down through " ~ fmtIn(GAME.hubOpeningHeight) ~ " at " ~
                        fmtIn(norm(vector(crossing[0], crossing[1], 0 * inch))) ~ " from robot center");

            // HUB opening marker (a flat hexagon) at the target
            const hexR = GAME.hubOpeningAcross / 2 / cos(30 * degree);
            var hex = [];
            for (var k = 0; k <= 6; k += 1)
                hex = append(hex, vector(hubCenter[0], hubCenter[1], GAME.hubOpeningHeight) + hexR * vector(cos(k * 60 * degree), sin(k * 60 * degree), 0));
            opPolyline(context, id + "hubMarker", { "points" : hex });
            styleReference(context, qCreatedBy(id + "hubMarker", EntityType.BODY), "HUB OPENING - target", COLOR_FIELD);
        }

        storeRecord(context, id, exitPt, {
                    "name" : definition.mechName,
                    "kind" : "shooter",
                    "parent" : parentName,
                    "stow" : mech.stow,
                    "deploy" : mech.swept,
                    "swept" : mech.swept,
                    "trench" : mech.swept,
                    "group" : "main",
                    "collide" : definition.collide,
                    "massLb" : definition.massLb,
                    "cg" : mech.cg,
                    "capStow" : 0,
                    "capDeploy" : 0,
                    "exitPoint" : exitPt,
                    "exitDir" : exitDir
                });

        reportFeatureInfo(context, id, joinStrings(notes, "; ") ~ ". No air drag: treat speeds as rough.");
    });
