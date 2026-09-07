using System;
using System.Reflection;
using System.Reflection.Emit;

namespace OopsAllBlinkArrows;

/// <summary>
/// Runtime replacement for the old [MonoModLinkTo] build-time trick. MonoMod.Patcher (the
/// package that attribute lives in, and the only thing FortRise's mod-load-time Relinker
/// pass needed it for) is being deprecated, so this project no longer references it at all.
///
/// The problem [MonoModLinkTo] solved: calling a specific base implementation of a virtual
/// method while skipping an intermediate override - e.g. LatencyArrow/MechArrow (both
/// TriggerArrow subclasses) need to run TowerFall.Arrow.Init(...) directly and skip
/// TriggerArrow's own Init() body, which crashes for arrows that don't set up TriggerArrow's
/// expected sprite fields. C# has no syntax for "skip one level of base" (only base.Method()),
/// and MonoModLinkTo used to solve it by having FortRise's relinker rewrite a stub method's IL
/// at mod-load time to call the named method directly.
///
/// CallBaseGen does the same thing at runtime with no external dependency: it emits a tiny
/// DynamicMethod whose IL uses a non-virtual `call` instruction (as opposed to `callvirt`)
/// against the target MethodInfo - the same instruction C# itself emits for `base.Method()`
/// calls, which invokes that exact implementation regardless of what overrides exist between
/// it and the runtime type of the instance. The resulting delegate is built once and cached
/// by the caller (a static readonly field), so there's no per-call reflection overhead.
/// </summary>
internal static class CallHelper
{
    public static Action<TInstance, TArg1, TArg2, TArg3> CallBaseGen<TInstance, TArg1, TArg2, TArg3>(
        Type declaringType, string methodName, params Type[] parameterTypes)
    {
        var target = declaringType.GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: parameterTypes,
            modifiers: null)
            ?? throw new MissingMethodException(declaringType.FullName, methodName);

        var dm = new DynamicMethod(
            name: $"CallBase_{declaringType.Name}_{methodName}",
            returnType: typeof(void),
            parameterTypes: new[] { typeof(TInstance), typeof(TArg1), typeof(TArg2), typeof(TArg3) },
            owner: declaringType,
            skipVisibility: true);

        var il = dm.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Ldarg_2);
        il.Emit(OpCodes.Ldarg_3);
        // Non-virtual call: invokes `target` itself, ignoring any override further down the
        // hierarchy - this is what [MonoModLinkTo] used to rewrite the stub method into.
        il.Emit(OpCodes.Call, target);
        il.Emit(OpCodes.Ret);

        return (Action<TInstance, TArg1, TArg2, TArg3>)
            dm.CreateDelegate(typeof(Action<TInstance, TArg1, TArg2, TArg3>));
    }
}
