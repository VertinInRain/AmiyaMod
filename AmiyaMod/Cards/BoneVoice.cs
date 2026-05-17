using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Cards;
[Pool(typeof(AmiyaCardPool))]

public partial class BoneVoice : BaseAmiyaCard
{ protected override IEnumerable<DynamicVar> CanonicalVars => new List<DynamicVar> { new BlockVar(6m, ValueProp.Move) };
  public BoneVoice() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
  protected override async Task OnPlay(PlayerChoiceContext cc, CardPlay cp) {
    await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cp);
    var exhaustPile = PileType.Exhaust.GetPile(Owner);
    int defendCount = exhaustPile.Cards.Count(c => c is Defend);
    for (int i = 0; i < defendCount; i++)
      await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Move, cp);
  } protected override void OnUpgrade() { DynamicVars.Block.UpgradeValueBy(3m); } }