using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amiya.Cards;
using Amiya.Relics;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;

namespace Amiya.Powers;

/// <summary>
/// 战斗钩子挂载器（无状态本体；战斗级状态用静态标记）。必须使用 ModelDb 规范实例。
/// 职责：
///  1) 墓园：阿米娅首回合抽牌前，把抽牌堆中带墓园词条的牌移入弃牌堆；
///  2) 首个回合开始把 FormManagerPower 施加到阿米娅玩家身上。
/// </summary>
public sealed class AmiyaCombatHarness : AbstractModel
{
    public override bool ShouldReceiveCombatHooks => true;

    private static bool GraveyardMoved;

    public override Task BeforeCombatStart()
    {
        GraveyardMoved = false;
        FormManagerPower.AmiyaDeathForm = null;
        return Task.CompletedTask;
    }

    /// <summary>
    /// 墓园：在每回合抽牌之前把抽牌堆中的墓园牌移入弃牌堆（一次性，GraveyardMoved 保证只执行一次）。
    /// 必须挂在 BeforeHandDraw——官方时序为 SetupPlayerTurn 内：BeforeHandDraw -> 固有牌置顶 -> 抽牌，
    /// 而 AfterAutoPrePlayPhaseEntered 在 setupPlayerTurnTask（已含开局抽牌）之后才触发，挂那里会导致墓园牌已被抽入手牌。
    /// </summary>
    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
    {
        await base.BeforeHandDraw(player, choiceContext, combatState);
        if (GraveyardMoved || PaleRelicHelper.TriggerCount(player) == 0)
        {
            return;
        }
        GraveyardMoved = true;

        var draw = PileType.Draw.GetPile(player);
        // 快照遍历：CardPileCmd.Add 会从抽牌堆移除卡牌，直接枚举会被"集合已修改"打断
        foreach (var card in draw.Cards.ToList())
        {
            if (card is BaseAmiyaCard amiyaCard && amiyaCard.HasAmiyaTag(AmiyaTag.Graveyard))
            {
                await CardPileCmd.Add(card, PileType.Discard, CardPilePosition.Bottom);
                Log.Info($"[Amiya] graveyard: {card.Id.Entry} -> discard at combat start");
            }
        }
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        await base.AfterPlayerTurnStart(choiceContext, player);

        if (player.Creature == null || player.Creature.HasPower<FormManagerPower>())
        {
            return;
        }

        // 仅阿米娅（以起始遗物苍白赐福/花冠判定）
        if (PaleRelicHelper.TriggerCount(player) == 0)
        {
            return;
        }

        Log.Info($"[Amiya] Harness applying FormManagerPower to {player.Creature.ModelId}");
        await PowerCmd.Apply<FormManagerPower>(choiceContext, player.Creature, 1m, player.Creature, null, silent: true);
        // 魔王之唤：懒加载——打出第一张魔王牌时由 FormManagerPower 施加（≥1 层才显示，作者要求）
    }
}
