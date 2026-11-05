
// ===========================================================================
// RB Intake
// ===========================================================================
//
// Three archetypes REBUILT teams used, on any side of the robot:
//   Over bumper (pivot)  side plates + rollers swing down over the bumper
//   Four-bar             rollers ride on parallel links, staying level
//   Linear slide-out     a roller box slides straight out past the frame
// "Full width" makes the rollers span the whole side of the frame.
//
// Everything is measured from the frame edge the intake faces (local +X =
// outward), so resizing RB Robot moves the intake with it.
// Pose: 0 = stowed (starting configuration), 1 = deployed.
//
// Makes:   rollers and side plates in the pose you pick, a see-through copy
//          of the other pose, ENV - <name> swept envelope, a FUEL ball at the
//          pickup point, and the mate connector HANDOFF - <name>.
// Checks:  reach past the frame and bumper (12 in limit), floor strike,
//          roller height vs. FUEL. The full extension, one-direction, TRENCH
//          and collision checks run in RB Robot Check.

annotation { "Feature Type Name" : "RB Intake",
             "Feature Type Description" : "Over-bumper pivot, four-bar or slide-out FUEL intake on one side of the robot, with stowed and deployed poses and a swept envelope." }
export const rbIntake = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Name", "Default" : "Intake" }
        definition.mechName is string;

        annotation { "Name" : "Intake type" }
        definition.intakeType is RBIntakeType;

        annotation { "Name" : "Side", "UIHint" : UIHint.HORIZONTAL_ENUM }
        definition.side is RBSide;

        annotation { "Name" : "Full width of the frame side", "Default" : true }
        definition.fullWidth is boolean;

        if (definition.fullWidth)
        {
            annotation { "Name" : "Margin at each end" }
            isLength(definition.sideMargin, RB_SIDE_MARGIN_BOUNDS);
        }
        else
        {
            annotation { "Name" : "Intake width" }
            isLength(definition.intakeWidth, RB_INTAKE_WIDTH_BOUNDS);
        }

        annotation { "Name" : "Roller diameter" }
        isLength(definition.rollerDia, RB_ROLLER_DIA_BOUNDS);

        annotation { "Name" : "Number of rollers" }
        isInteger(definition.rollerCount, RB_ROLLER_COUNT_BOUNDS);

        annotation { "Name" : "Roller spacing (center to center)" }
        isLength(definition.rollerSpacing, RB_ROLLER_SPACING_BOUNDS);

        annotation { "Name" : "Side plate height" }
        isLength(definition.plateHeight, RB_PLATE_HEIGHT_BOUNDS);

        if (definition.intakeType == RBIntakeType.SLIDE)
        {
            annotation { "Name" : "Box front stowed, inside the frame edge" }
            isLength(definition.setback, RB_SETBACK_BOUNDS);

            annotation { "Name" : "Box length (in the slide direction)" }
            isLength(definition.boxLength, RB_BOX_LENGTH_BOUNDS);

            annotation { "Name" : "Box bottom above floor" }
            isLength(definition.boxBottom, RB_BOX_BOTTOM_BOUNDS);

            annotation { "Name" : "Slide travel" }
            isLength(definition.travel, RB_TRAVEL_BOUNDS);
        }
        else
        {
            annotation { "Name" : "Pivot inside the frame edge" }
            isLength(definition.setback, RB_SETBACK_BOUNDS);

            annotation { "Name" : "Pivot height above floor" }
            isLength(definition.pivotHeight, RB_PIVOT_HEIGHT_BOUNDS);

            annotation { "Name" : "Pivot to elbow (four-bar: link length)" }
            isLength(definition.armLength, RB_ARM_LENGTH_BOUNDS);

            if (definition.intakeType == RBIntakeType.PIVOT)
            {
                annotation { "Name" : "Elbow to front roller" }
                isLength(definition.arm2Length, RB_ARM2_LENGTH_BOUNDS);

                annotation { "Name" : "Bend down at the elbow (0 = straight arm)" }
                isAngle(definition.bend, RB_BEND_BOUNDS);
            }

            if (definition.intakeType == RBIntakeType.FOUR_BAR)
            {
                annotation { "Name" : "Front roller ahead of link tip" }
                isLength(definition.couplerReach, RB_COUPLER_REACH_BOUNDS);

                annotation { "Name" : "Roller drop below link tip" }
                isLength(definition.couplerDrop, RB_COUPLER_DROP_BOUNDS);
            }

            annotation { "Name" : "Stowed angle (90 = straight up)" }
            isAngle(definition.stowAngle, RB_STOW_ANGLE_BOUNDS);

            annotation { "Name" : "Deployed angle (negative = below level)" }
            isAngle(definition.deployAngle, RB_DEPLOY_ANGLE_BOUNDS);
        }

        annotation { "Name" : "Pose to draw", "UIHint" : UIHint.HORIZONTAL_ENUM }
        definition.pose is RBPose;

        if (definition.pose == RBPose.PARTIAL)
        {
            annotation { "Name" : "Deployed fraction" }
            isReal(definition.partial, RB_FRACTION_BOUNDS);
        }

        annotation { "Name" : "Deploys" }
        definition.group is RBDeployGroup;

        rbMechCommonPredicate(definition);
    }
    {
        const robot = requireRobot(context, "RB Intake");
        const side = sideFrame(robot, definition.side);
        const toRobot = sideTransform(robot, definition.side);

        const w = definition.fullWidth ? side.span - 2 * definition.sideMargin : definition.intakeWidth;
        if (w <= 1 * inch)
            throw regenError("Intake is too narrow. Lower the end margin.", ["sideMargin"]);

        const rr = definition.rollerDia / 2;
        const pt = 0.25 * inch;                 // side plate thickness
        const ph = definition.plateHeight;
        const n = definition.rollerCount;
        const sp = definition.rollerSpacing;

        // ---- elements, in local coordinates, at pose 0 of each layout ----
        var elements = [];
        var poseAt;
        var frontRollerLocal;     // front roller center (local, at the element's own origin)

        if (definition.intakeType == RBIntakeType.PIVOT)
        {
            // Arm built pointing straight out (+X) from the pivot at the origin; poseAt rotates it.
            // The outer segment bends down at the elbow so the arm can clear the bumper.
            const e = definition.armLength;
            const a2 = definition.arm2Length;
            for (var s in [-1, 1])
            {
                elements = append(elements, { "label" : s > 0 ? "left inner plate" : "right inner plate", "kind" : "box", "outer" : false,
                            "c1" : vector(-1.5 * inch, s * w / 2, -ph / 2),
                            "c2" : vector(e + ph / 2, s * (w / 2 + pt), ph / 2) });
            }
            for (var s in [-1, 1])
            {
                elements = append(elements, { "label" : s > 0 ? "left outer plate" : "right outer plate", "kind" : "box", "outer" : true,
                            "c1" : vector(-ph / 2, s * w / 2, -ph / 2),
                            "c2" : vector(a2 + rr + 0.5 * inch, s * (w / 2 + pt), ph / 2) });
            }
            for (var k = 0; k < n; k += 1)
            {
                elements = append(elements, { "label" : "roller " ~ (k + 1), "kind" : "cyl", "outer" : true,
                            "bottom" : vector(a2 - k * sp, -w / 2, 0 * inch),
                            "top" : vector(a2 - k * sp, w / 2, 0 * inch),
                            "radius" : rr });
            }
            frontRollerLocal = vector(a2, 0 * inch, 0 * inch);
            const pivot = vector(-definition.setback, 0 * inch, definition.pivotHeight);
            const elbowT = transform(vector(e, 0 * inch, 0 * inch)) * pitchUp(WORLD_ORIGIN, -definition.bend);
            const stowA = definition.stowAngle;
            const depA = definition.deployAngle;
            poseAt = function(p)
                {
                    const t = toRobot * transform(pivot) * pitchUp(WORLD_ORIGIN, stowA + (depA - stowA) * p);
                    var out = [];
                    for (var el in elements)
                        out = append(out, el.outer ? t * elbowT : t);
                    return out;
                };
        }
        else if (definition.intakeType == RBIntakeType.FOUR_BAR)
        {
            // Two links rotate about the pivot; the roller carriage (coupler) stays level at the link tips.
            const a = definition.armLength;
            const reach = definition.couplerReach;
            const drop = definition.couplerDrop;
            for (var s in [-1, 1])
            {
                elements = append(elements, { "label" : s > 0 ? "left link" : "right link", "kind" : "box", "link" : true,
                            "c1" : vector(-0.75 * inch, s * w / 2, -0.5 * inch),
                            "c2" : vector(a + 0.75 * inch, s * (w / 2 + pt), 0.5 * inch) });
            }
            for (var s in [-1, 1])
            {
                elements = append(elements, { "label" : s > 0 ? "left carriage plate" : "right carriage plate", "kind" : "box", "link" : false,
                            "c1" : vector(-rr - 0.5 * inch - (n - 1) * sp + reach, s * (w / 2 + pt), -drop - rr - 0.5 * inch),
                            "c2" : vector(reach + rr + 0.5 * inch, s * (w / 2 + 2 * pt), max(0.5 * inch, ph - drop)) });
            }
            for (var k = 0; k < n; k += 1)
            {
                elements = append(elements, { "label" : "roller " ~ (k + 1), "kind" : "cyl", "link" : false,
                            "bottom" : vector(reach - k * sp, -w / 2, -drop),
                            "top" : vector(reach - k * sp, w / 2, -drop),
                            "radius" : rr });
            }
            frontRollerLocal = vector(reach, 0 * inch, -drop);
            const pivot = vector(-definition.setback, 0 * inch, definition.pivotHeight);
            const stowA = definition.stowAngle;
            const depA = definition.deployAngle;
            poseAt = function(p)
                {
                    const ang = stowA + (depA - stowA) * p;
                    const linkT = toRobot * transform(pivot) * pitchUp(WORLD_ORIGIN, ang);
                    const tipT = toRobot * transform(pivot + a * vector(cos(ang), 0, sin(ang)));
                    var out = [];
                    for (var e = 0; e < size(elements); e += 1)
                        out = append(out, elements[e].link ? linkT : tipT);
                    return out;
                };
        }
        else
        {
            // Roller box built with its front face at x = 0; poseAt slides it out.
            const bl = definition.boxLength;
            const z0 = definition.boxBottom;
            for (var s in [-1, 1])
            {
                elements = append(elements, { "label" : s > 0 ? "left plate" : "right plate", "kind" : "box",
                            "c1" : vector(-bl, s * w / 2, z0),
                            "c2" : vector(0 * inch, s * (w / 2 + pt), z0 + ph) });
            }
            for (var k = 0; k < n; k += 1)
            {
                elements = append(elements, { "label" : "roller " ~ (k + 1), "kind" : "cyl",
                            "bottom" : vector(-rr - k * sp, -w / 2, z0 + rr),
                            "top" : vector(-rr - k * sp, w / 2, z0 + rr),
                            "radius" : rr });
            }
            frontRollerLocal = vector(-rr, 0 * inch, z0 + rr);
            const setback = definition.setback;
            const travel = definition.travel;
            poseAt = function(p)
                {
                    const t = toRobot * transform(vector(-setback + travel * p, 0 * inch, 0 * inch));
                    var out = [];
                    for (var e = 0; e < size(elements); e += 1)
                        out = append(out, t);
                    return out;
                };
        }

        const pShow = poseValue(definition.pose, definition.partial);
        const mech = buildMech(context, id, {
                    "name" : definition.mechName,
                    "color" : COLOR_INTAKE,
                    "elements" : elements,
                    "poseAt" : poseAt,
                    "samples" : linearSamples(0, 1, min(definition.samples, MAX_SAMPLES)),
                    "pStow" : 0,
                    "pDeploy" : 1,
                    "pShow" : pShow,
                    "pGhost" : pShow < 0.5 ? 1 : 0,
                    "showEnvelope" : definition.showEnvelope
                });

        // front roller center when deployed, in robot coordinates (element index of roller 1)
        const rollerIndex = definition.intakeType == RBIntakeType.SLIDE ? 2 : 4;
        const frontDeployed = poseAt(1)[rollerIndex] * frontRollerLocal;
        const fuelR = GAME.pieceDia / 2;

        // a FUEL ball where the deployed front roller meets it on the floor
        const outward = toRobot.linear * X_DIRECTION;
        const fuelCenter = vector(frontDeployed[0], frontDeployed[1], fuelR) + outward * (rr + fuelR) * 0.6;
        opSphere(context, id + "fuel", { "center" : fuelCenter, "radius" : fuelR });
        styleReference(context, qCreatedBy(id + "fuel", EntityType.BODY), GAME.pieceName ~ " - " ~ definition.mechName ~ " pickup", COLOR_FUEL);
        tag(context, qCreatedBy(id + "fuel", EntityType.BODY), { "type" : "piece" });

        // handoff connector: just inside the frame edge, at FUEL center height, +X pointing inward
        const handoffT = toRobot * transform(vector(-definition.setback - fuelR, 0 * inch, fuelR + 1 * inch)) * yawAbout(WORLD_ORIGIN, 180 * degree);
        addConnector(context, id + "handoff", handoffT, mech.parts, "HANDOFF - " ~ definition.mechName);

        // ---- reach and roller checks (local to this feature) ----
        const sweptLocal = { "front" : mech.swept.hi[0] - robot.length / 2, "back" : -robot.length / 2 - mech.swept.lo[0],
                "left" : mech.swept.hi[1] - robot.width / 2, "right" : -robot.width / 2 - mech.swept.lo[1] };
        const reach = sweptLocal[side.name];
        var notes = [];
        var warnings = [];
        notes = append(notes, "Reaches " ~ fmtIn(reach) ~ " past the frame (" ~ fmtIn(reach - robot.bumperDepth) ~ " past the bumper)");
        if (reach > robot.limits.extensionLimit + TOL)
            warnings = append(warnings, "Reaches " ~ fmtIn(reach - robot.limits.extensionLimit) ~ " past the " ~ fmtIn(robot.limits.extensionLimit) ~ " extension limit (R105)");
        if (mech.swept.lo[2] < -TOL)
            warnings = append(warnings, "Goes " ~ fmtIn(-mech.swept.lo[2]) ~ " below the floor somewhere in its swing");
        const rollerZ = frontDeployed[2];
        notes = append(notes, "deployed front roller center at " ~ fmtIn(rollerZ) ~ " (" ~ GAME.pieceName ~ " center is " ~ fmtIn(fuelR) ~ ")");
        if (rollerZ - rr > GAME.pieceDia - TOL)
            warnings = append(warnings, "Deployed rollers are above the " ~ GAME.pieceName ~ ", so they can't reach it");
        if (definition.intakeType != RBIntakeType.SLIDE && reach < robot.bumperDepth + rr && definition.side != RBSide.LEFT && definition.side != RBSide.RIGHT)
            notes = append(notes, "rollers don't clear the bumper when deployed");

        storeRecord(context, id, toRobot * vector(-definition.setback, 0 * inch, 1 * inch), {
                    "name" : definition.mechName,
                    "kind" : "intake",
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

        if (size(warnings) > 0)
            reportFeatureWarning(context, id, joinStrings(warnings, ". ") ~ ". " ~ joinStrings(notes, ", ") ~ ".");
        else
            reportFeatureInfo(context, id, joinStrings(notes, ", ") ~ ". Width " ~ fmtIn(w) ~ ".");
    });
