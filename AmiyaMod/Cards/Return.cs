using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Cards;
/// <summary>洄还 - 领袖 随机造成3点伤�?/4�?/summary>
[Pool(typeof(AmiyaCardPool))]

public partial class Return : BaseAmiyaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new List<DynamicVar> { new DamageVar(3m, ValueProp.Move), new RepeatVar(3) };
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { AmiyaKeywords.Leader };
    public Return() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }
    protected override async Task OnPlay(PlayerChoiceContext cc, CardPlay cp)
    {
        int hitCount = DynamicVars.Repeat.IntValue;
        var enemies = CombatState.HittableEnemies.ToList();
        var rng = new System.Random();
        for (int i = 0; i < hitCount && enemies.Count > 0; i++)
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(enemies[rng.Next(enemies.Count)]).Execute(cc);
    }
    protected override void OnUpgrade() { DynamicVars.Repeat.UpgradeValueBy(1); }
}