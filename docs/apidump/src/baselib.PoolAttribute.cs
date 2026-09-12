using System;
using HarmonyLib;

namespace BaseLib.Utils;

[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class PoolAttribute : Attribute
{
	private Type? _poolType;

	private string? _poolClassName;

	public Type? PoolType => _poolType ?? (_poolType = TryGetPoolType());

	public PoolAttribute(Type poolType)
	{
		_poolType = poolType;
	}

	public PoolAttribute(string poolClassName)
	{
		_poolClassName = poolClassName;
	}

	private Type? TryGetPoolType()
	{
		if (_poolClassName == null)
		{
			return null;
		}
		Type type = AccessTools.TypeByName(_poolClassName);
		if (type == null)
		{
			BaseLibMain.Logger.Debug("Failed to find pool type " + _poolClassName + " by name.");
		}
		return type;
	}
}
