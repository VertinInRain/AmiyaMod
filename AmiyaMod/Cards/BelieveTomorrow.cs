using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Cards;
/// <summary>相信明天 - 感染 8格挡，选择2张非消耗攻/技牌本回合添加领袖(升级后含金刚)</summary>
[Pool(typeof(AmiyaCardPool))]

public partial class BelieveTomorrow : BaseAmiyaCard
{ public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { AmiyaKeywords.Infection };
  public BelieveTomorrow() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
  protected override async Task OnPlay(PlayerChoiceContext cc, CardPlay cp)
  {
    await CreatureCmd.GainBlock(Owner.Creature, 8m, ValueProp.Move, cp);
    // 选择手牌�?张无消耗词条的攻击或技能牌
    int count = IsUpgraded ? 2 : 1;
    var selected = await CardSelectCmd.FromHand(
      prefs: new CardSelectorPrefs(base.SelectionScreenPrompt, count),
      context: cc, player: Owner,
      filter: card => (card.Type == CardType.Attack || card.Type == CardType.Skill) && !card.Keywords.Contains(CardKeyword.Exhaust),
      source: this);
    foreach (var card in selected)
      card.AddKeyword(AmiyaKeywords.Leader); // 本回合有�?TODO: 需要仅限本回合的API)
  } protected override void OnUpgrade() { } }