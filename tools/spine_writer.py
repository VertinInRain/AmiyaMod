#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""spine-asset SkeletonData -> 标准 Spine 3.8 JSON 骨架写出器。

与 spine_asset.v38.SkeletonJson 阅读器严格互逆（字段名/结构一一对应）。
颜色 = "RRGGBBAA" 8位十六进制；曲线 = {"curve":"stepped"} 或 {"curve":cx1,"c2":cy1,"c3":cx2,"c4":cy2}。
"""
import json
import math

from spine_asset.v38 import SkeletonData
from spine_asset.v38.Enums import (
    TransformMode, BlendMode, PositionMode, SpacingMode, RotateMode, AttachmentType,
)
from spine_asset.v38.attachments import (
    RegionAttachment, MeshAttachment, BoundingBoxAttachment, PathAttachment,
    PointAttachment, ClippingAttachment, VertexAttachment,
)
from spine_asset.v38.timelines import (
    AttachmentTimeline, ColorTimeline, TwoColorTimeline, RotateTimeline,
    TranslateTimeline, ScaleTimeline, ShearTimeline, IkConstraintTimeline,
    TransformConstraintTimeline, PathConstraintPositionTimeline,
    PathConstraintSpacingTimeline, PathConstraintMixTimeline, DeformTimeline,
    DrawOrderTimeline, EventTimeline, CurveTimeline,
)


def _color(rgba) -> str:
    r, g, b, a = rgba
    return "%02X%02X%02X%02X" % (
        int(round(r * 255)), int(round(g * 255)), int(round(b * 255)), int(round(a * 255)),
    )


def _enum_name(enum_cls, value):
    for name, member in enum_cls.__members__.items():
        if member == value:
            return name
    return None


def _curve(timeline: CurveTimeline, frame_index: int):
    """时间线第 frame_index 帧的 JSON 曲线字段。
    spine-asset 的 curves 已预处理为 3.8 二进制采样；JSON 需控制点——
    暂以线性近似（阶跃保留），贝塞尔反演后续精修。"""
    if getattr(timeline, "curves", None) is None:
        return {}
    idx = frame_index * CurveTimeline.BEZIER_SIZE
    if idx >= len(timeline.curves):
        return {}
    ctype = timeline.curves[idx]
    if ctype == CurveTimeline.STEPPED:
        return {"curve": "stepped"}
    # LINEAR 或 BEZIER（暂近似线性）都不输出曲线字段
    return {}


def _find_parent_mesh_skin(sd: SkeletonData, parent_mesh):
    """在皮肤中反查父网格：返回 (皮肤名, 槽位名, 附件名)。"""
    for skin in sd.skins:
        for entry in skin.attachments:
            if entry.attachment is parent_mesh:
                slot = sd.slots[entry.slot_index]
                return skin.name, slot.name, entry.name
    raise ValueError("parent mesh not found in skins")


def _attachment_json(sd: SkeletonData, skin, slot_index: int, name: str, att):
    d = {}
    if isinstance(att, RegionAttachment):
        d["type"] = "region"
        d["path"] = att.path or name
        d["x"] = att.x
        d["y"] = att.y
        d["scaleX"] = att.scale_x
        d["scaleY"] = att.scale_y
        d["rotation"] = att.rotation
        d["width"] = att.width
        d["height"] = att.height
        if att.color is not None:
            d["color"] = _color(att.color)
    elif isinstance(att, MeshAttachment):
        if att.parent_mesh is not None:
            d["type"] = "linkedmesh"
            skin_name, parent_slot, parent_name = _find_parent_mesh_skin(sd, att.parent_mesh)
            d["parent"] = parent_name
            d["skin"] = skin_name
            d["deform"] = (att.deform_attachment is att.parent_mesh)
            d["width"] = att.width
            d["height"] = att.height
            if att.color is not None:
                d["color"] = _color(att.color)
        else:
            d["type"] = "mesh"
            d["path"] = att.path or name
            d["uvs"] = list(att.region_uvs)
            d["triangles"] = list(att.triangles)
            d["vertices"] = _vertices(att)
            if att.hull_length is not None and att.hull_length > 0:
                d["hull"] = att.hull_length // 2
            if att.edges is not None:
                d["edges"] = list(att.edges)
            d["width"] = att.width
            d["height"] = att.height
            if att.color is not None:
                d["color"] = _color(att.color)
    elif isinstance(att, BoundingBoxAttachment):
        d["type"] = "boundingbox"
        d["vertexCount"] = att.world_vertices_length // 2
        d["vertices"] = _vertices(att)
        if att.color is not None:
            d["color"] = _color(att.color)
    elif isinstance(att, PathAttachment):
        d["type"] = "path"
        d["closed"] = bool(att.closed)
        d["constantSpeed"] = bool(att.constant_speed)
        d["vertexCount"] = att.world_vertices_length // 2
        d["vertices"] = _vertices(att)
        d["lengths"] = list(att.lengths)
        if att.color is not None:
            d["color"] = _color(att.color)
    elif isinstance(att, PointAttachment):
        d["type"] = "point"
        d["x"] = att.x
        d["y"] = att.y
        d["rotation"] = att.rotation
        if att.color is not None:
            d["color"] = _color(att.color)
    elif isinstance(att, ClippingAttachment):
        d["type"] = "clipping"
        if att.end_slot is not None:
            d["end"] = att.end_slot.name
        d["vertexCount"] = att.world_vertices_length // 2
        d["vertices"] = _vertices(att)
        if att.color is not None:
            d["color"] = _color(att.color)
    else:
        raise ValueError("unknown attachment type: %r" % type(att))
    return d


def _vertices(att: VertexAttachment):
    if getattr(att, "bones", None) is None:
        return list(att.vertices)
    # 加权顶点：扁平 [数量, (骨骼,x,y,权重)*, ...]
    bones = att.bones
    verts = att.vertices
    out = []
    i = 0
    j = 0
    while i < len(bones):
        count = bones[i]
        i += 1
        out.append(count)
        for _ in range(count):
            out.append(bones[i]); i += 1
            out.append(verts[j]); out.append(verts[j + 1]); out.append(verts[j + 2])
            j += 3
    return out


def write_skeleton_data(sd: SkeletonData) -> str:
    root = {}

    # 头
    header = {"spine": sd.version or "3.8.99"}
    header["width"] = getattr(sd, "width", 0) or 0
    header["height"] = getattr(sd, "height", 0) or 0
    root["skeleton"] = header

    # 骨骼
    root["bones"] = []
    for b in sd.bones:
        d = {"name": b.name}
        if b.parent is not None:
            d["parent"] = b.parent.name
        d["length"] = b.length
        d["x"] = b.x
        d["y"] = b.y
        d["rotation"] = b.rotation
        d["scaleX"] = b.scale_x
        d["scaleY"] = b.scale_y
        d["shearX"] = b.shear_x
        d["shearY"] = b.shear_y
        d["transform"] = _enum_name(TransformMode, b.transform_mode) or "normal"
        if getattr(b, "skin_required", False):
            d["skin"] = True
        if getattr(b, "color", None) is not None:
            d["color"] = _color(b.color)
        root["bones"].append(d)

    # 槽位
    root["slots"] = []
    for s in sd.slots:
        d = {"name": s.name, "bone": s.bone_data.name}
        if s.color is not None:
            d["color"] = _color(s.color)
        if getattr(s, "dark_color", None) is not None:
            d["dark"] = _color(s.dark_color)
        if s.attachment_name:
            d["attachment"] = s.attachment_name
        d["blend"] = _enum_name(BlendMode, s.blend_mode) if s.blend_mode is not None else "normal"
        root["slots"].append(d)

    # IK
    root["ik"] = []
    for c in sd.ik_constraints:
        d = {
            "name": c.name, "order": c.order,
            "bones": [b.name for b in c.bones], "target": c.target.name,
            "mix": c.mix, "softness": c.softness,
            "bendPositive": c.bend_direction >= 0,
            "compress": bool(c.compress), "stretch": bool(c.stretch), "uniform": bool(c.uniform),
        }
        root["ik"].append(d)

    # 变换约束
    root["transform"] = []
    for c in sd.transform_constraints:
        d = {
            "name": c.name, "order": c.order,
            "bones": [b.name for b in c.bones], "target": c.target.name,
            "local": bool(c.local), "relative": bool(c.relative),
            "rotation": c.offset_rotation, "x": c.offset_x, "y": c.offset_y,
            "scaleX": c.offset_scale_x, "scaleY": c.offset_scale_y, "shearY": c.offset_shear_y,
            "rotateMix": c.rotate_mix, "translateMix": c.translate_mix,
            "scaleMix": c.scale_mix, "shearMix": c.shear_mix,
        }
        root["transform"].append(d)

    # 路径约束
    root["path"] = []
    for c in sd.path_constraints:
        d = {
            "name": c.name, "order": c.order,
            "bones": [b.name for b in c.bones], "target": c.target.name,
            "positionMode": _enum_name(PositionMode, c.position_mode) or "percent",
            "spacingMode": _enum_name(SpacingMode, c.spacing_mode) or "length",
            "rotateMode": _enum_name(RotateMode, c.rotate_mode) or "tangent",
            "rotation": c.offset_rotation, "position": c.position, "spacing": c.spacing,
            "rotateMix": c.rotate_mix, "translateMix": c.translate_mix,
        }
        root["path"].append(d)

    # 皮肤
    root["skins"] = []
    for skin in sd.skins:
        d = {"name": skin.name}
        if skin.bones:
            d["bones"] = [b.name for b in skin.bones]
        attachments = {}
        for entry in skin.attachments:
            slot = sd.slots[entry.slot_index]
            slot_map = attachments.setdefault(slot.name, {})
            slot_map[entry.name] = _attachment_json(sd, skin, entry.slot_index, entry.name, entry.attachment)
        d["attachments"] = attachments
        root["skins"].append(d)

    # 事件
    events = {}
    for e in sd.events:
        ed = {"int": e.int_value, "float": e.float_value, "string": e.string_value or ""}
        if e.audio_path:
            ed["audio"] = e.audio_path
            ed["volume"] = getattr(e, "volume", 1.0)
            ed["balance"] = getattr(e, "balance", 0.0)
        events[e.name] = ed
    if events:
        root["events"] = events

    # 动画
    animations = {}
    for anim in sd.animations:
        animations[anim.name] = _animation_json(sd, anim)
    if animations:
        root["animations"] = animations

    return json.dumps(root, ensure_ascii=False, separators=(",", ":"))


def _frame_count(tl) -> int:
    if isinstance(tl, AttachmentTimeline):
        return len(tl.frames)
    if isinstance(tl, DeformTimeline):
        return len(tl.frames)
    if isinstance(tl, DrawOrderTimeline):
        return len(tl.frames)
    if isinstance(tl, EventTimeline):
        return len(tl.events)
    return tl.get_frame_count()

def _animation_json(sd: SkeletonData, anim) -> dict:
    d = {}
    for tl in anim.timelines:
        if isinstance(tl, (AttachmentTimeline, ColorTimeline, TwoColorTimeline)):
            slot = sd.slots[tl.slot_index]
            slot_map = d.setdefault("slots", {}).setdefault(slot.name, {})
            if isinstance(tl, AttachmentTimeline):
                frames = []
                for i in range(_frame_count(tl)):
                    frame = {"time": tl.frames[i]}
                    name = tl.attachment_names[i]
                    if name:
                        frame["name"] = name
                    frames.append(frame)
                slot_map["attachment"] = frames
            elif isinstance(tl, ColorTimeline):
                frames = []
                for i in range(_frame_count(tl)):
                    t = tl.frames[i * 5]
                    frame = {"time": t, "color": _color(tl.frames[i * 5 + 1: i * 5 + 5])}
                    if i < tl.get_frame_count() - 1:
                        frame.update(_curve(tl, i))
                    frames.append(frame)
                slot_map["color"] = frames
            elif isinstance(tl, TwoColorTimeline):
                frames = []
                for i in range(_frame_count(tl)):
                    t = tl.frames[i * 8]
                    frame = {
                        "time": t,
                        "light": _color(tl.frames[i * 8 + 1: i * 8 + 5]),
                        "dark": _color((*tl.frames[i * 8 + 5: i * 8 + 8], 1.0)),
                    }
                    if i < tl.get_frame_count() - 1:
                        frame.update(_curve(tl, i))
                    frames.append(frame)
                slot_map["twoColor"] = frames
        elif isinstance(tl, (RotateTimeline, TranslateTimeline, ScaleTimeline, ShearTimeline)):
            bone = sd.bones[tl.bone_index]
            bone_map = d.setdefault("bones", {}).setdefault(bone.name, {})
            if isinstance(tl, RotateTimeline):
                frames = []
                for i in range(_frame_count(tl)):
                    frame = {"time": tl.frames[i * 2], "angle": tl.frames[i * 2 + 1]}
                    if i < tl.get_frame_count() - 1:
                        frame.update(_curve(tl, i))
                    frames.append(frame)
                bone_map["rotate"] = frames
            else:
                key = "translate" if isinstance(tl, TranslateTimeline) else (
                    "scale" if isinstance(tl, ScaleTimeline) else "shear")
                frames = []
                for i in range(_frame_count(tl)):
                    frame = {"time": tl.frames[i * 3], "x": tl.frames[i * 3 + 1], "y": tl.frames[i * 3 + 2]}
                    if i < tl.get_frame_count() - 1:
                        frame.update(_curve(tl, i))
                    frames.append(frame)
                bone_map[key] = frames
        elif isinstance(tl, IkConstraintTimeline):
            ik = sd.ik_constraints[tl.ik_constraint_index]
            frames = []
            for i in range(_frame_count(tl)):
                t = tl.frames[i * 6]
                frames.append({
                    "time": t, "mix": tl.frames[i * 6 + 1], "softness": tl.frames[i * 6 + 2],
                    "bendPositive": tl.frames[i * 6 + 3] >= 0,
                    "compress": bool(tl.frames[i * 6 + 4]), "stretch": bool(tl.frames[i * 6 + 5]),
                    **(_curve(tl, i) if i < tl.get_frame_count() - 1 else {}),
                })
            d.setdefault("ik", {})[ik.name] = frames
        elif isinstance(tl, TransformConstraintTimeline):
            tc = sd.transform_constraints[tl.transform_constraint_index]
            frames = []
            for i in range(_frame_count(tl)):
                t = tl.frames[i * 5]
                frames.append({
                    "time": t,
                    "rotateMix": tl.frames[i * 5 + 1], "translateMix": tl.frames[i * 5 + 2],
                    "scaleMix": tl.frames[i * 5 + 3], "shearMix": tl.frames[i * 5 + 4],
                    **(_curve(tl, i) if i < tl.get_frame_count() - 1 else {}),
                })
            d.setdefault("transform", {})[tc.name] = frames
        elif isinstance(tl, (PathConstraintPositionTimeline, PathConstraintSpacingTimeline, PathConstraintMixTimeline)):
            pc = sd.path_constraints[tl.path_constraint_index]
            pm = d.setdefault("paths", {}).setdefault(pc.name, {})
            if isinstance(tl, PathConstraintMixTimeline):
                frames = []
                for i in range(_frame_count(tl)):
                    t = tl.frames[i * 3]
                    frames.append({
                        "time": t, "rotateMix": tl.frames[i * 3 + 1], "translateMix": tl.frames[i * 3 + 2],
                        **(_curve(tl, i) if i < tl.get_frame_count() - 1 else {}),
                    })
                pm["mix"] = frames
            else:
                key = "position" if isinstance(tl, PathConstraintPositionTimeline) else "spacing"
                frames = []
                for i in range(_frame_count(tl)):
                    frames.append({
                        "time": tl.frames[i * 2], key: tl.frames[i * 2 + 1],
                        **(_curve(tl, i) if i < tl.get_frame_count() - 1 else {}),
                    })
                pm[key] = frames
        elif isinstance(tl, DeformTimeline):
            skin = _find_skin_of_attachment(sd, tl.attachment, tl.slot_index)
            slot = sd.slots[tl.slot_index]
            frames = []
            for i in range(_frame_count(tl)):
                frames.append({"time": tl.frames[i], "vertices": list(tl.frame_vertices[i])})
                if i < tl.get_frame_count() - 1:
                    frames[-1].update(_curve(tl, i))
            d.setdefault("deform", {}).setdefault(skin, {}).setdefault(slot.name, {})[
                tl.attachment.name] = frames
        elif isinstance(tl, DrawOrderTimeline):
            # 源文件个别动画的 drawOrder 段解析产生垃圾时间戳（-1e23 等）——整条丢弃
            if any(not (math.isfinite(t) and 0 <= t < 1e6) for t in tl.frames):
                return d if False else _skip_timeline(d)
            frames = []
            for i in range(_frame_count(tl)):
                order = tl.draw_orders[i]
                if order is None:
                    frames.append({"time": tl.frames[i]})
                    continue
                offsets = []
                for s in range(len(order)):
                    if s not in order:
                        continue  # 未变槽位
                    new_pos = order.index(s)
                    if new_pos != s:
                        offsets.append({"slot": sd.slots[s].name, "offset": new_pos - s})
                frames.append({"time": tl.frames[i], "offsets": offsets})
            d["drawOrder"] = frames
        elif isinstance(tl, EventTimeline):
            if any(not (math.isfinite(t) and 0 <= t < 1e6) for t in tl.frames):
                return d if False else _skip_timeline(d)
            frames = []
            for i in range(_frame_count(tl)):
                ev = tl.events[i]
                frame = {
                    "time": ev.time, "name": ev.data.name,
                    "int": ev.int_value, "float": ev.float_value, "string": ev.string_value or "",
                }
                if ev.data.audio_path:
                    frame["volume"] = ev.volume
                    frame["balance"] = ev.balance
                frames.append(frame)
            d["events"] = frames
        else:
            raise ValueError("unknown timeline type: %r" % type(tl))
    return d


def _skip_timeline(d: dict) -> dict:
    """丢弃当前时间线：直接返回动画 dict（时间线未写入）。"""
    return d

def _find_skin_of_attachment(sd: SkeletonData, attachment, slot_index: int) -> str:
    for skin in sd.skins:
        for entry in skin.attachments:
            if entry.slot_index == slot_index and entry.attachment is attachment:
                return skin.name
    # 兜底：默认皮肤名
    return sd.skins[0].name if sd.skins else "default"
