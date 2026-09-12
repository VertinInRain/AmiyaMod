using System;
using System.Collections.Generic;
using BaseLib.Patches.Content;
using BaseLib.Patches.UI;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Abstracts;

public abstract class CustomRelicPoolModel : RelicPoolModel, ICustomModel, ICustomEnergyIconPool
{
	public virtual bool IsShared => false;

	public override string EnergyColorName => CustomEnergyIconPatches.GetEnergyColorName(base.Id);

	public virtual string? BigEnergyIconPath => null;

	public virtual string? TextEnergyIconPath => null;

	public virtual bool SeenByDefault => false;

	public CustomRelicPoolModel()
	{
		if (IsShared)
		{
			ModelDbSharedRelicPoolsPatch.Register(this);
		}
	}

	protected override IEnumerable<RelicModel> GenerateAllRelics()
	{
		return Array.Empty<RelicModel>();
	}
}
