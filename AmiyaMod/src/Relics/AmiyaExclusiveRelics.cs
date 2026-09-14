using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Relics;

/// <summary>长生者之证：每场战斗第一次损失生命时，获得损失量一半的再生。</summary>
[Pool(typeof(Amiya.Models.AmiyaRelicPool))]
public sealed class LongevityProofRelic : CustomRelicModel
{
    private bool _used;

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override string PackedIconPath => "res://Amiya/images/relic/LongevityProofRelic.png";
    protected override string BigIconPath => "res://Amiya/images/relic/LongevityProofRelic.png";

    public override List<(string, string)>? Localization => new()
    {
        ("title", "长生者之证"),
        ("description", "每场战斗第一次损失生命时，获得损失量一半的再生。"),
        ("flavor", "岁月不语，唯证长存。")
    };

    public LongevityProofRelic() : base(autoAdd: true)
    {
    }

    public override Task BeforeCombatStart()
    {
        _used = false;
        return Task.CompletedTask;
    }

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        await base.AfterDamageReceived(choiceContext, target, result, props, dealer, cardSource);
        if (_used || target != Owner?.Creature || result.UnblockedDamage <= 0)
        {
            return;
        }
        _used = true;
        decimal regen = System.Math.Max(1m, result.UnblockedDamage / 2m);
        await PowerCmd.Apply<RegenPower>(choiceContext, target, regen, target, null, silent: true);
    }
}

/// <summary>追猎：每回合开始时，对场上血量最低的敌方施加一层易伤。</summary>
[Pool(typeof(Amiya.Models.AmiyaRelicPool))]
public sealed class HuntRelic : CustomRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override string PackedIconPath => "res://Amiya/images/relic/HuntRelic.png";
    protected override string BigIconPath => "res://Amiya/images/relic/HuntRelic.png";

    public override List<(string, string)>? Localization => new()
    {
        ("title", "追猎"),
        ("description", "每回合开始时，对场上血量最低的敌方施加一层易伤。"),
        ("flavor", "猎物越是虚弱，猎人越不会放过。")
    };

    public HuntRelic() : base(autoAdd: true)
    {
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        await base.AfterPlayerTurnStart(choiceContext, player);
        if (player != Owner || Owner?.Creature == null)
        {
            return;
        }
        Creature? lowest = Owner.Creature.CombatState.Enemies
            .Where(e => e.IsAlive)
            .OrderBy(e => e.CurrentHp)
            .FirstOrDefault();
        if (lowest != null)
        {
            await PowerCmd.Apply<VulnerablePower>(choiceContext, lowest, 1m, Owner.Creature, null, silent: true);
        }
    }
}

/// <summary>无名图腾：本回合造成伤害大于敌方意图时，获得"本回合受到伤害减半"状态。</summary>
[Pool(typeof(Amiya.Models.AmiyaRelicPool))]
public sealed class NamelessTotemRelic : CustomRelicModel
{
    private decimal _turnDamageDealt;
    private bool _statusApplied;

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override string PackedIconPath => "res://Amiya/images/relic/NamelessTotemRelic.png";
    protected override string BigIconPath => "res://Amiya/images/relic/NamelessTotemRelic.png";

    public override List<(string, string)>? Localization => new()
    {
        ("title", "无名图腾"),
        ("description", "本回合造成伤害大于敌方意图时，本回合受到伤害减半。"),
        ("flavor", "无人知晓其名，唯有伤痕记得。")
    };

    public NamelessTotemRelic() : base(autoAdd: true)
    {
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        await base.AfterPlayerTurnStart(choiceContext, player);
        // 多人：只处理持有者自己的回合，否则别人的回合开始会把持有者的图腾状态提前清掉
        if (player != Owner)
        {
            return;
        }
        _turnDamageDealt = 0m;
        if (_statusApplied && Owner?.Creature != null && Owner.Creature.HasPower<NamelessTotemPower>())
        {
            await PowerCmd.Remove(Owner.Creature.GetPower<NamelessTotemPower>()!);
        }
        _statusApplied = false;
    }

    private static decimal EnemyIntent(Creature enemy)
    {
        try
        {
            IReadOnlyList<AbstractIntent>? intents = enemy.Monster?.NextMove?.Intents;
            if (intents == null || enemy.CombatState == null)
            {
                return 0m;
            }
            IEnumerable<Creature> targets = enemy.CombatState.Players.Select(p => p.Creature);
            return intents.OfType<AttackIntent>().Sum(i => i.GetTotalDamage(targets, enemy));
        }
        catch
        {
            return 0m;
        }
    }

    private static decimal TotalEnemyIntent(Creature owner)
    {
        try
        {
            if (owner.CombatState == null)
            {
                return 0m;
            }
            return owner.CombatState.Enemies.Where(e => e.IsAlive).Sum(EnemyIntent);
        }
        catch
        {
            return 0m;
        }
    }

    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        await base.AfterDamageGiven(choiceContext, dealer, result, props, target, cardSource);
        if (dealer != Owner?.Creature || Owner?.Creature == null)
        {
            return;
        }
        _turnDamageDealt += result.UnblockedDamage;
        if (_statusApplied)
        {
            return;
        }
        // 多个敌人时：累计伤害必须超过所有存活敌人的攻击意图之和才触发
        if (_turnDamageDealt > TotalEnemyIntent(Owner.Creature))
        {
            await PowerCmd.Apply<NamelessTotemPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, null, silent: false);
            _statusApplied = true;
        }
    }
}

/// <summary>无名图腾状态：受到的伤害（含格挡前）减半；敌方意图显示值同步减半。</summary>
public sealed class NamelessTotemPower : CustomPowerModel
{
    public override string? CustomPackedIconPath => "res://Amiya/images/relic/NamelessTotemRelic.png";
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;

    public override List<(string, string)>? Localization => new()
    {
        ("title", "无名图腾"),
        ("description", "本回合受到的伤害减半。")
    };

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        // 作用在格挡计算之前：格挡按减半后的伤害吸收
        if (target == Owner)
        {
            return 0.5m;
        }
        return 1m;
    }
}

/// <summary>大静谧：每击杀一个敌人，对其余所有敌人造成其最大生命一半的伤害。</summary>
[Pool(typeof(Amiya.Models.AmiyaRelicPool))]
public sealed class GreatSilenceRelic : CustomRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override string PackedIconPath => "res://Amiya/images/relic/GreatSilenceRelic.png";
    protected override string BigIconPath => "res://Amiya/images/relic/GreatSilenceRelic.png";

    public override List<(string, string)>? Localization => new()
    {
        ("title", "大静谧"),
        ("description", "每击杀一个敌人，对其余所有敌人造成其最大生命一半的伤害。"),
        ("flavor", "死亡是最安静的声音。")
    };

    public GreatSilenceRelic() : base(autoAdd: true)
    {
    }

    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        await base.AfterDeath(choiceContext, creature, wasRemovalPrevented, deathAnimLength);
        if (wasRemovalPrevented || creature.Side != CombatSide.Enemy || Owner?.Creature == null)
        {
            return;
        }
        var others = Owner.Creature.CombatState.Enemies.Where(e => e != creature && e.IsAlive).ToList();
        if (others.Count > 0)
        {
            // 固定值伤害：Unpowered 不吃力量/易伤等任何加成
            decimal damage = creature.MaxHp / 2m;
            await CreatureCmd.Damage(choiceContext, others, damage, ValueProp.Move | ValueProp.Unpowered, Owner.Creature, null, null);
            await CombatManager.Instance.CheckWinCondition();
        }
    }
}

/// <summary>不死的終結：受到不超过当前生命二倍的致死伤害时，最大生命值减半并复活。</summary>
[Pool(typeof(Amiya.Models.AmiyaRelicPool))]
public sealed class UndyingEndRelic : CustomRelicModel
{
    private bool _pendingRevive;
    private bool _capped;

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override string PackedIconPath => "res://Amiya/images/relic/UndyingEndRelic.png";
    protected override string BigIconPath => "res://Amiya/images/relic/UndyingEndRelic.png";

    public override List<(string, string)>? Localization => new()
    {
        ("title", "不死的終結"),
        ("description", "受到不超过当前生命二倍的致死伤害时，最大生命值减半并复活。"),
        ("flavor", "终结并非终点。")
    };

    public UndyingEndRelic() : base(autoAdd: true)
    {
    }

    public override Task BeforeDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        // 每次受击都重置标记，再按完整伤害（格挡前）判定是否武装复活
        _capped = false;
        _pendingRevive = target == Owner?.Creature && amount >= target.CurrentHp && amount <= target.CurrentHp * 2m;
        if (target == Owner?.Creature)
        {
            Log.Info($"[Amiya] undying full-hit: amount={amount} hp={target.CurrentHp} trigger={_pendingRevive}");
        }
        return Task.CompletedTask;
    }

    public override decimal ModifyHpLostBeforeOsty(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        // 兜底：武装中且本次落血量确实致死时，砍到剩 1 血（标记真正触发复活）
        if (target == Owner?.Creature && _pendingRevive && amount >= target.CurrentHp)
        {
            _capped = true;
            Log.Info("[Amiya] undying: cap lethal damage, leave 1 hp");
            return System.Math.Max(0m, target.CurrentHp - 1m);
        }
        return amount;
    }

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        await base.AfterDamageReceived(choiceContext, target, result, props, dealer, cardSource);
        bool revive = _capped && target == Owner?.Creature && target.IsAlive;
        _pendingRevive = false;
        _capped = false;
        if (!revive)
        {
            return;
        }
        // 最大生命减半后回满（LoseMaxHp 不能用负的 GainMaxHp，否则抛异常卡死战斗）
        await CreatureCmd.LoseMaxHp(choiceContext, target, target.MaxHp / 2m, isFromCard: false);
        await CreatureCmd.Heal(target, target.MaxHp);
        Log.Info($"[Amiya] undying end: revived with max hp {target.MaxHp}");
    }
}

/// <summary>诸王的冠冕：体力低于 20 时，造成的伤害翻倍（独立乘区，与其他伤害翻倍乘算）。</summary>
[Pool(typeof(Amiya.Models.AmiyaRelicPool))]
public sealed class CrownOfKingsRelic : CustomRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override string PackedIconPath => "res://Amiya/images/relic/CrownOfKingsRelic.png";
    protected override string BigIconPath => "res://Amiya/images/relic/CrownOfKingsRelic.png";

    public override List<(string, string)>? Localization => new()
    {
        ("title", "诸王的冠冕"),
        ("description", "体力低于 20 时，造成的伤害翻倍。"),
        ("flavor", "王冠之下，诸王皆兵。")
    };

    public CrownOfKingsRelic() : base(autoAdd: true)
    {
    }

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (dealer == Owner?.Creature && Owner.Creature.CurrentHp < 20)
        {
            return 2m;
        }
        return 1m;
    }
}

/// <summary>国王的铠甲：体力低于最大生命值的一半时，不再受到非攻击伤害。</summary>
[Pool(typeof(Amiya.Models.AmiyaRelicPool))]
public sealed class KingArmorRelic : CustomRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override string PackedIconPath => "res://Amiya/images/relic/KingArmorRelic.png";
    protected override string BigIconPath => "res://Amiya/images/relic/KingArmorRelic.png";

    public override List<(string, string)>? Localization => new()
    {
        ("title", "国王的铠甲"),
        ("description", "体力低于最大生命值的一半时，完全抵挡非攻击伤害（不损失生命，也不消耗格挡）。"),
        ("flavor", "王的躯体由钢铁守护。")
    };

    public KingArmorRelic() : base(autoAdd: true)
    {
    }

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        // 在格挡计算之前整体归零：不仅不掉血，格挡也不被消耗
        if (target == Owner?.Creature
            && target.CurrentHp < target.MaxHp / 2m
            && (dealer == null || dealer == target))
        {
            Log.Info($"[Amiya] king armor Multiplicative: hp={target.CurrentHp}/{target.MaxHp} dealer={(dealer == null ? "null" : "self")} card={cardSource?.Id.Entry} amount={amount} blocked");
            return 0m;
        }
        return 1m;
    }

    public override decimal ModifyHpLostBeforeOsty(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
        => Guard("Before", target, amount, props, dealer, cardSource);

    public override decimal ModifyHpLostBeforeOstyLate(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
        => Guard("BeforeLate", target, amount, props, dealer, cardSource);

    public override decimal ModifyHpLostAfterOsty(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
        => Guard("After", target, amount, props, dealer, cardSource);

    public override decimal ModifyHpLostAfterOstyLate(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
        => Guard("AfterLate", target, amount, props, dealer, cardSource);

    /// <summary>
    /// 非攻击伤害 = 无来源（dealer==null）或自损（dealer==自身，如凋萎/黑冠）的伤害。
    /// 四个阶段都拦截：其他 mod 若在某个阶段把伤害加回来，后置阶段会再次清零。
    /// </summary>
    private decimal Guard(string phase, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner?.Creature || amount <= 0)
        {
            return amount;
        }
        bool belowHalf = target.CurrentHp < target.MaxHp / 2m;
        bool nonAttack = dealer == null || dealer == target;
        Log.Info($"[Amiya] king armor {phase}: hp={target.CurrentHp}/{target.MaxHp} belowHalf={belowHalf} nonAttack={nonAttack} dealer={(dealer == null ? "null" : (dealer == target ? "self" : "enemy"))} card={cardSource?.Id.Entry} props={props} amount={amount} block={(nonAttack && belowHalf)}");
        return (nonAttack && belowHalf) ? 0m : amount;
    }
}

/// <summary>国王的延伸：回合开始时，若体力低于最大生命值的一半，额外抽一张牌。</summary>
[Pool(typeof(Amiya.Models.AmiyaRelicPool))]
public sealed class KingExtensionRelic : CustomRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override string PackedIconPath => "res://Amiya/images/relic/KingExtensionRelic.png";
    protected override string BigIconPath => "res://Amiya/images/relic/KingExtensionRelic.png";

    public override List<(string, string)>? Localization => new()
    {
        ("title", "国王的延伸"),
        ("description", "回合开始时，若体力低于最大生命值的一半，额外抽一张牌。"),
        ("flavor", "王的意志不断延伸。")
    };

    public KingExtensionRelic() : base(autoAdd: true)
    {
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        await base.AfterPlayerTurnStart(choiceContext, player);
        if (player == Owner && player.Creature.CurrentHp < player.Creature.MaxHp / 2m)
        {
            await CardPileCmd.Draw(choiceContext, 1m, player);
        }
    }
}

/// <summary>国王的新枪：回合开始时，若体力低于最大生命值的一半，获得一点能量。</summary>
[Pool(typeof(Amiya.Models.AmiyaRelicPool))]
public sealed class KingNewGunRelic : CustomRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override string PackedIconPath => "res://Amiya/images/relic/KingNewGunRelic.png";
    protected override string BigIconPath => "res://Amiya/images/relic/KingNewGunRelic.png";

    public override List<(string, string)>? Localization => new()
    {
        ("title", "国王的新枪"),
        ("description", "回合开始时，若体力低于最大生命值的一半，获得一点能量。"),
        ("flavor", "新枪上膛，王的怒火不息。")
    };

    public KingNewGunRelic() : base(autoAdd: true)
    {
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        await base.AfterPlayerTurnStart(choiceContext, player);
        if (player == Owner && player.Creature.CurrentHp < player.Creature.MaxHp / 2m)
        {
            await PlayerCmd.GainEnergy(1m, player);
        }
    }
}

/// <summary>兔兔玩偶：进入战斗时，每拥有 150 金币，获得一层缓冲。</summary>
[Pool(typeof(Amiya.Models.AmiyaRelicPool))]
public sealed class BunnyDollRelic : CustomRelicModel
{
    private bool _applied;

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override string PackedIconPath => "res://Amiya/images/relic/BunnyDollRelic.png";
    protected override string BigIconPath => "res://Amiya/images/relic/BunnyDollRelic.png";

    public override List<(string, string)>? Localization => new()
    {
        ("title", "兔兔玩偶"),
        ("description", "进入战斗时，每拥有 150 金币，获得一层缓冲。"),
        ("flavor", "毛茸茸的守护。")
    };

    public BunnyDollRelic() : base(autoAdd: true)
    {
    }

    public override Task BeforeCombatStart()
    {
        _applied = false;
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        await base.AfterPlayerTurnStart(choiceContext, player);
        if (_applied || player != Owner || Owner?.Creature == null)
        {
            return;
        }
        _applied = true;
        int stacks = Owner.Gold / 150;
        if (stacks > 0)
        {
            await PowerCmd.Apply<BufferPower>(choiceContext, Owner.Creature, stacks, Owner.Creature, null, silent: true);
        }
    }
}
