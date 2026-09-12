using System.Collections.Generic;
using System.Threading.Tasks;
using Amiya.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Cards;

/// <summary>
/// 相变不息（稀有）：打出此牌后，本回合每获得一次格挡（卡牌来源），触发一次形态转换。
/// 由 FormManagerPower.AfterBlockGained 结算（形态入形态效果的格挡不触发，防无限循环）。
/// </summary>
public sealed class PhaseChange : BaseAmiyaCard
{
    public PhaseChange() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "相变不息"),
        ("description", "#本回合每从卡牌中获得一次格挡，触发一次形态转换。{IfUpgraded:show:获得5格挡。|}")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 先开启"格挡→形态转换"结算，再获得升级后的 5 点格挡，
        // 这样这张牌自己给的格挡也会触发一次形态转换。
        if (FormManagerPower.Current != null)
        {
            FormManagerPower.Current.PhaseChangeActive = true;
        }
        if (IsUpgraded)
        {
            await CreatureCmd.GainBlock(Owner!.Creature, 5m, ValueProp.Move, cardPlay);
        }
    }

    protected override void OnUpgrade()
    {
    }
}
