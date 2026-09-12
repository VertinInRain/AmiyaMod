using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Cards;

/// <summary>骸骨传声（罕见）：获得 6 点格挡；消耗堆中每有一张牌，额外获得 1/2 点格挡。</summary>
public sealed class BoneVoice : BaseAmiyaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new BlockVar(6m, ValueProp.Move) };

    public BoneVoice() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "骸骨传声"),
        // -1-+2+ = 升级前每张消耗牌 +1 格挡，升级后 +2（BaseLib SimpleLoc 升级交换语法）
        ("description", "#获得 !B! 点格挡。消耗堆中每有一张牌，额外获得 -1-+2+ 点格挡。")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int exhausted = PileType.Exhaust.GetPile(Owner!).Cards.Count;
        decimal perCard = IsUpgraded ? 2m : 1m;
        await CreatureCmd.GainBlock(Owner!.Creature, 6m + exhausted * perCard, ValueProp.Move, cardPlay);
    }
}
