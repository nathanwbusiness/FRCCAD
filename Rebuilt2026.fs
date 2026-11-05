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

// ---- dialog limits and defaults (min, default, max), generated: inches, degrees ----
const RB_POS_BOUNDS = { (meter) : [-5.08, 0, 5.08], (inch) : 0, (millimeter) : 0 } as LengthBoundSpec;
const RB_POS_Z_BOUNDS = { (meter) : [0, 0.1016, 1.524], (inch) : 4, (millimeter) : 101.6 } as LengthBoundSpec;
const RB_BATTERY_X_BOUNDS = { (meter) : [-5.08, -0.2286, 5.08], (inch) : -9, (millimeter) : -228.6 } as LengthBoundSpec;
const RB_END_X_BOUNDS = { (meter) : [-5.08, 0.1016, 5.08], (inch) : 4, (millimeter) : 101.6 } as LengthBoundSpec;
const RB_END_Z_BOUNDS = { (meter) : [0, 0.4064, 1.524], (inch) : 16, (millimeter) : 406.4 } as LengthBoundSpec;
const RB_FIELD_POS_BOUNDS = { (meter) : [-15.24, 0, 15.24], (inch) : 0, (millimeter) : 0 } as LengthBoundSpec;
const RB_FRAME_LENGTH_BOUNDS = { (meter) : [0.254, 0.7112, 1.524], (inch) : 28, (millimeter) : 711.2 } as LengthBoundSpec;
const RB_FRAME_WIDTH_BOUNDS = { (meter) : [0.254, 0.6604, 1.524], (inch) : 26, (millimeter) : 660.4 } as LengthBoundSpec;
const RB_RAIL_HEIGHT_BOUNDS = { (meter) : [0.0127, 0.0508, 0.1524], (inch) : 2, (millimeter) : 50.8 } as LengthBoundSpec;
const RB_RAIL_WIDTH_BOUNDS = { (meter) : [0.0127, 0.0254, 0.1016], (inch) : 1, (millimeter) : 25.4 } as LengthBoundSpec;
const RB_FRAME_BOTTOM_BOUNDS = { (meter) : [0, 0.0381, 0.1524], (inch) : 1.5, (millimeter) : 38.1 } as LengthBoundSpec;
const RB_BUMPER_BOTTOM_BOUNDS = { (meter) : [0, 0.0381, 0.1524], (inch) : 1.5, (millimeter) : 38.1 } as LengthBoundSpec;
const RB_BUMPER_HEIGHT_BOUNDS = { (meter) : [0.0254, 0.127, 0.254], (inch) : 5, (millimeter) : 127 } as LengthBoundSpec;
const RB_BUMPER_DEPTH_BOUNDS = { (meter) : [0.0127, 0.08255, 0.1524], (inch) : 3.25, (millimeter) : 82.5 } as LengthBoundSpec;
const RB_GAP_WIDTH_BOUNDS = { (meter) : [0.0254, 0.3048, 1.524], (inch) : 12, (millimeter) : 304.8 } as LengthBoundSpec;
const RB_WHEEL_INSET_BOUNDS = { (meter) : [0.0127, 0.066675, 0.1524], (inch) : 2.625, (millimeter) : 66.7 } as LengthBoundSpec;
const RB_WHEEL_DIA_BOUNDS = { (meter) : [0.0508, 0.1016, 0.2032], (inch) : 4, (millimeter) : 101.6 } as LengthBoundSpec;
const RB_MODULE_SIZE_BOUNDS = { (meter) : [0.0508, 0.13335, 0.254], (inch) : 5.25, (millimeter) : 133.3 } as LengthBoundSpec;
const RB_MODULE_TOP_BOUNDS = { (meter) : [0.0254, 0.1524, 0.3048], (inch) : 6, (millimeter) : 152.4 } as LengthBoundSpec;
const RB_PERIMETER_BOUNDS = { (meter) : [0.508, 2.794, 7.62], (inch) : 110, (millimeter) : 2794 } as LengthBoundSpec;
const RB_HEIGHT_LIMIT_BOUNDS = { (meter) : [0.127, 0.762, 3.048], (inch) : 30, (millimeter) : 762 } as LengthBoundSpec;
const RB_EXTENSION_BOUNDS = { (meter) : [0, 0.3048, 1.524], (inch) : 12, (millimeter) : 304.8 } as LengthBoundSpec;
const RB_TRENCH_BOUNDS = { (meter) : [0.127, 0.56515, 1.524], (inch) : 22.25, (millimeter) : 565.1 } as LengthBoundSpec;
const RB_SIDE_MARGIN_BOUNDS = { (meter) : [0, 0.0254, 0.254], (inch) : 1, (millimeter) : 25.4 } as LengthBoundSpec;
const RB_INTAKE_WIDTH_BOUNDS = { (meter) : [0.0508, 0.508, 1.524], (inch) : 20, (millimeter) : 508 } as LengthBoundSpec;
const RB_ROLLER_DIA_BOUNDS = { (meter) : [0.0127, 0.0508, 0.1524], (inch) : 2, (millimeter) : 50.8 } as LengthBoundSpec;
const RB_ROLLER_SPACING_BOUNDS = { (meter) : [0.0127, 0.0762, 0.3048], (inch) : 3, (millimeter) : 76.2 } as LengthBoundSpec;
const RB_PLATE_HEIGHT_BOUNDS = { (meter) : [0.00635, 0.0635, 0.2032], (inch) : 2.5, (millimeter) : 63.5 } as LengthBoundSpec;
const RB_SETBACK_BOUNDS = { (meter) : [-0.3048, 0.0508, 0.6096], (inch) : 2, (millimeter) : 50.8 } as LengthBoundSpec;
const RB_BOX_LENGTH_BOUNDS = { (meter) : [0.0254, 0.1524, 0.6096], (inch) : 6, (millimeter) : 152.4 } as LengthBoundSpec;
const RB_BOX_BOTTOM_BOUNDS = { (meter) : [0, 0.0381, 0.508], (inch) : 1.5, (millimeter) : 38.1 } as LengthBoundSpec;
const RB_TRAVEL_BOUNDS = { (meter) : [0, 0.254, 0.762], (inch) : 10, (millimeter) : 254 } as LengthBoundSpec;
const RB_PIVOT_HEIGHT_BOUNDS = { (meter) : [0.0254, 0.254, 1.016], (inch) : 10, (millimeter) : 254 } as LengthBoundSpec;
const RB_ARM_LENGTH_BOUNDS = { (meter) : [0.0254, 0.1905, 1.016], (inch) : 7.5, (millimeter) : 190.5 } as LengthBoundSpec;
const RB_ARM2_LENGTH_BOUNDS = { (meter) : [0.0127, 0.1651, 0.762], (inch) : 6.5, (millimeter) : 165.1 } as LengthBoundSpec;
const RB_COUPLER_REACH_BOUNDS = { (meter) : [-0.254, 0.1016, 0.508], (inch) : 4, (millimeter) : 101.6 } as LengthBoundSpec;
const RB_COUPLER_DROP_BOUNDS = { (meter) : [-0.254, 0.0508, 0.508], (inch) : 2, (millimeter) : 50.8 } as LengthBoundSpec;
const RB_HOPPER_FLOOR_BOUNDS = { (meter) : [0, 0.1016, 1.016], (inch) : 4, (millimeter) : 101.6 } as LengthBoundSpec;
const RB_HOPPER_LENGTH_BOUNDS = { (meter) : [0.0762, 0.4572, 1.524], (inch) : 18, (millimeter) : 457.2 } as LengthBoundSpec;
const RB_HOPPER_WIDTH_BOUNDS = { (meter) : [0.0762, 0.508, 1.524], (inch) : 20, (millimeter) : 508 } as LengthBoundSpec;
const RB_HOPPER_HEIGHT_BOUNDS = { (meter) : [0.0508, 0.3048, 1.016], (inch) : 12, (millimeter) : 304.8 } as LengthBoundSpec;
const RB_EXPAND_BOUNDS = { (meter) : [0, 0.254, 1.016], (inch) : 10, (millimeter) : 254 } as LengthBoundSpec;
const RB_CHANNEL_WIDTH_BOUNDS = { (meter) : [0.0254, 0.1778, 0.762], (inch) : 7, (millimeter) : 177.8 } as LengthBoundSpec;
const RB_DISC_DIA_BOUNDS = { (meter) : [0.1016, 0.4572, 1.016], (inch) : 18, (millimeter) : 457.2 } as LengthBoundSpec;
const RB_WALL_HEIGHT_BOUNDS = { (meter) : [0.0127, 0.1524, 0.762], (inch) : 6, (millimeter) : 152.4 } as LengthBoundSpec;
const RB_RING_Z_BOUNDS = { (meter) : [0, 0.2032, 1.016], (inch) : 8, (millimeter) : 203.2 } as LengthBoundSpec;
const RB_RING_DIA_BOUNDS = { (meter) : [0.0508, 0.3048, 0.762], (inch) : 12, (millimeter) : 304.8 } as LengthBoundSpec;
const RB_RING_HEIGHT_BOUNDS = { (meter) : [0.00635, 0.0381, 0.1524], (inch) : 1.5, (millimeter) : 38.1 } as LengthBoundSpec;
const RB_PAYLOAD_LENGTH_BOUNDS = { (meter) : [0.0254, 0.3048, 1.016], (inch) : 12, (millimeter) : 304.8 } as LengthBoundSpec;
const RB_PAYLOAD_WIDTH_BOUNDS = { (meter) : [0.0254, 0.3048, 1.016], (inch) : 12, (millimeter) : 304.8 } as LengthBoundSpec;
const RB_PAYLOAD_HEIGHT_BOUNDS = { (meter) : [0.0127, 0.0762, 0.762], (inch) : 3, (millimeter) : 76.2 } as LengthBoundSpec;
const RB_PAYLOAD_OFFSET_BOUNDS = { (meter) : [-0.508, 0, 0.508], (inch) : 0, (millimeter) : 0 } as LengthBoundSpec;
const RB_SHOOTER_Z_BOUNDS = { (meter) : [-0.508, 0, 1.016], (inch) : 0, (millimeter) : 0 } as LengthBoundSpec;
const RB_FLYWHEEL_DIA_BOUNDS = { (meter) : [0.0254, 0.0762, 0.254], (inch) : 3, (millimeter) : 76.2 } as LengthBoundSpec;
const RB_FLYWHEEL_WIDTH_BOUNDS = { (meter) : [0.0254, 0.254, 0.762], (inch) : 10, (millimeter) : 254 } as LengthBoundSpec;
const RB_AXLE_HEIGHT_BOUNDS = { (meter) : [0, 0.0508, 0.508], (inch) : 2, (millimeter) : 50.8 } as LengthBoundSpec;
const RB_COMPRESSION_BOUNDS = { (meter) : [0, 0.0127, 0.0762], (inch) : 0.5, (millimeter) : 12.7 } as LengthBoundSpec;
const RB_TARGET_DIST_BOUNDS = { (meter) : [0.3048, 3.048, 15.24], (inch) : 120, (millimeter) : 3048 } as LengthBoundSpec;
const RB_CLIMB_BASE_BOUNDS = { (meter) : [0, 0.1016, 0.762], (inch) : 4, (millimeter) : 101.6 } as LengthBoundSpec;
const RB_CLIMB_TUBE_BOUNDS = { (meter) : [0.0127, 0.0381, 0.1016], (inch) : 1.5, (millimeter) : 38.1 } as LengthBoundSpec;
const RB_SEAT_HEIGHT_BOUNDS = { (meter) : [0.1016, 0.2794, 0.762], (inch) : 11, (millimeter) : 279.4 } as LengthBoundSpec;
const RB_STROKE_BOUNDS = { (meter) : [0, 0.4699, 1.016], (inch) : 18.5, (millimeter) : 469.9 } as LengthBoundSpec;
const RB_THROAT_BOUNDS = { (meter) : [0.0127, 0.05715, 0.1524], (inch) : 2.25, (millimeter) : 57.1 } as LengthBoundSpec;
const RB_LIP_BOUNDS = { (meter) : [0.00635, 0.0381, 0.1524], (inch) : 1.5, (millimeter) : 38.1 } as LengthBoundSpec;
const RB_HOOK_THICK_BOUNDS = { (meter) : [0.00254, 0.0127, 0.0762], (inch) : 0.5, (millimeter) : 12.7 } as LengthBoundSpec;
const RB_CLIMB_CLEAR_BOUNDS = { (meter) : [0, 0.0127, 0.0762], (inch) : 0.5, (millimeter) : 12.7 } as LengthBoundSpec;
const RB_LIFT_BOUNDS = { (meter) : [0, 0.0508, 0.3048], (inch) : 2, (millimeter) : 50.8 } as LengthBoundSpec;
const RB_HOOK_SPACING_BOUNDS = { (meter) : [0, 0.508, 1.524], (inch) : 20, (millimeter) : 508 } as LengthBoundSpec;
const RB_TRENCH_MARGIN_BOUNDS = { (meter) : [0, 0.0127, 0.1524], (inch) : 0.5, (millimeter) : 12.7 } as LengthBoundSpec;
const RB_STOW_ANGLE_BOUNDS = { (degree) : [-360, 160, 360], (radian) : 2.792527 } as AngleBoundSpec;
const RB_DEPLOY_ANGLE_BOUNDS = { (degree) : [-360, -5, 360], (radian) : -0.087266 } as AngleBoundSpec;
const RB_DIRECTION_BOUNDS = { (degree) : [-360, 0, 360], (radian) : 0 } as AngleBoundSpec;
const RB_TURRET_MIN_BOUNDS = { (degree) : [-720, -180, 720], (radian) : -3.141593 } as AngleBoundSpec;
const RB_TURRET_MAX_BOUNDS = { (degree) : [-720, 180, 720], (radian) : 3.141593 } as AngleBoundSpec;
const RB_ANGLE_NOW_BOUNDS = { (degree) : [-720, 0, 720], (radian) : 0 } as AngleBoundSpec;
const RB_EXIT_MIN_BOUNDS = { (degree) : [0, 40, 90], (radian) : 0.698132 } as AngleBoundSpec;
const RB_EXIT_MAX_BOUNDS = { (degree) : [0, 75, 90], (radian) : 1.308997 } as AngleBoundSpec;
const RB_EXIT_NOW_BOUNDS = { (degree) : [0, 55, 90], (radian) : 0.959931 } as AngleBoundSpec;
const RB_HOOD_WRAP_BOUNDS = { (degree) : [10, 90, 270], (radian) : 1.570796 } as AngleBoundSpec;
const RB_BEND_BOUNDS = { (degree) : [0, 60, 150], (radian) : 1.047198 } as AngleBoundSpec;
const RB_MASS_BOUNDS = { (unitless) : [0, 6, 100] } as RealBoundSpec;
const RB_BASE_MASS_BOUNDS = { (unitless) : [0, 70, 200] } as RealBoundSpec;
const RB_BUMPER_MASS_BOUNDS = { (unitless) : [0, 15, 40] } as RealBoundSpec;
const RB_BATTERY_MASS_BOUNDS = { (unitless) : [0, 13, 20] } as RealBoundSpec;
const RB_FRACTION_BOUNDS = { (unitless) : [0, 0.5, 1] } as RealBoundSpec;
const RB_PACKING_BOUNDS = { (unitless) : [0.05, 0.5, 0.74] } as RealBoundSpec;
const RB_LOST_VOLUME_BOUNDS = { (unitless) : [0, 0, 100000] } as RealBoundSpec;
const RB_EXIT_SPEED_BOUNDS = { (unitless) : [1, 30, 100] } as RealBoundSpec;
const RB_SPEED_RATIO_BOUNDS = { (unitless) : [0.05, 0.5, 1] } as RealBoundSpec;
const RB_SAMPLE_BOUNDS = { (unitless) : [2, 7, 25] } as IntegerBoundSpec;
const RB_ROLLER_COUNT_BOUNDS = { (unitless) : [1, 2, 4] } as IntegerBoundSpec;
const RB_SECTION_BOUNDS = { (unitless) : [1, 1, 3] } as IntegerBoundSpec;
const RB_PADDLE_BOUNDS = { (unitless) : [2, 6, 12] } as IntegerBoundSpec;
const RB_HOOK_COUNT_BOUNDS = { (unitless) : [1, 2, 4] } as IntegerBoundSpec;

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
    var ghostQueries = [];   // each ghost has its own top-level id: Onshape won't let one parent id span other operations

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
            opPattern(context, id + ("ghost" ~ e), {
                        "entities" : body,
                        "transforms" : [spec.poseAt(spec.pGhost)[e]],
                        "instanceNames" : ["g"],
                        "copyPropertiesAndAttributes" : false
                    });
            ghostQueries = append(ghostQueries, qCreatedBy(id + ("ghost" ~ e), EntityType.BODY));
        }

        opTransform(context, id + ("move" ~ e), { "bodies" : body, "transform" : showPose[e] });
        style(context, body, spec.name ~ " - " ~ el.label, spec.color);
        tag(context, body, { "type" : "part", "mech" : spec.name });
        parts = append(parts, body);
    }

    const partQuery = qUnion(parts);
    const ghostQuery = qUnion(ghostQueries);
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
