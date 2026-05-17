using System.Collections.Generic;
using System.Threading.Tasks;
using Amiya.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Amiya.Cards;

[Pool(typeof(AmiyaCardPool))]

public partial class IronGuard : BaseAmiyaCard
{
    public IronGuard() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext cc, CardPlay cp)
    {
        decimal amount = IsUpgraded ? 4m : 3m;
        await PowerCmd.Apply<IronGuardPower>(cc, new[] { Owner.Creature }, amount, Owner.Creature, this);
    }
    protected override void OnUpgrade() { }
}

[Pool(typeof(AmiyaCardPool))]

public partial class Turbulence : BaseAmiyaCard
{
    public Turbulence() : base(2, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext cc, CardPlay cp)
    {
        await PowerCmd.Apply<TurbulencePower>(cc, new[] { Owner.Creature }, 1m, Owner.Creature, this);
    }
    protected override void OnUpgrade() { UpgradeStarCostBy(-1); }
}

[Pool(typeof(AmiyaCardPool))]

public partial class DemonLordVessel : BaseAmiyaCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { AmiyaKeywords.DemonLord };
    public DemonLordVessel() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext cc, CardPlay cp)
    {
        decimal amount = IsUpgraded ? 6m : 4m;
        await PowerCmd.Apply<DemonLordVesselPower>(cc, new[] { Owner.Creature }, amount, Owner.Creature, this);
    }
    protected override void OnUpgrade() { }
}

[Pool(typeof(AmiyaCardPool))]

public partial class DemonLordBanner : BaseAmiyaCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { AmiyaKeywords.DemonLord };
    public DemonLordBanner() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext cc, CardPlay cp)
    {
        decimal amount = IsUpgraded ? 2m : 1m;
        await PowerCmd.Apply<DemonLordBannerPower>(cc, new[] { Owner.Creature }, amount, Owner.Creature, this);
    }
    protected override void OnUpgrade() { }
}