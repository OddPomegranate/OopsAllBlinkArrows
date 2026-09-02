using FortRise;
using Microsoft.Extensions.Logging;

namespace OopsAllBlinkArrows;

// The single Mod-derived entry point for the combined mod. FortRise only instantiates the
// FIRST type in the assembly whose BaseType is exactly Mod (ModuleManager.LoadAssembly loops
// asm.GetTypes() and returns on the first match) - so OopsAllArrowsModule and BlinkModule,
// which used to each be their own Mod subclass back when they were separate mods, are now
// plain static classes with a Setup(context, content) method, both called from here.
public class OopsAllBlinkArrowsModule : Mod
{
    public static OopsAllBlinkArrowsModule Instance { get; private set; } = null!;

    public OopsAllBlinkArrowsModule(IModContent content, IModuleContext context, ILogger logger)
        : base(content, context, logger)
    {
        Instance = this;

        // Both halves' registrations touch the GraphicsDevice (atlas PNGs -> Texture2D),
        // which doesn't exist yet when this constructor runs (it's called from
        // RiseCore.Start(), before TFGame.LoadContent()) - so both wait for OnInitialize.
        OnInitialize += _ =>
        {
            OopsAllArrowsModule.Setup(context, content);
            BlinkModule.Setup(context, content);

            // Shared defensive fix for both arrow sets - registered once here rather than
            // once per half, since it patches one shared game type (ArrowTypePickup), not
            // anything specific to either arrow set. See ArrowTypePickupHooks.cs.
            ArrowTypePickupHooks.RegisterHooks(context.Harmony, logger);
        };
    }

    // Deliberately NOT overriding CreateSettings(): neither original mod had any real
    // configurable options, and an empty-but-non-null ModuleSettings is actively dangerous
    // here - FortRise's mod-list screen only skips transitioning into the Options menu for
    // a mod when CreateSettings() returns null (UIModMenu.InitMods' OnConfirmed check); a
    // non-null settings object with nothing in Create() gets past that guard and then
    // crashes MainMenu.CreateOptions(), which unconditionally does `list[1]` assuming the
    // options list always has a header plus at least one real button. Returning null (the
    // Mod base class default) makes clicking this mod in the mod list correctly a no-op
    // instead.
}
