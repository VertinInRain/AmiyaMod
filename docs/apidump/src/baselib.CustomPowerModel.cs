using System;
using System.Collections.Generic;
using BaseLib.Hooks;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Abstracts;

public abstract class CustomPowerModel : PowerModel, ICustomPower, ICustomModel, ILocalizationProvider, IHealthBarForecastSource
{
	public virtual string? CustomPackedIconPath => null;

	public virtual string? CustomBigIconPath => null;

	public virtual string? CustomBigBetaIconPath => null;

	public virtual List<(string, string)>? Localization => null;

	public virtual IEnumerable<HealthBarForecastSegment> GetHealthBarForecastSegments(HealthBarForecastContext context)
	{
		return Array.Empty<HealthBarForecastSegment>();
	}
}
