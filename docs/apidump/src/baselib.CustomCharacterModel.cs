using System;
using System.Collections.Generic;
using BaseLib.Extensions;
using BaseLib.Patches.Content;
using BaseLib.Patches.UI;
using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace BaseLib.Abstracts;

public abstract class CustomCharacterModel : CharacterModel, ICustomModel, ILocalizationProvider, ISceneConversions
{
	public virtual List<(string, string)>? Localization => null;

	public virtual bool HideFromVanillaCharacterSelect => false;

	public virtual bool AllowInVanillaRandomCharacterSelect => !HideFromVanillaCharacterSelect;

	public virtual bool HideInCompendium => false;

	public virtual ModelId DefaultCompendiumOpenModelId => base.Id;

	public virtual string? CustomVisualPath => null;

	public virtual string? CustomTrailPath => null;

	public virtual string? CustomIconTexturePath => null;

	public virtual string? CustomIconOutlineTexturePath => null;

	public virtual string? CustomIconPath => null;

	public virtual Control? CustomIcon => null;

	public virtual CustomEnergyCounter? CustomEnergyCounter => null;

	public virtual string? CustomEnergyCounterPath => null;

	public virtual string? CustomRestSiteAnimPath => null;

	public virtual string? CustomMerchantAnimPath => null;

	public virtual string? CustomArmPointingTexturePath => null;

	public virtual string? CustomArmRockTexturePath => null;

	public virtual string? CustomArmPaperTexturePath => null;

	public virtual string? CustomArmScissorsTexturePath => null;

	public virtual RelicIconData? CustomYummyCookie => null;

	public virtual string? CustomCharacterSelectBg => null;

	public virtual string? CustomCharacterSelectIconPath => null;

	public virtual string? CustomCharacterSelectLockedIconPath => null;

	public virtual string? CustomCharacterSelectTransitionPath => null;

	public virtual string? CustomMapMarkerPath => null;

	public virtual string? CustomAttackSfx => null;

	public virtual string? CustomCastSfx => null;

	public virtual string? CustomDeathSfx => null;

	public override int StartingGold => 99;

	public override float AttackAnimDelay => 0.15f;

	public override float CastAnimDelay => 0.25f;

	protected override CharacterModel? UnlocksAfterRunAs => null;

	public virtual float DeathAnimTime => 1.5f;

	public CustomCharacterModel()
	{
		CustomContentDictionary.AddCharacter(this);
	}

	public virtual NCreatureVisuals? CreateCustomVisuals()
	{
		return null;
	}

	public virtual CreatureAnimator? SetupCustomAnimationStates(MegaSprite controller)
	{
		return null;
	}

	public static CreatureAnimator SetupAnimationState(MegaSprite controller, string idleName, string? deadName = null, bool deadLoop = false, string? hitName = null, bool hitLoop = false, string? attackName = null, bool attackLoop = false, string? castName = null, bool castLoop = false, string? relaxedName = null, bool relaxedLoop = true)
	{
		AnimState animState = new AnimState(idleName, isLooping: true);
		AnimState state = ((deadName == null) ? animState : new AnimState(deadName, deadLoop));
		AnimState state2 = ((hitName == null) ? animState : new AnimState(hitName, hitLoop)
		{
			NextState = animState
		});
		AnimState state3 = ((attackName == null) ? animState : new AnimState(attackName, attackLoop)
		{
			NextState = animState
		});
		AnimState state4 = ((castName == null) ? animState : new AnimState(castName, castLoop)
		{
			NextState = animState
		});
		AnimState animState2;
		if (relaxedName == null)
		{
			animState2 = animState;
		}
		else
		{
			animState2 = new AnimState(relaxedName, relaxedLoop);
			animState2.AddBranch("Idle", animState);
		}
		CreatureAnimator creatureAnimator = new CreatureAnimator(animState, controller);
		creatureAnimator.AddAnyState("Idle", animState);
		creatureAnimator.AddAnyState("Dead", state);
		creatureAnimator.AddAnyState("Hit", state2);
		creatureAnimator.AddAnyState("Attack", state3);
		creatureAnimator.AddAnyState("Cast", state4);
		creatureAnimator.AddAnyState("Relaxed", animState2);
		return creatureAnimator;
	}

	public void RegisterSceneConversions()
	{
		CustomVisualPath?.RegisterSceneForConversion<NCreatureVisuals>((Action<NCreatureVisuals>?)null);
		CustomRestSiteAnimPath?.RegisterSceneForConversion<NRestSiteCharacter>((Action<NRestSiteCharacter>?)null);
		CustomMerchantAnimPath?.RegisterSceneForConversion<NMerchantCharacter>((Action<NMerchantCharacter>?)null);
		CustomEnergyCounterPath?.RegisterSceneForConversion<NEnergyCounter>((Action<NEnergyCounter>?)null);
	}
}
