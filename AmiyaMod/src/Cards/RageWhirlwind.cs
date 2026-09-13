using System.Collections.Generic;
using System.Threading.Tasks;
using Amiya.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Cards;

/// <summary>
/// 盛怒旋风（罕见，X费）：对所有敌人造成 1 点伤害，获得 3/4 点临时力量，重放 X 次。
/// X = 投入能量（Q 已裁）。重放语义=重复结算伤害+临时力量（每重放一次都再次获得临时力量）。
/// </summary>
public sealed class RageWhirlwind : BaseAmiyaCard
{
    protected override bool HasEnergyCostX => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DamageVar(1m, ValueProp.Move) };

    public RageWhirlwind() : base(0, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "盛怒旋风"),
        ("description", "#对所有敌人造成 1 点伤害。获得 {IfUpgraded:show:4|3} 点临时力量。重放 X 次。")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int x = EnergyCost.CapturedXValue;
        decimal tempStrength = IsUpgraded ? 4m : 3m;

        // 首段 + 重放 X 段（每段：全体 1 伤 + 临时力量）
        for (int i = 0; i <= x; i++)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, cardPlay)
                .TargetingAllOpponents(CombatState)
                .Execute(choiceContext);
            if (FormManagerPower.Of(Owner) is { } fm)
            {
                await fm.GrantTempStrength(choiceContext, tempStrength);
            }
        }

        // 今时明日的偏差适配：手动循环的重放段引擎不计为出牌，
        // 这里把 X 个重放段额外计入下回合格挡池（出牌本身的那次由正常钩子计入，合计 X+1 次）
        if (x > 0
            && FormManagerPower.Of(Owner) is { } fm2
            && fm2.Owner.GetPower<TodayTomorrowDeviationPower>() is { } deviation)
        {
            fm2.AddPendingBlockNextTurn(x * (int)deviation.Amount);
        }
    }

    protected override void OnUpgrade()
    {
        // 临时力量 3 → 4（由 IsUpgraded 判定）
    }
}
