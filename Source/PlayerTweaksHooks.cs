using System;
using FortRise;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace OopsAllBlinkArrows;

// Modern replacement for the original mod's On.TowerFall.Player.ShootArrow += hook (there is
// no MMHOOK_TowerFall.dll / hookgen assembly in this FortRise 5.x install, so Harmony is used
// instead). The original hook ran its own custom logic INSTEAD of orig(self) whenever the
// player's front arrow was a Decoy arrow, and otherwise fell through to orig(self) -- which
// maps directly onto a Harmony Prefix that returns false to skip the original method when it
// handled the shot itself, and true to let the original method run normally otherwise.
public static class PlayerTweaksHooks
{
    public static void Register(IHarmony harmony)
    {
        harmony.Patch(
            AccessTools.Method(typeof(Player), "ShootArrow"),
            prefix: new HarmonyMethod(typeof(PlayerTweaksHooks), nameof(ShootArrow_Prefix)));
    }

    private static bool ShootArrow_Prefix(Player __instance)
    {
        var self = __instance;
        if (self.Arrows.HasArrows && self.Arrows.Arrows[0] == BlinkModule.DecoyEntry.ArrowTypes)
        {
            self.Aiming = false;
            self.ArrowHUD.OnShoot();
            self.ArcherData.SFX.FireArrow.Play(self.X);
            DecoyArrow arrow = (DecoyArrow)DecoyArrow.Create(self.Arrows.UseArrow(), self, self.Position + Player.ArrowOffset, FindLockAngle(self));
            Sprite<string> BodySpriteForArrow = TFGame.SpriteData.GetSpriteString(self.ArcherData.Sprites.Body);
            Sprite<string> HeadSpriteForArrow = TFGame.SpriteData.GetSpriteString(self.ArcherData.Sprites.HeadNormal);
            Sprite<string> bowSpriteForArrow = TFGame.SpriteData.GetSpriteString(self.ArcherData.Sprites.Bow);

            string XMLText = TFGame.SpriteData.GetXML(self.ArcherData.Sprites.Body).OuterXml;
            string headYPoss = XMLText.Substring(XMLText.IndexOf("<HeadYOrigins>"), XMLText.IndexOf("</HeadYOrigins>") - XMLText.IndexOf("<HeadYOrigins>"));
            string noBow = "";
            if (XMLText.Contains("<HideBowIdle>"))
            {
                noBow = XMLText.Substring(XMLText.IndexOf("<HideBowIdle>"), XMLText.IndexOf("</HideBowIdle>") - XMLText.IndexOf("<HideBowIdle>"));
            }

            headYPoss = headYPoss.Remove(0, 14);
            headYPoss = headYPoss.Substring(0, headYPoss.IndexOf(","));

            HeadSpriteForArrow.Position.Y -= Int32.Parse(headYPoss);

            if (XMLText.Contains("PlayerBody8"))
            {
                HeadSpriteForArrow.Position.Y += 7;
            }

            BodySpriteForArrow.Play("stand");
            HeadSpriteForArrow.Play("idle");
            bowSpriteForArrow.Play("idle");
            arrow.playerSprite = BodySpriteForArrow;
            arrow.playerHeadSprite = HeadSpriteForArrow;
            if (!noBow.Contains("True"))
            {
                arrow.playerBowSprite = bowSpriteForArrow;
            }
            else
            {
                arrow.playerBowSprite = bowSpriteForArrow;
                arrow.ignoreBow = true;
            }

            self.Level.Add(arrow);

            self.Level.Session.MatchStats[self.PlayerIndex].ArrowsShot++;
            SaveData.Instance.Stats.ArrowsShot++;
            return false;
        }

        return true;
    }

    private static float FindLockAngle(Player self)
    {
        LevelEntity levelEntity = null;
        float num = 0f;
        float result = self.AimDirection;
        foreach (LevelEntity item in self.Level[GameTags.Target])
        {
            if (item == self || item.SeekPriority <= 0 || !self.IsEnemy(item))
            {
                continue;
            }

            Vector2 vector = self.Position + Player.ArrowOffset;
            float num2 = Vector2.DistanceSquared(vector, item.Position);
            if (levelEntity == null || item.SeekPriority > levelEntity.SeekPriority)
            {
                if (num2 > 1296f)
                {
                    continue;
                }
            }
            else if (num2 >= num)
            {
                continue;
            }

            float num3 = Calc.Angle(item.Position - vector);
            if (Math.Abs(Calc.AngleDiff(self.AimDirection, num3)) <= (float)Math.PI * 13f / 36f && !self.Level.CollideCheck(vector, item.Position, GameTags.Solid))
            {
                levelEntity = item;
                num = num2;
                result = num3;
            }
        }

        return result;
    }
}
