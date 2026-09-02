using System;
using System.Collections;
using FortRise;
using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace OopsAllBlinkArrows;

public class SeekerArrow : Arrow
{
    // This is automatically been set by the mod loader
    public override ArrowTypes ArrowType { get; set; }
    private bool used, canDie;
    private Image normalImage;
    private Image buriedImage;
    private Image angyImage;
    private Sprite<int> tentacles;
    private bool dash = false;
    private int dashTimer = 35;
    private Vector2 dashDirec = new Vector2(0, 0);

    protected override float SeekMinDistSq => 0f;
    protected override float SeekRadiusSq => 100000f;
    protected override float SeekMaxAngle => 2.7925268f;
    protected override float SeekTurnRate => (float)Math.PI / 25f;

    private Vector2 speedHelper = new Vector2(2f, 2f);
    protected override float StartSpeed => 2f;

    private bool firstDash = true;

    public static IArrowEntry Register(IModuleContext context, IModContent content)
    {
        var seekerHud = context.Registry.Subtextures.RegisterTexture(
            "SeekerArrowHud", () => BlinkModule.Atlas["SeekerArrowHud"]);

        return context.Registry.Arrows.RegisterArrows("Seeker", new ArrowConfiguration
        {
            ArrowPickupName = "Seeker",
            CreateArrow = () => new SeekerArrow(),
            CreateArrowPickupSprite = _ =>
            {
                var pickupSprite = new Sprite<int>(BlinkModule.Atlas["SeekerArrowPickup"], 12, 12, 0);
                pickupSprite.CenterOrigin();
                return pickupSprite;
            },
            HUD = seekerHud
        });
    }

    public SeekerArrow() : base()
    {

    }
    protected override void Init(LevelEntity owner, Vector2 position, float direction)
    {
        base.Init(owner, position, direction);
        used = (canDie = false);
        StopFlashing();
        dash = false;
        dashTimer = 35;
        firstDash = true;

    }
    protected override void CreateGraphics()
    {
        normalImage = new Image(BlinkModule.Atlas["SeekerArrow"]);
        normalImage.Origin = new Vector2(14f, 3f);
        buriedImage = new Image(BlinkModule.Atlas["SeekerArrowBuried"]);
        buriedImage.Origin = new Vector2(14f, 3f);
        angyImage = new Image(BlinkModule.Atlas["SeekerArrowAngy"]);
        angyImage.Origin = new Vector2(14f, 3f);
        tentacles = new Sprite<int>(BlinkModule.Atlas["SeekerArrowTentacles"], 5, 5);
        tentacles.Origin = new Vector2(14f, 3f);
        tentacles.Add(0, 0.1f, new int[8] { 0, 1, 2, 3, 4, 5, 6, 7 });
        tentacles.Play(0, false);

        Graphics = new Image[4] { normalImage, buriedImage, angyImage, tentacles };
        Add(Graphics);
    }

    protected override void InitGraphics()
    {
        normalImage.Visible = true;
        buriedImage.Visible = false;
        angyImage.Visible = false;
        tentacles.Visible = true;
    }

    protected override void SwapToBuriedGraphics()
    {
        normalImage.Visible = false;
        buriedImage.Visible = true;
        angyImage.Visible = false;
        tentacles.Origin = new Vector2(11f, 3f);
        tentacles.Visible = true;
    }

    protected override void SwapToUnburiedGraphics()
    {
        normalImage.Visible = true;
        buriedImage.Visible = false;
        angyImage.Visible = false;
        tentacles.Origin = new Vector2(14f, 3f);
        tentacles.Visible = true;
    }

    private void SwapToAngyGraphics()
    {
        normalImage.Visible = false;
        buriedImage.Visible = false;
        angyImage.Visible = true;
        tentacles.Visible = false;
    }

    public override bool CanCatch(LevelEntity catcher)
    {
        return !used && base.CanCatch(catcher);
    }
    public override void ShootUpdate()
    {
        if (!dash)
        {
            UpdateSeeking();
        }
    }
    public override void Update()
    {
        if (dash)
        {
            Speed = dashDirec * 3f;
            if (base.Level.OnInterval(4))
            {
                Add(new Coroutine(SeekerTrail.CreateSeekerTrail(Level, Position, normalImage.Rotation)));
            }
            if (State != ArrowStates.Shooting && State != ArrowStates.Buried)
            {
                dash = false;
                dashTimer = 300;
            }
        }

        base.Update();
        if (canDie)
        {
            RemoveSelf();
        }
        if (dashTimer > 0 && Level.Session.MatchSettings.Variants.GetCustomVariant("OopsAllBlinkArrows/IntrusiveThoughts"))
        {
            dashTimer--;
        }
        if (FindSeekTarget() != null && State == ArrowStates.Shooting && !firstDash)
        {

            SwapToAngyGraphics();

            if (Level.Session.MatchSettings.Variants.GetCustomVariant("OopsAllBlinkArrows/IntrusiveThoughts") && dash == false && dashTimer <= 0)
            {
                float num = (float)Math.PI;
                LevelEntity levelEntity = FindSeekTarget();
                Direction += MathHelper.Clamp(Calc.AngleDiff(Direction, WrapMath.WrapAngle(Position, levelEntity.Position + levelEntity.SeekOffset)), 0f - num, num);
                Speed = Calc.AngleToVector(Direction, StartSpeed);

                BlinkModule.SeekerDashSfx.SFX?.Play(base.X);

                dash = true;
                dashDirec = Speed;
            }
        }
        else
        {
            Speed.X /= 1.01f;
            Speed.Y /= 1.01f;
            if (State != ArrowStates.Buried)
            {
                SwapToUnburiedGraphics();
            }
            if (firstDash)
            {
                firstDash = false;
            }
        }
        if (State == ArrowStates.Buried)
        {
            SwapToBuriedGraphics();
        }
        if (State == ArrowStates.Gravity)
        {
            State = ArrowStates.Shooting;
        }



    }
    protected override void HitWall(Platform platform)
    {
        base.HitWall(platform);
        SwapToBuriedGraphics();
    }

    protected override void OnCollideH(Platform platform)
    {
        if (base.State != 0)
        {
            base.OnCollideH(platform);
            return;
        }

        if (Level.Session.MatchSettings.Variants.GetCustomVariant("OopsAllBlinkArrows/IntrusiveThoughts") && dash)
        {
            dash = false;
            dashTimer = 300;
        }

        Speed.X *= -1f;
        base.Direction = Calc.Angle(Speed);
    }

    protected override void OnCollideV(Platform platform)
    {
        if (base.State != 0)
        {
            base.OnCollideV(platform);
            return;
        }

        if (Level.Session.MatchSettings.Variants.GetCustomVariant("OopsAllBlinkArrows/IntrusiveThoughts") && dash)
        {
            dash = false;
            dashTimer = 300;
        }

        Speed.Y *= -1f;
        base.Direction = Calc.Angle(Speed);

    }
}
public class SeekerTrail : LevelEntity
{
    private Sprite<int> image;
    private int timer = 32;
    public SeekerTrail(Vector2 position, float rotation) : base(position)
    {
        Position = position;
        image = new Sprite<int>(BlinkModule.Atlas["SeekerArrowTrail"], 14, 5);
        image.Add(0, 0.1f, new int[4] { 0, 1, 2, 3});
        base.Collider = new Hitbox(1f, 1f, -4f, -4f);
        base.Collidable = false;
        image.CenterOrigin();
        image.Rotation = rotation;
        image.CurrentFrame = 0;
        Add(image);
    }

    public static IEnumerator CreateSeekerTrail(Level level, Vector2 at, float rotation)
    {
        SeekerTrail MySeekerTrail = new SeekerTrail(at, rotation);
        level.Add(MySeekerTrail);
        yield return 0.000000001f;
    }

    public override void Update()
    {
        base.Update();
        if (base.Level.OnInterval(1))
        {

            timer -= 1;
            if (timer <= 0)
            {
                RemoveSelf();
            }
            else if (timer <= 8)
            {
                image.CurrentFrame = 3;
            }
            else if (timer <= 16)
            {
                image.CurrentFrame = 2;
            }
            else if (timer <= 24)
            {
                image.CurrentFrame = 1;

            }
        }
    }
}
