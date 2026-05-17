using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Cards;
[Pool(typeof(AmiyaCardPool))]

public partial class TomorrowAgain : BaseAmiyaCard
{ protected override IEnumerable<DynamicVar> CanonicalVars => new List<DynamicVar> { new BlockVar(16m, ValueProp.Move) }; public TomorrowAgain() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy) { }
  protected override async Task OnPlay(PlayerChoiceContext cc, CardPlay cp) {
    await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cp);
    if (cp.Target != null) await PowerCmd.Apply<RitualPower>(cc, new[] { cp.Target }, 1m, Owner.Creature, this);
  } protected override void OnUpgrade() { DynamicVars.Block.UpgradeValueBy(4m); } }