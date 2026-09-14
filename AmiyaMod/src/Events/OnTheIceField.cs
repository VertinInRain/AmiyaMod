using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amiya.Cards;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Events;

/// <summary>
/// 事件「在冰原之上」：第一阶段第一个 ? 房间强制出现（见 AmiyaEventSpawnPatch）。
/// 选项：暂且无视（失去 7 点生命，获得 100 金币）／追寻根源（获得任务牌【路网】）。
/// 本地化键遵循 BaseLib 约定：选项键 = &lt;事件entry&gt;.pages.INITIAL.options.&lt;方法名大写蛇形&gt;.title/.description。
/// </summary>
public sealed class OnTheIceField : CustomEventModel
{
    public OnTheIceField()
        : base(autoAdd: true)
    {
    }

    /// <summary>多人共享事件（与原版战史学家一致）：所有玩家看到同一份状态。</summary>
    public override bool IsShared => true;

    /// <summary>不参与随机事件池，只能由 PaleBlessingRelic / PaleCrownRelic 强制插入。</summary>
    public override bool IsAllowed(IRunState runState) => false;

    public override List<(string, string)>? Localization => new()
    {
        ("title", "在冰原之上"),
        ("pages.INITIAL.description", "你看见荒原上的巨大黑影。\n你决定……"),
        ("pages.INITIAL.options.IGNORE.title", "暂且无视"),
        ("pages.INITIAL.options.IGNORE.description", "失去 7 点生命，获得 100 金币。"),
        ("pages.INITIAL.options.SEEK_SOURCE.title", "追寻根源"),
        ("pages.INITIAL.options.SEEK_SOURCE.description", "获得【路网】。"),
        ("pages.IGNORE_RESULT.description", "你转过身去。寒风割过皮肤，但口袋里沉了一点。"),
        ("pages.SEEK_RESULT.description", "你朝那道黑影走去，脚下多了一张路网。")
    };

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() => new[]
    {
        Option(Ignore),
        Option(SeekSource)
    };

    /// <summary>暂且无视：失去 7 点生命（无视格挡），获得 100 金币。</summary>
    private async Task Ignore()
    {
        if (Owner?.Creature == null)
        {
            return;
        }
        var choiceContext = new ThrowingPlayerChoiceContext();
        await CreatureCmd.Damage(choiceContext, Owner.Creature, 7m,
            DamageProps.nonCardHpLoss, Owner.Creature, null, null);
        await PlayerCmd.GainGold(100m, Owner);
        SetEventFinished(new LocString("events", Id.Entry + ".pages.IGNORE_RESULT.description"));
    }

    /// <summary>追寻根源：把任务牌【路网】加入牌组。</summary>
    private async Task SeekSource()
    {
        if (Owner == null)
        {
            return;
        }
        CardModel card = Owner.RunState.CreateCard<RoadNetwork>(Owner);
        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(card, PileType.Deck), 1.2f, CardPreviewStyle.EventLayout);
        await Cmd.CustomScaledWait(0.5f, 1.2f);
        SetEventFinished(new LocString("events", Id.Entry + ".pages.SEEK_RESULT.description"));
    }
}
