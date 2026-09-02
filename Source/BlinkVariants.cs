using FortRise;

namespace OopsAllBlinkArrows;

// Modern replacement for the original mod's VariantManager.AddVariant(...)/AddArrowVariant(...)
// calls. Two things are intentionally NOT ported here, both documented gaps rather than bugs:
//   1. The old AddArrowVariant(entry, startWithIcon, excludeIcon) call created BOTH a "start
//      with X arrow" variant AND an "exclude X arrow" variant per arrow. FortRise 5.x's
//      VariantConfiguration only exposes a StartWith field (an arrow to start the match with);
//      there is no confirmed modern equivalent for "never let this arrow spawn", so only the
//      8 "start with" variants are registered below, using each arrow's existing StartWith
//      icon from the shared atlas. The "exclude" icons in the atlas go unused.
//   2. "Cerberus" (DecoyArrowCerberus) was registered in the original mod too, but was never
//      actually read/checked anywhere in DecoyArrow's gameplay code -- it was already an
//      unfinished stub in the source we ported from, so it's carried over the same way here:
//      registered, with no gameplay effect.
public static class BlinkVariants
{
    public static void Register(IModuleContext context, IModContent content)
    {
        var variants = context.Registry.Variants;
        var subtextures = context.Registry.Subtextures;

        variants.RegisterVariant("MatterDisplacement", new VariantConfiguration
        {
            Title = "MATTER DISPLACEMENT",
            Icon = subtextures.RegisterTexture("BlinkArrowMatterDisplacementIcon", () => BlinkModule.Atlas["BlinkArrowMatterDisplacement"]),
            Description = "BLINK ARROWS HAVE A LITTLE EXTRA KICK",
            Flags = CustomVariantFlags.CanRandom
        });

        variants.RegisterVariant("Monostasis", new VariantConfiguration
        {
            Title = "MONOSTASIS",
            Icon = subtextures.RegisterTexture("GombocArrowMonostasisIcon", () => BlinkModule.Atlas["GombocArrowMonostasis"]),
            Description = "GOMBOC ARROWS MAINTAIN THEIR NATURAL EQUILIBRIUM",
            Flags = CustomVariantFlags.CanRandom
        });

        variants.RegisterVariant("KineticFusion", new VariantConfiguration
        {
            Title = "KINETIC FUSION",
            Icon = subtextures.RegisterTexture("NyoomArrowKinesisIcon", () => BlinkModule.Atlas["NyoomArrowKinesis"]),
            Description = "NYOOM ARROWS CONTAIN VOLATILE NUCLEAR MATERIAL",
            Flags = CustomVariantFlags.CanRandom
        });

        variants.RegisterVariant("Photosynthesis", new VariantConfiguration
        {
            Title = "PHOTOSYNTHESIS",
            Icon = subtextures.RegisterTexture("PerimeterArrowPhotosynthesisIcon", () => BlinkModule.Atlas["PerimeterArrowPhotosynthesis"]),
            Description = "BRAMBLES CREATED BY PERIMETER ARROWS NO LONGER DECAY",
            Flags = CustomVariantFlags.CanRandom
        });

        variants.RegisterVariant("IntrusiveThoughts", new VariantConfiguration
        {
            Title = "INTRUSIVE THOUGHTS",
            Icon = subtextures.RegisterTexture("SeekerArrowIntrusiveThoughtsIcon", () => BlinkModule.Atlas["SeekerArrowIntrusiveThoughts"]),
            Description = "SEEKER ARROWS GAIN A DASH",
            Flags = CustomVariantFlags.CanRandom
        });

        variants.RegisterVariant("Grudge", new VariantConfiguration
        {
            Title = "GRUDGE",
            Icon = subtextures.RegisterTexture("SokobanArrowGrudgeIcon", () => BlinkModule.Atlas["SokobanArrowGrudge"]),
            Description = "SOKOBAN ARROW BLOCKS KEEP CRUSHING",
            Flags = CustomVariantFlags.CanRandom
        });

        variants.RegisterVariant("Resynchronization", new VariantConfiguration
        {
            Title = "RESYNCHRONIZATION",
            Icon = subtextures.RegisterTexture("LatencyArrowResynchronizationIcon", () => BlinkModule.Atlas["LatencyArrowResynchronization"]),
            Description = "LATENCY ARROWS READJUST THEIR AIM",
            Flags = CustomVariantFlags.CanRandom
        });

        variants.RegisterVariant("Cerberus", new VariantConfiguration
        {
            Title = "CERBERUS",
            Icon = subtextures.RegisterTexture("DecoyArrowCerberusIcon", () => BlinkModule.Atlas["DecoyArrowCerberus"]),
            Description = "DECOY ARROWS TRIFURCATE MIDFLIGHT",
            Flags = CustomVariantFlags.CanRandom
        });

        variants.RegisterVariant("StartWithBlink", new VariantConfiguration
        {
            Title = "START WITH BLINK ARROWS",
            Icon = subtextures.RegisterTexture("BlinkArrowStartWithIcon", () => BlinkModule.Atlas["BlinkArrowStartWith"]),
            Flags = CustomVariantFlags.CanRandom,
            StartWith = BlinkModule.BlinkEntry
        });

        variants.RegisterVariant("StartWithGomboc", new VariantConfiguration
        {
            Title = "START WITH GOMBOC ARROWS",
            Icon = subtextures.RegisterTexture("GombocArrowStartWithIcon", () => BlinkModule.Atlas["GombocArrowStartWith"]),
            Flags = CustomVariantFlags.CanRandom,
            StartWith = BlinkModule.GombocEntry
        });

        variants.RegisterVariant("StartWithNyoom", new VariantConfiguration
        {
            Title = "START WITH NYOOM ARROWS",
            Icon = subtextures.RegisterTexture("NyoomArrowStartWithIcon", () => BlinkModule.Atlas["NyoomArrowStartWith"]),
            Flags = CustomVariantFlags.CanRandom,
            StartWith = BlinkModule.NyoomEntry
        });

        variants.RegisterVariant("StartWithPerimeter", new VariantConfiguration
        {
            Title = "START WITH PERIMETER ARROWS",
            Icon = subtextures.RegisterTexture("PerimeterArrowStartWithIcon", () => BlinkModule.Atlas["PerimeterArrowStartWith"]),
            Flags = CustomVariantFlags.CanRandom,
            StartWith = BlinkModule.PerimeterEntry
        });

        variants.RegisterVariant("StartWithSeeker", new VariantConfiguration
        {
            Title = "START WITH SEEKER ARROWS",
            Icon = subtextures.RegisterTexture("SeekerArrowStartWithIcon", () => BlinkModule.Atlas["SeekerArrowStartWith"]),
            Flags = CustomVariantFlags.CanRandom,
            StartWith = BlinkModule.SeekerEntry
        });

        variants.RegisterVariant("StartWithSokoban", new VariantConfiguration
        {
            Title = "START WITH SOKOBAN ARROWS",
            Icon = subtextures.RegisterTexture("SokobanArrowStartWithIcon", () => BlinkModule.Atlas["SokobanArrowStartWith"]),
            Flags = CustomVariantFlags.CanRandom,
            StartWith = BlinkModule.SokobanEntry
        });

        variants.RegisterVariant("StartWithLatency", new VariantConfiguration
        {
            Title = "START WITH LATENCY ARROWS",
            Icon = subtextures.RegisterTexture("LatencyArrowStartWithIcon", () => BlinkModule.Atlas["LatencyArrowStartWith"]),
            Flags = CustomVariantFlags.CanRandom,
            StartWith = BlinkModule.LatencyEntry
        });

        variants.RegisterVariant("StartWithDecoy", new VariantConfiguration
        {
            Title = "START WITH DECOY ARROWS",
            Icon = subtextures.RegisterTexture("DecoyArrowStartWithIcon", () => BlinkModule.Atlas["DecoyArrowStartWith"]),
            Flags = CustomVariantFlags.CanRandom,
            StartWith = BlinkModule.DecoyEntry
        });
    }
}
