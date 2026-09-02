using System;
using System.Collections;
using FortRise;
using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace OopsAllBlinkArrows;

public class NyoomArrow : Arrow
{
    // This is automatically been set by the mod loader
    public override ArrowTypes ArrowType { get; set; }
    private bool used, canDie;
    private Image normalImage;
    private Image buriedImage;
    private Image unfiredImage;

    public static IArrowEntry Register(IModuleContext context, IModContent content)
    {
        var nyoomHud = context.Registry.Subtextures.RegisterTexture(
            "NyoomArrowHud", () => BlinkModule.Atlas["NyoomArrowHud"]);

        return context.Registry.Arrows.RegisterArrows("Nyoom", new ArrowConfiguration
        {
            ArrowPickupName = "Nyoom",
            CreateArrow = () => new NyoomArrow(),
            CreateArrowPickupSprite = _ =>
            {
                var pickupSprite = new Sprite<int>(BlinkModule.Atlas["NyoomArrowPickup"], 12, 12, 0);
                pickupSprite.CenterOrigin();
                return pickupSprite;
            },
            HUD = nyoomHud
        });
    }

    public NyoomArrow() : base()
    {
    }
    protected override void Init(LevelEntity owner, Vector2 position, float direction)
    {
        base.Init(owner, position, direction);
        Sounds.pu_brambleGrow.Play();
        used = (canDie = false);
        StopFlashing();
    }
    protected override void CreateGraphics()
    {
        normalImage = new Image(BlinkModule.Atlas["NyoomArrow"]);
        normalImage.Origin = new Vector2(13f, 3f);
        buriedImage = new Image(BlinkModule.Atlas["NyoomArrowBuried"]);
        buriedImage.Origin = new Vector2(13f, 3f);
        unfiredImage = new Image(BlinkModule.Atlas["NyoomArrowUnfired"]);
        unfiredImage.Origin = new Vector2(13f, 3f);
        Graphics = new Image[3] { normalImage, buriedImage, unfiredImage };
        Add(Graphics);
    }

    protected override void InitGraphics()
    {
        normalImage.Visible = true;
        buriedImage.Visible = false;
        unfiredImage.Visible = false;
    }

    protected override void SwapToBuriedGraphics()
    {
        normalImage.Visible = false;
        buriedImage.Visible = true;
        unfiredImage.Visible = false;
    }

    protected override void SwapToUnburiedGraphics()
    {
        normalImage.Visible = true;
        buriedImage.Visible = false;
        unfiredImage.Visible = false;
    }

    private void SwapToFallingGraphics()
    {
        normalImage.Visible = false;
        buriedImage.Visible = false;
        unfiredImage.Visible = true;
    }

    public override bool CanCatch(LevelEntity catcher)
    {
        return !used && base.CanCatch(catcher);
    }

    public override void Update()
    {

        if (State == ArrowStates.Shooting)
        {
            for (int i = 0; i < 8; i++)
            {
                base.Update();

            }
            Add(new Coroutine(NyoomSpeedLoop.CreateNyoomSpeedLoop(Level, Position, normalImage.Rotation)));
        }
        else
        {
            base.Update();
        }
        if (canDie)
        {
            RemoveSelf();
        }
        if (Level.Session.MatchSettings.Variants.GetCustomVariant("OopsAllBlinkArrows/KineticFusion") && State != ArrowStates.Shooting)
        {
            this.used = true;
            Explosion.Spawn(base.Level, Position, PlayerIndex, true, false, false);
            Sounds.pu_superBombExplode.Play(base.X);
            canDie = true;
        }
        else if (State == ArrowStates.Falling)
        {
            SwapToFallingGraphics();
        }

    }

    protected override void HitWall(Platform platform)
    {
        SwapToBuriedGraphics();
        base.HitWall(platform);
    }

    public override void ShootUpdate()
    {
        UpdateSeeking();
    }
}
public class NyoomSpeedLoop : LevelEntity
{
    private Image image;
    private int timer = 8;
    public NyoomSpeedLoop(Vector2 position, float rotation) : base(position)
    {
        Position = position;
        image = new Image(BlinkModule.Atlas["NyoomArrowSpeedLoop"]);
        base.Collider = new Hitbox(1f, 1f, -4f, -4f);
        base.Collidable = false;
        image.CenterOrigin();
        image.Rotation = (float)(rotation + (Math.PI / 2));
        image.Scale = new Vector2(0.01f, 0.01f);
        Add(image);
    }

    public static IEnumerator CreateNyoomSpeedLoop(Level level, Vector2 at, float rotation)
    {
        NyoomSpeedLoop MyNyoomLoop = new NyoomSpeedLoop(at, rotation);
        level.Add(MyNyoomLoop);
        yield return 0.000000001f;
    }

    public override void Update()
    {
        base.Update();
        if (base.Level.OnInterval(1))
        {

            timer -= 1;
            if (timer <= -24)
            {
                RemoveSelf();
            }
            else if (timer <= -16)
            {
                image.Scale -= new Vector2(0.1f, 0.1f);
            }
            else if (timer <= -8)
            {
                image.Scale -= new Vector2(0.04f, 0.04f);
            }
            else if (timer <= -0)
            {
                image.Scale += new Vector2(0.04f, 0.04f);
            }
            else if (timer <= 8)
            {
                image.Scale += new Vector2(0.1f, 0.1f);
            }
        }
    }
}
