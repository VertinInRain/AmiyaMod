using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amiya.Cards;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Amiya.Boss;

/// <summary>
/// 克雷松的【飘忽】：每个玩家各自计数——每打出 4 张牌，
/// 向其手牌加入一张【锚点】；若其手牌中已有【未升级】的锚点，则改为升级那一张（不再新增）。
///
/// 写法照抄原版永世沙漏的 WitheringPresencePower（凋萎存在）：
///   · PowerInstanceType.Instanced → 每个玩家一份独立实例，计数各自独立；
///   · Target = 该玩家 → 只有本人能看到自己的剩余张数（多人下互不干扰）。
///
/// 剩余张数直接放在 Amount 上：角标显示 Amount，状态描述里的 {Amount} 也会被引擎填成同一个数值，
/// 所以"下标数字"和"描述里的 x"永远一致。
/// 实例挂在克雷松身上（与原版一致），只是 Target 指向玩家。
/// </summary>
public sealed class KresonFadePower : CustomPowerModel
{
    /// <summary>每多少张牌给一张锚点。</summary>
    public const int BaseCardsLeft = 4;

    public override string? CustomPackedIconPath => "res://Amiya/images/powers/KresonFadePower.png";

    public override List<(string, string)>? Localization => new()
    {
        ("title", "飘忽"),
        ("description", "再打出 {Amount} 张牌后向手牌中加入一张【锚点】，若手牌中已有未升级的锚点则升级这张锚点。")
    };

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player? owner = Target?.Player;
        if (owner == null || cardPlay.Card.Owner != owner)
        {
            return;
        }
        if (Amount > 1)
        {
            SetAmount(Amount - 1, silent: true);
            return;
        }

        await Cmd.Wait(0.4f);
        await GrantAnchor(owner);
        Flash();
        SetAmount(BaseCardsLeft, silent: true);
    }

    /// <summary>给该玩家一张锚点：手牌里已有未升级的锚点就升级它，否则新加一张。</summary>
    private static async Task GrantAnchor(Player player)
    {
        CardPile hand = PileType.Hand.GetPile(player);
        // 快照遍历：下面的升级/加牌都会改动牌堆
        CardModel? upgradable = hand.Cards
            .ToList()
            .FirstOrDefault(c => c is Anchor && !c.IsUpgraded && c.IsUpgradable);
        if (upgradable != null)
        {
            CardCmd.Upgrade(upgradable);
            await BaseAmiyaCard.RefreshCardVisualsAsync(upgradable);
        }
        else
        {
            await CardPileCmd.AddToCombatAndPreview<Anchor>(player.Creature, PileType.Hand, 1, null);
        }
    }
}
