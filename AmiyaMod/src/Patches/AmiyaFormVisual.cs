using System;
using System.Collections.Generic;
using Amiya.Powers;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Amiya.Patches;

/// <summary>
/// 形态切换视觉（PNG 贴图版）：四个形态各一张透明背景 PNG（1000×1000），
/// 加载后按内容包围盒（GetUsedRect）居中并缩放到统一身高，脚底对齐地面线。
/// 低频轮询（0.15s）：一次动作内连续多次形态转换自然合并，转换完成后才切换。
///
/// 多人注意：贴图必须跟着「本节点所属生物」的形态走。
/// 之前用的是静态 FormManagerPower.Local（本地玩家的形态机），
/// 于是两名阿米娅玩家的贴图会一起跟着本地玩家变化，哪怕两人形态并不相同。
/// </summary>
public partial class AmiyaFormVisual : Node
{
    private static readonly string Dir = AmiyaPaths.SpineDir + "/";

    private readonly Dictionary<AmiyaForm, Node2D> _sprites = new();
    private AmiyaForm _shown = (AmiyaForm)(-1);
    private double _accum;

    /// <summary>本节点所属生物（缓存；节点随生物一起销毁，不需要失效检查）。</summary>
    private Creature? _creature;

    public override void _Process(double delta)
    {
        _accum += delta;
        if (_accum < 0.15)
        {
            return;
        }
        _accum = 0;
        try
        {
            // 按自己所属生物的形态机取形态（不是本地玩家的）
            var form = FormManagerPower.Of(ResolveCreature())?.Form ?? AmiyaForm.Guard;
            if (form == _shown)
            {
                return;
            }
            _shown = form;
            foreach (var kv in _sprites)
            {
                kv.Value.Visible = kv.Key == form;
            }
            Log.Info($"[Amiya] form visual: show {form}");
        }
        catch (Exception ex)
        {
            Log.Error($"[Amiya] form visual update failed: {ex}");
        }
    }

    /// <summary>沿节点树往上找到所属的 NCreature，取出它对应的生物。</summary>
    private Creature? ResolveCreature()
    {
        if (_creature != null)
        {
            return _creature;
        }
        Node? node = this;
        while (node != null)
        {
            if (node is NCreature creatureNode)
            {
                _creature = creatureNode.Entity;
                break;
            }
            node = node.GetParent();
        }
        return _creature;
    }

    /// <summary>挂到 visuals 节点上（本节点仅作轮询器）；四张贴图为 siblings。</summary>
    public static void Attach(Node visuals)
    {
        try
        {
            var parent = visuals.GetParent();
            if (parent == null)
            {
                Log.Error("[Amiya] form visual: visuals has no parent");
                return;
            }
            // 隐藏铁甲战士 SpineSprite（动画控制器已失效，纯静态贴图即可）
            visuals.Set("visible", false);

            var self = new AmiyaFormVisual();
            visuals.AddChild(self);

            // 统一规格：所有形态同一目标高度、同一地面线（内容底边对齐），切换不违和
            const float height = 344f;
            const float groundY = 5f;
            float cy = groundY - height / 2f; // 内容中心 Y = 地面 - 高度/2

            self.CreateSprite(parent, AmiyaForm.Medic, "form_medic.png", height, -43f, cy);
            self.CreateSprite(parent, AmiyaForm.Guard, "form_guard.png", height, -23f, cy);
            self.CreateSprite(parent, AmiyaForm.Caster, "form_caster.png", height, -43f, cy);
            self.CreateSprite(parent, AmiyaForm.DemonLord, "form_demon.png", height, -43f, cy);
            Log.Info("[Amiya] form visual: png sprites attached");
        }
        catch (Exception ex)
        {
            Log.Error($"[Amiya] form visual attach failed: {ex}");
        }
    }

    private void CreateSprite(Node parent, AmiyaForm form, string pngName, float targetHeight, float cx, float cy)
    {
        try
        {
            var img = Image.LoadFromFile(Dir + pngName);
            var used = img.GetUsedRect();
            var tex = ImageTexture.CreateFromImage(img);
            var sprite = new Sprite2D { Texture = tex };
            // 内容中心对齐：Offset 使贴图内容中心落在 sprite 的 Position 上
            float centerX = used.Position.X + used.Size.X / 2f;
            float centerY = used.Position.Y + used.Size.Y / 2f;
            sprite.Offset = new Vector2(img.GetWidth() / 2f - centerX, img.GetHeight() / 2f - centerY);
            float s = used.Size.Y > 0 ? targetHeight / used.Size.Y : 1f;
            sprite.Scale = new Vector2(s, s);
            sprite.Position = new Vector2(cx, cy);
            sprite.Visible = false;
            parent.Call("add_child", sprite);
            _sprites[form] = sprite;
            Log.Info($"[Amiya] form png {pngName}: used={used.Position} {used.Size} scale={s:0.###}");
        }
        catch (Exception ex)
        {
            Log.Error($"[Amiya] form png load failed ({pngName}): {ex}");
        }
    }
}
