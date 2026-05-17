using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Amiya.Cards;

namespace Amiya.Powers;

/// <summary>
/// 预言显像 - 每回合开始时将存续先兆加入手�?
/// 参�? InfiniteBladesPower (回合初加入卡牌到手牌)
/// </summary>
public partial class ProphecyImagePower : BaseAmiyaPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner) return;
        Flash();
        // 创建存续先兆并加入手�?
        var card = CombatState.CreateCard<SurvivalOmen>(player);
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, player);
    }
}