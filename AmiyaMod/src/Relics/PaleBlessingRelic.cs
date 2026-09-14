using System.Collections.Generic;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;

namespace Amiya.Relics;

/// <summary>
/// 苍白赐福（起始遗物）：除第一回合外，每回合开始时触发一次形态转换（由 FormManagerPower 代为结算）。
/// 被先古遗物升级后，替换为 苍白花冠（GetUpgradeReplacement，官方 TouchOfOrobas 管线）。
/// </summary>
[Pool(typeof(Amiya.Models.AmiyaRelicPool))]
public sealed class PaleBlessingRelic : BaseLib.Abstracts.CustomRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    public override string PackedIconPath => "res://Amiya/images/relic/PaleBlessingRelic.png";

    protected override string BigIconPath => "res://Amiya/images/relic/PaleBlessingRelic.png";

    public override RelicModel? GetUpgradeReplacement() => ModelDb.Relic<PaleCrownRelic>();

    public override List<(string, string)>? Localization => new()
    {
        ("title", "苍白赐福"),
        ("description", "除第一回合外，每回合开始时触发一次形态转换。"),
        ("flavor", "苍白的王冠静候加冕。")
    };

    public PaleBlessingRelic()
        : base(autoAdd: true)
    {
    }

    /// <summary>第一层第一个 ? 房间锁定为事件房（为了「在冰原之上」必定出现）。</summary>
    public override IReadOnlySet<MegaCrit.Sts2.Core.Rooms.RoomType> ModifyUnknownMapPointRoomTypes(
        IReadOnlySet<MegaCrit.Sts2.Core.Rooms.RoomType> roomTypes)
        => IceFieldEventForcer.CanOverrideRoomTypes(Owner)
            ? new HashSet<MegaCrit.Sts2.Core.Rooms.RoomType> { MegaCrit.Sts2.Core.Rooms.RoomType.Event }
            : roomTypes;

    /// <summary>把第一层第一个 ? 房间抽到的事件换成「在冰原之上」。</summary>
    public override EventModel ModifyNextEvent(EventModel currentEvent)
        => IceFieldEventForcer.ForceEvent(Owner, currentEvent);
}
