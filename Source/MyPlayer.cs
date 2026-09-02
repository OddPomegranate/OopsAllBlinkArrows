using System.Collections.Generic;
using FortRise;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TowerFall;

namespace OopsAllBlinkArrows;

public static class MyPlayer
{
    public static Dictionary<int, bool> SlimePlayer = new Dictionary<int, bool>();

    public static void RegisterHooks(IHarmony harmony)
    {
        harmony.Patch(
            AccessTools.Method(typeof(Player), "Added"),
            postfix: new HarmonyMethod(typeof(MyPlayer), nameof(Added_Postfix)));

        harmony.Patch(
            AccessTools.Method(typeof(Player), "Update"),
            postfix: new HarmonyMethod(typeof(MyPlayer), nameof(Update_Postfix)));

        harmony.Patch(
            AccessTools.PropertyGetter(typeof(Player), "MaxRunSpeed"),
            prefix: new HarmonyMethod(typeof(MyPlayer), nameof(MaxRunSpeed_Prefix)));

        harmony.Patch(
            AccessTools.Method(typeof(Player), "CatchArrow", [typeof(Arrow)]),
            prefix: new HarmonyMethod(typeof(MyPlayer), nameof(CatchArrow_Prefix)));

        harmony.Patch(
            AccessTools.Method(typeof(Player), "CollectArrows", [typeof(ArrowTypes[])]),
            prefix: new HarmonyMethod(typeof(MyPlayer), nameof(CollectArrows_Prefix)));
    }

    private static void Added_Postfix(Player __instance)
    {
        SlimePlayer[__instance.PlayerIndex] = false;
    }

    private static void Update_Postfix(Player __instance)
    {
        var slime = __instance.CollideFirst(GameTags.Mud);
        SlimePlayer[__instance.PlayerIndex] = slime != null;
    }

    private static bool MaxRunSpeed_Prefix(Player __instance, ref float __result)
    {
        if (SlimePlayer.GetValueOrDefault(__instance.PlayerIndex))
        {
            var playerData = DynamicData.For(__instance);
            __result = playerData.Get("inMud") != null ? 0.2f : 0.4f;
            return false;
        }
        return true;
    }

    private static bool CatchArrow_Prefix(Player __instance, Arrow arrow)
    {
        var infiniteWarping = __instance.Level.Session.MatchSettings.Variants.GetCustomVariant("OopsAllBlinkArrows/InfiniteWarping");
        if (infiniteWarping != null && infiniteWarping.Value)
        {
            return true;
        }

        var playerdata = DynamicData.For(__instance);
        if (arrow.CanCatch(__instance) && !arrow.IsCollectible && arrow.CannotHit != __instance
            && (!__instance.HasShield || !arrow.Dangerous) && arrow != playerdata.Get("lastCaught"))
        {
            if (OopsAllArrowsModule.FreakyEntry != null && arrow.ArrowType == OopsAllArrowsModule.FreakyEntry.ArrowTypes)
            {
                arrow.OnPlayerCatch(__instance);
                Sounds.sfx_cyanWarp.Play();
                arrow.RemoveSelf();
                return false;
            }
        }
        return true;
    }

    // Matches the original mod's CollectArrowPatchs exactly: MiniMech "arrows" left lying
    // around after a Mech arrow detonates are picked up as a single-element [MiniMechType]
    // array, and the original just swallowed that silently (return true, no orig call) -
    // MiniMech was never meant to become a real quiver entry. Guarded against both
    // MiniMechEntry and arrows being null, which is the actual fix for the crash that
    // originally brought us to this method (see OopsAllArrowsMod.MyPlayer.CollectArrowsPrefix
    // in the crash log from earlier in this mod's history).
    private static bool CollectArrows_Prefix(ArrowTypes[] arrows, ref bool __result)
    {
        if (OopsAllArrowsModule.MiniMechEntry != null
            && arrows != null && arrows.Length == 1
            && arrows[0] == OopsAllArrowsModule.MiniMechEntry.ArrowTypes)
        {
            __result = true;
            return false;
        }
        return true;
    }
}