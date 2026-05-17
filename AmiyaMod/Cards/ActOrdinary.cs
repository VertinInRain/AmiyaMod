using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Amiya.Cards;
/// <summary>演绎平凡 - 获得等同于手牌打击防御数量的能量(升级�?�?</summary>
[Pool(typeof(AmiyaCardPool))]

public partial class ActOrdinary : BaseAmiyaCard
{ public ActOrdinary() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
  protected override async Task OnPlay(PlayerChoiceContext cc, CardPlay cp)
  {
    var handPile = PileType.Hand.GetPile(Owner);
    int count = handPile.Cards.Count(c => c is Strike || c is Defend);
    if (count > 0)
      await PlayerCmd.GainEnergy(count, Owner);
  } protected override void OnUpgrade() { UpgradeStarCostBy(-1); } }