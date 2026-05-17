using System.Collections.Generic;
using System.Threading.Tasks;
using Amiya.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Cards;

/// <summary>
/// 思念 - 初始技能牌
/// 获得8点格挡，触发一次形态转�?
/// 升级后耗能-1
/// </summary>
[Pool(typeof(AmiyaCardPool))]

public partial class FormSwitch : BaseAmiyaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new List<DynamicVar>
    {
        new BlockVar(8m, ValueProp.Move)
    };

    public FormSwitch() 
        : base(2, CardType.Skill, CardRarity.Basic, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        
        // 确保形态管理器存在（amount必须>0）
        if (!Owner.Creature.HasPower<FormManagerPower>())
            await PowerCmd.Apply<FormManagerPower>(choiceContext, new[] { Owner.Creature }, 1m, Owner.Creature, this);
        
        // 触发形态转换
        var formManager = Owner.Creature.GetPower<FormManagerPower>();
        if (formManager != null)
            await formManager.TriggerFormSwitch(choiceContext);
    }

    protected override void OnUpgrade()
    {
        // 升级后耗能-1（从2变为1�?
        UpgradeStarCostBy(-1);
    }
}
