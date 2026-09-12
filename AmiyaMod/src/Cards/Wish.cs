using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Amiya.Cards;

/// <summary>
/// 祈愿（罕见能力牌，1费，升级后0费）：可打出耗能超出剩余能量的牌，并减少差值的手牌上限。
/// 打出牌后若手牌数大于削减后的手牌上限，丢弃手牌中最后几张牌直到相等。X 费牌不受影响。
/// </summary>
public sealed class Wish : BaseAmiyaCard
{
    public Wish() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "祈愿"),
        ("description", "#可打出超出当前能量的牌，并减少能量差值的手牌上限。")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<WishPower>(choiceContext, Owner!.Creature, 1m, Owner.Creature, null, silent: true);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1); // 1 → 0
    }
}

/// <summary>祈愿 Power：超支出牌许可 + 赤字计入手牌上限减少 + 弃牌到上限。</summary>
public sealed class WishPower : CustomPowerModel
{
    /// <summary>SpendResources 前缀补丁记录的本回合超支赤字（BeforeCardPlayed 时结算）。</summary>
    public static int PendingDeficit;

    public override string? CustomPackedIconPath => "res://Amiya/images/powers/WishPower.png";
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;

    public override List<(string, string)>? Localization => new()
    {
        ("title", "祈愿"),
        ("description", "可打出超出当前能量的牌，并减少能量差值的手牌上限。")
    };

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await base.AfterCardPlayed(choiceContext, cardPlay);
        if (Owner?.Player == null || cardPlay.Player != Owner.Player)
        {
            return;
        }

        // 1) 超支赤字 → 手牌上限减少层数
        if (PendingDeficit > 0)
        {
            await PowerCmd.Apply<HandLimitReductionPower>(choiceContext, Owner, PendingDeficit, Owner, null, silent: true);
            PendingDeficit = 0;
        }

        // 2) 手牌数超出削减后的上限 → 丢弃最后几张牌直到相等（任何上限削减都适用）
        await HandLimitHelper.DiscardDownToLimit(choiceContext, Owner.Player);
    }
}
