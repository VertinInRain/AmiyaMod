using System.Collections.Generic;
using System.Threading.Tasks;
using Amiya.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Cards;
[Pool(typeof(AmiyaCardPool))]

public partial class Her : BaseAmiyaCard
{ public Her() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
  protected override async Task OnPlay(PlayerChoiceContext cc, CardPlay cp) {
    var dlc = Owner.Creature.GetPower<DemonLordCallPower>();
    if (dlc != null && dlc.Amount > 0) {
      decimal blockPer = IsUpgraded ? 15m : 12m;
      await CreatureCmd.GainBlock(Owner.Creature, blockPer * dlc.Amount, ValueProp.Move, cp);
      // 消耗所有魔王之唤：设置�?
      await PowerCmd.ModifyAmount(cc, dlc, -dlc.Amount, Owner.Creature, null);
    }
  } protected override void OnUpgrade() { } }