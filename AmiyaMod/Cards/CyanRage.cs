using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Amiya.Cards;
/// <summary>青色怒火 - 领袖 检索抽牌堆+手牌所有打击防御，攻击打敌人防御打自己(升级后先升级再打�?</summary>
[Pool(typeof(AmiyaCardPool))]

public partial class CyanRage : BaseAmiyaCard
{ public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { AmiyaKeywords.Leader };
  public CyanRage() : base(3, CardType.Skill, CardRarity.Rare, TargetType.Self) { }
  protected override async Task OnPlay(PlayerChoiceContext cc, CardPlay cp)
  {
    var enemies = CombatState.HittableEnemies.ToList();
    if (enemies.Count == 0) return;
    var target = enemies[0];
    // 抽牌堆中的打击防御：直接从抽牌堆AutoPlay
    var drawPile = PileType.Draw.GetPile(Owner);
    foreach (var card in drawPile.Cards.Where(c => c is Strike || c is Defend).ToList())
    {
      if (IsUpgraded) CardCmd.Upgrade(card);
      if (card is Strike)
        await CardCmd.AutoPlay(cc, card, target, AutoPlayType.Default, false, true);
      else
        await CardCmd.AutoPlay(cc, card, null, AutoPlayType.Default, false, true);
    }
    // 手牌中的打击防御：先Exhaust消耗显示，再AutoPlay从消耗堆打出
    var handPile = PileType.Hand.GetPile(Owner);
    var handCards = handPile.Cards.Where(c => (c is Strike || c is Defend) && c != this).ToList();
    foreach (var card in handCards)
    {
      if (IsUpgraded) CardCmd.Upgrade(card);
      // Exhaust从手牌移入消耗堆（消除手牌UI残留），AutoPlay从消耗堆打出
      await CardCmd.Exhaust(cc, card);
      if (card is Strike)
        await CardCmd.AutoPlay(cc, card, target, AutoPlayType.Default, false, true);
      else
        await CardCmd.AutoPlay(cc, card, null, AutoPlayType.Default, false, true);
    }
  }
  // Leader choice disabled temporarily to prevent crashes
  // public override async Task AfterCardPlayedLate(PlayerChoiceContext cc, CardPlay cp)
  // { if (cp.Card == this) await LeaderChoiceHelper.DoLeaderChoice(cc, this, Owner); }
  protected override void OnUpgrade() { } }