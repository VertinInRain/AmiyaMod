using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;

namespace Amiya.Cards;
/// <summary>追寻 - 感染 将手牌中所有攻击牌变为灵魂</summary>
[Pool(typeof(AmiyaCardPool))]

public partial class Pursuit : BaseAmiyaCard
{ public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { AmiyaKeywords.Infection };
  public Pursuit() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
  protected override async Task OnPlay(PlayerChoiceContext cc, CardPlay cp)
  {
    var handPile = PileType.Hand.GetPile(Owner);
    var attackCards = handPile.Cards.Where(c => c.Type == CardType.Attack).ToList();
    foreach (var card in attackCards)
    {
      var soul = CombatState.CreateCard<Soul>(Owner);
      if (IsUpgraded) CardCmd.Upgrade(soul);
      await CardCmd.Transform(card, soul);
    }
  } protected override void OnUpgrade() { } }