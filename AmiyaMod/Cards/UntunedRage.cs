using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Cards;
/// <summary>待调弦的怒火 - 领袖 全体12/16伤，若未斩杀受到4点伤�?/summary>
[Pool(typeof(AmiyaCardPool))]

public partial class UntunedRage : BaseAmiyaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new List<DynamicVar> { new DamageVar(12m, ValueProp.Move) };
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { AmiyaKeywords.Leader };
    public UntunedRage() : base(1, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies) { }
    protected override async Task OnPlay(PlayerChoiceContext cc, CardPlay cp)
    {
        bool anyKilled = false;
        foreach (var enemy in CombatState.HittableEnemies)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(enemy).Execute(cc);
            if (enemy.IsDead) anyKilled = true;
        }
        if (!anyKilled)
            await CreatureCmd.Damage(cc, Owner.Creature, 4m, ValueProp.Move, this);
    }
    protected override void OnUpgrade() { DynamicVars.Damage.UpgradeValueBy(4m); }
}