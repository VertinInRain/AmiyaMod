using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amiya.Powers;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Cards;

/// <summary>
/// 决意：造成 5/7 点伤害；本场战斗每触发过一次形态转换额外命中一次（最多 3 次/升级后 5 次）。
/// 卡面直接显示当前命中次数（Hits 计算变量，随战斗内切换次数实时预览）。
/// </summary>
public sealed class AmiyaResolve : BaseAmiyaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            var vars = new List<DynamicVar> { new DamageVar(5m, ValueProp.Move) };
            vars.AddRange(MakeCalculatedVar("Hits", 1, HitsBonus));
            return vars;
        }
    }

    /// <summary>命中次数 = 1 + min(本场战斗已切换次数, 上限-1)。</summary>
    private static decimal HitsBonus(CardModel card, Creature? target)
    {
        int cap = card.IsUpgraded ? 5 : 3;
        int extra = Math.Min(FormManagerPower.Current?.FormSwitchTotal ?? 0, cap - 1);
        return extra;
    }

    public AmiyaResolve() : base(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "决意"),
        // -3-+5+ = 升级前显示3，升级后显示5（BaseLib SimpleLoc 升级交换语法）
        // !Hits! = diff 预览渲染：卡面随战斗内切换次数实时更新（普通 {Hits} 只渲染静态基础值）
        ("description", "#造成 !D! 点伤害。命中 !Hits! 次。本场战斗每触发过一次形态转换，额外命中一次（最多-3-+5+次）。")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        int formSwitches = FormManagerPower.Current?.FormSwitchTotal ?? 0;
        int cap = IsUpgraded ? 5 : 3;
        int hits = Math.Min(1 + formSwitches, cap);

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(hits)
            .WithValueProp(ValueProp.Move)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        // 升级仅提升命中上限 3→5（伤害保持 5，作者 2026-09 修订）
    }
}
