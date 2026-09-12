using System.Collections.Generic;
using Amiya.Cards;
using Amiya.Relics;
using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;

namespace Amiya.Models;

/// <summary>
/// 阿米娅角色：70 HP；近卫/术士/医疗 三形态 + 魔王系统。
/// 起始牌组 = 打击×4 + 防御×4 + 决意×1 + 思念×1；起始遗物 = 苍白赐福。
/// </summary>
public sealed class AmiyaCharacter : CustomCharacterModel
{
    public override Color NameColor => new Color(0.2f, 0.6f, 0.9f, 1f);

    public override CharacterGender Gender => CharacterGender.Feminine;

    public override int StartingHp => 70;

    public override List<(string, string)>? Localization => new()
    {
        ("title", "阿米娅"),
        ("titleObject", "阿米娅"),
        ("description", "罗德岛的公开领袖。在近卫、术士、医疗三种形态之间切换，积累魔王之唤以释放内心深处的另一种可能。"),
        ("possessiveAdjective", "她的"),
        ("pronounObject", "她"),
        ("pronounPossessive", "她的"),
        ("pronounSubject", "她")
    };

    // —— 占位资源：全部指向原版铁甲战士的现成资源（M5 前用占位图策略）——
    public override string? CustomVisualPath => "res://scenes/creature_visuals/ironclad.tscn";
    public override string? CustomTrailPath => "res://scenes/vfx/card_trail_ironclad.tscn";
    public override string? CustomIconTexturePath => "res://Amiya/images/icon/character_icon_amiya.png";
    public override string? CustomIconOutlineTexturePath => "res://Amiya/images/icon/character_icon_amiya.png";
    public override string? CustomIconPath => "res://Amiya/scenes/AmiyaIcon.tscn";
    public override string? CustomEnergyCounterPath => "res://scenes/combat/energy_counters/ironclad_energy_counter.tscn";
    public override string? CustomRestSiteAnimPath => "res://scenes/rest_site/characters/ironclad_rest_site.tscn";
    public override string? CustomMerchantAnimPath => "res://Amiya/scenes/AmiyaMerchant.tscn";
    public override string? CustomCharacterSelectBg => "res://Amiya/scenes/AmiyaSelectBg.tscn";
    public override string? CustomCharacterSelectIconPath => "res://Amiya/images/icon/char_select_amiya.png";
    public override string? CustomCharacterSelectLockedIconPath => "res://Amiya/images/icon/char_select_amiya_locked.png";
    public override string? CustomCharacterSelectTransitionPath => "res://materials/transitions/ironclad_transition_mat.tres";
    public override string? CustomMapMarkerPath => "res://images/packed/map/icons/map_marker_ironclad.png";
    public override string? CustomArmPointingTexturePath => "res://images/ui/hands/multiplayer_hand_ironclad_point.png";
    public override string? CustomArmRockTexturePath => "res://images/ui/hands/multiplayer_hand_ironclad_rock.png";
    public override string? CustomArmPaperTexturePath => "res://images/ui/hands/multiplayer_hand_ironclad_paper.png";
    public override string? CustomArmScissorsTexturePath => "res://images/ui/hands/multiplayer_hand_ironclad_scissors.png";
    public override string? CustomAttackSfx => "event:/sfx/characters/ironclad/ironclad_attack";
    public override string? CustomCastSfx => "event:/sfx/characters/ironclad/ironclad_cast";
    public override string? CustomDeathSfx => "event:/sfx/characters/ironclad/ironclad_die";
    // 能量计：不覆写，BaseLib 自动回落铁甲战士版（反编译确认）。

    public override CardPoolModel CardPool => ModelDb.CardPool<AmiyaCardPool>();

    public override RelicPoolModel RelicPool => ModelDb.RelicPool<AmiyaRelicPool>();

    public override PotionPoolModel PotionPool => ModelDb.PotionPool<AmiyaPotionPool>();

    /// <summary>
    /// 【正式牌组】打击×4 + 防御×4 + 决意×1 + 思念×1（作者 2026-09 定稿）。
    /// </summary>
    public override IEnumerable<CardModel> StartingDeck => new List<CardModel>
    {
        ModelDb.Card<AmiyaStrike>(),
        ModelDb.Card<AmiyaStrike>(),
        ModelDb.Card<AmiyaStrike>(),
        ModelDb.Card<AmiyaStrike>(),
        ModelDb.Card<AmiyaDefend>(),
        ModelDb.Card<AmiyaDefend>(),
        ModelDb.Card<AmiyaDefend>(),
        ModelDb.Card<AmiyaDefend>(),
        ModelDb.Card<AmiyaResolve>(),
        ModelDb.Card<AmiyaFormSwitch>()
    };

    /// <summary>【正式】起始遗物 = 苍白赐福。</summary>
    public override IReadOnlyList<RelicModel> StartingRelics => new List<RelicModel>
    {
        ModelDb.Relic<PaleBlessingRelic>()
    };

    public override List<string> GetArchitectAttackVfx()
    {
        return new List<string>
        {
            "event:/vfx/architect/attack/ironclad"
        };
    }
}
