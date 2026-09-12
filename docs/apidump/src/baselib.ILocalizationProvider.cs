using System.Collections.Generic;

namespace BaseLib.Abstracts;

public interface ILocalizationProvider
{
	string? LocTable => null;

	List<(string, string)>? Localization { get; }
}
