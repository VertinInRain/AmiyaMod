using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Amiya.Cards;

/// <summary>良夜将死（罕见）：将两张打击加入你的抽牌堆；下回合造成的伤害翻倍（费用 1/0）。</summary>
public sealed class GoodNightDying : BaseAmiyaCard
{
    public GoodNightDying() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "良夜将死"),
        ("description", "#将两张打击加入你的抽牌堆。下回合造成的伤害翻倍。")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 官方 TURBO 同款：CombatState.CreateCard + AddGeneratedCardToCombat + PreviewCardPileAdd（保证入堆动画与计数刷新）
        for (int i = 0; i < 2; i++)
        {
            var strike = CombatState.CreateCard<AmiyaStrike>(Owner);
            CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(strike, PileType.Draw, Owner, CardPilePosition.Random));
        }
        // 暗影步：下回合攻击造成双倍伤害（官方状态）
        await PowerCmd.Apply<ShadowStepPower>(choiceContext, Owner!.Creature, 1m, Owner.Creature, null, silent: true);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1); // 1 → 0
    }
}
