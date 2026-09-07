using System;
using FortRise;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TowerFall;

namespace OopsAllBlinkArrows;

// Same porting strategy as BlinkArrows' LatencyArrow (also a TriggerArrow subclass): the
// original bypassed TriggerArrow.Init via a MonoModLinkTo/CallHelper.CallBaseGen trick, calling
// straight into Arrow.Init and skipping TriggerArrow's own Init body. That skip isn't cosmetic:
// TriggerArrow.Init() itself expects a base "normalSprite"/"buriedSprite" Sprite<int> pair (set
// up via CreateGraphics(), like the vanilla TriggerBrambleArrow reference does) and plays a "0"
// animation frame on them. MechArrow's own CreateGraphics() below builds plain Image fields
// instead and never populates those base sprite fields at all, so letting TriggerArrow.Init()
// run for real is a crash here too (LatencyArrow's version of the same bug threw
// KeyNotFoundException; this one would fail the same way TriggerArrow.Init() expects those
// fields to exist). This used to rely on FortRise's mod loader running every mod's assembly
// through a full MonoMod relinker pass at load time, rewriting a [MonoModLinkTo]-tagged
// base_Init stub to call TowerFall.Arrow.Init directly. MonoMod.Patcher (the package that
// attribute lives in) is being deprecated, so base_Init now gets the same "call this exact
// base implementation, skip TriggerArrow's override" behavior from CallHelper.CallBaseGen - a
// small runtime-emitted non-virtual call, no relinker or extra build tooling needed. See
// CallHelper.cs for how it works. The detonator methods (which fully replace behavior rather
// than augment it) still go through Harmony prefixes that return false to skip the original.
public class MechArrow : TriggerArrow
{
    private static readonly Action<TriggerArrow, LevelEntity, Vector2, float> ArrowInit =
        CallHelper.CallBaseGen<TriggerArrow, LevelEntity, Vector2, float>(
            typeof(Arrow), "Init", typeof(LevelEntity), typeof(Vector2), typeof(float));

    protected void base_Init(LevelEntity owner, Vector2 position, float direction)
    {
        ArrowInit(this, owner, position, direction);
    }

    // This is automatically been set by the mod loader
    public override ArrowTypes ArrowType { get; set; }
    private bool used, canDie;
    private Image normalImage;
    private Image buriedImage;
    private Alarm explodeAlarm;
    public bool CanExplode;
    private Counter cannotPickupCounter;

    public static IArrowEntry Register(IModuleContext context, IModContent content)
    {
        var mechHud = context.Registry.Subtextures.RegisterTexture(
            "MechArrowHud", () => OopsAllArrowsModule.ArrowAtlas["MechArrowHud"]);

        return context.Registry.Arrows.RegisterArrows("Mech", new ArrowConfiguration
        {
            ArrowPickupName = "Mech Arrows",
            CreateArrow = () => new MechArrow(),
            CreateArrowPickupSprite = _ =>
            {
                var pickupSprite = new Sprite<int>(OopsAllArrowsModule.ArrowAtlas["MechArrowPickup"], 12, 12, 0);
                pickupSprite.Add(0, 0.3f, new int[2] { 0, 0 });
                pickupSprite.Play(0, false);
                pickupSprite.CenterOrigin();
                return pickupSprite;
            },
            HUD = mechHud
        });
    }

    public static void RegisterHooks(IHarmony harmony)
    {
        harmony.Patch(
            AccessTools.Method(typeof(TriggerArrow), "SetDetonator", [typeof(Player)]),
            prefix: new HarmonyMethod(typeof(MechArrow), nameof(SetDetonatorPlayer_Prefix)));

        harmony.Patch(
            AccessTools.Method(typeof(TriggerArrow), "SetDetonator", [typeof(Enemy)]),
            prefix: new HarmonyMethod(typeof(MechArrow), nameof(SetDetonatorEnemy_Prefix)));

        harmony.Patch(
            AccessTools.Method(typeof(TriggerArrow), "Detonate"),
            prefix: new HarmonyMethod(typeof(MechArrow), nameof(Detonate_Prefix)));

        harmony.Patch(
            AccessTools.Method(typeof(TriggerArrow), "RemoveDetonator"),
            prefix: new HarmonyMethod(typeof(MechArrow), nameof(RemoveDetonator_Prefix)));
    }

    private static bool SetDetonatorPlayer_Prefix(TriggerArrow __instance, Player player)
    {
        if (__instance is MechArrow)
        {
            DynamicData.For(__instance).Set("playerDetonator", player);
            return false;
        }
        return true;
    }

    private static bool SetDetonatorEnemy_Prefix(TriggerArrow __instance, Enemy enemy)
    {
        if (__instance is MechArrow)
        {
            var dyn = DynamicData.For(__instance);
            dyn.Set("enemyDetonator", enemy);
            dyn.Get<Alarm>("enemyDetonateCheck").Start();
            return false;
        }
        return true;
    }

    private static bool Detonate_Prefix(TriggerArrow __instance)
    {
        if (__instance is MechArrow mech)
        {
            var dyn = DynamicData.For(__instance);
            dyn.Set("enemyDetonator", null);
            dyn.Set("playerDetonator", null);
            if (__instance.Scene != null && !__instance.MarkedForRemoval)
            {
                mech.Explode();
            }
            return false;
        }
        return true;
    }

    private static bool RemoveDetonator_Prefix(TriggerArrow __instance)
    {
        if (__instance is MechArrow)
        {
            __instance.LightVisible = false;
            var dyn = DynamicData.For(__instance);
            Player player = dyn.Get<Player>("playerDetonator");
            dyn.Set("playerDetonator", null);
            if (player != null)
            {
                player.RemoveTriggerArrow(__instance);
            }
            dyn.Set("enemyDetonator", null);
            return false;
        }
        return true;
    }

    public MechArrow() : base()
    {
    }
    protected override void Init(LevelEntity owner, Vector2 position, float direction)
    {
        cannotPickupCounter = new Counter();
        cannotPickupCounter.Set(0);
        base_Init(owner, position, direction);
        used = (canDie = false);
        CanExplode = false;

        explodeAlarm = Alarm.Create(Alarm.AlarmMode.Persist, Explode, 30);
        explodeAlarm.Start();
        StopFlashing();
    }

    protected override void HitWall(TowerFall.Platform platform)
    {
        base.HitWall(platform);
        cannotPickupCounter.Set(15);
    }
    public override void Bury(ArrowCushion buryIn, float moveIn = 0f, bool drawHead = false)
    {
        base.Bury(buryIn, moveIn, drawHead);
        cannotPickupCounter.Set(15);
        Speed = Vector2.Zero;
    }
    public override bool IsCollectible
    {
        get
        {
            if (!cannotPickupCounter)
            {
                return State >= ArrowStates.Stuck;
            }
            return false;
        }
    }

    public override void Render()
    {
        normalImage.DrawOutline();
        buriedImage.DrawOutline();
        normalImage.Render();
        buriedImage.Render();
    }

    protected override void CreateGraphics()
    {
        normalImage = new Image(OopsAllArrowsModule.ArrowAtlas["MechArrow"]);
        normalImage.Origin = new Vector2(13f, 3f);
        buriedImage = new Image(OopsAllArrowsModule.ArrowAtlas["MechArrowBuried"]);
        buriedImage.Origin = new Vector2(13f, 3f);
        Graphics = new Image[2] { normalImage, buriedImage };
        Add(Graphics);
    }

    protected override void InitGraphics()
    {
        normalImage.Visible = true;
        buriedImage.Visible = false;
    }
    public void Explode()
    {
        CanExplode = true;
    }
    protected override void SwapToBuriedGraphics()
    {
        Graphics[0].Visible = false;
        Graphics[1].Visible = true;
    }

    protected override void SwapToUnburiedGraphics()
    {
        Graphics[0].Visible = true;
        Graphics[1].Visible = false;
    }

    public override bool CanCatch(LevelEntity catcher)
    {
        return !used && base.CanCatch(catcher);
    }
    public override void Update()
    {
        if ((bool)cannotPickupCounter)
        {
            cannotPickupCounter.Update();
        }
        base.Update();
        if (canDie)
        {
            RemoveSelf();
        }
        if (explodeAlarm.Active)
        {
            explodeAlarm.Update();
        }
        if (State != ArrowStates.Stuck && State != ArrowStates.Buried && State != ArrowStates.LayingOnGround)
        {
            if (!used && CanExplode)
            {
                var miniMechType = OopsAllArrowsModule.MiniMechEntry.ArrowTypes;
                if (!Level.Session.MatchSettings.Variants.GetCustomVariant("OopsAllBlinkArrows/DoubleSpread"))
                {
                    var middle = Arrow.Create(miniMechType, Owner, Position, Direction);
                    var top = Arrow.Create(miniMechType, Owner, Position + new Vector2(0, 1), Direction + 0.6011317f);
                    var bottom = Arrow.Create(miniMechType, Owner, Position - new Vector2(0, 1), Direction - 0.6011317f);
                    Level.Add(middle, top, bottom);
                    canDie = true;
                    used = true;
                }
                else
                {
                    var corebottom = Arrow.Create(miniMechType, Owner, Position - new Vector2(0, 0.5f), Direction - 0.151f);
                    var coretop = Arrow.Create(miniMechType, Owner, Position + new Vector2(0, 0.5f), Direction + 0.151f);
                    var top = Arrow.Create(miniMechType, Owner, Position + new Vector2(0, 1), Direction + 0.4511317f);
                    var bottom = Arrow.Create(miniMechType, Owner, Position - new Vector2(0, 1), Direction - 0.4511317f);
                    var finaltop = Arrow.Create(miniMechType, Owner, Position + new Vector2(0, 1.5f), Direction + 0.7011317f);
                    var finalbottom = Arrow.Create(miniMechType, Owner, Position - new Vector2(0, 1.5f), Direction - 0.7011317f);
                    Level.Add(corebottom, coretop, finaltop, finalbottom, top, bottom);
                    canDie = true;
                    used = true;
                }
            }
        }
    }
}
