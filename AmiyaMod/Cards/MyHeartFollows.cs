using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Cards;
/// <summary>我心永随 - 8/10格挡，回合开始若在消耗堆自动打出 消�?/summary>
[Pool(typeof(AmiyaCardPool))]

public partial class MyHeartFollows : BaseAmiyaCard
{ protected override IEnumerable<DynamicVar> CanonicalVars => new List<DynamicVar> { new BlockVar(8m, ValueProp.Move) };
  public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };
  public MyHeartFollows() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self) { }
  protected override async Task OnPlay(PlayerChoiceContext cc, CardPlay cp)
  {
    await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cp);
  }
  public override async Task AfterPlayerTurnStart(PlayerChoiceContext cc, Player player)
  {
    if (Pile?.Type == PileType.Exhaust && player == Owner)
      await CardCmd.AutoPlay(cc, this, null);
  }
  protected override void OnUpgrade() { DynamicVars.Block.UpgradeValueBy(2m); } }