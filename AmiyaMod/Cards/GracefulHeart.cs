using System.Collections.Generic;
using System.Threading.Tasks;
using Amiya.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Amiya.Cards;
[Pool(typeof(AmiyaCardPool))]

public partial class GracefulHeart : BaseAmiyaCard
{ public GracefulHeart() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }
  protected override async Task OnPlay(PlayerChoiceContext cc, CardPlay cp) {
    int draw = IsUpgraded ? 3 : 2;
    var fm = Owner.Creature.GetPower<FormManagerPower>();
    if (fm != null && fm.CurrentForm == 1) draw++;
    await CardPileCmd.Draw(cc, draw, Owner);
  } protected override void OnUpgrade() { } }