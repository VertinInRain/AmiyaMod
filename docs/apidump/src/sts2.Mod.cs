using System.Collections.Generic;
using System.Reflection;
using MegaCrit.Sts2.Core.Debug;
using MegaCrit.Sts2.Core.Localization;

namespace MegaCrit.Sts2.Core.Modding;

public class Mod
{
	public ModSource modSource;

	public required string path;

	public ModLoadState state;

	public ModManifest? manifest;

	public SemanticVersion? version;

	public List<Assembly> assemblies = new List<Assembly>();

	public List<LocString>? errors;

	public ulong? workshopId;
}
