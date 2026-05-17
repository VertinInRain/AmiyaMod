using System.Collections.Generic;
using System.Threading.Tasks;
using Amiya.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Amiya.Cards;
[Pool(typeof(AmiyaCardPool))]

public partial class SeeWhatISee : BaseAmiyaCard
{ public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { AmiyaKeywords.DemonLord };
  public SeeWhatISee() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
  protected override async Task OnPlay(PlayerChoiceContext cc, CardPlay cp) {
    var dlc = Owner.Creature.GetPower<DemonLordCallPower>();
    if (dlc != null) await dlc.AddStack(cc);
    int draw = IsUpgraded ? 4 : 3;
    await CardPileCmd.Draw(cc, draw, Owner);
  } protected override void OnUpgrade() { } }