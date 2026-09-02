using System;
using System.Collections;
using FortRise;
using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace OopsAllBlinkArrows;

public class PerimeterArrow : Arrow
{
    // This is automatically been set by the mod loader
    public override ArrowTypes ArrowType { get; set; }
    private bool used, canDie;
    private Image normalImage;
    private Image buriedImage;
    private int timeAlive = 0;

    public static IArrowEntry Register(IModuleContext context, IModContent content)
    {
        var perimeterHud = context.Registry.Subtextures.RegisterTexture(
            "PerimeterArrowHud", () => BlinkModule.Atlas["PerimeterArrowHud"]);

        return context.Registry.Arrows.RegisterArrows("Perimeter", new ArrowConfiguration
        {
            ArrowPickupName = "Perimeter",
            CreateArrow = () => new PerimeterArrow(),
            CreateArrowPickupSprite = _ =>
            {
                var pickupSprite = new Sprite<int>(BlinkModule.Atlas["PerimeterArrowPickup"], 12, 12, 0);
                pickupSprite.CenterOrigin();
                return pickupSprite;
            },
            HUD = perimeterHud
        });
    }

    public PerimeterArrow() : base()
    {
    }
    protected override void Init(LevelEntity owner, Vector2 position, float direction)
    {
        base.Init(owner, position, direction);
        used = (canDie = false);
        StopFlashing();
        timeAlive = 0;
    }
    protected override void CreateGraphics()
    {
        normalImage = new Image(BlinkModule.Atlas["PerimeterArrow"]);
        normalImage.Origin = new Vector2(18f, 3f);
        buriedImage = new Image(BlinkModule.Atlas["PerimeterArrowBuried"]);
        buriedImage.Origin = new Vector2(18f, 3f);
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
        if (State == ArrowStates.Shooting || State == ArrowStates.Gravity)
        {
            if (timeAlive > 2 && base.Level.OnInterval(1))
            {
                Add(new Coroutine(PerimeterBramble.CreatePerimeterBramble(Level, Position, normalImage.Rotation, PlayerIndex)));
            }
            else
            {
                timeAlive += 1;
            }
        }
    }
    protected override void HitWall(Platform platform)
    {
        base.HitWall(platform);
        SwapToBuriedGraphics();
    }
}
public class PerimeterBramble : Actor
{
    private FlashingImage image;
    private int lifetime = 600;
    private Vector2 movementhelper = new Vector2(0, 0);
    public int OwnerIndex { get; private set; }
    public PerimeterBramble(Vector2 position, float rotation) : base(position)
    {
        Position = position;
        movementhelper = Position;
        Tag(GameTags.PlayerCollider, GameTags.LavaCollider, GameTags.ExplosionCollider, GameTags.ShockCollider);

        ScreenWrap = true;
        base.Collider = new WrapHitbox(6f, 6f, -6f, -6f);
        base.Collidable = true;
        base.Pushable = false;
        base.IgnoreJumpThrus = true;
        image = new FlashingImage(BlinkModule.Atlas["PerimeterArrowBramble"]);
        image.CenterOrigin();
        image.Rotation = (float)((new Random().NextDouble()) * Math.PI);

        Add(image);
    }

    public static IEnumerator CreatePerimeterBramble(Level level, Vector2 at, float rotation, int ownerIndex)
    {
        PerimeterBramble MyPerimBramble = new PerimeterBramble(at, rotation);
        MyPerimBramble.OwnerIndex = ownerIndex;
        int depthmod = new Random().Next(-1, 2);
        MyPerimBramble.Depth += depthmod;
        level.Add(MyPerimBramble);
        yield return 0.000000001f;


    }
    public static void GetBrambleColors(int ownerIndex, bool teamsMode, Allegiance teamColor, out Color colorA, out Color colorB)
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
    public override void Added()
    {
        base.Added();
        GetBrambleColors(OwnerIndex, base.Level.Session.MatchSettings.TeamMode, base.Level.Session.MatchSettings.Teams[OwnerIndex], out var colorA, out var colorB);
        image.StartFlashing(4, colorA, colorB);
        if (base.Level.OnInterval(8))
        {
            Sounds.pu_brambleGrow.Play();
        }


    }
    public override void Update()
    {
        base.Update();
        if (base.Level.OnInterval(1))
        {
            if (!Level.Session.MatchSettings.Variants.GetCustomVariant("OopsAllBlinkArrows/Photosynthesis"))
            {
                lifetime -= 1;
            }

            if (lifetime <= 0)
            {
                if (base.Level.OnInterval(8))
                {
                    Sounds.pu_brambleDisappear.Play();
                }
                RemoveSelf();
            }
        }
    }

    public override void OnPlayerCollide(Player player)
    {
        player.Hurt(DeathCause.Brambles, Position, OwnerIndex, Sounds.pu_brambleDisappear);
    }

    public override void OnExplode(Explosion explosion, Vector2 normal)
    {
        RemoveSelf();
    }

    public override void OnShock(ShockCircle shock)
    {
        RemoveSelf();
    }

    public override void DoWrapRender()
    {
        image.DrawOutline();
        base.DoWrapRender();
    }
}
