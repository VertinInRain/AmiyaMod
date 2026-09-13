using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amiya.Art;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Amiya.Cards;

/// <summary>
/// 天边的火烧云（罕见，1/0 费）：回合结束时，先于状态牌/诅咒牌的回合结束效果（如凋萎的伤害），
/// 消耗手牌中所有状态牌和诅咒牌（由 HorizonFireCloudPower 在 AutoPostPlay 阶段结算）。
/// </summary>
public sealed class HorizonFireCloud : BaseAmiyaCard
{
    public HorizonFireCloud() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "天边的火烧云"),
        ("description", "#回合结束时，消耗手牌中所有状态牌和诅咒牌。")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<HorizonFireCloudPower>(choiceContext, Owner!.Creature, 1m, Owner.Creature, null, silent: true);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1); // 1 → 0
    }
}

/// <summary>天边的火烧云 Power：回合结束（AutoPostPlay 阶段，先于虚无消耗与回合结束手牌效果）时，消耗手牌中所有状态牌和诅咒牌。</summary>
public sealed class HorizonFireCloudPower : CustomPowerModel
{
    public override string? CustomPackedIconPath => "res://Amiya/images/powers/HorizonFireCloudPower.png";
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;

    public override List<(string, string)>? Localization => new()
    {
        ("title", "天边的火烧云"),
        ("description", "回合结束时，消耗手牌中所有状态牌和诅咒牌。")
    };

    public override async Task AfterAutoPostPlayPhaseEntered(PlayerChoiceContext choiceContext, Player player)
    {
        await base.AfterAutoPostPlayPhaseEntered(choiceContext, player);
        if (player != Owner?.Player || Owner?.Player == null)
        {
            return;
        }
        // 本钩子在 DoTurnEnd（凋萎等 HasTurnEndInHandEffect 结算）之前执行，确保先消耗它们
        foreach (var card in PileType.Hand.GetPile(Owner.Player).Cards.ToList())
        {
            if (card.Rarity == CardRarity.Status || card.Rarity == CardRarity.Curse)
            {
                await CardCmd.Exhaust(choiceContext, card);
            }
        }
        // 一次性效果：结算后移除
        await PowerCmd.Remove(this);
    }
}
