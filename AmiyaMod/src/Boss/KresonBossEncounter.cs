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

    /// <summary>地图上的 boss 节点图标（自制的克雷松图标，运行时注册进资源缓存）。</summary>
    public override string BossNodePath => KresonVisuals.IconNodePath;

    /// <summary>顶部血条 / 历史记录里的 boss 头像。</summary>
    public override string? CustomRunHistoryIconPath => KresonVisuals.IconPath;

    public override string? CustomRunHistoryIconOutlinePath => KresonVisuals.IconOutlinePath;

    public List<(string, string)>? Localization => new()
    {
        ("title", "无垠回荡克雷松"),
        ("loss", "被无垠的回响吞没了……")
    };
}
