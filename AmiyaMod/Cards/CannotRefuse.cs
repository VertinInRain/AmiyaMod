using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amiya.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Amiya.Cards;
/// <summary>不容拒绝 - 为所有打击防御添加魔王词�?消�?升级�?�?</summary>
[Pool(typeof(AmiyaCardPool))]

public partial class CannotRefuse : BaseAmiyaCard
{ public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { AmiyaKeywords.DemonLord };
  public CannotRefuse() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self) { }
  protected override async Task OnPlay(PlayerChoiceContext cc, CardPlay cp)
  {
    var dlc = Owner.Creature.GetPower<DemonLordCallPower>();
    if (dlc != null) await dlc.AddStack(cc);
    // 遍历所有牌�?
    var allPiles = new[] { PileType.Draw, PileType.Hand, PileType.Discard };
    foreach (var pileType in allPiles)
    {
      foreach (var card in pileType.GetPile(Owner).Cards.ToList())
      {
        if (card is Strike || card is Defend)
          card.AddKeyword(AmiyaKeywords.DemonLord);
      }
    }
  } protected override void OnUpgrade() { UpgradeStarCostBy(-1); } }