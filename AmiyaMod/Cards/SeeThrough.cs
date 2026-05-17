using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Amiya.Cards;
/// <summary>勘破虚妄 - 选择任意张手牌变为防�?(升级后获得保�? 消�?/summary>
[Pool(typeof(AmiyaCardPool))]

public partial class SeeThrough : BaseAmiyaCard
{ public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };
  public SeeThrough() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
  protected override async Task OnPlay(PlayerChoiceContext cc, CardPlay cp)
  {
    var selected = await CardSelectCmd.FromHand(
      prefs: new CardSelectorPrefs(base.SelectionScreenPrompt, 0, 999),
      context: cc, player: Owner, filter: null, source: this);
    foreach (var card in selected.ToList())
    {
      var defend = CombatState.CreateCard<Defend>(Owner);
      CardCmd.Upgrade(defend);
      await CardCmd.Transform(card, defend);
    }
  } protected override void OnUpgrade() { AddKeyword(CardKeyword.Retain); } }