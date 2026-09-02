using System.Collections.Generic;
using FortRise;
using TowerFall;

namespace OopsAllBlinkArrows;

// Modern replacement for both original mods' per-tower treasure boosts - OopsAllArrows'
// "Patcher.VERSUS_<Tower>.IncreaseTreasureRates(...)" calls and BlinkArrows' 7 "TowerPatch"
// subclasses alike (neither FortRise.OnTower/Patcher nor FortRise.TowerPatch exists in 5.x
// anymore). One of these gets registered per themed tower via
// context.Registry.TowerHooks.RegisterTowerHook(...) in each half's own Setup(), bumping one
// arrow's pickup rate in that tower's treasure chests - matching each original mod's
// 1-arrow-per-tower mapping exactly. Shared between both arrow sets since the logic is
// identical; only the tower/pickup pair passed in differs per call site.
public sealed class ArrowTreasureHook : ITowerHook
{
    public HashSet<string> TargetTowers { get; }
    private readonly Pickups pickup;

    public ArrowTreasureHook(string targetTower, Pickups pickup)
    {
        TargetTowers = new HashSet<string> { targetTower };
        this.pickup = pickup;
    }

    public void VersusTowerTreasurePatch(IVersusTowerTreasurePatchContext ctx)
    {
        ctx.IncreaseTreasureRates(pickup);
    }
}
