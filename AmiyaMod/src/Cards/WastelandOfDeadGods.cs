using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amiya.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Cards;

/// <summary>
/// 众神死亡的荒原（稀有，墓园，保留，消耗）：消耗所有魔王之唤；
/// 若消耗层数等于 9（升级后不小于 9），则失去 9 点生命并斩杀所有体力低于 999 的敌人（Q39：恰好 9）。
/// </summary>
public sealed class WastelandOfDeadGods : BaseAmiyaCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => new CardKeyword[] { CardKeyword.Retain, CardKeyword.Exhaust };

    public override AmiyaTag AmiyaTags => AmiyaTag.Graveyard;

    public WastelandOfDeadGods() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "众神死亡的荒原"),
        ("description", "#消耗所有魔王之唤。若消耗层数{IfUpgraded:show:不小于 9|等于 9}，失去 9 点生命并斩杀所有体力低于 999 的敌人。")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int stacks = DemonLordCallPower.Current?.Amount ?? 0;
        if (stacks > 0)
        {
            await PowerCmd.ModifyAmount(choiceContext, DemonLordCallPower.Current!, -stacks, Owner!.Creature, null, silent: true);
        }

        bool nineHit = IsUpgraded ? stacks >= 9 : stacks == 9;
        if (nineHit)
        {
            // 失去 9 点生命（不可格挡）
            await CreatureCmd.Damage(choiceContext, Owner!.Creature, 9m, ValueProp.Unblockable | ValueProp.Unpowered, Owner.Creature, null, null);
            // 斩杀所有体力低于 999 的敌人。
            // 必须先 ToList() 快照再走官方批量 Kill：逐个 Kill 会从 Enemies 集合中移除目标，
            // foreach 中途集合被改会抛异常卡死整场战斗。
            var targets = Owner.Creature.CombatState.Enemies
                .Where(e => e.IsAlive && e.CurrentHp < 999m)
                .ToList();
            if (targets.Count > 0)
            {
                await CreatureCmd.Kill(targets, force: true);
                // 官方 Kill 不自动判定胜负，手动触发
                await MegaCrit.Sts2.Core.Combat.CombatManager.Instance.CheckWinCondition();
            }
        }
    }

    protected override void OnUpgrade()
    {
    }
}
