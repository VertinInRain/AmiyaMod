using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Cards;

/// <summary>洞见之眼（普通）：获得 6 点格挡，抽 2/3 张牌，丢弃 2 张牌（玩家自选）。</summary>
public sealed class InsightEye : BaseAmiyaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new BlockVar(6m, ValueProp.Move),
        new CardsVar(2)
    };

    public InsightEye() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "洞见之眼"),
        ("description", "#获得 !B! 点格挡。抽 !C! 张牌。丢弃 2 张牌。")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardBlock(this, cardPlay);
        await CommonActions.Draw(this, choiceContext);

        var selected = await CommonActions.SelectCards(this, new LocString("cards", Id.Entry + ".title"), choiceContext, PileType.Hand, 2);
        foreach (var card in selected)
        {
            await CardPileCmd.Add(card, PileType.Discard, CardPilePosition.Bottom);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}
