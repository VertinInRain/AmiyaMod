using System.Collections.Generic;
using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Abstracts;

public abstract class CustomRelicModel : RelicModel, ICustomModel, ILocalizationProvider
{
	public virtual List<(string, string)>? Localization => null;

	public CustomRelicModel(bool autoAdd = true)
	{
		if (autoAdd)
		{
			CustomContentDictionary.AddModel(GetType());
		}
	}

	public virtual RelicModel? GetUpgradeReplacement()
	{
		return null;
	}
}
