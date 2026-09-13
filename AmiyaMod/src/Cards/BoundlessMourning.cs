using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Cards;

/// <summary>
/// 痛悼无垠（罕见，1 费，虚无）：手牌上限增加 2；
/// 被消耗时失去 1 点生命，升级后改为受到 2 点伤害。升级不改变费用。
/// </summary>
public sealed class BoundlessMourning : BaseAmiyaCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => new CardKeyword[] { CardKeyword.Ethereal };

    public BoundlessMourning() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "痛悼无垠"),
        ("description", "#手牌上限增加 2。被消耗时{IfUpgraded:show:受到 2 点伤害|失去 1 点生命}。")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<HandLimitIncreasePower>(choiceContext, Owner!.Creature, 2m, Owner.Creature, null, silent: true);
    }

    /// <summary>
    /// 被消耗时的代价。由 FormManagerPower.AfterCardExhausted 转发调用——
    /// 卡牌本身不是稳定的战斗钩子监听者（会随所在牌堆变化），挂在常驻力量上更可靠。
    /// </summary>
    public async Task OnExhausted(PlayerChoiceContext choiceContext)
    {
        if (Owner?.Creature == null)
        {
            return;
        }
        if (IsUpgraded)
        {
            // 升级后：受到 2 点伤害（可被格挡）
            await CreatureCmd.Damage(choiceContext, Owner.Creature, 2m, ValueProp.Move, Owner.Creature, null, null);
        }
        else
        {
            // 未升级：失去 1 点生命（无视格挡、不吃力量）
            await CreatureCmd.Damage(choiceContext, Owner.Creature, 1m, ValueProp.Unblockable | ValueProp.Unpowered, Owner.Creature, null, null);
        }
    }

    protected override void OnUpgrade()
    {
        // 升级只改"被消耗时"的代价文本与结算，费用不变
    }
}

/// <summary>手牌上限增加（层数 = 增加量）。与 HandLimitReductionPower 相反，二者按玩家分别结算。</summary>
public sealed class HandLimitIncreasePower : CustomPowerModel
{
    public override string? CustomPackedIconPath => Amiya.Art.PlaceholderArt.Power("no_draw_power");
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization => new()
    {
        ("title", "手牌上限增加"),
        ("description", "手牌上限增加（增加量 = 层数）。")
    };
}
