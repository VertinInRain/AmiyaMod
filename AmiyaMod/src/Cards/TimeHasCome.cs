using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace Amiya.Cards;

/// <summary>时辰已到（罕见，墓园，魔王，消耗）：重放 2/3（本体无其他效果，用于积累魔王之唤）。</summary>
public sealed class TimeHasCome : BaseAmiyaCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => new CardKeyword[] { CardKeyword.Exhaust };

    public override AmiyaTag AmiyaTags => AmiyaTag.Graveyard | AmiyaTag.DemonLord;

    public TimeHasCome() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    public override void AfterCreated()
    {
        base.AfterCreated();
        BaseReplayCount = IsUpgraded ? 3 : 2;
    }

    protected override void AfterCloned()
    {
        // 关键：起始牌组建库走 ToMutable→AfterCloned（不走 AfterCreated），重放数必须在这里设置；
        // 且必须按 IsUpgraded 区分——战斗中的卡是升级实例的克隆，写死 2 会把升级后的 3 重置掉。
        base.AfterCloned();
        BaseReplayCount = IsUpgraded ? 3 : 2;
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "时辰已到"),
        ("description", "#")
    };

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new[]
    {
        HoverTipFactory.Static(StaticHoverTip.ReplayStatic)
    };

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        return Task.CompletedTask;
    }

    protected override void OnUpgrade()
    {
        BaseReplayCount = 3;
    }
}
