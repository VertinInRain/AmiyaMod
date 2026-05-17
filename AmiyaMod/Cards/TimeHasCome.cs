using System.Collections.Generic;
using System.Threading.Tasks;
using Amiya.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Amiya.Cards;
[Pool(typeof(AmiyaCardPool))]
public partial class TimeHasCome : BaseAmiyaCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { AmiyaKeywords.Graveyard, AmiyaKeywords.DemonLord, CardKeyword.Exhaust };
    public TimeHasCome() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext cc, CardPlay cp)
    {
        int replay = IsUpgraded ? 3 : 2;
        for (int i = 0; i < replay; i++)
        {
            var dlc = Owner.Creature.GetPower<DemonLordCallPower>();
            if (dlc != null) await dlc.AddStack(cc);
        }
    }
    protected override void OnUpgrade() { }
}