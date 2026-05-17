using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Amiya.Cards;
/// <summary>天边的火烧云 - 本回合为所有手牌添加虚�?/summary>
[Pool(typeof(AmiyaCardPool))]

public partial class HorizonFireCloud : BaseAmiyaCard
{ public HorizonFireCloud() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
  protected override async Task OnPlay(PlayerChoiceContext cc, CardPlay cp)
  {
    var handPile = PileType.Hand.GetPile(Owner);
    foreach (var card in handPile.Cards.ToList())
      card.AddKeyword(CardKeyword.Ethereal);
  } protected override void OnUpgrade() { } }