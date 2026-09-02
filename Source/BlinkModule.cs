using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using FortRise;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace OopsAllBlinkArrows;

// The BlinkArrows half of this combined mod. Originally its own Mod-derived entry point;
// now a plain static holder invoked once from OopsAllBlinkArrowsModule.Setup(), since
// FortRise only instantiates the first "BaseType == typeof(Mod)" class it finds in a mod's
// assembly - a second one would silently never load.
public static class BlinkModule
{
    // NOTE: same fix as OopsAllArrowsModule's atlas loading - the vanilla Monocle.Atlas
    // constructor string-concatenates whatever path it's given onto the game's own
    // Content directory, so `new Atlas(...)` can't be used for a mod resource file.
    // We parse the XML via IResourceInfo.Xml and load the PNG via IResourceInfo.Stream
    // into a Monocle.Texture instead, then build Subtexture objects directly.
    public static Dictionary<string, Subtexture> Atlas { get; private set; } = null!;

    // The 3 raw .wav files this mod ships (Content/Audio/*.wav) were originally played
    // with System.Media.SoundPlayer against a hard-coded absolute path built from
    // AppDomain.CurrentDomain.BaseDirectory + "Mods/BlinkArrows/Content/Audio/...". That
    // both bypasses the game's own audio engine (volume sliders, pitch, etc. all ignored)
    // and hard-codes the old standalone mod's folder name, so it would've broken the
    // moment that folder was renamed. Registered as proper FortRise SFX entries instead,
    // played the same way as any vanilla Sounds.sfx_xxx.Play(x) call.
    public static ISFXEntry TzzSfx { get; private set; } = null!;
    public static ISFXEntry ZztSfx { get; private set; } = null!;
    public static ISFXEntry SeekerDashSfx { get; private set; } = null!;

    public static IArrowEntry BlinkEntry { get; private set; } = null!;
    public static IArrowEntry GombocEntry { get; private set; } = null!;
    public static IArrowEntry NyoomEntry { get; private set; } = null!;
    public static IArrowEntry PerimeterEntry { get; private set; } = null!;
    public static IArrowEntry SeekerEntry { get; private set; } = null!;
    public static IArrowEntry SokobanEntry { get; private set; } = null!;
    public static IArrowEntry LatencyEntry { get; private set; } = null!;
    public static IArrowEntry DecoyEntry { get; private set; } = null!;

    // Registering an Arrow (above) only teaches FortRise how to spawn/behave the arrow
    // once it exists - it does NOT make it appear anywhere in the game world. That's a
    // *separate* registry (context.Registry.Pickups) with its own Pickups enum values;
    // ArrowsRegistry and PickupsRegistry don't know about each other otherwise. Without
    // one of these, an arrow can never appear in a treasure chest, on the ground, or
    // anywhere else - which is exactly why none of these 8 arrows were spawning at all.
    // DecoyEntry has no Pickup registration on purpose, matching the original mod (it had
    // no matching "TowerPatch" tower/arrow pairing either - see Setup() below).
    public static IPickupEntry BlinkPickup { get; private set; } = null!;
    public static IPickupEntry GombocPickup { get; private set; } = null!;
    public static IPickupEntry NyoomPickup { get; private set; } = null!;
    public static IPickupEntry PerimeterPickup { get; private set; } = null!;
    public static IPickupEntry SeekerPickup { get; private set; } = null!;
    public static IPickupEntry SokobanPickup { get; private set; } = null!;
    public static IPickupEntry LatencyPickup { get; private set; } = null!;

    public static void Setup(IModuleContext context, IModContent content)
    {
        Atlas = LoadAtlasDictionary(content, "Content/Atlas/Blink/ArrowAtlas.xml");

        TzzSfx = context.Registry.SFXs.RegisterSFX("Tzz", content.Root.GetRelativePath("Content/Audio/tzz.wav"));
        ZztSfx = context.Registry.SFXs.RegisterSFX("Zzt", content.Root.GetRelativePath("Content/Audio/zzt.wav"));
        SeekerDashSfx = context.Registry.SFXs.RegisterSFX("SeekerDash", content.Root.GetRelativePath("Content/Audio/seekerdash.wav"));

        BlinkEntry = BlinkArrow.Register(context, content);
        GombocEntry = GombocArrow.Register(context, content);
        NyoomEntry = NyoomArrow.Register(context, content);
        PerimeterEntry = PerimeterArrow.Register(context, content);
        SeekerEntry = SeekerArrow.Register(context, content);
        SokobanEntry = SokobanArrow.Register(context, content);
        LatencyEntry = LatencyArrow.Register(context, content);
        DecoyEntry = DecoyArrow.Register(context, content);

        BlinkVariants.Register(context, content);

        // Registering an Arrow never registers a matching Pickup - that's a fully separate
        // registry with its own Pickups enum values (see the properties above). Without
        // this, PickupsRegistry.GetAllPickups() never sees these arrows, TreasureSpawner's
        // ExtendTreasures() never gives them a slot in DefaultTreasureChances/FullTreasureMask,
        // and they can NEVER appear anywhere in the game - not in chests, not on the ground,
        // regardless of any TowerHook. RegisterArrowPickup builds the ArrowTypePickup wiring
        // for us and defaults Configuration.Chance to 1f, which is what actually makes each
        // arrow spawn at a normal baseline rate in every tower.
        BlinkPickup = context.Registry.Pickups.RegisterArrowPickup("BlinkArrowPickup", BlinkEntry);
        GombocPickup = context.Registry.Pickups.RegisterArrowPickup("GombocArrowPickup", GombocEntry);
        NyoomPickup = context.Registry.Pickups.RegisterArrowPickup("NyoomArrowPickup", NyoomEntry);
        PerimeterPickup = context.Registry.Pickups.RegisterArrowPickup("PerimeterArrowPickup", PerimeterEntry);
        SeekerPickup = context.Registry.Pickups.RegisterArrowPickup("SeekerArrowPickup", SeekerEntry);
        SokobanPickup = context.Registry.Pickups.RegisterArrowPickup("SokobanArrowPickup", SokobanEntry);
        LatencyPickup = context.Registry.Pickups.RegisterArrowPickup("LatencyArrowPickup", LatencyEntry);
        // DecoyEntry intentionally has no pickup registration - see the property comment above.

        // Now the per-tower boost: the original mod's 7 "TowerPatch" classes (Mirage,
        // Backfire, KingsCourt, Thornwood, TwilightSpire, Towerforge, SunkenCity) each
        // nudged up one arrow's treasure-chest rate in one matching vanilla tower (e.g.
        // Mirage drops more Blink arrows). FortRise.TowerPatch itself is gone in 5.x, but
        // context.Registry.TowerHooks + ITowerHook.VersusTowerTreasurePatch is the direct
        // modern equivalent - it fires per-tower with a context whose
        // IncreaseTreasureRates(Pickups) bumps that tower's rate for the given pickup. This
        // reproduces the original's exact 7 arrow/tower pairings; it stacks additively on
        // top of the baseline rate from RegisterArrowPickup above (that's the whole point -
        // it's a boost, not a replacement), so there's no double-counting to worry about.
        context.Registry.TowerHooks.RegisterTowerHook("BlinkTreasure", new ArrowTreasureHook(VanillaConstants.Towers.Mirage, BlinkPickup.Pickups));
        context.Registry.TowerHooks.RegisterTowerHook("GombocTreasure", new ArrowTreasureHook(VanillaConstants.Towers.Backfire, GombocPickup.Pickups));
        context.Registry.TowerHooks.RegisterTowerHook("NyoomTreasure", new ArrowTreasureHook(VanillaConstants.Towers.KingsCourt, NyoomPickup.Pickups));
        context.Registry.TowerHooks.RegisterTowerHook("PerimeterTreasure", new ArrowTreasureHook(VanillaConstants.Towers.Thornwood, PerimeterPickup.Pickups));
        context.Registry.TowerHooks.RegisterTowerHook("SeekerTreasure", new ArrowTreasureHook(VanillaConstants.Towers.TwilightSpire, SeekerPickup.Pickups));
        context.Registry.TowerHooks.RegisterTowerHook("SokobanTreasure", new ArrowTreasureHook(VanillaConstants.Towers.Towerforge, SokobanPickup.Pickups));
        context.Registry.TowerHooks.RegisterTowerHook("LatencyTreasure", new ArrowTreasureHook(VanillaConstants.Towers.SunkenCity, LatencyPickup.Pickups));

        LatencyArrow.RegisterHooks(context.Harmony);
        PlayerTweaksHooks.Register(context.Harmony);
    }

    private static Dictionary<string, Subtexture> LoadAtlasDictionary(IModContent content, string xmlRelativePath)
    {
        var xmlRes = content.Root.GetRelativePath(xmlRelativePath);
        var xml = xmlRes.Xml
            ?? throw new InvalidOperationException($"[{content.Metadata.Name}] Failed to parse '{xmlRelativePath}' as XML.");
        var textureAtlasElement = xml["TextureAtlas"]
            ?? throw new InvalidOperationException($"[{content.Metadata.Name}] '{xmlRelativePath}' is missing a <TextureAtlas> root element.");
        var pngRelativePath = Path.ChangeExtension(xmlRelativePath, "png");
        var pngRes = content.Root.GetRelativePath(pngRelativePath);

        Monocle.Texture texture;
        using (var pngStream = pngRes.Stream)
        {
            texture = new Monocle.Texture(Texture2D.FromStream(Engine.Instance.GraphicsDevice, pngStream));
        }

        var subtextures = new Dictionary<string, Subtexture>();
        foreach (XmlElement subTextureElement in textureAtlasElement.GetElementsByTagName("SubTexture"))
        {
            var name = subTextureElement.GetAttribute("name");
            int x = int.Parse(subTextureElement.GetAttribute("x"));
            int y = int.Parse(subTextureElement.GetAttribute("y"));
            int width = int.Parse(subTextureElement.GetAttribute("width"));
            int height = int.Parse(subTextureElement.GetAttribute("height"));
            subtextures[name] = new Subtexture(texture, new Microsoft.Xna.Framework.Rectangle(x, y, width, height));
        }
        return subtextures;
    }
}
