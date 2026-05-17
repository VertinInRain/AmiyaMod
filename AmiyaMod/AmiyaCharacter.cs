using System.Collections.Generic;
using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using Amiya.Cards;
using Amiya.Relics;

namespace Amiya;

/// <summary>
/// 阿米娅角色类
/// </summary>
public partial class AmiyaCharacter : PlaceholderCharacterModel
{
    public override Color NameColor => new Color(0.2f, 0.6f, 0.9f, 1f);
    
    public override CharacterGender Gender => CharacterGender.Feminine;
    
    public override int StartingHp => 70;
    
    public override string? CustomIconTexturePath => "res://Amiya/images/icon/character_icon.png";
    
    public override string? CustomVisualPath => "res://Amiya/scenes/character_visual.tscn";

    public override string? CustomIconPath => "res://Amiya/scenes/AmiyaIcon.tscn";

    public override string? CustomCharacterSelectIconPath => "res://Amiya/images/icon/char_select_amiya.png";

    public override string? CustomCharacterSelectLockedIconPath => "res://Amiya/images/icon/char_select_amiya_locked.png";

    public override string? CustomCharacterSelectBg => "res://Amiya/scenes/AmiyaSelectBg.tscn";
    
    public override CardPoolModel CardPool => ModelDb.CardPool<AmiyaCardPool>();
    
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<AmiyaRelicPool>();
    
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<AmiyaPotionPool>();
    
    public override IEnumerable<CardModel> StartingDeck => new List<CardModel>
    {
        ModelDb.Card<Strike>(),
        ModelDb.Card<Strike>(),
        ModelDb.Card<Strike>(),
        ModelDb.Card<Strike>(),
        ModelDb.Card<Defend>(),
        ModelDb.Card<Defend>(),
        ModelDb.Card<Defend>(),
        ModelDb.Card<Defend>(),
        ModelDb.Card<Resolve>(),
        ModelDb.Card<FormSwitch>()
    };
    
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
    
    public AmiyaCharacter()
    {
    }
}
