using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amiya.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Cards;

/// <summary>
/// 怒号光明（普通，感染）：造成 7/9 点伤害；每次打出（含重放）本回合失去 1 点力量（回合结束恢复）；
/// 重放 7/9 → 伤害依次 7,6,5,4,3,2,1,0（作者 2026-09 修订）。
/// </summary>
public sealed class RoaringLight : BaseAmiyaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DamageVar(7m, ValueProp.Move) };

    public override AmiyaTag AmiyaTags => AmiyaTag.Infection;

    public RoaringLight() : base(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    public override void AfterCreated()
    {
        base.AfterCreated();
        BaseReplayCount = IsUpgraded ? 9 : 7;
    }

    protected override void AfterCloned()
    {
        // 关键：起始牌组建库走 ToMutable→AfterCloned（不走 AfterCreated），重放数必须在这里设置；
        // 且必须按 IsUpgraded 区分——战斗中的卡是升级实例的克隆，写死 7 会把升级后的 9 重置掉。
        base.AfterCloned();
        BaseReplayCount = IsUpgraded ? 9 : 7;
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "怒号光明"),
        ("description", "#造成 !D! 点伤害。每次打出：本回合失去 1 点力量。")
    };

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new[]
    {
        HoverTipFactory.Static(StaticHoverTip.ReplayStatic)
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await CommonActions.CardAttack(this, cardPlay).Execute(choiceContext);

        // 每次打出（含每段重放）：本回合失去 1 点力量（官方临时力量下降，回合结束自动恢复）
        await PowerCmd.Apply<AmiyaTempStrengthDownPower>(choiceContext, Owner!.Creature, 1m, Owner.Creature, null, silent: true);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m); // 7 → 9
        BaseReplayCount = 9;                    // 重放 7 → 9
    }
}
