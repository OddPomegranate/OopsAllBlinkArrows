using System;
using System.Collections.Generic;
using FortRise;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod;
using MonoMod.Utils;
using TowerFall;

namespace OopsAllBlinkArrows;

// NOTE ON PORTING STRATEGY: the original mod bypassed TriggerArrow.Init entirely via a
// MonoModLinkTo/CallHelper.CallBaseGen trick, calling straight into Arrow.Init and skipping
// TriggerArrow's own Init body. That skip isn't cosmetic - TriggerArrow.Init() itself calls
// Sprite<int>.Play(0, ...) on the normalSprite/buriedSprite fields CreateGraphics() below
// builds, and since we never .Add() any frames to them (matching the vanilla
// TriggerBrambleArrow reference, which has the same bare Sprite<int> setup), that Play(0)
// throws KeyNotFoundException the instant this arrow spawns. FortRise's own mod loader runs
// every mod's assembly through a full MonoMod relinker pass (TowerFall.FortRise.mm's
// Relinker.Relink, modeled on Everest's), so the same [MonoModLinkTo] bypass
// TriggerBrambleArrow uses works here with no extra build tooling - base_Init below is
// rewritten by the relinker to call TowerFall.Arrow.Init directly, so LatencyArrow.Init can
// call it in place of base.Init(...) and skip TriggerArrow's own crashing Init body while
// still running Arrow's.
public class LatencyArrow : TriggerArrow
{
    [MonoModLinkTo("TowerFall.Arrow", "System.Void Init(TowerFall.LevelEntity,Microsoft.Xna.Framework.Vector2,System.Single)")]
    protected void base_Init(LevelEntity owner, Vector2 position, float direction)
    {
        base.Init(owner, position, direction);
    }

    // This is automatically been set by the mod loader
    public override ArrowTypes ArrowType { get; set; }
    private bool used, canDie;
    private List<Vector2> posHist = new List<Vector2>();
    private List<float> directionHist = new List<float>();
    private List<Vector2> speedHist = new List<Vector2>();
    private bool seekUse = false;

    private Image fletching;

    private float turnMod = 90f;
    protected override float SeekTurnRate => (float)Math.PI / turnMod;

    private float seekMod = 4900f;
    protected override float SeekRadiusSq => seekMod;

    private float seekMaxMod = 0.6981317f;
    protected override float SeekMaxAngle => seekMaxMod;

    public static IArrowEntry Register(IModuleContext context, IModContent content)
    {
        var latencyHud = context.Registry.Subtextures.RegisterTexture(
            "LatencyArrowHud", () => BlinkModule.Atlas["LatencyArrowHud"]);

        return context.Registry.Arrows.RegisterArrows("Latency", new ArrowConfiguration
        {
            ArrowPickupName = "Latency",
            CreateArrow = () => new LatencyArrow(),
            CreateArrowPickupSprite = _ =>
            {
                var pickupSprite = new Sprite<int>(BlinkModule.Atlas["LatencyArrowPickup"], 12, 12, 0);
                pickupSprite.CenterOrigin();
                return pickupSprite;
            },
            HUD = latencyHud
        });
    }

    // Modern replacement for the old On.TowerFall.TriggerArrow.* MonoMod hooks: there is no
    // MMHOOK_TowerFall.dll / hookgen assembly in this FortRise 5.x install, so method
    // interception goes through Harmony instead. SetDetonator is overloaded on TriggerArrow
    // (Player/Enemy) which is why the old hookgen names carried a _Player/_Enemy suffix to
    // disambiguate -- the real, reflectable method name underneath is just "SetDetonator".
    public static void RegisterHooks(IHarmony harmony)
    {
        harmony.Patch(
            AccessTools.Method(typeof(TriggerArrow), "SetDetonator", [typeof(Player)]),
            prefix: new HarmonyMethod(typeof(LatencyArrow), nameof(SetDetonatorPlayer_Prefix)));

        harmony.Patch(
            AccessTools.Method(typeof(TriggerArrow), "SetDetonator", [typeof(Enemy)]),
            prefix: new HarmonyMethod(typeof(LatencyArrow), nameof(SetDetonatorEnemy_Prefix)));

        harmony.Patch(
            AccessTools.Method(typeof(TriggerArrow), "Detonate"),
            prefix: new HarmonyMethod(typeof(LatencyArrow), nameof(Detonate_Prefix)));

        harmony.Patch(
            AccessTools.Method(typeof(TriggerArrow), "RemoveDetonator"),
            prefix: new HarmonyMethod(typeof(LatencyArrow), nameof(RemoveDetonator_Prefix)));
    }

    private static bool SetDetonatorPlayer_Prefix(TriggerArrow __instance, Player player)
    {
        if (__instance is LatencyArrow)
        {
            DynamicData.For(__instance).Set("playerDetonator", player);
            return false;
        }
        return true;
    }

    private static bool SetDetonatorEnemy_Prefix(TriggerArrow __instance, Enemy enemy)
    {
        if (__instance is LatencyArrow)
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
        if (__instance is LatencyArrow latency)
        {
            var dyn = DynamicData.For(__instance);
            dyn.Set("enemyDetonator", null);
            dyn.Set("playerDetonator", null);
            if (__instance.Scene != null && !__instance.MarkedForRemoval)
            {
                latency.UseBramblePower();
            }
            return false;
        }
        return true;
    }

    private static bool RemoveDetonator_Prefix(TriggerArrow __instance)
    {
        if (__instance is LatencyArrow)
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

    public LatencyArrow() : base()
    {
    }

    public static void GetOwnerColors(int ownerIndex, bool teamsMode, Allegiance teamColor, out Color colorA, out Color colorB)
    {
        if (teamsMode)
        {
            colorA = ArcherData.GetColorA(ownerIndex, teamColor);
            colorB = ArcherData.GetColorB(ownerIndex, teamColor);
        }
        else
        {
            colorA = ArcherData.GetColorA(ownerIndex);
            colorB = ArcherData.GetColorB(ownerIndex);
        }
    }
    protected override void CreateGraphics()
    {
        var self = DynamicData.For(this);
        var normalSprite = new Sprite<int>(BlinkModule.Atlas["LatencyArrow"], 13, 4);
        normalSprite.Origin = new Vector2(12, 3);
        normalSprite.OnAnimationComplete = (s) => { };

        fletching = new FlashingImage(BlinkModule.Atlas["LatencyArrowFletching"]);
        fletching.Origin = new Vector2(12f, 3f);
        Color colorA = ArcherData.GetColorA(PlayerIndex);
        Color colorB = ArcherData.GetColorB(PlayerIndex);

        var buriedSprite = new Sprite<int>(BlinkModule.Atlas["LatencyArrowBuried"], 13, 4);
        buriedSprite.Origin = new Vector2(12, 3);
        var eyeballSprite = TFGame.SpriteData.GetSpriteInt("TriggerArrowEyeball");
        var pupilSprite = TFGame.SpriteData.GetSpriteInt("TriggerArrowPupil");
        eyeballSprite.Visible = false;
        pupilSprite.Visible = false;

        this.Graphics = new Image[]
        {
            normalSprite,
            buriedSprite,
            eyeballSprite,
            pupilSprite,
            fletching
        };

        self.Set("normalSprite", normalSprite);
        self.Set("buriedSprite", buriedSprite);
        self.Set("eyeballSprite", eyeballSprite);
        self.Set("pupilSprite", pupilSprite);
        base.Add(this.Graphics);
    }

    protected override void InitGraphics()
    {
        base.InitGraphics();
        fletching.Color = ArcherData.GetColorA(base.CharacterIndex, ArcherData.ArcherTypes.Normal, base.TeamColor);
    }

    protected override void SwapToBuriedGraphics()
    {
        base.SwapToBuriedGraphics();
        fletching.Origin = new Vector2(8f, 3f);
    }

    protected override void SwapToUnburiedGraphics()
    {
        base.SwapToUnburiedGraphics();
        fletching.Origin = new Vector2(12f, 3f);
    }

    protected override void Init(LevelEntity owner, Vector2 position, float direction)
    {
        base_Init(owner, position, direction);
        posHist = new List<Vector2>();
        directionHist = new List<float>();
        speedHist = new List<Vector2>();
        seekUse = false;
        turnMod = 90f;
        seekMod = 4900f;
        LightVisible = true;
        var dynData = DynamicData.For(this);
        dynData.Get<Alarm>("enemyDetonateCheck").Stop();
        dynData.Set("playerDetonator", null);
        dynData.Set("enemyDetonator", null);
        if (owner is Enemy)
        {
            SetDetonator(owner as Enemy);
        }

        used = canDie = false;
        StopFlashing();
    }

    public override bool CanCatch(LevelEntity catcher)
    {
        return !used && base.CanCatch(catcher);
    }

    public void UseBramblePower()
    {
        if (posHist.Count > 0)
        {
            Sounds.char_dodgeStallGrab.Play();
            Position = posHist[0];
            Direction = directionHist[0];
            Speed = speedHist[0];
            State = ArrowStates.Shooting;

            if (Level.Session.MatchSettings.Variants.GetCustomVariant("OopsAllBlinkArrows/Resynchronization"))
            {
                seekUse = true;

            }
        }
    }


    public override void Update()
    {
        base.Update();
        if (canDie)
        {
            RemoveSelf();
        }

        if (State == ArrowStates.Shooting || State == ArrowStates.Gravity || State == ArrowStates.Falling)
        {

            posHist.Add(Position);
            directionHist.Add(Direction);
            speedHist.Add(Speed);

            if (posHist.Count > 30)
            {
                posHist.RemoveAt(0);
                directionHist.RemoveAt(0);
                speedHist.RemoveAt(0);
            }
        }
    }
    public override void ShootUpdate()
    {
        if (seekUse == true)
        {
            turnMod = 1f;
            seekMod = 50000f;
            seekMaxMod = (float)Math.PI;
        }
        this.UpdateSeeking();
        if (seekUse == true)
        {
            turnMod = 90f;
            seekMod = 4900f;
            seekMaxMod = 0.6981317f;
            seekUse = false;
        }
    }
}
