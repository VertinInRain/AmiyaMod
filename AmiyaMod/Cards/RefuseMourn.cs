using System.Collections.Generic;
using System.Threading.Tasks;
using Amiya.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Amiya.Cards;
/// <summary>拒绝哀�?- 每当感染牌变为打�?防御时对全体造成10/13伤害</summary>
[Pool(typeof(AmiyaCardPool))]

public partial class RefuseMourn : BaseAmiyaCard
{ public RefuseMourn() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }
  protected override async Task OnPlay(PlayerChoiceContext cc, CardPlay cp)
  {
    decimal amount = IsUpgraded ? 13m : 10m;
    await PowerCmd.Apply<RefuseMournPower>(cc, new[] { Owner.Creature }, amount, Owner.Creature, this);
  } protected override void OnUpgrade() { } }