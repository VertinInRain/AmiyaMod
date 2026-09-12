using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Cards;

/// <summary>
/// 我心永随（稀有，消耗）：获得 8/10 点格挡。回合开始时，若这张牌在消耗堆中，将其打出。
/// 由 FormManagerPower 回合开始自动打出（完整结算后再次消耗，形成每回合免费格挡循环，Q38 已裁）。
/// </summary>
public sealed class MyHeartFollows : BaseAmiyaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new BlockVar(8m, ValueProp.Move) };

    public override IEnumerable<CardKeyword> CanonicalKeywords => new CardKeyword[] { CardKeyword.Exhaust };

    public MyHeartFollows() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "我心永随"),
        ("description", "#获得 !B! 点格挡。回合开始时，若这张牌在消耗堆中，将其打出。")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardBlock(this, cardPlay);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2m); // 8 → 10
    }
}
