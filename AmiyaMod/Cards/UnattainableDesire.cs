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
/// <summary>求而不得之�?- 虚无 对随机敌�?/8伤害3次，回合开始若在消耗堆自动打出</summary>
[Pool(typeof(AmiyaCardPool))]

public partial class UnattainableDesire : BaseAmiyaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new List<DynamicVar> { new DamageVar(6m, ValueProp.Move), new RepeatVar(3) };
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Ethereal };
    public UnattainableDesire() : base(4, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { }
    protected override async Task OnPlay(PlayerChoiceContext cc, CardPlay cp)
    {
        var enemies = CombatState.HittableEnemies.ToList();
        var rng = new System.Random();
        for (int i = 0; i < DynamicVars.Repeat.IntValue && enemies.Count > 0; i++)
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(enemies[rng.Next(enemies.Count)]).Execute(cc);
    }
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext cc, Player player)
    {
        if (Pile?.Type == PileType.Exhaust && player == Owner)
            await CardCmd.AutoPlay(cc, this, null);
    }
    protected override void OnUpgrade() { DynamicVars.Damage.UpgradeValueBy(2m); }
}