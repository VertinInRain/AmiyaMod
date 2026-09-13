using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amiya.Art;
using Amiya.Cards;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Amiya.Boss;

/// <summary>
/// 克雷松的【飘忽】：每个玩家各自计数——每打出 4 张牌，
/// 向其手牌加入一张【锚点】；若其手牌中已有【未升级】的锚点，则改为升级那一张（不再新增）。
///
/// 写法照抄原版永世沙漏的 WitheringPresencePower（凋萎存在）：
///   · PowerInstanceType.Instanced → 每个玩家一份独立实例，计数各自独立；
///   · Target = 该玩家 → 只有本人能看到自己的剩余张数（多人下互不干扰）；
///   · 角标用 DynamicVar("CardsLeft") 显示剩余张数。
/// 实例挂在克雷松身上（与原版一致），只是 Target 指向玩家。
/// </summary>
public sealed class KresonFadePower : CustomPowerModel
{
    private const int BaseCardsLeft = 4;

    private const string CardsKey = "CardsLeft";

    public override string? CustomPackedIconPath => PlaceholderArt.Power("no_draw_power");

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    public override int DisplayAmount => DynamicVars[CardsKey].IntValue;

    protected override IEnumerable<DynamicVar> CanonicalVars => new[] { new DynamicVar(CardsKey, BaseCardsLeft) };

    public override List<(string, string)>? Localization => new()
    {
        ("title", "飘忽"),
        ("description", "每打出 4 张牌，向手牌加入一张【锚点】（若已有未升级的锚点则改为升级它）。角标为距离下一张锚点还需打出的牌数。")
    };

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player? owner = Target?.Player;
        if (owner == null || cardPlay.Card.Owner != owner)
        {
            return;
        }
        DynamicVars[CardsKey].BaseValue--;
        InvokeDisplayAmountChanged();
        if (DynamicVars[CardsKey].IntValue > 0)
        {
            return;
        }

        await Cmd.Wait(0.4f);
        await GrantAnchor(owner);
        Flash();
        DynamicVars[CardsKey].BaseValue = BaseCardsLeft;
        InvokeDisplayAmountChanged();
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
