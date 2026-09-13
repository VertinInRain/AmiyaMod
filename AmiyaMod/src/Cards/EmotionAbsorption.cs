using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Cards;

/// <summary>情绪吸收（普通）：造成 7/9 点伤害，对目标敌人随机施加 2 层虚弱或 2 层易伤。</summary>
public sealed class EmotionAbsorption : BaseAmiyaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DamageVar(7m, ValueProp.Move) };

    public EmotionAbsorption() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "情绪吸收"),
        ("description", "#造成 !D! 点伤害。对敌人随机施加 2 层虚弱或 2 层易伤。")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        await CommonActions.CardAttack(this, cardPlay).Execute(choiceContext);

        // 多人安全：随机分支必须走联机同步的 Run Rng（Rng.Chaotic 按本地时间播种，
        // 各客户端结果不同会造成状态分歧掉线）
        bool weak = Owner!.RunState.Rng.Niche.NextInt(2) == 0;
        if (weak)
        {
            await PowerCmd.Apply<WeakPower>(choiceContext, cardPlay.Target, 2m, Owner!.Creature, null, silent: true);
        }
        else
        {
            await PowerCmd.Apply<VulnerablePower>(choiceContext, cardPlay.Target, 2m, Owner!.Creature, null, silent: true);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m); // 7 → 9
    }
}
