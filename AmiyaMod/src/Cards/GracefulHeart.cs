using System.Collections.Generic;
using System.Threading.Tasks;
using Amiya.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace Amiya.Cards;

/// <summary>蕙质兰心（普通）：抽 2/3 张牌；若处于术士或魔王形态，额外抽 2 张。</summary>
public sealed class GracefulHeart : BaseAmiyaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new CardsVar(2) };

    public GracefulHeart() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "蕙质兰心"),
        ("description", "#抽 !C! 张牌。若处于术士或魔王形态，额外抽 2 张。")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.Draw(this, choiceContext);

        var form = FormManagerPower.Current?.Form ?? AmiyaForm.Guard;
        if (form == AmiyaForm.Caster || form == AmiyaForm.DemonLord)
        {
            await CardPileCmd.Draw(choiceContext, 2m, Owner!);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m); // 2 → 3
    }
}
