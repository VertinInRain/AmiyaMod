using System.Collections.Generic;
using System.Threading.Tasks;
using Amiya.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Amiya.Cards;
[Pool(typeof(AmiyaCardPool))]

public partial class SculptureInstallation : BaseAmiyaCard
{ public SculptureInstallation() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
  protected override async Task OnPlay(PlayerChoiceContext cc, CardPlay cp) {
    var fm = Owner.Creature.GetPower<FormManagerPower>();
    if (fm != null && fm.CurrentForm == 2)
      await CreatureCmd.Heal(Owner.Creature, IsUpgraded ? 12m : 8m);
  } protected override void OnUpgrade() { } }