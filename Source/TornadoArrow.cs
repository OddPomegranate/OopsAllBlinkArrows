using System;
using FortRise;
using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace OopsAllBlinkArrows;

public class TornadoArrow : Arrow
{
    // This is automatically been set by the mod loader
    public override ArrowTypes ArrowType { get; set; }
    private bool used, canDie;
    private Image normalImage;
    private Image buriedImage;

    public static IArrowEntry Register(IModuleContext context, IModContent content)
    {
        var tornadoHud = context.Registry.Subtextures.RegisterTexture(
            "TornadoArrowHud", () => OopsAllArrowsModule.ArrowAtlas["TornadoArrowHud"]);

        return context.Registry.Arrows.RegisterArrows("Tornado", new ArrowConfiguration
        {
            ArrowPickupName = "Tornado Arrows",
            CreateArrow = () => new TornadoArrow(),
            CreateArrowPickupSprite = _ =>
            {
                var pickupSprite = new Sprite<int>(OopsAllArrowsModule.ArrowAtlas["TornadoArrowPickup"], 12, 12, 0);
                pickupSprite.Add(0, 0.3f, new int[2] { 0, 0 });
                pickupSprite.Play(0, false);
                pickupSprite.CenterOrigin();
                return pickupSprite;
            },
            HUD = tornadoHud
        });
    }

    public TornadoArrow() : base()
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
        normalImage = new Image(OopsAllArrowsModule.ArrowAtlas["TornadoArrow"]);
        normalImage.Origin = new Vector2(13f, 3f);
        buriedImage = new Image(OopsAllArrowsModule.ArrowAtlas["TornadoArrowBuried"]);
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
            int Target = 0;
            float currentDistance = 1000f;
            foreach (Player item in base.Level[GameTags.Player])
            {
                float Distance = Vector2.Distance(Position, item.Position);
                if (Distance < currentDistance) {
                    currentDistance = Distance;
                    Target = item.PlayerIndex;
                }
            }
            Level.Add(new Tornado(Position + GetOffset(), Level.GetPlayerOrCorpse(Target)));
            canDie = true;
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
            int Target = 0;
            float currentDistance = 1000f;
            foreach (Player item in base.Level[GameTags.Player])
            {
                float Distance = Vector2.Distance(Position, item.Position);
                if (Distance < currentDistance)
                {
                    currentDistance = Distance;
                    Target = item.PlayerIndex;
                }
            }
            Level.Add(new Tornado(Position + GetOffset(), Level.GetPlayerOrCorpse(Target)));
            canDie = true;
        }
    }

    public Vector2 GetOffset()
    {
        float X = 0, Y = 0;
        Entity entity = Level.CollideFirst(new Rectangle((int)Position.X-8, (int)Position.Y-8,16,16), GameTags.Solid);
        int TestY = 4, TestX = 4;
        if (entity != null)
        {
           for(bool done = false; done != true;)
           {
                Entity testentity = Level.CollideFirst(new Rectangle((int)Position.X, (int)Position.Y + TestY -8, 8, 8), GameTags.Solid);
                if (testentity == null)
                {
                    X = TestX; break;
                }
                testentity = Level.CollideFirst(new Rectangle((int)Position.X + TestX -8, (int)Position.Y, 8, 8), GameTags.Solid);
                if (testentity == null)
                {
                    Y = TestY; break;


                }
                testentity = Level.CollideFirst(new Rectangle((int)Position.X - 8 + TestX, (int)Position.Y - 8 + TestY, 8, 8), GameTags.Solid);
                if (testentity == null)
                {
                    X = TestX; Y = TestY; break;
                }
                testentity = Level.CollideFirst(new Rectangle((int)Position.X - TestX -8, (int)Position.Y, 8, 8), GameTags.Solid);
                if (testentity == null)
                {
                    X = -TestX; break;
                }
                testentity = Level.CollideFirst(new Rectangle((int)Position.X, (int)Position.Y - TestY - 8, 8, 8), GameTags.Solid);
                if (testentity == null)
                {
                    Y = -TestY; break;
                }
                testentity = Level.CollideFirst(new Rectangle((int)Position.X - 8 - TestX, (int)Position.Y - 8 - TestY, 8, 8), GameTags.Solid);
                if (testentity == null)
                {

                    X = -TestX - 8; Y = -TestY - 8; break;
                }
                testentity = Level.CollideFirst(new Rectangle((int)Position.X - 8 + TestX, (int)Position.Y - 8 - TestY, 8, 8), GameTags.Solid);
                if (testentity == null)
                {
                    X = TestX; Y = -TestY - 8; break;
                }
                testentity = Level.CollideFirst(new Rectangle((int)Position.X - 8 - TestX, (int)Position.Y - 8 + TestY, 8, 8), GameTags.Solid);
                if (testentity == null)
                {
                    X = -TestX-8; Y = TestY; break;
                }
                TestX += 2;
                TestY += 2;
                TestX += 2;
                TestY += 2;
            }
        }
        return new Vector2(X, Y);
    }
}
