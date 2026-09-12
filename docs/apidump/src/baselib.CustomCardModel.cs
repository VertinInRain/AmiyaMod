using System;
using System.Collections.Generic;
using System.Linq;
using BaseLib.Cards.Variables;
using BaseLib.Patches.Content;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace BaseLib.Abstracts;

public abstract class CustomCardModel : CardModel, ICustomModel, ILocalizationProvider
{
	private bool _initializedFrameMaterial;

	private Material? _frameMaterial;

	private bool _initializedBannerMaterial;

	private Material? _bannerMaterial;

	public override bool GainsBlock => base.DynamicVars.Any<KeyValuePair<string, DynamicVar>>(delegate(KeyValuePair<string, DynamicVar> dynVar)
	{
		DynamicVar value = dynVar.Value;
		return (value is BlockVar || value is CalculatedBlockVar) ? true : false;
	});

	public virtual Texture2D? CustomFrame => null;

	public Material? CustomFrameMaterial
	{
		get
		{
			if (!_initializedFrameMaterial)
			{
				_frameMaterial = CreateCustomFrameMaterial;
				_initializedFrameMaterial = true;
			}
			return _frameMaterial;
		}
	}

	public Material? CustomBannerMaterial
	{
		get
		{
			if (!_initializedBannerMaterial)
			{
				_bannerMaterial = CreateCustomBannerMaterial;
				_initializedBannerMaterial = true;
			}
			return _bannerMaterial;
		}
	}

	public virtual Material? CreateCustomFrameMaterial => null;

	public virtual Material? CreateCustomBannerMaterial => null;

	public virtual string? CustomBannerMaterialPath => null;

	public virtual string? CustomPortraitPath => null;

	public virtual Texture2D? CustomPortrait => null;

	public virtual List<(string, string)>? Localization => null;

	public CustomCardModel(int baseCost, CardType type, CardRarity rarity, TargetType target, bool showInCardLibrary = true, bool autoAdd = true)
		: base(baseCost, type, rarity, target, showInCardLibrary)
	{
		if (autoAdd)
		{
			CustomContentDictionary.AddModel(GetType());
		}
	}

	public static IEnumerable<DynamicVar> FinishMakeCalculatedVar(CalculatedVar var, int baseVal, int bonusVal)
	{
		if (!(var is CustomCalculatedVar) && !(var is CustomCalculatedBlockVar))
		{
			if (!(var is CustomCalculatedDamageVar))
			{
				if (var is CalculatedDamageVar)
				{
					yield return new CalculationBaseVar(baseVal);
					yield return new ExtraDamageVar(bonusVal);
				}
				else
				{
					yield return new CalculationBaseVar(baseVal);
					yield return new CalculationExtraVar(bonusVal);
				}
			}
			else
			{
				yield return new DynamicVar(var.Name + "Base", baseVal);
				yield return new CustomExtraDamageVar(var.Name, bonusVal);
			}
		}
		else
		{
			yield return new DynamicVar(var.Name + "Base", baseVal);
			yield return new DynamicVar(var.Name + "Extra", bonusVal);
		}
		yield return var;
	}

	public static IEnumerable<DynamicVar> MakeCalculatedVar(string name, int baseVal, Func<CardModel, Creature?, decimal> bonus, int mult = 1)
	{
		return FinishMakeCalculatedVar(new CustomCalculatedVar(name).WithMultiplier(bonus), baseVal, mult);
	}

	public static IEnumerable<DynamicVar> MakeCalculatedDamage(int baseVal, Func<CardModel, Creature?, decimal> bonus, int mult = 1, ValueProp props = ValueProp.Move)
	{
		return FinishMakeCalculatedVar(new CalculatedDamageVar(props).WithMultiplier(bonus), baseVal, mult);
	}

	public static IEnumerable<DynamicVar> MakeCalculatedDamage(string name, int baseVal, Func<CardModel, Creature?, decimal> bonus, int mult = 1, ValueProp props = ValueProp.Move)
	{
		return FinishMakeCalculatedVar(new CustomCalculatedDamageVar(name, props).WithMultiplier(bonus), baseVal, mult);
	}

	public static IEnumerable<DynamicVar> MakeCalculatedBlock(int baseVal, Func<CardModel, Creature?, decimal> bonus, int mult = 1, ValueProp props = ValueProp.Move)
	{
		return FinishMakeCalculatedVar(new CalculatedBlockVar(props).WithMultiplier(bonus), baseVal, mult);
	}

	public static IEnumerable<DynamicVar> MakeCalculatedBlock(string name, int baseVal, Func<CardModel, Creature?, decimal> bonus, int mult = 1, ValueProp props = ValueProp.Move)
	{
		return FinishMakeCalculatedVar(new CustomCalculatedBlockVar(name, props).WithMultiplier(bonus), baseVal, mult);
	}
}
