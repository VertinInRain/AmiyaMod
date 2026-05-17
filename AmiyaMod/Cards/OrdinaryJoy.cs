using System.Collections.Generic;
using System.Threading.Tasks;
using Amiya.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Amiya.Cards;
[Pool(typeof(AmiyaCardPool))]

public partial class OrdinaryJoy : BaseAmiyaCard
{ public OrdinaryJoy() : base(2, CardType.Power, CardRarity.Common, TargetType.Self) { }
  protected override async Task OnPlay(PlayerChoiceContext cc, CardPlay cp) {
    await PowerCmd.Apply<OrdinaryJoyPower>(cc, new[] { Owner.Creature }, 2m, Owner.Creature, this);
    // 抽一张牌
    await CardPileCmd.Draw(cc, 1m, Owner);
  } protected override void OnUpgrade() { UpgradeStarCostBy(-1); } }