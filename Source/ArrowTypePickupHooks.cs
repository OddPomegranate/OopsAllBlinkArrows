using System;
using FortRise;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using MonoMod.Utils;
using TowerFall;

namespace OopsAllBlinkArrows;

// The doubled "ARROWS ARROWS" text: OnPlayerCollide always creates two FloatTexts - one for
// the arrow's display name, one right underneath that's *always* the literal string "ARROWS"
// (vanilla Bramble shows "BRAMBLE" / "ARROWS" the same way). The name line comes back null
// for every modded arrow on this FortRise build, which used to crash FloatText's constructor
// outright; substituting a generic "ARROWS" there (below) stopped the crash but produced the
// doubled text, since the real per-arrow name never made it in.
//
// The logging added while diagnosing this proved the field write really does happen - a
// postfix on ArrowTypePickup's own constructor wrote "Name" via DynamicData and read the
// correct value straight back (e.g. "ICE ARROWS") - but by the time OnPlayerCollide runs on
// the very same pickup a moment later, DynamicData reads that same field back as null. So
// whatever this build's OnPlayerCollide actually reads for the name either isn't that field,
// or something resets it in between - not something worth reverse-engineering further from
// outside the assembly.
//
// Instead of touching that field at all, we sidestep it: OnPlayerCollideNamePrefix runs right
// before the real OnPlayerCollide and stashes the correct name (from the same ArrowsRegistry
// entry every arrow in this mod is already registered under) in pendingArrowName. Because the
// two FloatTexts are created synchronously inside that same call, FloatTextCtor_Prefix only
// ever needs to hand out pendingArrowName to the *next* null/empty-text FloatText it sees
// (the name line - the "ARROWS" line already has real text and never hits this branch), then
// clears it. This never has to know or trust which internal field the game actually reads.
public static class ArrowTypePickupHooks
{
    private static ILogger? logger;
    private static string? pendingArrowName;

    public static void RegisterHooks(IHarmony harmony, ILogger logger)
    {
        ArrowTypePickupHooks.logger = logger;

        harmony.Patch(
            AccessTools.Method(typeof(ArrowTypePickup), nameof(ArrowTypePickup.OnPlayerCollide), [typeof(Player)]),
            prefix: new HarmonyMethod(typeof(ArrowTypePickupHooks), nameof(OnPlayerCollide_Prefix)));

        harmony.Patch(
            AccessTools.Constructor(typeof(FloatText), [typeof(Vector2), typeof(string), typeof(Color), typeof(Color), typeof(float), typeof(float), typeof(bool)]),
            prefix: new HarmonyMethod(typeof(ArrowTypePickupHooks), nameof(FloatTextCtor_Prefix)));
    }

    // Never skips the real method - just figures out (from data we fully control) what the
    // upcoming FloatText's real name text should be, for FloatTextCtor_Prefix to pick up.
    // The second FloatText OnPlayerCollide creates is always the literal word "ARROWS", so
    // any ArrowPickupName that already ends in "Arrows" (e.g. "Ice Arrows", "Mech Arrows" -
    // several of ours read naturally with the word baked in) would otherwise show up twice
    // stacked together, e.g. "ICE ARROWS" / "ARROWS". Trimming a trailing " Arrows" here
    // leaves just "Ice" / "Arrows" - it only strips the suffix, so names that don't have it
    // (Latency, Blink, Gomboc, ...) are untouched.
    private const string ArrowsSuffix = " Arrows";

    private static void OnPlayerCollide_Prefix(ArrowTypePickup __instance)
    {
        var type = DynamicData.For(__instance).Get<ArrowTypes>("arrowType");
        var arrow = ArrowsRegistry.GetArrow(type);
        var name = arrow?.Configuration.ArrowPickupName;
        if (name is not null && name.EndsWith(ArrowsSuffix, StringComparison.OrdinalIgnoreCase))
        {
            name = name[..^ArrowsSuffix.Length];
        }
        pendingArrowName = name?.ToUpperInvariant();

        logger?.LogInformation(
            "[OopsAllBlinkArrows] OnPlayerCollide: arrowType={Type}, pendingArrowName={PendingName}",
            type, pendingArrowName ?? "<none>");
    }

    private static void FloatTextCtor_Prefix(ref string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            text = pendingArrowName ?? "ARROWS";
            pendingArrowName = null;
        }
    }
}
