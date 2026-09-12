using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Patches.UI;
using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;

namespace BaseLib.Patches.Content;

[HarmonyPatch(typeof(ModelDb), "InitIds")]
public static class CustomContentDictionary
{
	public static readonly HashSet<Type> RegisteredTypes;

	private static readonly Dictionary<Type, Type> PoolTypes;

	public static readonly List<CustomCharacterModel> CustomCharacters;

	public static readonly List<CustomEncounterModel> CustomEncounters;

	public static readonly List<CustomAncientModel> CustomAncients;

	public static readonly List<Type> CustomBadgeTypes;

	public static readonly List<CustomEventModel> ActCustomEvents;

	public static readonly List<CustomEventModel> SharedCustomEvents;

	public static readonly List<CustomActModel> CustomActs;

	static CustomContentDictionary()
	{
		RegisteredTypes = new HashSet<Type>();
		PoolTypes = new Dictionary<Type, Type>();
		CustomCharacters = new List<CustomCharacterModel>();
		CustomEncounters = new List<CustomEncounterModel>();
		CustomAncients = new List<CustomAncientModel>();
		CustomBadgeTypes = new List<Type>();
		ActCustomEvents = new List<CustomEventModel>();
		SharedCustomEvents = new List<CustomEventModel>();
		CustomActs = new List<CustomActModel>();
		PoolTypes.Add(typeof(CardPoolModel), typeof(CardModel));
		PoolTypes.Add(typeof(RelicPoolModel), typeof(RelicModel));
		PoolTypes.Add(typeof(PotionPoolModel), typeof(PotionModel));
	}

	public static bool RegisterType(Type t)
	{
		return RegisteredTypes.Add(t);
	}

	public static void AddModel(Type modelType)
	{
		if (RegisterType(modelType))
		{
			PoolAttribute poolAttribute = modelType.GetCustomAttribute<PoolAttribute>() ?? throw new Exception("Model " + modelType.FullName + " must be marked with a PoolAttribute to determine which pool to add it to.");
			if (!IsValidPool(modelType, poolAttribute.PoolType))
			{
				throw new Exception($"Model {modelType.FullName} is assigned to incorrect type of pool {poolAttribute.PoolType.FullName}.");
			}
			ModHelper.AddModelToPool(poolAttribute.PoolType, modelType);
		}
	}

	public static void AddEncounter(CustomEncounterModel encounter)
	{
		if (RegisterType(encounter.GetType()))
		{
			CustomEncounters.InsertSorted(encounter);
		}
	}

	public static void AddAncient(CustomAncientModel ancient)
	{
		if (RegisterType(ancient.GetType()))
		{
			CustomAncients.InsertSorted(ancient);
		}
	}

	public static void AddEvent(CustomEventModel eventModel)
	{
		if (RegisterType(eventModel.GetType()))
		{
			if (eventModel.Acts.Length == 0)
			{
				SharedCustomEvents.InsertSorted(eventModel);
			}
			else
			{
				ActCustomEvents.InsertSorted(eventModel);
			}
		}
	}

	public static bool AddBadge(Type badgeType)
	{
		if (!RegisterType(badgeType))
		{
			return false;
		}
		CustomBadgeTypes.Add(badgeType);
		return true;
	}

	public static void AddAct(CustomActModel actModel)
	{
		if (RegisterType(actModel.GetType()))
		{
			CustomActs.InsertSorted(actModel);
		}
	}

	public static void AddCharacter(CustomCharacterModel character)
	{
		if (!RegisterType(character.GetType()))
		{
			return;
		}
		CustomCharacters.InsertSorted(character);
		RelicIconData customYummyCookie = character.CustomYummyCookie;
		if (customYummyCookie != null)
		{
			RelicImageOverridePatch.AddOverride<YummyCookie>(customYummyCookie, (RelicModel relic) => relic.IsMutable && character.Id.Equals(relic.Owner?.Character.Id));
		}
	}

	private static bool IsValidPool(Type modelType, Type poolType)
	{
		Type baseType = poolType.BaseType;
		while (baseType != null)
		{
			if (PoolTypes.TryGetValue(baseType, out Type value))
			{
				return modelType.IsAssignableTo(value);
			}
			baseType = baseType.BaseType;
		}
		throw new Exception($"Model {modelType.FullName} is assigned to {poolType.FullName} which is not a valid pool type.");
	}

	[HarmonyPostfix]
	private static void ScanCustomBadges()
	{
		foreach (Type item in from t in ReflectionHelper.GetSubtypesInMods<CustomBadge>()
			where !t.IsAbstract
			select t)
		{
			AddBadge(item);
		}
	}
}
