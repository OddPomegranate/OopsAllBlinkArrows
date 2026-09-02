using FortRise;

namespace OopsAllBlinkArrows;

// Modern replacement for the original mod's VariantManager.AddVariant(...)/AddArrowVariant(...)
// calls in OnVariantsRegister. Two differences from the original, both intentional:
//
//   1. AddArrowVariant(entry, startWithIcon, excludeIcon) created BOTH a "start with X" and an
//      "exclude X" variant per arrow. FortRise 5.x's VariantConfiguration only exposes a
//      StartWith field - there's no confirmed modern equivalent for "never let this arrow
//      spawn" - so only the 11 "start with" variants are registered below (matching the
//      BlinkArrows port of this same limitation). The "exclude" icons in VariantAtlas go
//      unused, same as before.
//   2. The original registered "ChoaticBaits" (typo) while BaitArrow.cs itself checked for
//      "ChaoticBaits" (different typo) - a mismatch that meant the variant could never
//      actually trigger. Registered here as "ChaoticBaits" (matching the check in
//      BaitArrow.cs and the correctly-spelled art asset key) so the feature actually works.
//
// "DoubleSpread" is registered even though it was commented out in the original module -
// MechArrow.Update() unconditionally checks GetCustomVariant("DoubleSpread"), which throws
// if the variant was never registered, so leaving it out would crash on every Mech arrow
// detonation.
//
// Crystal and Shock arrows were left commented out/disabled in the source this was ported
// from (looked unfinished/experimental), so neither their arrows nor their start-with/
// exclude variants are registered here, matching that original disabled state.
public static class OopsVariants
{
    public static void Register(IModuleContext context, IModContent content)
    {
        var variants = context.Registry.Variants;
        var subtextures = context.Registry.Subtextures;

        variants.RegisterVariant("SonicBoom", new VariantConfiguration
        {
            Title = "SONIC BOOM",
            Icon = subtextures.RegisterTexture("SonicBoomIcon", () => OopsAllArrowsModule.VariantAtlas["variants/sonicBoom"]),
            Description = "WHY DID I STICK A LANDMINE ON A BOOMERANG?",
            Flags = CustomVariantFlags.CanRandom
        });

        variants.RegisterVariant("ChaoticBaits", new VariantConfiguration
        {
            Title = "CHAOTIC BAITS",
            Icon = subtextures.RegisterTexture("ChaoticBaitsIcon", () => OopsAllArrowsModule.VariantAtlas["variants/chaoticBaits"]),
            Description = "SUMMONS THE RECKONING",
            Flags = CustomVariantFlags.CanRandom
        });

        variants.RegisterVariant("InfiniteWarping", new VariantConfiguration
        {
            Title = "INFINITE WARPING",
            Icon = subtextures.RegisterTexture("InfiniteWarpingIcon", () => OopsAllArrowsModule.VariantAtlas["variants/infiniteWarping"]),
            Description = "FREAKY ARROWS ARE NOT DESTROYED ON WARP",
            Flags = CustomVariantFlags.CanRandom
        });

        variants.RegisterVariant("DoubleSpread", new VariantConfiguration
        {
            Title = "DOUBLE SPREAD",
            Icon = subtextures.RegisterTexture("DoubleSpreadIcon", () => OopsAllArrowsModule.VariantAtlas["variants/doubleSpread"]),
            Description = "MECH ARROWS SPLIT INTO 6 INSTEAD OF 3",
            Flags = CustomVariantFlags.CanRandom
        });

        RegisterStartWith(variants, subtextures, "StartWithBait", "variants/startWithBaitArrows", OopsAllArrowsModule.BaitEntry);
        RegisterStartWith(variants, subtextures, "StartWithBoomerang", "variants/startWithBoomerangArrows", OopsAllArrowsModule.BoomerangEntry);
        RegisterStartWith(variants, subtextures, "StartWithFreaky", "variants/startWithFreakyArrows", OopsAllArrowsModule.FreakyEntry);
        RegisterStartWith(variants, subtextures, "StartWithIce", "variants/startWithIceArrows", OopsAllArrowsModule.IceEntry);
        RegisterStartWith(variants, subtextures, "StartWithLandMine", "variants/startWithLandMineArrows", OopsAllArrowsModule.LandMineEntry);
        RegisterStartWith(variants, subtextures, "StartWithMech", "variants/startWithMechArrows", OopsAllArrowsModule.MechEntry);
        RegisterStartWith(variants, subtextures, "StartWithMissle", "variants/startWithMissleArrows", OopsAllArrowsModule.MissleEntry);
        RegisterStartWith(variants, subtextures, "StartWithPrismTrap", "variants/startWithPrismTrapArrows", OopsAllArrowsModule.PrismTrapEntry);
        RegisterStartWith(variants, subtextures, "StartWithSlime", "variants/startWithSlimeArrows", OopsAllArrowsModule.SlimeEntry);
        RegisterStartWith(variants, subtextures, "StartWithTornado", "variants/startWithTornadoArrows", OopsAllArrowsModule.TornadoEntry);
    }

    private static void RegisterStartWith(IModVariants variants, IModSubtextures subtextures, string id, string iconKey, IArrowEntry entry)
    {
        variants.RegisterVariant(id, new VariantConfiguration
        {
            Title = id.Replace("StartWith", "START WITH ").ToUpperInvariant() + " ARROWS",
            Icon = subtextures.RegisterTexture(id + "Icon", () => OopsAllArrowsModule.VariantAtlas[iconKey]),
            Flags = CustomVariantFlags.CanRandom,
            StartWith = entry
        });
    }
}
