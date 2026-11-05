
// ===========================================================================
// RB Robot: frame perimeter, bumpers, swerve, battery, game limits
// ===========================================================================
//
// Makes:   4 frame rails, bumpers (with an optional gap), 4 swerve module
//          blocks and wheels, a battery block, and mate connectors:
//            FRAME TOP          center of the frame top, +X forward
//            EDGE - front/back/left/right   middle of each frame edge, +X outward
// Checks:  frame perimeter (R104), bumper zone coverage (R405), bumper depth
//          (R403), bumper height (R402), bumper gap corners (R401)
// Feeds:   every other feature reads the frame size and limits from here;
//          RB Robot Check uses the masses for weight and CG.

function modulePreset(definition is map) returns map
{
    if (definition.modulePreset == RBModulePreset.SDS_MK4I)
        return { "inset" : 2.625 * inch, "wheelDia" : 4 * inch, "wheelWidth" : 1.5 * inch, "size" : 5.25 * inch, "top" : 6 * inch };
    if (definition.modulePreset == RBModulePreset.WCP_X2I)
        return { "inset" : 2.75 * inch, "wheelDia" : 4 * inch, "wheelWidth" : 1.5 * inch, "size" : 5.5 * inch, "top" : 6 * inch };
    return {
            "inset" : definition.wheelInset,
            "wheelDia" : definition.wheelDia,
            "wheelWidth" : 1.5 * inch,
            "size" : definition.moduleSize,
            "top" : definition.moduleTop
        };
}

annotation { "Feature Type Name" : "RB Robot",
             "Feature Type Description" : "REBUILT frame perimeter, bumpers, swerve modules and battery. Put this first: every other RB feature reads the frame and the game limits from it." }
export const rbRobot = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Group Name" : "Frame", "Collapsed By Default" : false }
        {
            annotation { "Name" : "Frame length (front to back)" }
            isLength(definition.length, RB_FRAME_LENGTH_BOUNDS);

            annotation { "Name" : "Frame width (side to side)" }
            isLength(definition.width, RB_FRAME_WIDTH_BOUNDS);

            annotation { "Name" : "Rail height" }
            isLength(definition.railHeight, RB_RAIL_HEIGHT_BOUNDS);

            annotation { "Name" : "Rail width" }
            isLength(definition.railWidth, RB_RAIL_WIDTH_BOUNDS);

            annotation { "Name" : "Frame bottom above floor" }
            isLength(definition.frameBottom, RB_FRAME_BOTTOM_BOUNDS);
        }

        annotation { "Group Name" : "Bumpers", "Collapsed By Default" : false }
        {
            annotation { "Name" : "Bumper bottom above floor" }
            isLength(definition.bumperBottom, RB_BUMPER_BOTTOM_BOUNDS);

            annotation { "Name" : "Bumper height" }
            isLength(definition.bumperHeight, RB_BUMPER_HEIGHT_BOUNDS);

            annotation { "Name" : "Bumper depth (backing + padding)" }
            isLength(definition.bumperDepth, RB_BUMPER_DEPTH_BOUNDS);

            annotation { "Name" : "Bumper color", "UIHint" : UIHint.HORIZONTAL_ENUM }
            definition.bumperColor is RBBumperColor;

            annotation { "Name" : "Bumper gap (one large gap allowed)" }
            definition.gapSide is RBGapSide;

            if (definition.gapSide != RBGapSide.NONE)
            {
                annotation { "Name" : "Gap width (centered)" }
                isLength(definition.gapWidth, RB_GAP_WIDTH_BOUNDS);
            }
        }

        annotation { "Group Name" : "Drivetrain and battery", "Collapsed By Default" : true }
        {
            annotation { "Name" : "Swerve module" }
            definition.modulePreset is RBModulePreset;

            if (definition.modulePreset == RBModulePreset.CUSTOM)
            {
                annotation { "Name" : "Wheel center inset from frame edge" }
                isLength(definition.wheelInset, RB_WHEEL_INSET_BOUNDS);

                annotation { "Name" : "Wheel diameter" }
                isLength(definition.wheelDia, RB_WHEEL_DIA_BOUNDS);

                annotation { "Name" : "Module block size (square)" }
                isLength(definition.moduleSize, RB_MODULE_SIZE_BOUNDS);

                annotation { "Name" : "Module top above floor" }
                isLength(definition.moduleTop, RB_MODULE_TOP_BOUNDS);
            }

            annotation { "Name" : "Show battery", "Default" : true }
            definition.showBattery is boolean;

            if (definition.showBattery)
            {
                annotation { "Name" : "Battery center X (forward)" }
                isLength(definition.batteryX, RB_BATTERY_X_BOUNDS);

                annotation { "Name" : "Battery center Y (left)" }
                isLength(definition.batteryY, RB_POS_BOUNDS);
            }
        }

        annotation { "Group Name" : "Mass estimates", "Collapsed By Default" : true }
        {
            annotation { "Name" : "Frame + drivetrain + electronics (lb)" }
            isReal(definition.baseMassLb, RB_BASE_MASS_BOUNDS);

            annotation { "Name" : "Bumpers (lb)" }
            isReal(definition.bumperMassLb, RB_BUMPER_MASS_BOUNDS);

            annotation { "Name" : "Battery (lb)" }
            isReal(definition.batteryMassLb, RB_BATTERY_MASS_BOUNDS);
        }

        annotation { "Group Name" : "Game limits", "Collapsed By Default" : true }
        {
            annotation { "Name" : "Override REBUILT limits", "Default" : false }
            definition.overrideLimits is boolean;

            if (definition.overrideLimits)
            {
                annotation { "Name" : "Max frame perimeter" }
                isLength(definition.maxPerimeter, RB_PERIMETER_BOUNDS);

                annotation { "Name" : "Max height (start and match)" }
                isLength(definition.maxHeight, RB_HEIGHT_LIMIT_BOUNDS);

                annotation { "Name" : "Horizontal extension limit" }
                isLength(definition.extensionLimit, RB_EXTENSION_BOUNDS);

                annotation { "Name" : "TRENCH clearance height" }
                isLength(definition.trenchHeight, RB_TRENCH_BOUNDS);
            }
        }
    }
    {
        const L = definition.length;
        const W = definition.width;
        const t = definition.railWidth;
        const z0 = definition.frameBottom;
        const z1 = z0 + definition.railHeight;

        if (2 * t >= L || 2 * t >= W)
            throw regenError("Rails are wider than the frame.", ["railWidth"]);

        // ---- limits: GAME constants unless overridden ----
        var limits = {
                "maxPerimeter" : GAME.maxPerimeter,
                "maxStartHeight" : GAME.maxStartHeight,
                "maxHeight" : GAME.maxHeight,
                "extensionLimit" : GAME.extensionLimit,
                "trenchHeight" : GAME.trenchClearHeight
            };
        if (definition.overrideLimits)
        {
            limits.maxPerimeter = definition.maxPerimeter;
            limits.maxStartHeight = definition.maxHeight;
            limits.maxHeight = definition.maxHeight;
            limits.extensionLimit = definition.extensionLimit;
            limits.trenchHeight = definition.trenchHeight;
        }

        // ---- swerve modules ----
        const m = modulePreset(definition);
        const r = m.wheelDia / 2;
        const wx = L / 2 - m.inset;
        const wy = W / 2 - m.inset;
        if (wx <= 0 * inch || wy <= 0 * inch)
            throw regenError("Frame is too small for these modules.", ["length", "width"]);
        const corners = [["FL", 1, 1], ["FR", 1, -1], ["BL", -1, 1], ["BR", -1, -1]];
        for (var i = 0; i < 4; i += 1)
        {
            const center = vector(corners[i][1] * wx, corners[i][2] * wy, 0 * inch);
            const modBody = makeBox(context, id + ("module" ~ i),
                center + vector(-m.size / 2, -m.size / 2, r),
                center + vector(m.size / 2, m.size / 2, max(m.top, r + 0.25 * inch)));
            style(context, modBody, "SWERVE - " ~ corners[i][0] ~ " module", COLOR_DRIVE);
            tag(context, modBody, { "type" : "robotPart", "part" : "module" });
            const wheel = makeCylinder(context, id + ("wheel" ~ i),
                center + vector(0 * inch, -m.wheelWidth / 2, r),
                center + vector(0 * inch, m.wheelWidth / 2, r), r);
            style(context, wheel, "SWERVE - " ~ corners[i][0] ~ " wheel", COLOR_WHEEL);
            tag(context, wheel, { "type" : "robotPart", "part" : "wheel" });
        }

        // ---- battery (a typical FRC SLA battery is about 7.1 x 3.0 x 6.6 in) ----
        var batteryCenter = vector(0, 0, 0) * inch;
        if (definition.showBattery)
        {
            const bc = vector(definition.batteryX, definition.batteryY, z1 + 3.3 * inch);
            batteryCenter = bc;
            const bat = makeBox(context, id + "battery", bc - vector(1.5, 3.55, 3.3) * inch, bc + vector(1.5, 3.55, 3.3) * inch);
            style(context, bat, "BATTERY", COLOR_BATTERY);
            tag(context, bat, { "type" : "robotPart", "part" : "battery" });
        }

        // ---- warnings collected for the feature ----
        var warnings = [];
        const perimeter = 2 * (L + W);
        if (perimeter > limits.maxPerimeter + TOL)
            warnings = append(warnings, "Frame perimeter " ~ fmtIn(perimeter) ~ " is over the " ~ fmtIn(limits.maxPerimeter) ~ " limit (R104)");

        // ---- bumpers ----
        const b = definition.bumperDepth;
        const zb0 = definition.bumperBottom;
        const zb1 = zb0 + definition.bumperHeight;
        const bumperColor = definition.bumperColor == RBBumperColor.RED ? COLOR_BUMPER_RED : COLOR_BUMPER_BLUE;

        // Each side runs corner to corner; the front and back wrap the corners.
        var segments = [
            ["front", vector(L / 2, -W / 2 - b, zb0), vector(L / 2 + b, W / 2 + b, zb1), RBGapSide.FRONT, 1],
            ["back", vector(-L / 2 - b, -W / 2 - b, zb0), vector(-L / 2, W / 2 + b, zb1), RBGapSide.BACK, 1],
            ["left", vector(-L / 2, W / 2, zb0), vector(L / 2, W / 2 + b, zb1), RBGapSide.LEFT, 0],
            ["right", vector(-L / 2, -W / 2 - b, zb0), vector(L / 2, -W / 2, zb1), RBGapSide.RIGHT, 0]
        ];
        var bumperBodies = [];
        for (var i = 0; i < size(segments); i += 1)
        {
            const seg = segments[i];
            if (definition.gapSide == seg[3])
            {
                // split this side into two pieces around a centered gap
                const axisIndex = seg[4];          // 1 = gap runs along Y, 0 = along X
                const sideLength = axisIndex == 1 ? W : L;
                const g = definition.gapWidth;
                const coverEach = (sideLength - g) / 2;
                if (coverEach < GAME.bumperCornerCover - TOL)
                    warnings = append(warnings, "Bumper gap leaves only " ~ fmtIn(coverEach) ~ " of bumper beside each corner; " ~
                            fmtIn(GAME.bumperCornerCover) ~ " is needed (R401)");
                if (g >= sideLength)
                    throw regenError("Bumper gap is wider than that side of the frame.", ["gapWidth"]);
                var lo1 = seg[1];
                var hi1 = seg[2];
                var lo2 = seg[1];
                var hi2 = seg[2];
                hi1[axisIndex] = -g / 2;
                lo2[axisIndex] = g / 2;
                bumperBodies = append(bumperBodies, [seg[0] ~ " A", lo1, hi1]);
                bumperBodies = append(bumperBodies, [seg[0] ~ " B", lo2, hi2]);
            }
            else
            {
                bumperBodies = append(bumperBodies, [seg[0], seg[1], seg[2]]);
            }
        }
        for (var i = 0; i < size(bumperBodies); i += 1)
        {
            const body = makeBox(context, id + ("bumper" ~ i), bumperBodies[i][1], bumperBodies[i][2]);
            style(context, body, "BUMPER - " ~ bumperBodies[i][0], bumperColor);
            tag(context, body, { "type" : "robotPart", "part" : "bumper" });
        }

        // R405: bumpers must fill the whole zone (they may extend below it).
        if (zb0 > GAME.bumperZoneBottom + TOL)
            warnings = append(warnings, "Bumper bottom is " ~ fmtIn(zb0 - GAME.bumperZoneBottom) ~ " too high to cover the bumper zone (R405)");
        if (zb1 < GAME.bumperZoneTop - TOL)
            warnings = append(warnings, "Bumper top is " ~ fmtIn(GAME.bumperZoneTop - zb1) ~ " too low to cover the bumper zone (R405)");
        if (b > GAME.bumperMaxDepth + TOL)
            warnings = append(warnings, "Bumpers stick out " ~ fmtIn(b) ~ "; the limit is " ~ fmtIn(GAME.bumperMaxDepth) ~ " (R403)");
        if (definition.bumperHeight < GAME.bumperMinHeight - TOL)
            warnings = append(warnings, "Bumper height " ~ fmtIn(definition.bumperHeight) ~ " is under the " ~ fmtIn(GAME.bumperMinHeight) ~ " minimum (R402)");

        // ---- frame rails (made last so the robot record sits on them) ----
        const rails = [
                ["front", vector(L / 2 - t, -W / 2, z0), vector(L / 2, W / 2, z1)],
                ["back", vector(-L / 2, -W / 2, z0), vector(-L / 2 + t, W / 2, z1)],
                ["left", vector(-L / 2 + t, W / 2 - t, z0), vector(L / 2 - t, W / 2, z1)],
                ["right", vector(-L / 2 + t, -W / 2, z0), vector(L / 2 - t, -W / 2 + t, z1)]
            ];
        var railQueries = [];
        for (var i = 0; i < size(rails); i += 1)
        {
            const body = makeBox(context, id + ("rail" ~ i), rails[i][1], rails[i][2]);
            style(context, body, "FRAME - " ~ rails[i][0] ~ " rail", COLOR_FRAME);
            railQueries = append(railQueries, body);
        }
        tag(context, qUnion(railQueries), { "type" : "robotPart", "part" : "frame" });

        // The robot record lives on a point body at the origin.
        opPoint(context, id + "robotRecord", { "point" : WORLD_ORIGIN });
        const recordPoint = qCreatedBy(id + "robotRecord", EntityType.BODY);
        const fixedTop = max(max(z1, zb1), max(m.top, definition.showBattery ? z1 + 6.6 * inch : z1));
        tag(context, recordPoint, {
                    "type" : "robot",
                    "length" : L,
                    "width" : W,
                    "frameBottom" : z0,
                    "frameTop" : z1,
                    "bumperBottom" : zb0,
                    "bumperTop" : zb1,
                    "bumperDepth" : b,
                    "fixedTop" : fixedTop,
                    "limits" : limits,
                    "massItems" : [
                        { "name" : "Frame + drivetrain", "lb" : definition.baseMassLb, "cg" : vector(0 * inch, 0 * inch, (z0 + z1) / 2), "counts" : true },
                        { "name" : "Bumpers", "lb" : definition.bumperMassLb, "cg" : vector(0 * inch, 0 * inch, (zb0 + zb1) / 2), "counts" : false },
                        { "name" : "Battery", "lb" : definition.showBattery ? definition.batteryMassLb : 0, "cg" : batteryCenter, "counts" : false }
                    ]
                });

        // ---- mate connectors ----
        const owner = railQueries[0];
        addConnector(context, id + "mcTop", transform(vector(0 * inch, 0 * inch, z1)), owner, "FRAME TOP");
        const edgeData = [["front", RBSide.FRONT], ["back", RBSide.BACK], ["left", RBSide.LEFT], ["right", RBSide.RIGHT]];
        const robotLite = { "length" : L, "width" : W };
        for (var i = 0; i < 4; i += 1)
        {
            const edgeT = transform(vector(0 * inch, 0 * inch, z1)) * sideTransform(robotLite, edgeData[i][1]);
            addConnector(context, id + ("mcEdge" ~ i), edgeT, owner, "EDGE - " ~ edgeData[i][0]);
        }

        // ---- report ----
        if (size(warnings) > 0)
            reportFeatureWarning(context, id, joinStrings(warnings, ". ") ~ ".");
        else
            reportFeatureInfo(context, id, "Perimeter " ~ fmtIn(perimeter) ~ " of " ~ fmtIn(limits.maxPerimeter) ~
                    ". With bumpers: " ~ fmtIn(L + 2 * b) ~ " x " ~ fmtIn(W + 2 * b) ~ ". Wheelbase " ~ fmtIn(2 * wx) ~ " x " ~ fmtIn(2 * wy) ~ ".");
    });
