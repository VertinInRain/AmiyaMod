using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Cards;

[Pool(typeof(AmiyaCardPool))]
public partial class RedDawnDraw : BaseAmiyaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new List<DynamicVar> { new DamageVar(9m, ValueProp.Move) };
    public RedDawnDraw() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext cc, CardPlay cp)
    {
        var target = cp.Target; if (target == null) return;
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(target).Execute(cc);

        // 抽牌直到抽到打击/防御，参考Pillage
        CardModel drawn;
        do
        {
            drawn = await CardPileCmd.Draw(cc, Owner);
            if (drawn != null && (drawn is Strike || drawn is Defend))
                break;
        }
        while (drawn != null && PileType.Hand.GetPile(Owner).Cards.Count() < 10);
    }
    protected override void OnUpgrade() { DynamicVars.Damage.UpgradeValueBy(2m); }
}