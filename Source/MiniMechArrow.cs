using FortRise;
using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace OopsAllBlinkArrows;

// MiniMechArrow is a "sub-munition" - it's never dropped in a treasure chest on its own,
// only spawned directly (3 or 6 at once) by MechArrow's Explode(). It still needs to be
// a fully registered arrow (graphics, HUD icon) since Arrow.Create(...) needs an ArrowTypes
// to spawn it as, and CollectArrowsPrefix in MyPlayer.cs special-cases it to allow it into
// the quiver via that direct-spawn path (see MyPlayer.cs).
public class MiniMechArrow : Arrow
{
    // This is automatically been set by the mod loader
    public override ArrowTypes ArrowType { get; set; }
    private bool used, canDie;
    private Image normalImage;
    private Image buriedImage;

    public static IArrowEntry Register(IModuleContext context, IModContent content)
    {
        var miniMechHud = context.Registry.Subtextures.RegisterTexture(
            "MiniMechArrowHud", () => OopsAllArrowsModule.ArrowAtlas["MechArrowHud"]);

        return context.Registry.Arrows.RegisterArrows("MiniMechArrow", new ArrowConfiguration
        {
            ArrowPickupName = "MiniMech Arrows",
            CreateArrow = () => new MiniMechArrow(),
            CreateArrowPickupSprite = _ =>
            {
                var pickupSprite = new Sprite<int>(OopsAllArrowsModule.ArrowAtlas["MechArrowPickup"], 12, 12, 0);
                pickupSprite.Add(0, 0.3f, new int[2] { 0, 0 });
                pickupSprite.Play(0, false);
                pickupSprite.CenterOrigin();
                return pickupSprite;
            },
            HUD = miniMechHud
        });
    }

    public MiniMechArrow() : base()
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
        normalImage = new Image(OopsAllArrowsModule.ArrowAtlas["MiniMechArrow"]);
        normalImage.Origin = new Vector2(13f, 3f);
        buriedImage = new Image(OopsAllArrowsModule.ArrowAtlas["MiniMechArrowBuried"]);
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
}
