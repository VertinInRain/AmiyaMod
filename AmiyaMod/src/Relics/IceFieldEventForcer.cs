using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Runs.History;

namespace Amiya.Relics;

/// <summary>
/// 「在冰原之上」事件的强制出现：第一阶段（第一层）的第一个 ? 房间必定是它。
///
/// 写法参考原版「灯火钥匙 → 战史学家」与「黄金罗盘」：遗物本身就是 run hook 监听者
/// （RunState.IterateHookListeners 会遍历玩家遗物），所以覆写
///   · ModifyUnknownMapPointRoomTypes → 把该 ? 房间的类型锁成 Event（否则 ? 可能根本不是事件房）
///   · ModifyNextEvent → 把抽到的事件换成我们的
/// 即可强制出现，不需要 Harmony 补丁。
///
/// "每局只出一次"用 runState.VisitedEventIds 判断（事件抽出的瞬间就会被记进去，且随存档持久化），
/// 多人下两端读到的是同一份状态，判定一致。
/// </summary>
internal static class IceFieldEventForcer
{
    /// <summary>是否可以把这个 ? 房间锁定为事件房（无副作用，可反复查询）。</summary>
    public static bool CanOverrideRoomTypes(Player? owner)
        => Resolve(owner) != null;

    /// <summary>把抽到的事件换成「在冰原之上」。</summary>
    public static EventModel ForceEvent(Player? owner, EventModel currentEvent)
    {
        if (Resolve(owner) == null)
        {
            return currentEvent;
        }
        MegaCrit.Sts2.Core.Logging.Log.Info("[Amiya] forced event OnTheIceField in the first ? room of act 1");
        return ModelDb.Event<Amiya.Events.OnTheIceField>();
    }

    private static IRunState? Resolve(Player? owner)
    {
        IRunState? runState = owner?.RunState;
        if (runState == null)
        {
            return null;
        }
        // 只在第一层、? 房间触发
        if (runState.CurrentActIndex != 0)
        {
            return null;
        }
        if (runState.CurrentMapPoint?.PointType != MapPointType.Unknown)
        {
            return null;
        }
        // 只认"第一个" ? 房间：本层历史里还没出现过 ? 房间。
        // 注意 MapPointHistory 的当前节点是在抽完事件之后才写入的，所以这里看不到自己。
        IReadOnlyList<IReadOnlyList<MapPointHistoryEntry>> history = runState.MapPointHistory;
        if (history.Count > 0 && history[0].Any(e => e.MapPointType == MapPointType.Unknown))
        {
            return null;
        }
        // 阿米娅的局才出（遗物本身只属于阿米娅，这里再确认一次，多人下按"任一玩家是阿米娅"判定）
        return runState.Players.Any(p => p.Relics.Any(r => r is PaleBlessingRelic or PaleCrownRelic))
            ? runState
            : null;
    }
}
