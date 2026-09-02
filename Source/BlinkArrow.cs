using System;
using System.IO;
using FortRise;
using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace OopsAllBlinkArrows;

public class BlinkArrow : Arrow
{
    // This is automatically been set by the mod loader
    public override ArrowTypes ArrowType { get; set; }
    private bool used, canDie;
    private Image normalImage;
    private Image buriedImage;
    bool blinked = false;

    public static IArrowEntry Register(IModuleContext context, IModContent content)
    {
        var blinkHud = context.Registry.Subtextures.RegisterTexture(
            "BlinkArrowHud", () => BlinkModule.Atlas["BlinkArrowHud"]);

        return context.Registry.Arrows.RegisterArrows("Blink", new ArrowConfiguration
        {
            ArrowPickupName = "Blink",
            CreateArrow = () => new BlinkArrow(),
            CreateArrowPickupSprite = _ =>
            {
                var pickupSprite = new Sprite<int>(BlinkModule.Atlas["BlinkArrowPickup"], 12, 12, 0);
                pickupSprite.CenterOrigin();
                return pickupSprite;
            },
            HUD = blinkHud
        });
    }

    public BlinkArrow() : base()
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
        normalImage = new Image(BlinkModule.Atlas["BlinkArrow"]);
        normalImage.Origin = new Vector2(13f, 3f);
        buriedImage = new Image(BlinkModule.Atlas["BlinkArrowBuried"]);
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
    protected override bool CheckForTargetCollisions()
    {
        if (Level.Session.MatchSettings.Variants.GetCustomVariant("OopsAllBlinkArrows/MatterDisplacement"))
        {
            foreach (Entity entity in base.Level[GameTags.Target])
            {
                var levelEntity = (LevelEntity)entity;
                if (levelEntity.ArrowCheck(this) && levelEntity != this.CannotHit)
                {
                    if (!used)
                    {
                        this.used = true;
                        var pos = Owner.Position;
                        Owner.Position = Position + new Vector2(0, -4);
                        Position = pos;
                        BlinkModule.TzzSfx.SFX?.Play(base.X);
                        canDie = true;
                    }
                }
            }
            return false;
        }
        else
        {
            return base.CheckForTargetCollisions();
        }

    }
    public override void Update()
    {

        if (State == ArrowStates.Shooting && Level.Session.MatchSettings.Variants.GetCustomVariant("OopsAllBlinkArrows/MatterDisplacement"))
        {
            for (int i = 0; i < 8; i++)
            {
                base.Update();

            }
        }
        else
        {
            base.Update();
        }
        if (canDie)
        {
            RemoveSelf();
        }
    }
    public override void ShootUpdate()
    {
        if (!Level.Session.MatchSettings.Variants.GetCustomVariant("OopsAllBlinkArrows/MatterDisplacement"))
        {
            base.ShootUpdate();
        }
    }

    protected override void HitWall(TowerFall.Platform platform)
    {
        if (!used)
        {
            this.used = true;
            var pos = Owner.Position;
            Owner.Position = Position;
            if (CollidedH)
            {
                if (Speed.X < 0)
                {
                    Owner.Position.X += 5;
                }
                else
                {
                    Owner.Position.X -= 5;
                }
            }
            else
            {
                if (Speed.Y > 0)
                {
                    Owner.Position.Y -= 7;
                }
                else
                {
                    Owner.Position.Y += 5;
                }
            }
            Position = pos;
            BlinkModule.TzzSfx.SFX?.Play(base.X);
            canDie = true;
        }
    }

    public override void HitLava()
    {
        this.used = true;
        var pos = Owner.Position;
        Owner.Position = Position + (Speed * 2);
        Position = pos;
        BlinkModule.ZztSfx.SFX?.Play(base.X);
        canDie = true;
    }

    public override void EnterFallMode(bool bounce = true, bool zeroX = false, bool sound = true)
    {
        if (bounce && Level.Session.MatchSettings.Variants.GetCustomVariant("OopsAllBlinkArrows/MatterDisplacement"))
        {
            this.used = true;
            Sounds.pu_superBombExplode.Play(base.X);
            canDie = true;

        }
        else
        {
            base.EnterFallMode(bounce, zeroX, sound);
        }
    }
}
