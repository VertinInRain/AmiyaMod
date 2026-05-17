using System.Collections.Generic;
using System.Threading.Tasks;
using Amiya.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Amiya.Cards;
/// <summary>存续先兆 - 魔王 消耗（升级后取消消耗）</summary>
[Pool(typeof(AmiyaCardPool))]

public partial class SurvivalOmen : BaseAmiyaCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { AmiyaKeywords.DemonLord, CardKeyword.Exhaust };
    public SurvivalOmen() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 魔王关键�?
        var dlc = Owner.Creature.GetPower<DemonLordCallPower>();
        if (dlc != null) await dlc.AddStack(choiceContext);
        // 本身无其他效果，仅作为魔王词条触发器+消�?
    }
    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Exhaust);
    }
}