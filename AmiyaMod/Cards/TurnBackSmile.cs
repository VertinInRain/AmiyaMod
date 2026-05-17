using System.Collections.Generic;
using System.Threading.Tasks;
using Amiya.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Amiya.Cards;
/// <summary>回身一�?- 触发一次形态转换，�?/2张牌</summary>
[Pool(typeof(AmiyaCardPool))]

public partial class TurnBackSmile : BaseAmiyaCard
{
    public TurnBackSmile() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var fm = Owner.Creature.GetPower<FormManagerPower>();
        if (fm != null) await fm.TriggerFormSwitch(choiceContext);
        int drawCount = IsUpgraded ? 2 : 1;
        await CardPileCmd.Draw(choiceContext, drawCount, Owner);
    }
    protected override void OnUpgrade() { /* 抽牌+1在OnPlay中处�?*/ }
}