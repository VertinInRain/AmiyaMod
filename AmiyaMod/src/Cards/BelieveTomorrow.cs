using System.Collections.Generic;
using System.Threading.Tasks;
using Amiya.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Cards;

/// <summary>相信明天（罕见，感染）：获得 8 点格挡，选择手牌中 1/2 张非消耗的攻击或技能牌添加领袖。</summary>
public sealed class BelieveTomorrow : BaseAmiyaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new BlockVar(8m, ValueProp.Move) };

    public override AmiyaTag AmiyaTags => AmiyaTag.Infection;

    public BelieveTomorrow() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "相信明天"),
        ("description", "#获得 !B! 点格挡，选择手牌中 -1-+2+ 张非消耗的攻击或技能牌添加领袖。")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardBlock(this, cardPlay);

        int count = IsUpgraded ? 2 : 1;
        var selected = await CardSelectCmd.FromHand(
            choiceContext, Owner!,
            new CardSelectorPrefs(new LocString("cards", Id.Entry + ".title"), 1, count),
            c => (c.Type == CardType.Attack || c.Type == CardType.Skill) && !c.Keywords.Contains(CardKeyword.Exhaust),
            this);
        foreach (var card in selected)
        {
            if (card is BaseAmiyaCard ac)
            {
                ac.InstanceTags |= AmiyaTag.Leader;
                // 官方 HandTrick/ApplySingleTurnSly 同款刷新；带重试（选牌容器收回期间节点暂不可达）
                _ = BaseAmiyaCard.RefreshCardVisualsAsync(card);
                // 领袖白光特效随奇偶刷新（白光/黑雾是挂在牌上的状态，必须 await 在确定性流程内完成）
                if (FormManagerPower.Of(Owner) is { } fm)
                {
                    await fm.RefreshLeaderGlow(Owner!);
                }
            }
        }
    }

    protected override void OnUpgrade()
    {
    }
}
