using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amiya.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Amiya.Relics;

[Pool(typeof(AmiyaRelicPool))]
public partial class PaleBlessingRelic : BaseAmiyaRelic
{
    public override RelicRarity Rarity => RelicRarity.Starter;
    private bool _isFirstTurn = true;
    private bool _isUpgraded = false;

    /// <summary>战前处理墓园：将抽牌堆中带"墓园"的牌移入弃牌堆</summary>
    public override async Task BeforeCombatStart()
    {
        await base.BeforeCombatStart();
        _isFirstTurn = true;
        if (Owner == null) return;
        var drawPile = PileType.Draw.GetPile(Owner);
        var discardPile = PileType.Discard.GetPile(Owner);
        foreach (var card in drawPile.Cards.ToList())
            if (card.Keywords.Contains(AmiyaKeywords.Graveyard))
                await CardPileCmd.Add(card, discardPile, CardPilePosition.Bottom, this, true);
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext cc, Player player)
    {
        await base.AfterPlayerTurnStart(cc, player);
        if (player.Creature != Owner.Creature) return;
        if (_isFirstTurn) {
            _isFirstTurn = false;
            if (!Owner.Creature.HasPower<FormManagerPower>())
                await PowerCmd.Apply<FormManagerPower>(cc, new[] { Owner.Creature }, 1m, Owner.Creature, null);
            return;
        }
        var fm = Owner.Creature.GetPower<FormManagerPower>();
        if (fm != null) { await fm.TriggerFormSwitch(cc); if (_isUpgraded) await fm.TriggerFormSwitch(cc); }
    }
    public override RelicModel? GetUpgradeReplacement() { _isUpgraded = true; return null; }
}
