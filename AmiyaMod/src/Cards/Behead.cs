using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Cards;

/// <summary>
/// 斩首（稀有）：触发一次形态转换，造成 7 点伤害，抽 2 张牌，消耗。升级后获得固有。
/// 消耗/固有 为关键词，由关键词系统自动渲染，描述文本不再重复。
/// </summary>
public sealed class Behead : BaseAmiyaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DamageVar(7m, ValueProp.Move) };

    public override IEnumerable<CardKeyword> CanonicalKeywords => new CardKeyword[] { CardKeyword.Exhaust };

    public Behead() : base(0, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "斩首"),
        ("description", "#触发一次形态转换。造成 !D! 点伤害。抽 {IfUpgraded:show:3|2} 张牌。")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await TriggerFormSwitch(choiceContext);
        await CommonActions.CardAttack(this, cardPlay).Execute(choiceContext);
        if (Owner != null)
        {
            await CardPileCmd.Draw(choiceContext, IsUpgraded ? 3m : 2m, Owner);
        }
    }

    protected override void OnUpgrade()
    {
        // 升级获得固有：官方做法为实例级 AddKeyword（UI 立即刷新）
        AddKeyword(CardKeyword.Innate);
    }
}
