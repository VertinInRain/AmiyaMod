using System.Collections.Generic;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Relics;

namespace Amiya.Relics;

/// <summary>
/// 苍白花冠：苍白赐福被先古遗物升级后的形态——除第一回合外，每回合开始时触发两次形态转换。
/// 需挂 [Pool]（BaseLib 注册强制要求）；RelicRarity.Starter 权重 999999999，不会被随机奖励/商店选中，
/// 实际只能通过 PaleBlessingRelic.GetUpgradeReplacement() 获得。
/// </summary>
[Pool(typeof(Amiya.Models.AmiyaRelicPool))]
public sealed class PaleCrownRelic : BaseLib.Abstracts.CustomRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    public override string PackedIconPath => "res://Amiya/images/relic/PaleCrownRelic.png";

    protected override string BigIconPath => "res://Amiya/images/relic/PaleCrownRelic.png";

    public override List<(string, string)>? Localization => new()
    {
        ("title", "苍白花冠"),
        ("description", "除第一回合外，每回合开始时触发两次形态转换。"),
        ("flavor", "苍白的王冠已经加冕。")
    };

    public PaleCrownRelic()
        : base(autoAdd: true)
    {
    }

    /// <summary>第一层第一个 ? 房间锁定为事件房（花冠是赐福的升级形态，同样要保证事件出现）。</summary>
    public override IReadOnlySet<MegaCrit.Sts2.Core.Rooms.RoomType> ModifyUnknownMapPointRoomTypes(
        IReadOnlySet<MegaCrit.Sts2.Core.Rooms.RoomType> roomTypes)
        => IceFieldEventForcer.CanOverrideRoomTypes(Owner)
            ? new HashSet<MegaCrit.Sts2.Core.Rooms.RoomType> { MegaCrit.Sts2.Core.Rooms.RoomType.Event }
            : roomTypes;

    public override MegaCrit.Sts2.Core.Models.EventModel ModifyNextEvent(MegaCrit.Sts2.Core.Models.EventModel currentEvent)
        => IceFieldEventForcer.ForceEvent(Owner, currentEvent);
}
