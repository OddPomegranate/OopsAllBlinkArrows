using FortRise;
using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace OopsAllBlinkArrows;

public class GombocArrow : Arrow
{
    // This is automatically been set by the mod loader
    public override ArrowTypes ArrowType { get; set; }
    private bool used, canDie;
    private Image normalImage;
    private Image buriedImage;

    public static IArrowEntry Register(IModuleContext context, IModContent content)
    {
        var gombocHud = context.Registry.Subtextures.RegisterTexture(
            "GombocArrowHud", () => BlinkModule.Atlas["GombocArrowHud"]);

        return context.Registry.Arrows.RegisterArrows("Gomboc", new ArrowConfiguration
        {
            ArrowPickupName = "Gomboc",
            CreateArrow = () => new GombocArrow(),
            CreateArrowPickupSprite = _ =>
            {
                var pickupSprite = new Sprite<int>(BlinkModule.Atlas["GombocArrowPickup"], 12, 12, 0);
                pickupSprite.CenterOrigin();
                return pickupSprite;
            },
            HUD = gombocHud
        });
    }

    public GombocArrow() : base()
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
        normalImage = new Image(BlinkModule.Atlas["GombocArrow"]);
        normalImage.Origin = new Vector2(13f, 3f);
        buriedImage = new Image(BlinkModule.Atlas["GombocArrowBuried"]);
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
    public override void Update()
    {

        base.Update();
        if (canDie)
        {
            RemoveSelf();
        }
    }


    private Vector2 lastpos = new Vector2(-9999, -9999);
    private int slideenabler = 20;
    protected override void HitWall(Platform platform)
    {
        if (!Level.Session.MatchSettings.Variants.GetCustomVariant("OopsAllBlinkArrows/Monostasis"))
        {
            if (lastpos == Position && Speed.X == 0 && Speed.Y >= 0)
            {
                if (slideenabler <= 0)
                {
                    base.EnterFallModeBounceFrom(Position, false);
                }
                else
                {
                    slideenabler--;
                }
            }
            else
            {
                slideenabler = 20;
                lastpos = Position;
            }
        }
    }
}
