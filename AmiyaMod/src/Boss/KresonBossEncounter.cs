using System.Collections.Generic;
using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace Amiya.Boss;

/// <summary>
/// 克雷松的三层 boss 遭遇。
///
/// 刻意让 IsValidForAct 恒为 false：它不进任何一层的随机 boss 池，
/// 只由 KresonBossPatch 在开局/读档时强制替换进去。
/// 这样进阶 10（两层 boss）时它不会被随机抽成第一个 boss 而出现两次。
/// </summary>
public sealed class KresonBossEncounter : CustomEncounterModel, ILocalizationProvider
{
    public KresonBossEncounter()
        : base(RoomType.Boss)
    {
    }

    public override bool IsValidForAct(ActModel act) => false;

    public override IEnumerable<MonsterModel> AllPossibleMonsters => new[] { ModelDb.Monster<KresonMonster>() };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
        => new (MonsterModel, string?)[] { (ModelDb.Monster<KresonMonster>().ToMutable(), null) };

    public override float GetCameraScaling() => 0.9f;

    public override Vector2 GetCameraOffset() => Vector2.Down * 60f;

    /// <summary>沿用原版三层 boss 战音乐（原版三个三层 boss 用的都是这首）。</summary>
    public override string CustomBgm => "event:/music/act3_boss_queen";

    /// <summary>
    /// 自制图标是否真的可用。地图节点图 / 血条头像是引擎按 res:// 路径「预加载」的资源，
    /// 路径存在但读不到会直接抛 AssetLoadException 并让开图崩掉，
    /// 所以这里先探测一次，缺失就退回原版占位图（绝对不能把开局搞崩）。
    /// </summary>
    private static bool OwnIconsAvailable =>
        ResourceLoader.Exists(KresonVisuals.IconPath) && ResourceLoader.Exists(KresonVisuals.IconOutlinePath);

    /// <summary>地图上的 boss 节点图标（自制；缺失时退回原版占位）。</summary>
    public override string BossNodePath => OwnIconsAvailable
        ? KresonVisuals.IconNodePath
        : "res://images/map/placeholder/aeonglass_boss_icon";

    /// <summary>顶部血条 / 历史记录里的 boss 头像。</summary>
    public override string? CustomRunHistoryIconPath => OwnIconsAvailable
        ? KresonVisuals.IconPath
        : "res://images/ui/run_history/aeonglass_boss.png";

    public override string? CustomRunHistoryIconOutlinePath => OwnIconsAvailable
        ? KresonVisuals.IconOutlinePath
        : "res://images/ui/run_history/aeonglass_boss_outline.png";

    public List<(string, string)>? Localization => new()
    {
        ("title", "无垠回荡克雷松"),
        ("loss", "被无垠的回响吞没了……")
    };
}
