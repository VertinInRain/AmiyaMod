using System.Collections.Generic;
using System.Threading.Tasks;
using Amiya.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Amiya.Cards;

[Pool(typeof(AmiyaCardPool))]

public partial class Arrived : BaseAmiyaCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { AmiyaKeywords.DemonLord };
    public Arrived() : base(3, CardType.Power, CardRarity.Rare, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext cc, CardPlay cp)
    {
        var d = Owner.Creature.GetPower<DemonLordCallPower>();
        if (d != null) await d.AddStack(cc);
        await PowerCmd.Apply<ArrivedPower>(cc, new[] { Owner.Creature }, 1m, Owner.Creature, this);
    }
    protected override void OnUpgrade() { UpgradeStarCostBy(-1); }
}

[Pool(typeof(AmiyaCardPool))]

public partial class Spread : BaseAmiyaCard
{
    public Spread() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext cc, CardPlay cp)
    {
        await PowerCmd.Apply<SpreadPower>(cc, new[] { Owner.Creature }, 1m, Owner.Creature, this);
    }
    protected override void OnUpgrade() { UpgradeStarCostBy(-1); }
}

[Pool(typeof(AmiyaCardPool))]

public partial class Sowing : BaseAmiyaCard
{
    public Sowing() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext cc, CardPlay cp) { await PowerCmd.Apply<SowingPower>(cc, new[] { Owner.Creature }, 1m, Owner.Creature, this); }
    protected override void OnUpgrade() { }
}

[Pool(typeof(AmiyaCardPool))]

public partial class FinalShadow : BaseAmiyaCard
{
    public FinalShadow() : base(3, CardType.Power, CardRarity.Rare, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext cc, CardPlay cp) { await PowerCmd.Apply<FinalShadowPower>(cc, new[] { Owner.Creature }, 1m, Owner.Creature, this); }
    protected override void OnUpgrade() { }
}

[Pool(typeof(AmiyaCardPool))]

public partial class BuriedUnderground : BaseAmiyaCard
{
    public BuriedUnderground() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext cc, CardPlay cp) { decimal amount = IsUpgraded ? 2m : 1m; await PowerCmd.Apply<BuriedUndergroundPower>(cc, new[] { Owner.Creature }, amount, Owner.Creature, this); }
    protected override void OnUpgrade() { }
}

[Pool(typeof(AmiyaCardPool))]

public partial class WildRoar : BaseAmiyaCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { AmiyaKeywords.Graveyard };
    public WildRoar() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext cc, CardPlay cp) { await PowerCmd.Apply<WildRoarPower>(cc, new[] { Owner.Creature }, IsUpgraded ? 20m : 16m, Owner.Creature, this); }
    protected override void OnUpgrade() { }
}

[Pool(typeof(AmiyaCardPool))]

public partial class SeeLight : BaseAmiyaCard
{
    public SeeLight() : base(1, CardType.Power, CardRarity.Ancient, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext cc, CardPlay cp) { await PowerCmd.Apply<SeeLightPower>(cc, new[] { Owner.Creature }, 1m, Owner.Creature, this); }
    protected override void OnUpgrade() { UpgradeStarCostBy(-1); }
}