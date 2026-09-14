using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace Amiya.Cards;

/// <summary>
/// 路网（任务牌，事件「在冰原之上」的奖励）：不能被打出。
/// 放弃 4 次卡牌奖励后，本牌完成并将遗物【无垠花】加入遗物栏，自身从牌组移除。
///
/// 计数方式：卡牌奖励被放弃时，游戏会对该奖励调用 Reward.OnSkipped()（见 AmiyaQuestPatch），
/// 由补丁同步写入 Skips（[SavedProperty]，两端一致），完成动作放在本牌自己的房间钩子里 await 执行。
/// 写法参考原版任务牌 探寻（Dowsing）。
/// </summary>
public sealed class RoadNetwork : BaseAmiyaCard
{
    /// <summary>需要放弃的卡牌奖励次数。</summary>
    public const int RequiredSkips = 4;

    private const string SkipsKey = "Skips";

    private int _skips;

    public override int MaxUpgradeLevel => 0;

    protected override IEnumerable<DynamicVar> CanonicalVars => new[] { new DynamicVar(SkipsKey, RequiredSkips) };

    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Unplayable };

    /// <summary>已放弃的卡牌奖励次数（存档属性；setter 里同步描述里的剩余次数）。</summary>
    [SavedProperty]
    public int Skips
    {
        get => _skips;
        set
        {
            AssertMutable();
            _skips = value;
            DynamicVars[SkipsKey].BaseValue = System.Math.Max(0, RequiredSkips - value);
        }
    }

    public RoadNetwork()
        : base(-1, CardType.Quest, CardRarity.Quest, TargetType.None, showInCardLibrary: false, autoAdd: true)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "路网"),
        ("description", "不能被打出。还需放弃 {Skips} 次卡牌奖励，之后获得遗物【无垠花】。")
    };

    /// <summary>由 AmiyaQuestPatch 在「卡牌奖励被放弃」时调用（同步写入，两端一致）。</summary>
    public void RegisterSkippedCardReward()
    {
        if (Skips < RequiredSkips)
        {
            Skips++;
        }
    }

    /// <summary>满足条件后在进入下一个房间时完成：发遗物、移除本牌。</summary>
    public override async Task BeforeRoomEntered(AbstractRoom room)
    {
        if (Skips < RequiredSkips || Pile?.Type != PileType.Deck || Owner == null)
        {
            return;
        }
        PlayerCmd.CompleteQuest(this);
        await RelicCmd.Obtain<Amiya.Relics.BoundlessFlowerRelic>(Owner);
        await CardPileCmd.RemoveFromDeck(this);
        MegaCrit.Sts2.Core.Logging.Log.Info("[Amiya] road network quest complete -> BoundlessFlowerRelic");
    }
}
