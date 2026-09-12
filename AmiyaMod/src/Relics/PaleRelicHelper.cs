using MegaCrit.Sts2.Core.Entities.Players;

namespace Amiya.Relics;

/// <summary>
/// 苍白系列遗物判定：赐福=每回合 1 次转换，花冠=每回合 2 次转换。
/// 同时用作"该玩家是否阿米娅专属战斗"的判定（墓园/形态管理挂载）。
/// </summary>
public static class PaleRelicHelper
{
    public static int TriggerCount(Player player)
    {
        int count = 0;
        foreach (var relic in player.Relics)
        {
            if (relic is PaleCrownRelic)
            {
                count += 2;
            }
            else if (relic is PaleBlessingRelic)
            {
                count += 1;
            }
        }
        return count;
    }
}
