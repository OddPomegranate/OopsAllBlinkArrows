using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using FortRise;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace OopsAllBlinkArrows;

// The OopsAllArrows half of this combined mod. Originally its own Mod-derived entry point;
// now a plain static holder invoked once from OopsAllBlinkArrowsModule.Setup(), since
// FortRise only instantiates the first "BaseType == typeof(Mod)" class it finds in a mod's
// assembly - a second one would silently never load.
public static class OopsAllArrowsModule
{
    // Same fix as before (OopsAllArrowsPort's original ArrowHUD investigation, and every
    // mod ported since): new Atlas(...) string-concatenates onto the game's own Content
    // directory, so it can't load a mod resource file. We parse the XML via
    // IResourceInfo.Xml and the PNG via IResourceInfo.Stream instead, and build the
    // Subtexture dictionary by hand. This mod ships TWO atlases (arrows + variant icons),
    // so both go through the same helper.
    public static Dictionary<string, Subtexture> ArrowAtlas { get; private set; } = null!;
    public static Dictionary<string, Subtexture> VariantAtlas { get; private set; } = null!;

    public static IArrowEntry BaitEntry { get; private set; } = null!;
    public static IArrowEntry BoomerangEntry { get; private set; } = null!;
    public static IArrowEntry FreakyEntry { get; private set; } = null!;
    public static IArrowEntry IceEntry { get; private set; } = null!;
    public static IArrowEntry LandMineEntry { get; private set; } = null!;
    public static IArrowEntry MechEntry { get; private set; } = null!;
    public static IArrowEntry MiniMechEntry { get; private set; } = null!;
    public static IArrowEntry MissleEntry { get; private set; } = null!;
    public static IArrowEntry PrismTrapEntry { get; private set; } = null!;
    public static IArrowEntry SlimeEntry { get; private set; } = null!;
    public static IArrowEntry TornadoEntry { get; private set; } = null!;

    // Registering an Arrow (above) only teaches FortRise how to spawn/behave the arrow
    // once it exists - it does NOT make it appear anywhere in the game world. That's a
    // *separate* registry (context.Registry.Pickups) that hands out its own Pickups enum
    // value; ArrowsRegistry and PickupsRegistry don't know about each other otherwise.
    // Without one of these, an arrow can never appear in a treasure chest, on the ground,
    // or anywhere else - which is exactly why none of these arrows were spawning at all.
    // MiniMech is deliberately excluded: it's Mech's sub-munition, never a standalone pickup
    // in the original mod either.
    public static IPickupEntry BaitPickup { get; private set; } = null!;
    public static IPickupEntry BoomerangPickup { get; private set; } = null!;
    public static IPickupEntry FreakyPickup { get; private set; } = null!;
    public static IPickupEntry IcePickup { get; private set; } = null!;
    public static IPickupEntry LandMinePickup { get; private set; } = null!;
    public static IPickupEntry MechPickup { get; private set; } = null!;
    public static IPickupEntry MisslePickup { get; private set; } = null!;
    public static IPickupEntry PrismTrapPickup { get; private set; } = null!;
    public static IPickupEntry SlimePickup { get; private set; } = null!;
    public static IPickupEntry TornadoPickup { get; private set; } = null!;

    public static void Setup(IModuleContext context, IModContent content)
    {
        ArrowAtlas = LoadAtlasDictionary(content, "Content/Atlas/Oops/ArrowAtlas.xml");
        VariantAtlas = LoadAtlasDictionary(content, "Content/Atlas/Oops/VariantAtlas.xml");

        // Every single one of these MUST get a HUD wired (ArrowConfiguration.HUD) - on the
        // installed FortRise version, ArrowHUD's constructor dereferences
        // arrowObj.Configuration.HUD.Subtexture with no null-safety, so any registered
        // arrow left without a HUD crashes the HUD constructor the moment it's in a
        // player's inventory. This was the root cause of the very first crash fixed in
        // this mod - every Register() below wires one.
        BaitEntry = BaitArrow.Register(context, content);
        BoomerangEntry = BoomerangArrow.Register(context, content);
        FreakyEntry = FreakyArrow.Register(context, content);
        IceEntry = IceArrow.Register(context, content);
        LandMineEntry = LandMineArrow.Register(context, content);
        MechEntry = MechArrow.Register(context, content);
        MiniMechEntry = MiniMechArrow.Register(context, content);
        MissleEntry = MissleArrow.Register(context, content);
        PrismTrapEntry = PrismTrapArrow.Register(context, content);
        SlimeEntry = SlimeArrow.Register(context, content);
        TornadoEntry = TornadoArrow.Register(context, content);

        OopsVariants.Register(context, content);

        // Registering an Arrow never registers a matching Pickup - that's a fully separate
        // registry with its own Pickups enum values (see the properties above). Without
        // this, PickupsRegistry.GetAllPickups() never sees these arrows, TreasureSpawner's
        // ExtendTreasures() never gives them a slot in DefaultTreasureChances/FullTreasureMask,
        // and they can NEVER appear anywhere in the game - not in chests, not on the ground,
        // regardless of any TowerHook. RegisterArrowPickup builds the ArrowTypePickup wiring
        // for us and defaults Configuration.Chance to 1f, which is what actually makes each
        // arrow spawn at a normal baseline rate in every tower.
        BaitPickup = context.Registry.Pickups.RegisterArrowPickup("BaitArrowPickup", BaitEntry);
        BoomerangPickup = context.Registry.Pickups.RegisterArrowPickup("BoomerangArrowPickup", BoomerangEntry);
        FreakyPickup = context.Registry.Pickups.RegisterArrowPickup("FreakyArrowPickup", FreakyEntry);
        IcePickup = context.Registry.Pickups.RegisterArrowPickup("IceArrowPickup", IceEntry);
        LandMinePickup = context.Registry.Pickups.RegisterArrowPickup("LandMineArrowPickup", LandMineEntry);
        MechPickup = context.Registry.Pickups.RegisterArrowPickup("MechArrowPickup", MechEntry);
        MisslePickup = context.Registry.Pickups.RegisterArrowPickup("MissleArrowPickup", MissleEntry);
        PrismTrapPickup = context.Registry.Pickups.RegisterArrowPickup("PrismTrapArrowPickup", PrismTrapEntry);
        SlimePickup = context.Registry.Pickups.RegisterArrowPickup("SlimeArrowPickup", SlimeEntry);
        TornadoPickup = context.Registry.Pickups.RegisterArrowPickup("TornadoArrowPickup", TornadoEntry);
        // MiniMech has no pickup registration on purpose - see the property comment above.

        // Now the per-tower boost: the original mod's CreatePickups()/OnTower.Patcher calls
        // each nudged up one arrow's treasure-chest rate in one matching themed vanilla
        // tower (e.g. Darkfang drops more Ice arrows). OnTower/Patcher itself is gone in
        // 5.x, but context.Registry.TowerHooks + ITowerHook.VersusTowerTreasurePatch is the
        // direct modern equivalent - it fires per-tower with a context whose
        // IncreaseTreasureRates(Pickups) bumps that tower's rate for the given pickup. This
        // reproduces the original's exact 10 arrow/tower pairings; it stacks additively on
        // top of the baseline rate from RegisterArrowPickup above (that's the whole point -
        // it's a boost, not a replacement), so there's no double-counting to worry about.
        context.Registry.TowerHooks.RegisterTowerHook("IceTreasure", new ArrowTreasureHook(VanillaConstants.Towers.Darkfang, IcePickup.Pickups));
        context.Registry.TowerHooks.RegisterTowerHook("SlimeTreasure", new ArrowTreasureHook(VanillaConstants.Towers.Thornwood, SlimePickup.Pickups));
        context.Registry.TowerHooks.RegisterTowerHook("BaitTreasure", new ArrowTreasureHook(VanillaConstants.Towers.Moonstone, BaitPickup.Pickups));
        context.Registry.TowerHooks.RegisterTowerHook("PrismTrapTreasure", new ArrowTreasureHook(VanillaConstants.Towers.Ascension, PrismTrapPickup.Pickups));
        context.Registry.TowerHooks.RegisterTowerHook("LandMineTreasure", new ArrowTreasureHook(VanillaConstants.Towers.Towerforge, LandMinePickup.Pickups));
        context.Registry.TowerHooks.RegisterTowerHook("MissleTreasure", new ArrowTreasureHook(VanillaConstants.Towers.KingsCourt, MisslePickup.Pickups));
        context.Registry.TowerHooks.RegisterTowerHook("FreakyTreasure", new ArrowTreasureHook(VanillaConstants.Towers.Cataclysm, FreakyPickup.Pickups));
        context.Registry.TowerHooks.RegisterTowerHook("TornadoTreasure", new ArrowTreasureHook(VanillaConstants.Towers.Flight, TornadoPickup.Pickups));
        context.Registry.TowerHooks.RegisterTowerHook("MechTreasure", new ArrowTreasureHook(VanillaConstants.Towers.Backfire, MechPickup.Pickups));
        context.Registry.TowerHooks.RegisterTowerHook("BoomerangTreasure", new ArrowTreasureHook(VanillaConstants.Towers.Dreadwood, BoomerangPickup.Pickups));

        MechArrow.RegisterHooks(context.Harmony);
        MyPlayer.RegisterHooks(context.Harmony);

        // NOTE: Crystal and Shock arrows were left commented out/disabled in the source this
        // was ported from (looked unfinished/experimental), so neither is registered here -
        // see OopsVariants.cs for the matching variant-registration note.
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
