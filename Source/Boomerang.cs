using FortRise;
using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace OopsAllBlinkArrows;

public class BoomerangArrow : Arrow
{
    // This is automatically been set by the mod loader
    public override ArrowTypes ArrowType { get; set; }
    private bool used, canDie;
    private Sprite<int> normalImage;
    private Sprite<int> buriedImage;
    private Alarm explodeAlarm;
    private const float SPEED = 6f;
    public Vector2 turnPos;
    protected override float StartSpeed => 6f;

    public static IArrowEntry Register(IModuleContext context, IModContent content)
    {
        var boomerangHud = context.Registry.Subtextures.RegisterTexture(
            "BoomerangArrowHud", () => OopsAllArrowsModule.ArrowAtlas["BoomerangArrowHud"]);

        return context.Registry.Arrows.RegisterArrows("Boomerang", new ArrowConfiguration
        {
            ArrowPickupName = "Boomerang Arrows",
            CreateArrow = () => new BoomerangArrow(),
            CreateArrowPickupSprite = _ =>
            {
                var pickupSprite = new Sprite<int>(OopsAllArrowsModule.ArrowAtlas["BoomerangArrowPickup"], 12, 12, 0);
                pickupSprite.Add(0, 0.3f, new int[2] { 0, 0 });
                pickupSprite.Play(0, false);
                pickupSprite.CenterOrigin();
                return pickupSprite;
            },
            HUD = boomerangHud
        });
    }

    public BoomerangArrow() : base()
    {
    }
    protected override void Init(LevelEntity owner, Vector2 position, float direction)
    {
        base.Init(owner, position, direction);
        used = (canDie = false);
        explodeAlarm = Alarm.Create(Alarm.AlarmMode.Persist, Explode, 23);
        explodeAlarm.Start();
        StopFlashing();
        turnPos = Owner.Position;
    }
    public void Explode()
    {
        Turn(3.141593f);
    }
    protected override void CreateGraphics()
    {
        normalImage = new Sprite<int>(OopsAllArrowsModule.ArrowAtlas["BoomerangArrow"], 13, 4);
        normalImage.Origin = new Vector2(13f, 3f);
        normalImage.Add(0, 0.1f, new int[2] { 0, 1 });
        normalImage.Play(0, false);
        buriedImage = new Sprite<int>(OopsAllArrowsModule.ArrowAtlas["BoomerangArrowBuried"], 13, 4);
        buriedImage.Origin = new Vector2(13f, 3f);
        buriedImage.Add(0, 0.1f, new int[2] { 0, 0 });
        buriedImage.Play(0, false);
        Graphics = new Image[2] { normalImage, buriedImage };
        Add(Graphics);
    }

    protected override void InitGraphics()
    {
        normalImage.Visible = true;
        buriedImage.Visible = false;
    }

    protected override void SwapToBuriedGraphics()
    {
        normalImage.Visible = false;
        buriedImage.Visible = true;
    }

    protected override void SwapToUnburiedGraphics()
    {
        normalImage.Visible = true;
        buriedImage.Visible = false;
    }
    protected override void HitWall(TowerFall.Platform platform)
    {
        if (!used && Level.Session.MatchSettings.Variants.GetCustomVariant("OopsAllBlinkArrows/SonicBoom"))
        {
            this.used = true;
            Explosion.Spawn(platform.Level, Position, PlayerIndex, true, false, false);
            canDie = true;
        }

        base.HitWall(platform);
    }
    public override bool CanCatch(LevelEntity catcher)
    {
        return !used && base.CanCatch(catcher);
    }
    public override void Update()
    {
        if (explodeAlarm.Active)
        {
            explodeAlarm.Update();
        }
        if (canDie)
        {
            RemoveSelf();
        }
        if ((bool)BuriedIn && Level.Session.MatchSettings.Variants.GetCustomVariant("OopsAllBlinkArrows/SonicBoom"))
        {
            Explosion.Spawn(base.Level, Position, PlayerIndex, true, false, false);
            canDie = true;
        }
        base.Update();
    }
    private void Turn(float turnAngle)
    {
        float num = base.Direction;
        base.Direction += turnAngle;
        Sounds.sfx_boltArrowTurn.Play(base.X);
        base.Direction = WrapMath.WrapAngle(Position, turnPos);
        Speed = Calc.AngleToVector(base.Direction, StartSpeed);
        base.Direction = num;
    }
}
