using System;
using BaseLib.Patches.Content;
using BaseLib.Patches.UI;
using Godot;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Abstracts;

public abstract class CustomCardPoolModel : CardPoolModel, ICustomModel, ICustomEnergyIconPool
{
	public override string CardFrameMaterialPath => "card_frame_red";

	public virtual Color ShaderColor => new Color("FFFFFF");

	public virtual float H
	{
		get
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			Color shaderColor = ShaderColor;
			return ((Color)(ref shaderColor)).H;
		}
	}

	public virtual float S
	{
		get
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			Color shaderColor = ShaderColor;
			return ((Color)(ref shaderColor)).S;
		}
	}

	public virtual float V
	{
		get
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			Color shaderColor = ShaderColor;
			return ((Color)(ref shaderColor)).V;
		}
	}

	public virtual bool IsShared => false;

	public override string EnergyColorName => CustomEnergyIconPatches.GetEnergyColorName(base.Id);

	public virtual string? BigEnergyIconPath => null;

	public virtual string? TextEnergyIconPath => null;

	public virtual bool SeenByDefault => false;

	public CustomCardPoolModel()
	{
		if (IsShared)
		{
			ModelDbSharedCardPoolsPatch.Register(this);
		}
	}

	public virtual Texture2D? CustomFrame(CustomCardModel card)
	{
		return null;
	}

	protected override CardModel[] GenerateAllCards()
	{
		return Array.Empty<CardModel>();
	}
}
