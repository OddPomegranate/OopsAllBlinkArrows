using FortRise;
using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace OopsAllBlinkArrows;

public class LandMineArrow : Arrow
{
    // This is automatically been set by the mod loader
    public override ArrowTypes ArrowType { get; set; }
    private bool used, canDie;
    private Image normalImage;
    private Image buriedImage;
    private bool canExplode;

    public static IArrowEntry Register(IModuleContext context, IModContent content)
    {
        var landMineHud = context.Registry.Subtextures.RegisterTexture(
            "LandMineArrowHud", () => OopsAllArrowsModule.ArrowAtlas["LandMineArrowHud"]);

        return context.Registry.Arrows.RegisterArrows("LandMine", new ArrowConfiguration
        {
            ArrowPickupName = "Land Mine Arrows",
            CreateArrow = () => new LandMineArrow(),
            CreateArrowPickupSprite = _ =>
            {
                var pickupSprite = new Sprite<int>(OopsAllArrowsModule.ArrowAtlas["LandMineArrowPickup"], 12, 12, 0);
                pickupSprite.Add(0, 0.3f, new int[2] { 0, 0 });
                pickupSprite.Play(0, false);
                pickupSprite.CenterOrigin();
                return pickupSprite;
            },
            HUD = landMineHud
        });
    }
    public override void HitLava()
    {
        Explosion.Spawn(base.Level, Position, base.PlayerIndex, false, triggerBomb: false, bombTrap: false);
        RemoveSelf();
    }

    public override void OnPlayerCollide(Player player)
    {
        Collidable = false;
        Explosion.Spawn(player.Level, Position, PlayerIndex, false, false, false);
        RemoveSelf();
    }
    public LandMineArrow() : base()
    {
    }
    protected override void Init(LevelEntity owner, Vector2 position, float direction)
    {
        base.Init(owner, position, direction);
        used = (canDie = false);
        StopFlashing();
    }
    protected override void CreateGraphics()
    {
        normalImage = new Image(OopsAllArrowsModule.ArrowAtlas["LandMineArrow"]);
        normalImage.Origin = new Vector2(13f, 3f);
        buriedImage = new Image(OopsAllArrowsModule.ArrowAtlas["LandMineArrowBuried"]);
        buriedImage.Origin = new Vector2(13f, 3f);
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

    public override bool CanCatch(LevelEntity catcher)
    {
        return !used && base.CanCatch(catcher);
    }
    protected override void HitWall(TowerFall.Platform platform)
    {
        if (!used)
        {
            this.used = true;
            Add(new Coroutine(LandMine.CreateLandMine(Level, Position, buriedImage.Rotation, PlayerIndex, () => canDie = true)));
        }

        base.HitWall(platform);
    }
    public override void Update()
    {

        base.Update();
        if (canDie)
        {
            RemoveSelf();
        }
        if ((bool)BuriedIn)
        {
            Explosion.Spawn(base.Level, Position, PlayerIndex, true, false, false);
            canDie = true;
        }
    }
}
