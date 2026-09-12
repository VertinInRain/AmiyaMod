using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Extensions;
using BaseLib.Patches.Features;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace BaseLib.Utils;

public static class CommonActions
{
	[Obsolete("Use an overload that receives a CardPlay parameter. This is required on the beta branch.")]
	public static AttackCommand CardAttack(CardModel card, Creature? target, int hitCount = 1, string? vfx = null, string? sfx = null, string? tmpSfx = null)
	{
		if (card.DynamicVars.ContainsKey("CalculatedDamage"))
		{
			return CardAttack(card, target, card.DynamicVars.CalculatedDamage, card.DynamicVars.CalculatedDamage.Props, hitCount, vfx, sfx, tmpSfx);
		}
		if (card.DynamicVars.ContainsKey("Damage"))
		{
			return CardAttack(card, target, card.DynamicVars.Damage.BaseValue, card.DynamicVars.Damage.Props, hitCount, vfx, sfx, tmpSfx);
		}
		throw new Exception("Card " + card.Title + " does not have a damage variable supported by CommonActions.CardAttack");
	}

	public static AttackCommand CardAttack(CardModel card, CardPlay? play, int hitCount = 1, string? vfx = null, string? sfx = null, string? tmpSfx = null)
	{
		if (card.DynamicVars.ContainsKey("CalculatedDamage"))
		{
			return CardAttack(card, play, play?.Target, card.DynamicVars.CalculatedDamage, card.DynamicVars.CalculatedDamage.Props, hitCount, vfx, sfx, tmpSfx);
		}
		if (card.DynamicVars.ContainsKey("Damage"))
		{
			return CardAttack(card, play, play?.Target, card.DynamicVars.Damage.BaseValue, card.DynamicVars.Damage.Props, hitCount, vfx, sfx, tmpSfx);
		}
		throw new Exception("Card " + card.Title + " does not have a damage variable supported by CommonActions.CardAttack");
	}

	[Obsolete("Use the variant that has a CardPlay as the second parameter instead. This will be required for the beta branch.If no CardPlay is available, use null.")]
	public static AttackCommand CardAttack(CardModel card, Creature? target, decimal damage, int hitCount = 1, string? vfx = null, string? sfx = null, string? tmpSfx = null)
	{
		return CardAttack(card, target, damage, ValueProp.Move, hitCount, vfx, sfx, tmpSfx);
	}

	[Obsolete("Use the variant that has a CardPlay as the second parameter instead. This will be required for the beta branch.If no CardPlay is available, use null.")]
	public static AttackCommand CardAttack(CardModel card, Creature? target, decimal damage, ValueProp valueProp, int hitCount = 1, string? vfx = null, string? sfx = null, string? tmpSfx = null)
	{
		return CardAttack(card, null, target, damage, valueProp, hitCount, vfx, sfx, tmpSfx);
	}

	public static AttackCommand CardAttack(CardModel card, CardPlay? cardPlay, Creature? target, decimal damage, ValueProp valueProp, int hitCount = 1, string? vfx = null, string? sfx = null, string? tmpSfx = null)
	{
		AttackCommand attackCommand = DamageCmd.Attack(damage).WithHitCount(hitCount).WithValueProp(valueProp)
			.FromCardCompatibility(card, cardPlay);
		if (CustomTargetType.IsCustomSingleTargetType(card.TargetType))
		{
			if (target == null)
			{
				return attackCommand;
			}
			attackCommand.Targeting(target);
		}
		else if (CustomTargetType.IsCustomMultiTargetType(card.TargetType))
		{
			ICombatState combatState = card.CombatState;
			if (combatState == null)
			{
				return attackCommand;
			}
			IEnumerable<Creature> targets = combatState.Creatures.Where((Creature c) => CustomTargetType.CanMultiTarget(card.TargetType, c, card.Owner));
			attackCommand.TargetingFiltered(targets);
		}
		else
		{
			switch (card.TargetType)
			{
			case TargetType.AnyEnemy:
				if (target == null)
				{
					return attackCommand;
				}
				attackCommand.Targeting(target);
				break;
			case TargetType.AllEnemies:
			{
				ICombatState combatState3 = card.CombatState;
				if (combatState3 == null)
				{
					return attackCommand;
				}
				attackCommand.TargetingAllOpponents(combatState3);
				break;
			}
			case TargetType.RandomEnemy:
			{
				ICombatState combatState2 = card.CombatState;
				if (combatState2 == null)
				{
					return attackCommand;
				}
				attackCommand.TargetingRandomOpponents(combatState2);
				break;
			}
			default:
				throw new Exception($"Unsupported AttackCommand target type {card.TargetType} for card {card.Title}");
			}
		}
		if (vfx != null || sfx != null || tmpSfx != null)
		{
			attackCommand.WithHitFx(vfx, sfx, tmpSfx);
		}
		return attackCommand;
	}

	[Obsolete("Use the variant that has a CardPlay as the second parameter instead. This will be required for the beta branch.If no CardPlay is available, use null.")]
	public static AttackCommand CardAttack(CardModel card, Creature? target, CalculatedDamageVar calculatedDamage, int hitCount = 1, string? vfx = null, string? sfx = null, string? tmpSfx = null)
	{
		return CardAttack(card, target, calculatedDamage, ValueProp.Move, hitCount, vfx, sfx, tmpSfx);
	}

	public static AttackCommand CardAttack(CardModel card, CardPlay? cardPlay, Creature? target, CalculatedDamageVar calculatedDamage, int hitCount = 1, string? vfx = null, string? sfx = null, string? tmpSfx = null)
	{
		return CardAttack(card, cardPlay, target, calculatedDamage, ValueProp.Move, hitCount, vfx, sfx, tmpSfx);
	}

	[Obsolete("Use the variant that has a CardPlay as the second parameter instead. This will be required for the beta branch.If no CardPlay is available, use null.")]
	public static AttackCommand CardAttack(CardModel card, Creature? target, CalculatedDamageVar calculatedDamage, ValueProp valueProp, int hitCount = 1, string? vfx = null, string? sfx = null, string? tmpSfx = null)
	{
		return CardAttack(card, null, target, calculatedDamage, valueProp, hitCount, vfx, sfx, tmpSfx);
	}

	public static AttackCommand CardAttack(CardModel card, CardPlay? cardPlay, Creature? target, CalculatedDamageVar calculatedDamage, ValueProp valueProp, int hitCount = 1, string? vfx = null, string? sfx = null, string? tmpSfx = null)
	{
		AttackCommand attackCommand = DamageCmd.Attack(calculatedDamage).WithHitCount(hitCount).WithValueProp(valueProp)
			.FromCardCompatibility(card, cardPlay);
		if (CustomTargetType.IsCustomSingleTargetType(card.TargetType))
		{
			if (target == null)
			{
				return attackCommand;
			}
			attackCommand.Targeting(target);
		}
		else if (CustomTargetType.IsCustomMultiTargetType(card.TargetType))
		{
			ICombatState combatState = card.CombatState;
			if (combatState == null)
			{
				return attackCommand;
			}
			IEnumerable<Creature> targets = combatState.Creatures.Where((Creature c) => CustomTargetType.CanMultiTarget(card.TargetType, c, card.Owner));
			attackCommand.TargetingFiltered(targets);
		}
		else
		{
			switch (card.TargetType)
			{
			case TargetType.AnyEnemy:
				if (target == null)
				{
					return attackCommand;
				}
				attackCommand.Targeting(target);
				break;
			case TargetType.AllEnemies:
			{
				ICombatState combatState3 = card.CombatState;
				if (combatState3 == null)
				{
					return attackCommand;
				}
				attackCommand.TargetingAllOpponents(combatState3);
				break;
			}
			case TargetType.RandomEnemy:
			{
				ICombatState combatState2 = card.CombatState;
				if (combatState2 == null)
				{
					return attackCommand;
				}
				attackCommand.TargetingRandomOpponents(combatState2);
				break;
			}
			default:
				throw new Exception($"Unsupported AttackCommand target type {card.TargetType} for card {card.Title}");
			}
		}
		if (vfx != null || sfx != null || tmpSfx != null)
		{
			attackCommand.WithHitFx(vfx, sfx, tmpSfx);
		}
		return attackCommand;
	}

	public static async Task<decimal> CardBlock(CardModel card, CardPlay? play)
	{
		if (card.DynamicVars.TryGetValue("Block", out DynamicVar value))
		{
			return await CardBlock(card, value, play);
		}
		if (card.DynamicVars.TryGetValue("CalculatedBlock", out value))
		{
			return await CardBlock(card, value, play);
		}
		throw new InvalidOperationException($"No valid block var found in card {card.GetType()} for CommonActions.CardBlock; define a block var or " + "pass a variable in manually.");
	}

	public static async Task<decimal> CardBlock(CardModel card, BlockVar blockVar, CardPlay? play)
	{
		return await CreatureCmd.GainBlock(card.Owner.Creature, blockVar, play);
	}

	public static async Task<decimal> CardBlock(CardModel card, DynamicVar var, CardPlay? play, bool fast = false)
	{
		if (var is CalculatedBlockVar calculatedBlockVar)
		{
			return await CreatureCmd.GainBlock(card.Owner.Creature, calculatedBlockVar.Calculate(card.Owner.Creature), calculatedBlockVar.Props, play, fast);
		}
		return await CreatureCmd.GainBlock(card.Owner.Creature, var.BaseValue, (var as BlockVar)?.Props ?? ValueProp.Move, play, fast);
	}

	public static async Task<IEnumerable<CardModel>> Draw(CardModel card, PlayerChoiceContext context)
	{
		return await CardPileCmd.Draw(context, card.DynamicVars.Cards.BaseValue, card.Owner);
	}

	[Obsolete("Will be removed. Change to calling the overload that receives a PlayerChoiceContext if you are on the beta branch.")]
	public static async Task<T?> Apply<T>(Creature target, DynamicVarSource dynVarSource, bool silent = false) where T : PowerModel
	{
		return await BetaMainCompatibility.PowerCmd_.Apply.InvokeGeneric<Task<T>, T>(null, new object[6]
		{
			new ThrowingPlayerChoiceContext(),
			target,
			dynVarSource.DynamicVars.Power<T>().BaseValue,
			dynVarSource.Owner,
			dynVarSource.Card,
			silent
		});
	}

	[Obsolete("Will be removed. Change to calling the overload that receives a PlayerChoiceContext if you are on the beta branch.")]
	public static async Task<IReadOnlyList<T>> Apply<T>(IEnumerable<Creature> targets, DynamicVarSource dynVarSource, bool silent = false) where T : PowerModel
	{
		return await BetaMainCompatibility.PowerCmd_.ApplyMulti.InvokeGeneric<Task<IReadOnlyList<T>>, T>(null, new object[6]
		{
			new ThrowingPlayerChoiceContext(),
			targets,
			dynVarSource.DynamicVars.Power<T>().BaseValue,
			dynVarSource.Owner,
			dynVarSource.Card,
			silent
		});
	}

	[Obsolete("Will be removed. Change to calling the overload that receives a PlayerChoiceContext if you are on the beta branch.")]
	public static async Task<T?> Apply<T>(Creature target, CardModel card, bool silent = false) where T : PowerModel
	{
		return await BetaMainCompatibility.PowerCmd_.Apply.InvokeGeneric<Task<T>, T>(null, new object[6]
		{
			new ThrowingPlayerChoiceContext(),
			target,
			card.DynamicVars.Power<T>().BaseValue,
			card.Owner.Creature,
			card,
			silent
		});
	}

	[Obsolete("Will be removed. Change to calling the overload that receives a PlayerChoiceContext if you are on the beta branch.")]
	public static async Task<T?> Apply<T>(Creature target, CardModel? card, decimal amount, bool silent = false) where T : PowerModel
	{
		return await BetaMainCompatibility.PowerCmd_.Apply.InvokeGeneric<Task<T>, T>(null, new object[6]
		{
			new ThrowingPlayerChoiceContext(),
			target,
			amount,
			card?.Owner.Creature,
			card,
			silent
		});
	}

	[Obsolete("Will be removed. Change to calling the overload that receives a PlayerChoiceContext if you are on the beta branch.")]
	public static async Task<T?> ApplySelf<T>(CardModel card, bool silent = false) where T : PowerModel
	{
		return await ApplySelf<T>(new ThrowingPlayerChoiceContext(), card, card.DynamicVars.Power<T>().BaseValue, silent);
	}

	[Obsolete("Will be removed. Change to calling the overload that receives a PlayerChoiceContext if you are on the beta branch.")]
	public static async Task<T?> ApplySelf<T>(CardModel card, decimal amount, bool silent = false) where T : PowerModel
	{
		return await BetaMainCompatibility.PowerCmd_.Apply.InvokeGeneric<Task<T>, T>(null, new object[6]
		{
			new ThrowingPlayerChoiceContext(),
			card.Owner.Creature,
			amount,
			card.Owner.Creature,
			card,
			silent
		});
	}

	public static async Task<T?> Apply<T>(PlayerChoiceContext context, Creature target, DynamicVarSource dynVarSource, bool silent = false) where T : PowerModel
	{
		return await BetaMainCompatibility.PowerCmd_.Apply.InvokeGeneric<Task<T>, T>(null, new object[6]
		{
			context,
			target,
			dynVarSource.DynamicVars.Power<T>().BaseValue,
			dynVarSource.Owner,
			dynVarSource.Card,
			silent
		});
	}

	public static async Task<IReadOnlyList<T>> Apply<T>(PlayerChoiceContext context, IEnumerable<Creature> targets, DynamicVarSource dynVarSource, bool silent = false) where T : PowerModel
	{
		return await BetaMainCompatibility.PowerCmd_.ApplyMulti.InvokeGeneric<Task<IReadOnlyList<T>>, T>(null, new object[6]
		{
			context,
			targets,
			dynVarSource.DynamicVars.Power<T>().BaseValue,
			dynVarSource.Owner,
			dynVarSource.Card,
			silent
		});
	}

	public static async Task<T?> Apply<T>(PlayerChoiceContext context, Creature target, CardModel card, bool silent = false) where T : PowerModel
	{
		return await BetaMainCompatibility.PowerCmd_.Apply.InvokeGeneric<Task<T>, T>(null, new object[6]
		{
			context,
			target,
			card.DynamicVars.Power<T>().BaseValue,
			card.Owner.Creature,
			card,
			silent
		});
	}

	public static async Task<T?> Apply<T>(PlayerChoiceContext context, Creature target, CardModel? card, decimal amount, bool silent = false) where T : PowerModel
	{
		return await BetaMainCompatibility.PowerCmd_.Apply.InvokeGeneric<Task<T>, T>(null, new object[6]
		{
			context,
			target,
			amount,
			card?.Owner.Creature,
			card,
			silent
		});
	}

	public static async Task<T?> ApplySelf<T>(PlayerChoiceContext context, CardModel card, bool silent = false) where T : PowerModel
	{
		return await ApplySelf<T>(context, card, card.DynamicVars.Power<T>().BaseValue, silent);
	}

	public static async Task<T?> ApplySelf<T>(PlayerChoiceContext context, CardModel card, decimal amount, bool silent = false) where T : PowerModel
	{
		return await BetaMainCompatibility.PowerCmd_.Apply.InvokeGeneric<Task<T>, T>(null, new object[6]
		{
			context,
			card.Owner.Creature,
			amount,
			card.Owner.Creature,
			card,
			silent
		});
	}

	public static async Task<IEnumerable<CardModel>> SelectCards(CardModel card, CardSelectorPrefs prefs, PlayerChoiceContext context, PileType pileType)
	{
		return await SelectCards(card, prefs, context, pileType, null);
	}

	public static async Task<IEnumerable<CardModel>> SelectCards(CardModel card, CardSelectorPrefs prefs, PlayerChoiceContext context, PileType pileType, Func<CardModel, bool>? filter)
	{
		CardPile pile = pileType.GetPile(card.Owner);
		IReadOnlyList<CardModel> readOnlyList = pile.Cards;
		if (pile.Type == PileType.Draw)
		{
			readOnlyList = (from c in readOnlyList
				orderby c.Rarity, c.Id
				select c).ToList();
		}
		if (pile.Type == PileType.Hand)
		{
			return await CardSelectCmd.FromHand(context, card.Owner, prefs, filter, card);
		}
		if (filter != null)
		{
			readOnlyList = readOnlyList.Where(filter).ToList();
		}
		return await CardSelectCmd.FromSimpleGrid(context, readOnlyList, card.Owner, prefs);
	}

	public static async Task<IEnumerable<CardModel>> SelectCards(CardModel card, LocString selectionPrompt, PlayerChoiceContext context, PileType pileType, int count = 1)
	{
		return await SelectCards(card, selectionPrompt, context, pileType, null, count);
	}

	public static async Task<IEnumerable<CardModel>> SelectCards(CardModel card, LocString selectionPrompt, PlayerChoiceContext context, PileType pileType, Func<CardModel, bool>? filter, int count = 1)
	{
		CardSelectorPrefs prefs = new CardSelectorPrefs(selectionPrompt, count);
		return await SelectCards(card, prefs, context, pileType, filter);
	}

	public static async Task<IEnumerable<CardModel>> SelectCards(CardModel card, LocString selectionPrompt, PlayerChoiceContext context, PileType pileType, int minCount, int maxCount)
	{
		return await SelectCards(card, selectionPrompt, context, pileType, null, minCount, maxCount);
	}

	public static async Task<IEnumerable<CardModel>> SelectCards(CardModel card, LocString selectionPrompt, PlayerChoiceContext context, PileType pileType, Func<CardModel, bool>? filter, int minCount, int maxCount)
	{
		CardSelectorPrefs prefs = new CardSelectorPrefs(selectionPrompt, minCount, maxCount);
		return await SelectCards(card, prefs, context, pileType, filter);
	}

	public static async Task<CardModel?> SelectSingleCard(CardModel card, LocString selectionPrompt, PlayerChoiceContext context, PileType pileType)
	{
		return await SelectSingleCard(card, selectionPrompt, context, pileType, null);
	}

	public static async Task<CardModel?> SelectSingleCard(CardModel card, LocString selectionPrompt, PlayerChoiceContext context, PileType pileType, Func<CardModel, bool>? filter)
	{
		CardSelectorPrefs prefs = new CardSelectorPrefs(selectionPrompt, 1);
		return (await SelectCards(card, prefs, context, pileType, filter)).FirstOrDefault();
	}

	public static async Task<IReadOnlyList<T>> Apply<T>(PlayerChoiceContext ctx, CardModel card, CardPlay? cardPlay, bool silent = false) where T : PowerModel
	{
		if (cardPlay?.Target != null)
		{
			T val = await Apply<T>(ctx, cardPlay.Target, card, silent);
			IReadOnlyList<T> result;
			if (val == null)
			{
				IReadOnlyList<T> readOnlyList = Array.Empty<T>();
				result = readOnlyList;
			}
			else
			{
				IReadOnlyList<T> readOnlyList = new global::<>z__ReadOnlySingleElementList<T>(val);
				result = readOnlyList;
			}
			return result;
		}
		return await ApplyToCreatures<T>(card, ctx, card.GetTargets(), silent);
	}

	private static async Task<IReadOnlyList<T>> ApplyToCreatures<T>(CardModel card, PlayerChoiceContext ctx, params Creature[] targets) where T : PowerModel
	{
		return await Apply<T>(ctx, targets, card);
	}

	private static async Task<IReadOnlyList<T>> ApplyToCreatures<T>(CardModel card, PlayerChoiceContext ctx, IEnumerable<Creature> targets, bool silent = false) where T : PowerModel
	{
		return await Apply<T>(ctx, targets, card, silent);
	}

	public static IEnumerable<CardModel> GenerateCards(CardModel card, int count, Func<CardModel, bool>? filter = null)
	{
		Player owner = card.Owner;
		IEnumerable<CardModel> enumerable = owner.Character.CardPool.GetUnlockedCards(owner.UnlockState, owner.RunState.CardMultiplayerConstraint);
		if (filter != null)
		{
			enumerable = enumerable.Where(filter).ToList();
		}
		return CardFactory.GetDistinctForCombat(owner, enumerable, count, owner.RunState.Rng.CombatCardGeneration);
	}

	public static CardModel? GenerateSingleCard(CardModel card, Func<CardModel, bool>? filter = null)
	{
		return GenerateCards(card, 1, filter).FirstOrDefault();
	}
}
