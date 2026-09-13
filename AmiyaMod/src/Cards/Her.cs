using System.Collections.Generic;
using System.Threading.Tasks;
using Amiya.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Cards;

/// <summary>她？（罕见）：消耗所有魔王之唤，每消耗一层获得 12/15 点格挡。</summary>
public sealed class Her : BaseAmiyaCard
{
    public Her() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "她？"),
        ("description", "#消耗所有魔王之唤，每消耗一层获得 {IfUpgraded:show:15|12} 点格挡。")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 多人安全：按本牌归属玩家取魔王之唤，而不是本地玩家的静态 Current
        var call = DemonLordCallPower.Of(Owner);
        int stacks = call?.Amount ?? 0;
        if (Owner?.Creature != null && call != null && stacks > 0)
        {
            decimal perStack = IsUpgraded ? 15m : 12m;
            await CreatureCmd.GainBlock(Owner.Creature, stacks * perStack, ValueProp.Move, cardPlay);
            // 消耗全部层数（消耗不触发裁决——裁决只挂"获得"）
            await PowerCmd.ModifyAmount(choiceContext, call, -stacks, Owner.Creature, null, silent: true);
        }
    }

    protected override void OnUpgrade()
    {
        // 12 → 15 由 IsUpgraded 判定；费用 1 → 0
        EnergyCost.UpgradeBy(-1);
    }
}
