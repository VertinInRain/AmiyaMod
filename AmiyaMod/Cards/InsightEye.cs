using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Cards;
/// <summary>洞见之眼 - 获得5/8格挡，抽3张牌，丢�?张牌</summary>
[Pool(typeof(AmiyaCardPool))]

public partial class InsightEye : BaseAmiyaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new List<DynamicVar> { new BlockVar(5m, ValueProp.Move) };
    public InsightEye() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext cc, CardPlay cp)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cp);
        await CardPileCmd.Draw(cc, 3m, Owner);
        // 丢弃2张牌
        var toDiscard = await CardSelectCmd.FromHandForDiscard(cc, Owner, new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, 2), null, this);
        await CardCmd.Discard(cc, toDiscard);
    }
    protected override void OnUpgrade() { DynamicVars.Block.UpgradeValueBy(3m); }
}