using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amiya.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Cards;
/// <summary>以血还血 - 魔王 受到2点伤�?/4次，随机造成6点伤�?/4�?/summary>
[Pool(typeof(AmiyaCardPool))]

public partial class BloodForBlood : BaseAmiyaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new List<DynamicVar> { new DamageVar(6m, ValueProp.Move), new RepeatVar(3) };
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { AmiyaKeywords.DemonLord };
    public BloodForBlood() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }
    protected override async Task OnPlay(PlayerChoiceContext cc, CardPlay cp)
    {
        var dlc = Owner.Creature.GetPower<DemonLordCallPower>();
        if (dlc != null) await dlc.AddStack(cc);
        int count = DynamicVars.Repeat.IntValue;
        var enemies = CombatState.HittableEnemies.ToList();
        var rng = new System.Random();
        for (int i = 0; i < count; i++)
        {
            await CreatureCmd.Damage(cc, Owner.Creature, 2m, ValueProp.Unpowered, this);
            if (enemies.Count > 0)
                await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(enemies[rng.Next(enemies.Count)]).Execute(cc);
        }
    }
    protected override void OnUpgrade() { DynamicVars.Repeat.UpgradeValueBy(1m); }
}