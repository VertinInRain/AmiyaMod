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

public partial class Hello : BaseAmiyaCard
{ protected override IEnumerable<DynamicVar> CanonicalVars => new List<DynamicVar> { new BlockVar(8m, ValueProp.Move) };
  public Hello() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self) { }
  protected override async Task OnPlay(PlayerChoiceContext cc, CardPlay cp) {
    await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cp);
    var fm = Owner.Creature.GetPower<FormManagerPower>();
    if (fm != null) await fm.TriggerFormSwitch(cc);
    await CardPileCmd.Draw(cc, 2m, Owner);
  } protected override void OnUpgrade() { AddKeyword(CardKeyword.Innate); } }