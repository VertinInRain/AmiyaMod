using System.Collections.Generic;
using System.Threading.Tasks;
using Amiya.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Cards;
[Pool(typeof(AmiyaCardPool))]

public partial class ThousandVisions : BaseAmiyaCard
{ protected override IEnumerable<DynamicVar> CanonicalVars => new List<DynamicVar> { new BlockVar(8m, ValueProp.Move) };
  public ThousandVisions() : base(2, CardType.Skill, CardRarity.Ancient, TargetType.Self) { }
  protected override async Task OnPlay(PlayerChoiceContext cc, CardPlay cp) {
    await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cp);
    var fm = Owner.Creature.GetPower<FormManagerPower>();
    if (fm != null) for (int i = 0; i < 3; i++) await fm.TriggerFormSwitch(cc);
  } protected override void OnUpgrade() { UpgradeStarCostBy(-1); } }