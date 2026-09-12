using BaseLib.Abstracts;
using Godot;

namespace Amiya.Models;

public sealed class AmiyaCardPool : CustomCardPoolModel
{
    public override string Title => "Amiya";

    public override Color DeckEntryCardColor => new Color(0.4f, 0.7f, 1f, 1f);

    public override bool IsColorless => false;

    // 占位：能量图标借用原版铁甲战士（M5 前占位策略）
    public override string? BigEnergyIconPath => "res://images/packed/sprite_fonts/ironclad_energy_icon.png";
    public override string? TextEnergyIconPath => "res://images/packed/sprite_fonts/ironclad_energy_icon.png";
}
