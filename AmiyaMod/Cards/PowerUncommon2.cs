using System.Collections.Generic;using System.Threading.Tasks;
using Amiya.Powers;using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;using BaseLib.Utils;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
namespace Amiya.Cards;
[Pool(typeof(AmiyaCardPool))]

public partial class NursingCard : BaseAmiyaCard { public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { AmiyaKeywords.Infection };
  public NursingCard() : base(2, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }
  protected override async Task OnPlay(PlayerChoiceContext cc, CardPlay cp) { await PowerCmd.Apply<NursingCardPower>(cc, new[] { Owner.Creature }, 1m, Owner.Creature, this); } protected override void OnUpgrade() { UpgradeStarCostBy(-1); } }
[Pool(typeof(AmiyaCardPool))]

public partial class ProphecyImage : BaseAmiyaCard { public ProphecyImage() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }
  protected override async Task OnPlay(PlayerChoiceContext cc, CardPlay cp) { await PowerCmd.Apply<ProphecyImagePower>(cc, new[] { Owner.Creature }, 1m, Owner.Creature, this); } protected override void OnUpgrade() { UpgradeStarCostBy(-1); } }
[Pool(typeof(AmiyaCardPool))]

public partial class VoidFragment : BaseAmiyaCard { public VoidFragment() : base(2, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }
  protected override async Task OnPlay(PlayerChoiceContext cc, CardPlay cp) { await PowerCmd.Apply<VoidFragmentPower>(cc, new[] { Owner.Creature }, 1m, Owner.Creature, this); } protected override void OnUpgrade() { } }
[Pool(typeof(AmiyaCardPool))]

public partial class TodayTomorrowDeviation : BaseAmiyaCard { public TodayTomorrowDeviation() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }
  protected override async Task OnPlay(PlayerChoiceContext cc, CardPlay cp) { decimal amount = IsUpgraded ? 8m : 6m; await PowerCmd.Apply<TodayTomorrowDeviationPower>(cc, new[] { Owner.Creature }, amount, Owner.Creature, this); } protected override void OnUpgrade() { } }