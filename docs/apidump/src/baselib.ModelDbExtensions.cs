using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Extensions;

public static class ModelDbExtensions
{
	extension(ModelDb)
	{
		public static T CardModifier<T>() where T : CardModifier
		{
			return CardModifier<T>(mutableClone: false);
		}

		public static T CardModifier<T>(bool mutableClone = true) where T : CardModifier
		{
			T val = ModelDb.Get<T>();
			if (mutableClone)
			{
				return (T)val.MutableClone();
			}
			return val;
		}
	}
}
