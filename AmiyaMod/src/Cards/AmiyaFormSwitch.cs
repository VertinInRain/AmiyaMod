using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Cards;

/// <summary>
/// 思念：获得 8 点格挡，触发一次形态转换（费用 2/1）。
/// 实现 ITranscendenceCard：古老牙齿（ArchaicTooth）的原版"升华"机制会把它变化为万千愿景。
/// </summary>
public sealed class AmiyaFormSwitch : BaseAmiyaCard, ITranscendenceCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new BlockVar(8m, ValueProp.Move) };

    public AmiyaFormSwitch() : base(2, CardType.Skill, CardRarity.Basic, TargetType.Self)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "思念"),
        ("description", "#获得 !B! 点格挡。触发一次形态转换。")
    };

    /// <summary>古老牙齿升华目标：万千愿景（升级状态由原版流程自动继承）。</summary>
    public CardModel GetTranscendenceTransformedCard() => ModelDb.Card<ThousandVisions>();

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardBlock(this, cardPlay);
        await TriggerFormSwitch(choiceContext);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
