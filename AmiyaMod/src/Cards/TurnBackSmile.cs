using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace Amiya.Cards;

/// <summary>回身一笑（普通）：触发一次形态转换，抽 1 张牌（费用 1/0）。</summary>
public sealed class TurnBackSmile : BaseAmiyaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new CardsVar(1) };

    public TurnBackSmile() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "回身一笑"),
        ("description", "#触发一次形态转换。抽 1 张牌。")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await TriggerFormSwitch(choiceContext);
        await CommonActions.Draw(this, choiceContext);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
