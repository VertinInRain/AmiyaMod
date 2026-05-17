using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Amiya.Cards;
/// <summary>去往何方 - 领袖 获得1/2能量</summary>
[Pool(typeof(AmiyaCardPool))]

public partial class WhereToGo : BaseAmiyaCard
{ public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { AmiyaKeywords.Leader };
  public WhereToGo() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self) { }
  protected override async Task OnPlay(PlayerChoiceContext cc, CardPlay cp)
  {
    await PlayerCmd.GainEnergy(IsUpgraded ? 2m : 1m, Owner);
  }
  protected override void OnUpgrade() { } }