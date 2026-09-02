using System;
using System.Collections;
using FortRise;
using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace OopsAllBlinkArrows;

public class DecoyArrow : Arrow
{
    // This is automatically been set by the mod loader
    public override ArrowTypes ArrowType { get; set; }
    private bool used, canDie;
    private Image normalImage;
    private Image buriedImage;
    public Sprite<string> playerSprite;
    public Sprite<string> playerHeadSprite;
    public Sprite<string> playerBowSprite;
    public bool ignoreBow = false;

    public static IArrowEntry Register(IModuleContext context, IModContent content)
    {
        var decoyHud = context.Registry.Subtextures.RegisterTexture(
            "DecoyArrowHud", () => BlinkModule.Atlas["DecoyArrowHud"]);

        return context.Registry.Arrows.RegisterArrows("Decoy", new ArrowConfiguration
        {
            ArrowPickupName = "Decoy",
            CreateArrow = () => new DecoyArrow(),
            CreateArrowPickupSprite = _ =>
            {
                var pickupSprite = new Sprite<int>(BlinkModule.Atlas["DecoyArrowPickup"], 12, 12, 0);
                pickupSprite.CenterOrigin();
                return pickupSprite;
            },
            HUD = decoyHud
        });
    }

    public DecoyArrow() : base()
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
        normalImage = new Image(BlinkModule.Atlas["DecoyArrow"]);
        normalImage.Origin = new Vector2(13f, 3f);
        buriedImage = new Image(BlinkModule.Atlas["DecoyArrowBuried"]);
        buriedImage.Origin = new Vector2(13f, 3f);
        Graphics = new Image[2] { normalImage, buriedImage };
        Add(Graphics);
    }

    protected override void InitGraphics()
    {

        normalImage.Color = Color.Lerp(ArcherData.GetColorA(base.CharacterIndex, ArcherData.ArcherTypes.Normal, base.TeamColor), Color.White, 0.4f);
        buriedImage.Color = Color.Lerp(ArcherData.GetColorA(base.CharacterIndex, ArcherData.ArcherTypes.Normal, base.TeamColor), Color.White, 0.4f);
        normalImage.Visible = true;
        buriedImage.Visible = false;

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

    protected override void HitWall(Platform platform)
    {
        if (CollidedH)
        {
            if (Speed.X < 0)
            {
                Add(new Coroutine(UnrealArcher.CreateUnrealArcher(Level, Position + new Vector2(7, 0), playerSprite, playerHeadSprite, playerBowSprite, PlayerIndex, ignoreBow)));
            }
            else
            {
                Add(new Coroutine(UnrealArcher.CreateUnrealArcher(Level, Position - new Vector2(7, 0), playerSprite, playerHeadSprite, playerBowSprite, PlayerIndex, ignoreBow)));
            }
        }
        else
        {
            if (Speed.Y > 0)
            {
                Add(new Coroutine(UnrealArcher.CreateUnrealArcher(Level, Position - new Vector2(0, 8), playerSprite, playerHeadSprite, playerBowSprite, PlayerIndex, ignoreBow)));
            }
            else
            {
                Add(new Coroutine(UnrealArcher.CreateUnrealArcher(Level, Position + new Vector2(0, 8), playerSprite, playerHeadSprite, playerBowSprite, PlayerIndex, ignoreBow)));
            }
        }
        base.HitWall(platform);



    }
}
public class UnrealArcher : Actor
{
    public Sprite<string> body;
    public Sprite<string> head;
    public Sprite<string> bow;
    public bool ignoreBow = false;
    private float ySpeed = 0;
    public int OwnerIndex { get; private set; }
    public UnrealArcher(Vector2 position) : base(position)
    {
        Position = position;
        Tag(GameTags.PlayerCollider, GameTags.LavaCollider, GameTags.ExplosionCollider, GameTags.ShockCollider);

        ScreenWrap = true;
        base.Collider = new WrapHitbox(8f, 16f, -4f, -8f);
        base.Collidable = true;
        base.Pushable = true;
        base.IgnoreJumpThrus = false;
    }

    public static IEnumerator CreateUnrealArcher(Level level, Vector2 at, Sprite<string> bodySprite, Sprite<string> headSprite, Sprite<string> bowSprite, int ownerIndex, bool ignoretehBow)
    {
        UnrealArcher MyUnrealArcher = new UnrealArcher(at);
        MyUnrealArcher.OwnerIndex = ownerIndex;
        MyUnrealArcher.body = bodySprite;
        MyUnrealArcher.head = headSprite;
        MyUnrealArcher.bow = bowSprite;
        if (ignoretehBow)
        {
            MyUnrealArcher.bow.Visible = false;
            MyUnrealArcher.ignoreBow = true;
        }

        //never happens. tweak code later, make sprites repos themselves.
        if (new Random().Next(0, 1) == 1)
        {
            MyUnrealArcher.body.FlipX = true;
            MyUnrealArcher.head.FlipX = true;
            MyUnrealArcher.bow.FlipX = true;
            MyUnrealArcher.body.Position.X *= -1;
            MyUnrealArcher.head.Position.X *= -1;
            MyUnrealArcher.bow.Position.X *= -1;
        }

        MyUnrealArcher.Add(bodySprite);
        MyUnrealArcher.Add(headSprite);
        MyUnrealArcher.Add(bowSprite);


        level.Add(MyUnrealArcher);
        yield return 0.000000001f;


    }
    public override void Added()
    {
        base.Added();


    }
    public override void Update()
    {
        base.Update();
        if (ySpeed < 5)
        {
            ySpeed += 0.3f;
        }
        MoveV(ySpeed * Engine.TimeMult, onCollideV);
    }

    private void onCollideV(Platform platform)
    {
        if (ySpeed > 0f)
        {

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
        body.DrawOutline();
        head.DrawOutline();
        if (!ignoreBow)
        {
            bow.DrawOutline();
        }


        base.DoWrapRender();
    }
}
