using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Cards;

/// <summary>残缺长角（罕见）：获得 15/18 点格挡；若打出后手牌中没有技能牌，下回合额外抽 1/2 张牌并获得 1/2 能量。</summary>
public sealed class BrokenHorn : BaseAmiyaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new BlockVar(15m, ValueProp.Move) };

    public BrokenHorn() : base(3, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "残缺长角"),
        ("description", "#获得 !B! 点格挡。若打出后手牌中没有技能牌，下回合额外抽 {IfUpgraded:show:2|1} 张牌并获得 {IfUpgraded:show:2|1} 点能量。")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 打出后快照（本牌已进入出牌堆）：手牌中是否还有技能牌
        bool noSkill = PileType.Hand.GetPile(Owner!).Cards.All(c => c.Type != CardType.Skill);

        await CommonActions.CardBlock(this, cardPlay);

        if (noSkill)
        {
            decimal amount = IsUpgraded ? 2m : 1m;
            await PowerCmd.Apply<DrawCardsNextTurnPower>(choiceContext, Owner!.Creature, amount, Owner.Creature, null, silent: true);
            await PowerCmd.Apply<EnergyNextTurnPower>(choiceContext, Owner.Creature, amount, Owner.Creature, null, silent: true);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m); // 15 → 18
    }
}
