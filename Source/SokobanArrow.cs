using System;
using System.Collections;
using FortRise;
using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace OopsAllBlinkArrows;

public class SokobanArrow : Arrow
{
    // This is automatically been set by the mod loader
    public override ArrowTypes ArrowType { get; set; }
    private bool used, canDie;
    private Image normalImage;
    private Image buriedImage;
    private bool firstStep = true;

    public static IArrowEntry Register(IModuleContext context, IModContent content)
    {
        var sokobanHud = context.Registry.Subtextures.RegisterTexture(
            "SokobanArrowHud", () => BlinkModule.Atlas["SokobanArrowHud"]);

        return context.Registry.Arrows.RegisterArrows("Sokoban", new ArrowConfiguration
        {
            ArrowPickupName = "Sokoban",
            CreateArrow = () => new SokobanArrow(),
            CreateArrowPickupSprite = _ =>
            {
                var pickupSprite = new Sprite<int>(BlinkModule.Atlas["SokobanArrowPickup"], 12, 12, 0);
                pickupSprite.CenterOrigin();
                return pickupSprite;
            },
            HUD = sokobanHud
        });
    }

    public SokobanArrow() : base()
    {

    }
    protected override void Init(LevelEntity owner, Vector2 position, float direction)
    {
        base.Init(owner, position, direction);
        used = (canDie = false);
        StopFlashing();
        firstStep = true;

    }
    protected override void CreateGraphics()
    {
        normalImage = new Image(BlinkModule.Atlas["SokobanArrow"]);
        normalImage.Origin = new Vector2(14f, 3f);
        buriedImage = new Image(BlinkModule.Atlas["SokobanArrowBuried"]);
        buriedImage.Origin = new Vector2(14f, 3f);

        Graphics = new Image[2] { normalImage, buriedImage};
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
        if (!firstStep && State != ArrowStates.Buried)
        {
            canDie = true;
        }
        if (firstStep)
        {
            Vector2 pos = Position;
            Position = new Vector2(0, 0);
            Add(new Coroutine(SokobanSlidingBlock.CreateSokoBlock(Level, pos + (Speed * 6), 0f, Speed)));
            firstStep = false;
        }

        if (canDie)
        {
            RemoveSelf();
        }




    }
    protected override void HitWall(Platform platform)
    {
        base.HitWall(platform);
    }

    public override void ShootUpdate()
    {

    }


}
public class SokobanSlidingBlock : Solid
{

    private Image image;
    private Image eyesimage;
    private Vector2 Speed;
    private bool crushing;
    private int postCrushLifetime;
    private int bouncesNoise = 3;
    private Vector2 lastpos = new Vector2(0, 0);
    private Vector2 pointing = new Vector2(0, 0);
    public SokobanSlidingBlock(Vector2 position, int width, int height, float rotation) : base(position, width, height)
    {
        Position = position;

        Tag(GameTags.Solid);


        ScreenWrap = true;

        base.Collider = new WrapHitbox(20f, 20f, -10f, -10f);
        base.Collidable = true;
        image = new Image(BlinkModule.Atlas["SokobanArrowBlock"]);
        image.CenterOrigin();
        eyesimage = new Image(BlinkModule.Atlas["SokobanArrowBlockEyes"]);
        eyesimage.CenterOrigin();


        Add(image);
        Add(eyesimage);
    }

    public static IEnumerator CreateSokoBlock(Level level, Vector2 at, float rotation, Vector2 speed)
    {
        SokobanSlidingBlock MySokoBlock = new SokobanSlidingBlock(at, 10, 10, rotation);
        MySokoBlock.Speed = speed / 1f;
        MySokoBlock.crushing = true;
        level.Add(MySokoBlock);
        yield return 0.000000001f;


    }

    public override void Added()
    {
        postCrushLifetime = 300;
        base.Added();

        if (Speed.X <= 1 && Speed.X >= -1)
        {
            if (Speed.Y < 1)
            {
                eyesimage.Position.Y += -4;
                Position.Y += 3;
                pointing = new Vector2(0, -1);
            }
            else
            {
                eyesimage.Position.Y += 4;
                Position.Y -= 10;
                pointing = new Vector2(0, 1);
            }
        }
        else if (Speed.Y <= 1 && Speed.Y >= -1)
        {
            if (Speed.X < 1)
            {
                eyesimage.Position.X += -4;
                Position.Y -= 1;
                Position.X += 10;
                pointing = new Vector2(1, 0);
            }
            else
            {
                eyesimage.Position.X += 4;
                Position.Y -= 1;
                Position.X -= 10;
                pointing = new Vector2(-1, 0);
            }
        }
        else
        {
            if (Speed.X > 1)
            {
                if (Speed.Y > 1)
                {
                    eyesimage.Position.X += 4;
                    eyesimage.Position.Y += 4;
                    pointing = new Vector2(1, -1);
                }
                else
                {
                    eyesimage.Position.X += 4;
                    eyesimage.Position.Y += -4;
                    pointing = new Vector2(1, 1);
                }
            }
            else
            {
                if (Speed.Y > 1)
                {
                    eyesimage.Position.X += -4;
                    eyesimage.Position.Y += 4;
                    pointing = new Vector2(-1, -1);
                }
                else
                {
                    eyesimage.Position.X += -4;
                    eyesimage.Position.Y += -4;
                    pointing = new Vector2(-1, 1);
                }
            }
        }


    }
    public override void Update()
    {
        base.Update();

        if (!crushing)
        {
            postCrushLifetime -= 1;
            if (postCrushLifetime < 0)
            {
                image.Scale /= 1.2f;
                base.Collider = new WrapHitbox(image.Scale.X * 20f, image.Scale.X * 20f, image.Scale.X * -10f, image.Scale.X * -10f);
                if (image.Scale.X <= 0.05)
                {
                    RemoveSelf();
                }
            }
        }
        if (crushing)
        {
            MoveTo(Position + Speed);
        }


        if (CollideCheck(GameTags.Solid) && crushing)
        {
            bool leftCollide = false;
            bool rightCollide = false;
            bool upCollide = false;
            bool downCollide = false;
            MoveTo(Position - Speed);

            Position.X += Math.Abs(Speed.X);
            if (CollideCheck(GameTags.Solid))
            {
                leftCollide = true;
            }
            Position.X -= Math.Abs(Speed.X);

            Position.Y -= Math.Abs(Speed.Y);
            if (CollideCheck(GameTags.Solid))
            {
                upCollide = true;
            }
            Position.Y += Math.Abs(Speed.Y);

            Position.X -= Math.Abs(Speed.X);
            if (CollideCheck(GameTags.Solid))
            {
                rightCollide = true;
            }
            Position.X += Math.Abs(Speed.X);

            Position.Y += Math.Abs(Speed.Y);
            if (CollideCheck(GameTags.Solid))
            {
                downCollide = true;
            }
            Position.Y -= Math.Abs(Speed.Y);

            //Entity leftCollide = new WrapHitbox(-10, -9, 1, 18);
            //Collider upCollide = new WrapHitbox(-9, -10, 18, 1);
            //Collider rightCollide = new WrapHitbox(9, -9, 1, 18);
            //Collider downCollide = new WrapHitbox(-9, 9, 18, 1);

            if (Level.Session.MatchSettings.Variants.GetCustomVariant("OopsAllBlinkArrows/Grudge"))
            {
                if (leftCollide)
                {
                    Speed.X *= -1;
                    eyesimage.Position.X *= -1;
                }
                if (rightCollide)
                {
                    Speed.X *= -1;
                    eyesimage.Position.X *= -1;
                }
                if (upCollide)
                {
                    Speed.Y *= -1;
                    eyesimage.Position.Y *= -1;
                }
                if (downCollide)
                {
                    Speed.Y *= -1;
                    eyesimage.Position.Y *= -1;
                }
                if (leftCollide && rightCollide && upCollide && downCollide)
                {
                    crushing = false;
                }
                if (leftCollide || rightCollide || upCollide || downCollide)
                {
                    bouncesNoise -= 1;
                    if (bouncesNoise > 0)
                    {
                        base.Level.ScreenShake(12);
                    }

                    Sounds.env_movingBlockEnd.Play(base.X);
                    MoveTo(Position - Speed);
                }
                if (crushing == false)
                {
                    eyesimage.Position = new Vector2(0, 0);
                }
            }
            else
            {
                if ((leftCollide || rightCollide) && (upCollide || downCollide))
                {
                    crushing = false;
                }
                if (leftCollide || rightCollide || upCollide || downCollide)
                {
                    if (leftCollide)
                    {
                        if (pointing.X == 0)
                        {
                            MoveTo(Position + new Vector2(3, 0));
                        }
                        if (pointing.Y == 0)
                        {
                            crushing = false;
                            MoveTo(Position + new Vector2(4, 0));
                        }
                        else
                        {
                            {
                                pointing.X = 0;
                                Speed.X = 0;
                                eyesimage.Position.X = 0;
                            }
                        }
                    }
                    if (rightCollide)
                    {
                        if (pointing.X == 0)
                        {
                            MoveTo(Position + new Vector2(-3, 0));
                        }
                        if (pointing.Y == 0)
                        {
                            crushing = false;
                            MoveTo(Position + new Vector2(-4, 0));
                        }
                        else
                        {
                            {
                                pointing.X = 0;
                                Speed.X = 0;
                                eyesimage.Position.X = 0;
                            }
                        }
                    }
                    if (upCollide)
                    {
                        if (pointing.Y == 0)
                        {
                            MoveTo(Position + new Vector2(0, 3));
                        }
                        if (pointing.X == 0)
                        {
                            crushing = false;
                            MoveTo(Position + new Vector2(0, -4));
                        }
                        else
                        {
                            {
                                pointing.Y = 0;
                                Speed.Y = 0;
                                eyesimage.Position.Y = 0;
                            }
                        }
                    }
                    if (downCollide)
                    {
                        if (pointing.Y == 0)
                        {
                            MoveTo(Position + new Vector2(0, -3));
                        }
                        if (pointing.X == 0)
                        {
                            crushing = false;
                            MoveTo(Position + new Vector2(0, 4));
                        }
                        else
                        {
                            {
                                pointing.Y = 0;
                                Speed.Y = 0;
                                eyesimage.Position.Y = 0;
                            }
                        }
                    }
                    base.Level.ScreenShake(12);

                    Sounds.env_movingBlockEnd.Play(base.X);
                    MoveTo(Position - Speed);


                }
                if (crushing == false)
                {
                    eyesimage.Position = new Vector2(0, 0);
                }
            }


        }



    }


}
