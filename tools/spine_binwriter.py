#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""spine-asset SkeletonData -> spine-godot 4.2 fork 二进制写出器（版本 "4.2.43"）。

格式规格：tools/FORK_FORMAT.md（已实证）；读端镜像：tools/fork_read.py（已验证可
完整走通游戏实机 .spskel）。本模块自检直接 import fork_read 的 P 类走读输出。

关键约定：
- 所有 float / 4 字节 int = 大端（struct.pack(">f")）；varint 不变（7bit 分组）
- 字符串 = varint(UTF8字节数 + 1) + 字节（无 NUL）；空引用写 varint 0（NULL）
- 颜色 = 4 字节 R,G,B,A（0-255）；槽位暗色 = 4 字节 a,r,g,b（无暗色全 0xFF）
- 版本门 startsWith("4.2") → 必须写 "4.2.43"
- 头 = 8 字节零 hash + 版本串 + 5 个大端 float(x,y,width,height,referenceScale=1.0)
  + nonessential 字节 0（→ 全部 nonessential 分支不写）
- 字符串池：只收 readStringRef 引用的串（槽位默认附件名/皮肤附件名/附件显式名与
  路径/动画附件时间线名），索引=位置+1；其余（骨骼名等）内联
- 附件 = flags 字节开头（bit0-2 类型、bit3 显式名；bit4/5/6/7 按类型含义不同）
- 变换约束 4 mix 展开为 fork 6 值：mixX=mixY=translate_mix、mixScaleX=mixScaleY=scale_mix

CLI: py -3 tools\\spine_binwriter.py <input_arknights.skel> <output.skel>
"""
import copy
import math
import struct
import sys

from spine_asset.v38 import (
    IkConstraintData,
    PathConstraintData,
    SkeletonBinary,
    SkeletonData,
    TransformConstraintData,
)
from spine_asset.utils import SkeletonBinaryReader


def _patch_source_reader_big_endian():
    """阿基源文件（1037_amiya3_sale#13）的浮点/16位/32位整数为大端存储。
    spine-asset 的 SkeletonBinaryReader 硬编码小端，读出来全是天文数字垃圾坐标。
    这里替换为大端实现（varint/字节/字符串不受端序影响，保持原样）。"""
    def _be(self, fmt):
        return struct.unpack(">" + fmt, self._stream.read(struct.calcsize(fmt)))[0]

    SkeletonBinaryReader.read_float32 = lambda self: _be(self, "f")
    SkeletonBinaryReader.read_int32 = lambda self: _be(self, "i")
    SkeletonBinaryReader.read_int16 = lambda self: _be(self, "h")
    SkeletonBinaryReader.read_uint32 = lambda self: _be(self, "I")
from spine_asset.v38.attachments import (
    BoundingBoxAttachment,
    ClippingAttachment,
    MeshAttachment,
    PathAttachment,
    PointAttachment,
    RegionAttachment,
    VertexAttachment,
)
from spine_asset.v38.timelines import (
    AttachmentTimeline,
    ColorTimeline,
    CurveTimeline,
    DeformTimeline,
    DrawOrderTimeline,
    EventTimeline,
    IkConstraintTimeline,
    PathConstraintMixTimeline,
    PathConstraintPositionTimeline,
    PathConstraintSpacingTimeline,
    RotateTimeline,
    ScaleTimeline,
    ShearTimeline,
    TransformConstraintTimeline,
    TranslateTimeline,
    TwoColorTimeline,
)

from fork_read import P as _ForkP

VERSION = "4.2.43"

# 动画改名映射（游戏硬编码播放名）
ANIM_RENAME = {
    "Default": "idle_loop",
    "Special": "attack",
    "Interact": "hurt",
    "Sleep": "die",
    "Relax": "relaxed_loop",
}
# 游戏战斗控制器不用的动画：直接丢弃
ANIM_DROP = {"Move", "Sit"}
# 追加副本动画：新名 = 源名的浅拷贝（共享时间线），放在改名/过滤之后
ANIM_COPIES = [
    ("cast", "attack"),
    ("low_health_loop", "idle_loop"),
]

# 时间线类型码
BONE_ROTATE = 0
BONE_TRANSLATE = 1
BONE_SCALE = 4
BONE_SHEAR = 7
SLOT_ATTACHMENT = 0
SLOT_RGBA = 1
SLOT_RGBA2 = 3
ATTACHMENT_DEFORM = 0
PATH_POSITION = 0
PATH_SPACING = 1
PATH_MIX = 2

CURVE_LINEAR = 0
CURVE_STEPPED = 1
CURVE_BEZIER = 2

CURVE_STRIDE = CurveTimeline.BEZIER_SIZE  # 19


# ---------------------------------------------------------------------------
# 原子写出
# ---------------------------------------------------------------------------
class _Writer:
    def __init__(self):
        self.buf = bytearray()

    def u8(self, v: int) -> None:
        self.buf.append(int(v) & 0xFF)

    def varint(self, v: int) -> None:
        v = int(v)
        if v < 0:
            v = 0
        while True:
            b = v & 0x7F
            v >>= 7
            if v:
                self.buf.append(b | 0x80)
            else:
                self.buf.append(b)
                return

    def varint_signed(self, n: int) -> None:
        """有符号整数：zigzag 后写 varint（读端 readVarint(false)）。"""
        self.varint((n << 1) ^ (n >> 31))

    def f32(self, v) -> None:
        self.buf += struct.pack(">f", float(v))  # 大端！

    def string(self, s) -> None:
        """varint(UTF8字节数+1) + 字节（无 NUL）。"""
        b = (s or "").encode("utf-8")
        self.varint(len(b) + 1)
        self.buf += b

    def color(self, rgba) -> None:
        """0-1 浮点 RGBA -> 4 字节 0-255（R,G,B,A 顺序）。"""
        rgba = tuple(rgba or (1.0, 1.0, 1.0, 1.0))
        for i in range(4):
            v = rgba[i] if i < len(rgba) else 1.0
            self.u8(max(0, min(255, int(round(v * 255)))))

    def color3(self, rgb) -> None:
        """0-1 浮点 RGB -> 3 字节 0-255（R,G,B 顺序；RGBA2 暗色用）。"""
        rgb = tuple(rgb or (1.0, 1.0, 1.0))
        for i in range(3):
            v = rgb[i] if i < len(rgb) else 1.0
            self.u8(max(0, min(255, int(round(v * 255)))))


def _bend_positive(v) -> bool:
    """bendDirection：1/-1 -> True/False。源 read_byte 读出无符号（-1=255）。"""
    v = int(v)
    return 0 < v < 128


def _color_nonwhite(rgba) -> bool:
    if not rgba:
        return False
    return not all(float(c) == 1.0 for c in rgba[:4])


def _frame_count(tl) -> int:
    if isinstance(tl, (AttachmentTimeline, DeformTimeline, DrawOrderTimeline, EventTimeline)):
        return len(tl.frames)
    return tl.get_frame_count()


def _timeline_ok(tl) -> bool:
    """垃圾时间线过滤：帧时间非有限或 <0 或 >1e6 的 DrawOrder/Event 整条丢弃。"""
    if isinstance(tl, (DrawOrderTimeline, EventTimeline)):
        for t in tl.frames:
            if not math.isfinite(float(t)) or t < 0 or t > 1e6:
                return False
    return True


def _curve_type(tl: CurveTimeline, frame_index: int) -> int:
    if _FLATTEN_CURVES:
        return CURVE_LINEAR
    c = tl.curves[frame_index * CURVE_STRIDE]
    if c == CurveTimeline.LINEAR:
        return CURVE_LINEAR
    if c == CurveTimeline.STEPPED:
        return CURVE_STEPPED
    return CURVE_BEZIER


def _write_curve(w: _Writer, tl: CurveTimeline, frame_index: int, nvals: int) -> None:
    """曲线字节 + bezier 时每值 4 个 f32 (cx1,cy1,cx2,cy2)。
    源 curves 每曲线 19 项 [类型][18 floats(预处理采样)]；按规格每值取前 4 个，
    不足 4×N 时复用前 4 个。"""
    if _FLATTEN_CURVES:
        w.u8(CURVE_LINEAR)
        return
    ctype = _curve_type(tl, frame_index)
    if ctype == CURVE_LINEAR:
        w.u8(CURVE_LINEAR)
        return
    if ctype == CURVE_STEPPED:
        w.u8(CURVE_STEPPED)
        return
    w.u8(CURVE_BEZIER)
    idx = frame_index * CURVE_STRIDE
    base = idx + 1
    for j in range(nvals):
        s = base + 4 * j
        if s + 3 <= idx + CURVE_STRIDE - 1:
            a, b, c, d = tl.curves[s], tl.curves[s + 1], tl.curves[s + 2], tl.curves[s + 3]
        else:
            a, b, c, d = tl.curves[base], tl.curves[base + 1], tl.curves[base + 2], tl.curves[base + 3]
        w.f32(a)
        w.f32(b)
        w.f32(c)
        w.f32(d)


def _bezier_count(tl: CurveTimeline, nvals: int) -> int:
    """bezierCount = 贝塞尔曲线条数（每值一条）。"""
    if _FLATTEN_CURVES:
        return 0
    fc = tl.get_frame_count()
    n = 0
    for i in range(fc - 1):
        if _curve_type(tl, i) == CURVE_BEZIER:
            n += 1
    return n * nvals


# ---------------------------------------------------------------------------
# 字符串池
# ---------------------------------------------------------------------------
class _Pool:
    def __init__(self):
        self.strings = []
        self.index = {}
        self.locked = False

    def add(self, s) -> int:
        """返回池索引（位置+1）；None/空串返回 0（NULL 引用）。
        预收集阶段锁定后不允许出现新串（否则索引越界）。"""
        if not s:
            return 0
        idx = self.index.get(s)
        if idx is not None:
            return idx
        if self.locked:
            raise ValueError("pool: new string %r during locked write phase" % s)
        idx = len(self.strings) + 1
        self.index[s] = idx
        self.strings.append(s)
        return idx


# ---------------------------------------------------------------------------
# 皮肤 / 附件
# ---------------------------------------------------------------------------
def _ordered_skins(sd: SkeletonData):
    """写出顺序的皮肤列表：default 皮肤在前，其余按 sd.skins 顺序。"""
    others = [s for s in sd.skins if s is not sd.default_skin]
    out = []
    if sd.default_skin is not None:
        out.append(sd.default_skin)
    return out + others


def _skin_slot_groups(skin):
    by_slot = {}
    for entry in skin.attachments.values():
        by_slot.setdefault(entry.slot_index, []).append(entry)
    return by_slot


def _find_skin_entry(sd: SkeletonData, attachment, ordered_skins):
    """按对象同一性反查附件所在的 (有序皮肤索引, SkinEntry)。"""
    for si, skin in enumerate(ordered_skins):
        for entry in skin.attachments.values():
            if entry.attachment is attachment:
                return si, entry
    return None, None


def _vertex_count(att: VertexAttachment) -> int:
    if getattr(att, "bones", None) is None:
        return len(att.vertices) // 2
    n = 0
    i = 0
    bones = att.bones
    while i < len(bones):
        n += 1
        i += 1 + bones[i]
    return n


def _write_vertices(w: _Writer, att: VertexAttachment, weighted: bool) -> None:
    """fork readVertices 写向：varint vertexCount + 坐标/加权数据（写全部顶点）。"""
    vc = _vertex_count(att)
    w.varint(vc)
    if not weighted:
        verts = att.vertices
        for i in range(vc * 2):
            w.f32(verts[i] if i < len(verts) else 0.0)
        return
    bones = att.bones
    verts = att.vertices
    i = 0
    j = 0
    for _ in range(vc):
        if i >= len(bones):
            w.varint(0)
            continue
        count = bones[i]
        i += 1
        w.varint(count)
        for _ in range(count):
            w.varint(bones[i] if i < len(bones) else 0)
            i += 1
            w.f32(verts[j] if j < len(verts) else 0.0)
            w.f32(verts[j + 1] if j + 1 < len(verts) else 0.0)
            w.f32(verts[j + 2] if j + 2 < len(verts) else 0.0)
            j += 3


def _write_attachment(w: _Writer, sd: SkeletonData, pool: _Pool, ordered_skins,
                      entry_name: str, att) -> None:
    name = att.name if att.name else entry_name
    if isinstance(att, RegionAttachment):
        flags = 0 | 8 | 16  # 类型 region + 显式名 + 显式路径
        if _color_nonwhite(att.color):
            flags |= 32
        if att.rotation != 0:
            flags |= 128
        w.u8(flags)
        w.varint(pool.add(name))
        w.varint(pool.add(att.path if att.path else name))
        if flags & 32:
            w.color(att.color)
        if flags & 128:
            w.f32(att.rotation)
        w.f32(att.x)
        w.f32(att.y)
        w.f32(att.scale_x)
        w.f32(att.scale_y)
        w.f32(att.width)
        w.f32(att.height)
    elif isinstance(att, BoundingBoxAttachment):
        weighted = getattr(att, "bones", None) is not None
        w.u8(1 | 8 | (16 if weighted else 0))
        w.varint(pool.add(name))
        _write_vertices(w, att, weighted)
    elif isinstance(att, MeshAttachment):
        if att.parent_mesh is not None:
            # linkedmesh：类型 3；flags bit7 = inheritDeform；父皮肤索引 + 父网格名池索引
            flags = 3 | 8 | 16
            if _color_nonwhite(att.color):
                flags |= 32
            if att.deform_attachment is att.parent_mesh:
                flags |= 128
            w.u8(flags)
            w.varint(pool.add(name))
            w.varint(pool.add(att.path if att.path else name))
            if flags & 32:
                w.color(att.color)
            skin_index, _ = _find_skin_entry(sd, att.parent_mesh, ordered_skins)
            if skin_index is None:
                raise ValueError("linked mesh parent not found in skins: %r" % att.name)
            w.varint(skin_index)
            w.varint(pool.add(att.parent_mesh.name))
            return
        weighted = getattr(att, "bones", None) is not None
        flags = 2 | 8 | 16 | (128 if weighted else 0)
        if _color_nonwhite(att.color):
            flags |= 32
        w.u8(flags)
        w.varint(pool.add(name))
        w.varint(pool.add(att.path if att.path else name))
        if flags & 32:
            w.color(att.color)
        vc = _vertex_count(att)  # 全部顶点数
        hull = (att.hull_length or 0) // 2
        w.varint(hull)
        _write_vertices(w, att, weighted)
        uvs = att.region_uvs or []
        for v in uvs:
            w.f32(v)
        tris = att.triangles or []
        expected = (vc * 2 - hull - 2) * 3
        if len(tris) != expected:
            raise ValueError(
                "mesh %r triangle count %d != expected %d (vc=%d hull=%d)"
                % (name, len(tris), expected, vc, hull))
        for t in tris:
            w.varint(t)
    elif isinstance(att, PathAttachment):
        weighted = getattr(att, "bones", None) is not None
        flags = 4 | 8 | (16 if att.closed else 0) | (32 if att.constant_speed else 0) \
            | (64 if weighted else 0)
        w.u8(flags)
        w.varint(pool.add(name))
        _write_vertices(w, att, weighted)
        lengths = att.lengths or []
        for v in lengths:
            w.f32(v)
    elif isinstance(att, PointAttachment):
        w.u8(5 | 8)
        w.varint(pool.add(name))
        w.f32(att.rotation)
        w.f32(att.x)
        w.f32(att.y)
    elif isinstance(att, ClippingAttachment):
        weighted = getattr(att, "bones", None) is not None
        w.u8(6 | 8 | (16 if weighted else 0))
        w.varint(pool.add(name))
        w.varint(att.end_slot.index if att.end_slot is not None else 0)
        _write_vertices(w, att, weighted)
    else:
        raise ValueError("unknown attachment type: %r" % type(att))


def _write_skin(w: _Writer, sd: SkeletonData, pool: _Pool, ordered_skins, skin,
                is_default: bool) -> None:
    by_slot = _skin_slot_groups(skin)
    if not is_default:
        w.string(skin.name)
        # 5 张表：bones / ik / transform / path / physics
        w.varint(len(skin.bones))
        for b in skin.bones:
            w.varint(b.index)
        ik = [c for c in skin.constraints if isinstance(c, IkConstraintData)]
        tc = [c for c in skin.constraints if isinstance(c, TransformConstraintData)]
        pc = [c for c in skin.constraints if isinstance(c, PathConstraintData)]
        w.varint(len(ik))
        for c in ik:
            w.varint(sd.ik_constraints.index(c))
        w.varint(len(tc))
        for c in tc:
            w.varint(sd.transform_constraints.index(c))
        w.varint(len(pc))
        for c in pc:
            w.varint(sd.path_constraints.index(c))
        w.varint(0)  # physics
    w.varint(len(by_slot))
    for slot_index in sorted(by_slot):
        entries = by_slot[slot_index]
        w.varint(slot_index)
        w.varint(len(entries))
        for entry in entries:
            w.varint(pool.add(entry.name))
            _write_attachment(w, sd, pool, ordered_skins, entry.name, entry.attachment)


# ---------------------------------------------------------------------------
# 动画时间线分组
# ---------------------------------------------------------------------------
import os as _os

# 诊断开关（环境变量 AMIYA_STRIP，逗号分隔）：
#   hurt_att   = 跳过 Interact(hurt) 的附件切换时间线（眼部网格换装）
#   hurt_color = 跳过 Interact 的颜色时间线
#   att_deform / att_switch / att_bone / att_color = 跳过 Special 对应时间线
#   att_curve  = Special 全部贝塞尔曲线拍平为线性
_STRIP = set((_os.environ.get("AMIYA_STRIP", "") or "").split(","))

# 全局"拍平曲线"模式：仅在写 Special 且 att_curve 开关打开时置位
_FLATTEN_CURVES = False


def _group_animation(sd: SkeletonData, ordered_skins, anim):
    timelines = [tl for tl in anim.timelines if _timeline_ok(tl)]
    if "only_idle" in _STRIP and anim.name != "idle_loop":
        # 可玩版兜底：除待机外所有动画清空（保留动画名与时长入口）——
        # 无论游戏调用什么动画都不会触发任何时间线。
        timelines = []
    if anim.name == "hurt":  # 原名 Interact，此处为改名后
        if "hurt_att" in _STRIP:
            timelines = [tl for tl in timelines if not isinstance(tl, AttachmentTimeline)]
        if "hurt_color" in _STRIP:
            timelines = [tl for tl in timelines if not isinstance(tl, ColorTimeline)]
    if anim.name in ("attack", "cast"):  # 原名 Special，cast 为其副本
        if "att_deform" in _STRIP:
            timelines = [tl for tl in timelines if not isinstance(tl, DeformTimeline)]
        if "att_switch" in _STRIP:
            timelines = [tl for tl in timelines if not isinstance(tl, AttachmentTimeline)]
        if "att_bone" in _STRIP:
            timelines = [tl for tl in timelines if not isinstance(
                tl, (RotateTimeline, TranslateTimeline, ScaleTimeline, ShearTimeline))]
        if "att_color" in _STRIP:
            timelines = [tl for tl in timelines if not isinstance(tl, (ColorTimeline, TwoColorTimeline))]
        if "att_ik" in _STRIP:
            timelines = [tl for tl in timelines if not isinstance(tl, IkConstraintTimeline)]
        if "att_tc" in _STRIP:
            timelines = [tl for tl in timelines if not isinstance(tl, TransformConstraintTimeline)]
        if "att_path" in _STRIP:
            timelines = [tl for tl in timelines if not isinstance(
                tl, (PathConstraintPositionTimeline, PathConstraintSpacingTimeline,
                     PathConstraintMixTimeline))]
        if "att_events" in _STRIP:
            timelines = [tl for tl in timelines if not isinstance(tl, EventTimeline)]
    g = {"slot": {}, "bone": {}, "ik": [], "tc": [], "path": {},
         "deform": {}, "draw": None, "events": None}
    for tl in timelines:
        if isinstance(tl, (AttachmentTimeline, ColorTimeline, TwoColorTimeline)):
            g["slot"].setdefault(tl.slot_index, []).append(tl)
        elif isinstance(tl, (RotateTimeline, TranslateTimeline, ScaleTimeline, ShearTimeline)):
            g["bone"].setdefault(tl.bone_index, []).append(tl)
        elif isinstance(tl, IkConstraintTimeline):
            g["ik"].append(tl)
        elif isinstance(tl, TransformConstraintTimeline):
            g["tc"].append(tl)
        elif isinstance(tl, (PathConstraintPositionTimeline, PathConstraintSpacingTimeline,
                             PathConstraintMixTimeline)):
            g["path"].setdefault(tl.path_constraint_index, []).append(tl)
        elif isinstance(tl, DeformTimeline):
            skin_index, entry = _find_skin_entry(sd, tl.attachment, ordered_skins)
            if skin_index is None:
                skin_index = 0
            ename = entry.name if entry is not None else tl.attachment.name
            g["deform"].setdefault((skin_index, tl.slot_index), []).append((tl, ename))
        elif isinstance(tl, DrawOrderTimeline):
            g["draw"] = tl
        elif isinstance(tl, EventTimeline):
            g["events"] = tl
        else:
            raise ValueError("unknown timeline type: %r" % type(tl))
    g["ik"].sort(key=lambda t: t.ik_constraint_index)
    g["tc"].sort(key=lambda t: t.transform_constraint_index)
    return g


def _collect_pool(sd: SkeletonData, ordered_skins, anim_groups) -> _Pool:
    """按写出顺序预收集字符串池（首次引用顺序，去重）。"""
    pool = _Pool()
    for s in sd.slots:
        pool.add(s.attachment_name)
    for skin in ordered_skins:
        by_slot = _skin_slot_groups(skin)
        for slot_index in sorted(by_slot):
            for entry in by_slot[slot_index]:
                pool.add(entry.name)
                att = entry.attachment
                pool.add(att.name if att.name else entry.name)
                if isinstance(att, (RegionAttachment, MeshAttachment)):
                    pool.add(att.path if att.path else att.name)
                    if isinstance(att, MeshAttachment) and att.parent_mesh is not None:
                        pool.add(att.parent_mesh.name)
    for g in anim_groups:
        for si in sorted(g["slot"]):
            for tl in g["slot"][si]:
                if isinstance(tl, AttachmentTimeline):
                    for nm in tl.attachment_names:
                        pool.add(nm)
        for key in sorted(g["deform"]):
            for tl, ename in g["deform"][key]:
                pool.add(ename)
    return pool


# ---------------------------------------------------------------------------
# 动画时间线写出
# ---------------------------------------------------------------------------
def _write_slot_tl(w: _Writer, pool: _Pool, tl) -> None:
    if isinstance(tl, AttachmentTimeline):
        w.u8(SLOT_ATTACHMENT)
        fc = len(tl.frames)
        w.varint(fc)
        for i in range(fc):
            w.f32(tl.frames[i])
            nm = tl.attachment_names[i]
            w.varint(pool.add(nm) if nm else 0)
    elif isinstance(tl, ColorTimeline):  # RGBA（4 字节色）
        w.u8(SLOT_RGBA)
        fc = tl.get_frame_count()
        w.varint(fc)
        w.varint(_bezier_count(tl, 4))
        w.f32(tl.frames[0])
        w.color(tl.frames[1:5])
        for j in range(1, fc):
            w.f32(tl.frames[j * 5])
            w.color(tl.frames[j * 5 + 1: j * 5 + 5])
            _write_curve(w, tl, j - 1, 4)
    elif isinstance(tl, TwoColorTimeline):  # RGBA2（亮 4 字节 + 暗 3 字节）
        w.u8(SLOT_RGBA2)
        fc = tl.get_frame_count()
        w.varint(fc)
        w.varint(_bezier_count(tl, 7))
        w.f32(tl.frames[0])
        w.color(tl.frames[1:5])  # 亮色 RGBA
        w.color3(tl.frames[5:8])  # 暗色 RGB
        for j in range(1, fc):
            w.f32(tl.frames[j * 8])
            w.color(tl.frames[j * 8 + 1: j * 8 + 5])
            w.color3(tl.frames[j * 8 + 5: j * 8 + 8])
            _write_curve(w, tl, j - 1, 7)


def _write_bone_tl(w: _Writer, tl) -> None:
    if isinstance(tl, RotateTimeline):
        ttype, nvals = BONE_ROTATE, 1
    elif isinstance(tl, ScaleTimeline):
        ttype, nvals = BONE_SCALE, 2
    elif isinstance(tl, ShearTimeline):
        ttype, nvals = BONE_SHEAR, 2
    else:
        ttype, nvals = BONE_TRANSLATE, 2
    w.u8(ttype)
    fc = tl.get_frame_count()
    w.varint(fc)
    w.varint(_bezier_count(tl, nvals))
    if nvals == 1:
        w.f32(tl.frames[0])
        w.f32(tl.frames[1])
        for j in range(1, fc):
            w.f32(tl.frames[j * 2])
            w.f32(tl.frames[j * 2 + 1])
            _write_curve(w, tl, j - 1, 1)
    else:
        w.f32(tl.frames[0])
        w.f32(tl.frames[1])
        w.f32(tl.frames[2])
        for j in range(1, fc):
            w.f32(tl.frames[j * 3])
            w.f32(tl.frames[j * 3 + 1])
            w.f32(tl.frames[j * 3 + 2])
            _write_curve(w, tl, j - 1, 2)


def _write_ik_tl(w: _Writer, tl) -> None:
    w.varint(tl.ik_constraint_index)
    fc = tl.get_frame_count()
    w.varint(fc)
    w.varint(_bezier_count(tl, 2))
    for j in range(fc):
        mix = tl.frames[j * 6 + 1]
        soft = tl.frames[j * 6 + 2]
        bend = int(tl.frames[j * 6 + 3])
        flags = 0
        if mix != 0:
            flags |= 1
        if mix != 1:
            flags |= 2
        if soft != 0:
            flags |= 4
        if _bend_positive(bend):
            flags |= 8
        if tl.frames[j * 6 + 4]:
            flags |= 16
        if tl.frames[j * 6 + 5]:
            flags |= 32
        if j > 0:
            ct = _curve_type(tl, j - 1)
            if ct == CURVE_STEPPED:
                flags |= 64
            elif ct == CURVE_BEZIER:
                flags |= 128
        w.u8(flags)
        w.f32(tl.frames[j * 6])
        if (flags & 1) and (flags & 2):
            w.f32(mix)
        if flags & 4:
            w.f32(soft)
        if j > 0 and (flags & 128):
            _write_curve(w, tl, j - 1, 2)


def _write_tc_tl(w: _Writer, tl) -> None:
    w.varint(tl.transform_constraint_index)
    fc = tl.get_frame_count()
    w.varint(fc)
    w.varint(_bezier_count(tl, 6))
    for j in range(fc):
        rm = tl.frames[j * 5 + 1]
        tm = tl.frames[j * 5 + 2]
        sm = tl.frames[j * 5 + 3]
        hm = tl.frames[j * 5 + 4]
        w.f32(tl.frames[j * 5])
        # fork 6 值：mixRotate, mixX, mixY, mixScaleX, mixScaleY, mixShearY
        # 源 translate/scale 同时作用于两轴 → mixX=mixY=translate, mixScaleX=mixScaleY=scale
        w.f32(rm)
        w.f32(tm)
        w.f32(tm)
        w.f32(sm)
        w.f32(sm)
        w.f32(hm)
        if j > 0:
            _write_curve(w, tl, j - 1, 6)


def _write_path_tl(w: _Writer, tl) -> None:
    if isinstance(tl, PathConstraintSpacingTimeline):
        ttype = PATH_SPACING
        nvals = 1
    elif isinstance(tl, PathConstraintMixTimeline):
        ttype = PATH_MIX
        nvals = 3
    else:
        ttype = PATH_POSITION
        nvals = 1
    w.u8(ttype)
    fc = tl.get_frame_count()
    w.varint(fc)
    w.varint(_bezier_count(tl, nvals))
    if nvals == 1:
        w.f32(tl.frames[0])
        w.f32(tl.frames[1])
        for j in range(1, fc):
            w.f32(tl.frames[j * 2])
            w.f32(tl.frames[j * 2 + 1])
            _write_curve(w, tl, j - 1, 1)
    else:  # mix：mixRotate, mixX, mixY（mixX=mixY=translate_mix）
        w.f32(tl.frames[0])
        w.f32(tl.frames[1])
        w.f32(tl.frames[2])
        w.f32(tl.frames[2])
        for j in range(1, fc):
            w.f32(tl.frames[j * 3])
            w.f32(tl.frames[j * 3 + 1])
            w.f32(tl.frames[j * 3 + 2])
            w.f32(tl.frames[j * 3 + 2])
            _write_curve(w, tl, j - 1, 3)


def _write_deform_tl(w: _Writer, pool: _Pool, tl: DeformTimeline, ename: str) -> None:
    w.varint(pool.add(ename))
    w.u8(ATTACHMENT_DEFORM)
    fc = len(tl.frames)
    w.varint(fc)
    w.varint(_bezier_count(tl, 1))
    att = tl.attachment
    weighted = getattr(att, "bones", None) is not None
    base = att.vertices if not weighted else None
    w.f32(tl.frames[0])
    for i in range(fc):
        verts = tl.frame_vertices[i]
        if not weighted:
            offsets = [verts[k] - base[k] for k in range(len(verts))]
        else:
            offsets = verts  # 源加权 deform 帧已是偏移量
        w.varint(len(offsets))
        if offsets:
            w.varint(0)  # start
            for v in offsets:
                w.f32(v)
        if i < fc - 1:
            w.f32(tl.frames[i + 1])
            _write_curve(w, tl, i, 1)


def _draw_order_offsets(order):
    """draw_orders 数组转 offsets：(槽索引, 新位置−原位置)，按槽索引升序，跳过未变槽。"""
    if not order:
        return []
    out = []
    for i, orig in enumerate(order):
        if orig != -1 and orig != i:
            out.append((orig, i - orig))
    out.sort(key=lambda t: t[0])
    return out


def _event_index(sd: SkeletonData, event_data) -> int:
    for i, e in enumerate(sd.events):
        if e is event_data:
            return i
    return 0


def _write_animation(w: _Writer, sd: SkeletonData, pool: _Pool, anim, g) -> None:
    global _FLATTEN_CURVES
    _FLATTEN_CURVES = anim.name in ("attack", "cast") and "att_curve" in _STRIP
    w.string(anim.name)
    w.varint(0)  # numTimelines（读端忽略）
    # 槽位时间线（先！）：varint 块数 + 每块 [槽索引][时间线数][时间线...]
    w.varint(len(g["slot"]))
    for si in sorted(g["slot"]):
        tls = g["slot"][si]
        w.varint(si)
        w.varint(len(tls))
        for tl in tls:
            _write_slot_tl(w, pool, tl)
    # 骨骼时间线：varint 块数 + 每块 [骨索引][时间线数][时间线...]
    w.varint(len(g["bone"]))
    for bi in sorted(g["bone"]):
        tls = g["bone"][bi]
        w.varint(bi)
        w.varint(len(tls))
        for tl in tls:
            _write_bone_tl(w, tl)
    # IK 时间线：varint 块数 + 每块 [索引][帧数][bezierCount][帧...]
    w.varint(len(g["ik"]))
    for tl in g["ik"]:
        _write_ik_tl(w, tl)
    # transform 时间线：同上
    w.varint(len(g["tc"]))
    for tl in g["tc"]:
        _write_tc_tl(w, tl)
    # path 时间线：varint 块数 + 每块 [索引][时间线数][每时间线: 类型+帧数+bezierCount+帧]
    w.varint(len(g["path"]))
    for idx in sorted(g["path"]):
        tls = g["path"][idx]
        w.varint(idx)
        w.varint(len(tls))
        for tl in tls:
            _write_path_tl(w, tl)
    # 物理时间线：varint 块数 0
    w.varint(0)
    # 附件时间线（deform）：varint 皮肤块数 + 每块 [皮肤索引][槽组数][每组...]
    by_skin = {}
    for (sk, sl), tls in g["deform"].items():
        by_skin.setdefault(sk, {})[sl] = tls
    w.varint(len(by_skin))
    for sk in sorted(by_skin):
        slots = by_skin[sk]
        w.varint(sk)
        w.varint(len(slots))
        for sl in sorted(slots):
            tls = slots[sl]
            w.varint(sl)
            w.varint(len(tls))
            for tl, ename in tls:
                _write_deform_tl(w, pool, tl, ename)
    # drawOrder（帧数 varint 无条件写，读端总是读 dc）
    if g["draw"] is not None:
        fc = len(g["draw"].frames)
        w.varint(fc)
        for i in range(fc):
            w.f32(g["draw"].frames[i])
            offsets = _draw_order_offsets(g["draw"].draw_orders[i])
            w.varint(len(offsets))
            for oi, off in offsets:
                w.varint(oi)
                w.varint(off)
    else:
        w.varint(0)
    # 事件时间线（帧数 varint 无条件写，读端总是读 ec）
    if g["events"] is not None:
        fc = len(g["events"].frames)
        w.varint(fc)
        for i in range(fc):
            ev = g["events"].events[i]
            w.f32(g["events"].frames[i])
            w.varint(_event_index(sd, ev.data))
            w.varint_signed(ev.int_value)
            w.f32(ev.float_value)
            if ev.string_value:
                w.string(ev.string_value)
            else:
                w.varint(0)
            if ev.data.audio_path:
                w.f32(ev.volume)
                w.f32(ev.balance)
    else:
        w.varint(0)


# ---------------------------------------------------------------------------
# 主写出
# ---------------------------------------------------------------------------
def apply_animation_mapping(sd: SkeletonData):
    """动画改名 + 追加副本（浅拷贝共享时间线，改 .name）。
    兼容两类源命名：常规（Default/Special/...）与 enemy（A_Default/A_Idle/...）。
    返回最终动画名列表。"""
    names = [a.name for a in sd.animations]
    enemy_style = any(n == "A_Default" for n in names) or (any(n.startswith("A_") for n in names)
                                                           and "Default" not in names)
    if enemy_style:
        # enemy 命名：A_Default -> idle_loop，其余全部丢弃
        for anim in sd.animations:
            anim.name = "idle_loop" if anim.name == "A_Default" else "__drop__"
        sd.animations = [a for a in sd.animations if a.name == "idle_loop"]
    else:
        for anim in sd.animations:
            if anim.name in ANIM_RENAME:
                anim.name = ANIM_RENAME[anim.name]
        # 丢弃游戏战斗控制器不用的动画（改名之后、加副本之前）
        sd.animations = [a for a in sd.animations if a.name not in ANIM_DROP]
        if "hurt_idle" in _STRIP:
            # 诊断：把 hurt 的数据替换为 idle_loop 的副本（定位崩溃是否在 hurt 数据本身）
            idle = next(a for a in sd.animations if a.name == "idle_loop")
            clone = copy.copy(idle)
            clone.name = "hurt"
            sd.animations = [a for a in sd.animations if a.name != "hurt"] + [clone]
        for new_name, src_name in ANIM_COPIES:
            src = None
            for a in sd.animations:
                if a.name == src_name:
                    src = a
                    break
            if src is None:
                print("copy source animation not found: %r (skipped)" % src_name)
                continue
            clone = copy.copy(src)
            clone.name = new_name
            sd.animations.append(clone)
    # 兜底：游戏可能播放的所有动画名必须存在（缺失则用 idle_loop 副本占位，
    # only_idle 模式下这些副本会被清空成空动画）
    required = ["idle_loop", "attack", "hurt", "die", "relaxed_loop", "cast", "low_health_loop"]
    existing = {a.name for a in sd.animations}
    base = next(a for a in sd.animations if a.name == "idle_loop")
    for rn in required:
        if rn not in existing:
            clone = copy.copy(base)
            clone.name = rn
            sd.animations.append(clone)
            print("added missing animation %r (idle clone)" % rn)
    # 静态形态烘焙：把 idle_loop 第 0 帧的附件状态写入槽位默认值，
    # 使"不播动画的静态骨架"也呈现 idle 第 0 帧的外观（隐藏备用手/备选部件）。
    if "static_pose" in _STRIP:
        idle = next((a for a in sd.animations if a.name == "idle_loop"), None)
        if idle is None:
            raise ValueError("static_pose: no idle_loop animation")
        att_states = {}
        for tl in idle.timelines:
            if isinstance(tl, AttachmentTimeline):
                att_states[tl.slot_index] = tl.attachment_names[0]
        n_null = 0
        for si, nm in att_states.items():
            sd.slots[si].attachment_name = nm or ""
            if not nm:
                n_null += 1
        print("static_pose: baked idle frame-0 attachment states, slots=%d (NULL=%d)" %
              (len(att_states), n_null))
        # 骨骼第 0 帧烘焙：setup 姿态是 T-pose（手臂外张），idle 第 0 帧才是站姿
        bone_states = {}
        for tl in idle.timelines:
            if isinstance(tl, RotateTimeline):
                bone_states.setdefault(tl.bone_index, {})["rotation"] = tl.frames[1]
            elif isinstance(tl, TranslateTimeline):
                d = bone_states.setdefault(tl.bone_index, {})
                d["x"] = tl.frames[1]
                d["y"] = tl.frames[2]
            elif isinstance(tl, ScaleTimeline):
                d = bone_states.setdefault(tl.bone_index, {})
                d["scale_x"] = tl.frames[1]
                d["scale_y"] = tl.frames[2]
            elif isinstance(tl, ShearTimeline):
                d = bone_states.setdefault(tl.bone_index, {})
                d["shear_x"] = tl.frames[1]
                d["shear_y"] = tl.frames[2]
        for bi, st in bone_states.items():
            b = sd.bones[bi]
            if "rotation" in st:
                b.rotation = st["rotation"]
            if "x" in st:
                b.x = st["x"]
            if "y" in st:
                b.y = st["y"]
            if "scale_x" in st:
                b.scale_x = st["scale_x"]
            if "scale_y" in st:
                b.scale_y = st["scale_y"]
            if "shear_x" in st:
                b.shear_x = st["shear_x"]
            if "shear_y" in st:
                b.shear_y = st["shear_y"]
        print("static_pose: baked idle frame-0 bone transforms, bones=%d" % len(bone_states))
    return [a.name for a in sd.animations]


def write_skeleton_data(sd: SkeletonData) -> bytes:
    w = _Writer()
    ordered_skins = _ordered_skins(sd)
    anim_groups = [_group_animation(sd, ordered_skins, anim) for anim in sd.animations]
    pool = _collect_pool(sd, ordered_skins, anim_groups)
    pool.locked = True

    # 1. 头：8 字节零 hash
    for _ in range(8):
        w.u8(0)
    # 2. 版本串（版本门 startsWith("4.2")）
    w.string(VERSION)
    # 3. 5 个大端 float：x, y, width, height, referenceScale
    w.f32(getattr(sd, "x", 0.0) or 0.0)
    w.f32(getattr(sd, "y", 0.0) or 0.0)
    w.f32(getattr(sd, "width", 0.0) or 0.0)
    w.f32(getattr(sd, "height", 0.0) or 0.0)
    w.f32(1.0)
    # 4. nonessential = False
    w.u8(0)

    # 5. 字符串池
    w.varint(len(pool.strings))
    for s in pool.strings:
        w.string(s)

    # 6. 骨骼
    w.varint(len(sd.bones))
    for i, b in enumerate(sd.bones):
        w.string(b.name)
        if i > 0:
            w.varint(b.parent.index if b.parent is not None else 0)
        w.f32(b.rotation)
        w.f32(b.x)
        w.f32(b.y)
        w.f32(b.scale_x)
        w.f32(b.scale_y)
        w.f32(b.shear_x)
        w.f32(b.shear_y)
        w.f32(b.length)
        w.varint(int(b.transform_mode) if b.transform_mode is not None else 0)  # inherit
        w.u8(1 if b.skin_required else 0)

    # 7. 槽位
    w.varint(len(sd.slots))
    for s in sd.slots:
        w.string(s.name)
        w.varint(s.bone_data.index)
        w.color(s.color)
        w.u8(0xFF)
        w.u8(0xFF)
        w.u8(0xFF)
        w.u8(0xFF)  # 无暗色
        w.varint(pool.add(s.attachment_name))
        w.varint(int(s.blend_mode) if s.blend_mode is not None else 0)

    # 8. IK 约束
    w.varint(len(sd.ik_constraints))
    for c in sd.ik_constraints:
        w.string(c.name)
        w.varint(c.order)
        w.varint(len(c.bones))
        for b in c.bones:
            w.varint(b.index)
        w.varint(c.target.index if c.target is not None else 0)
        flags = 0
        if c.skin_required:
            flags |= 1
        if _bend_positive(c.bend_direction):
            flags |= 2
        if c.compress:
            flags |= 4
        if c.stretch:
            flags |= 8
        if c.uniform:
            flags |= 16
        if c.mix != 1:
            flags |= 32 | 64
        if c.softness != 0:
            flags |= 128
        w.u8(flags)
        if flags & 32 and flags & 64:
            w.f32(c.mix)
        if flags & 128:
            w.f32(c.softness)

    # 9. 变换约束（4 mix 展开为 6 值；offset_* 字段名一致）
    w.varint(len(sd.transform_constraints))
    for c in sd.transform_constraints:
        w.string(c.name)
        w.varint(c.order)
        w.varint(len(c.bones))
        for b in c.bones:
            w.varint(b.index)
        w.varint(c.target.index if c.target is not None else 0)
        flags1 = 0
        if c.skin_required:
            flags1 |= 1
        if c.local:
            flags1 |= 2
        if c.relative:
            flags1 |= 4
        if c.offset_rotation != 0:
            flags1 |= 8
        if c.offset_x != 0:
            flags1 |= 16
        if c.offset_y != 0:
            flags1 |= 32
        if c.offset_scale_x != 0:
            flags1 |= 64
        if c.offset_scale_y != 0:
            flags1 |= 128
        w.u8(flags1)
        if flags1 & 8:
            w.f32(c.offset_rotation)
        if flags1 & 16:
            w.f32(c.offset_x)
        if flags1 & 32:
            w.f32(c.offset_y)
        if flags1 & 64:
            w.f32(c.offset_scale_x)
        if flags1 & 128:
            w.f32(c.offset_scale_y)
        flags2 = 0
        if c.offset_shear_y != 0:
            flags2 |= 1
        if c.rotate_mix != 0:
            flags2 |= 2
        if c.translate_mix != 0:
            flags2 |= 4 | 8  # mixX 与 mixY 同源 translate
        if c.scale_mix != 0:
            flags2 |= 16 | 32  # mixScaleX 与 mixScaleY 同源 scale
        if c.shear_mix != 0:
            flags2 |= 64
        w.u8(flags2)
        if flags2 & 1:
            w.f32(c.offset_shear_y)
        if flags2 & 2:
            w.f32(c.rotate_mix)
        if flags2 & 4:
            w.f32(c.translate_mix)
        if flags2 & 8:
            w.f32(c.translate_mix)
        if flags2 & 16:
            w.f32(c.scale_mix)
        if flags2 & 32:
            w.f32(c.scale_mix)
        if flags2 & 64:
            w.f32(c.shear_mix)

    # 10. 路径约束
    w.varint(len(sd.path_constraints))
    for c in sd.path_constraints:
        w.string(c.name)
        w.varint(c.order)
        w.u8(1 if c.skin_required else 0)
        w.varint(len(c.bones))
        for b in c.bones:
            w.varint(b.index)
        w.varint(c.target.index if c.target is not None else 0)
        flags = (int(c.position_mode) if c.position_mode is not None else 0) \
            | ((int(c.spacing_mode) if c.spacing_mode is not None else 0) << 2) \
            | ((int(c.rotate_mode) if c.rotate_mode is not None else 0) << 4)
        if c.offset_rotation != 0:
            flags |= 128
        w.u8(flags)
        if flags & 128:
            w.f32(c.offset_rotation)
        w.f32(c.position)
        w.f32(c.spacing)
        w.f32(c.rotate_mix)
        w.f32(c.translate_mix)  # mixX
        w.f32(c.translate_mix)  # mixY

    # 11. 物理约束：0
    w.varint(0)

    # 12. 默认皮肤（必须写全，slotCount=0 会让游戏得到 NULL 皮肤）
    default_skin = ordered_skins[0] if ordered_skins else None
    if default_skin is not None:
        _write_skin(w, sd, pool, ordered_skins, default_skin, is_default=True)
    else:
        w.varint(0)
    # 13. 其余皮肤
    others = ordered_skins[1:]
    w.varint(len(others))
    for skin in others:
        _write_skin(w, sd, pool, ordered_skins, skin, is_default=False)

    # 14. 事件
    w.varint(len(sd.events))
    for e in sd.events:
        w.string(e.name)
        w.varint_signed(e.int_value)
        w.f32(e.float_value)
        if e.string_value:
            w.string(e.string_value)
        else:
            w.varint(0)
        w.string("")  # audioPath 空串 → 无 volume/balance

    # 15. 动画
    w.varint(len(sd.animations))
    for anim, g in zip(sd.animations, anim_groups):
        _write_animation(w, sd, pool, anim, g)

    return bytes(w.buf)


# ---------------------------------------------------------------------------
# 自检：fork 格式语义从 offset 0 走读输出直到 EOF（复用 fork_read.P）
# ---------------------------------------------------------------------------
def _read_fork_info(data: bytes) -> dict:
    p = _ForkP(data, 0)
    p.int_be()
    p.int_be()
    version = p.string()
    for _ in range(5):
        p.f32()
    noness = p.boolean()
    if noness:
        p.f32()
        p.string()
        p.string()
    n = p.varint()
    pool = [p.string() for _ in range(n)]
    skel = {"pool": pool}

    nb = p.varint()
    for i in range(nb):
        p.string()
        if i > 0:
            p.varint()
        for _ in range(8):
            p.f32()
        p.varint()
        p.boolean()

    ns = p.varint()
    for _ in range(ns):
        p.string()
        p.varint()
        for _ in range(8):
            p.byte()  # color + dark
        p.string_ref(pool)
        p.varint()

    for _ in range(p.varint()):  # ik
        p.string()
        p.varint()
        n2 = p.varint()
        for _ in range(n2):
            p.varint()
        p.varint()
        fl = p.byte()
        if fl & 32 and fl & 64:
            p.f32()
        if fl & 128:
            p.f32()

    for _ in range(p.varint()):  # transform
        p.string()
        p.varint()
        n2 = p.varint()
        for _ in range(n2):
            p.varint()
        p.varint()
        fl = p.byte()
        for bit in (8, 16, 32, 64, 128):
            if fl & bit:
                p.f32()
        fl = p.byte()
        for bit in (1, 2, 4, 8, 16, 32, 64):
            if fl & bit:
                p.f32()

    for _ in range(p.varint()):  # path
        p.string()
        p.varint()
        p.boolean()
        n2 = p.varint()
        for _ in range(n2):
            p.varint()
        p.varint()
        fl = p.byte()
        if fl & 128:
            p.f32()
        p.f32()
        p.f32()
        p.f32()
        p.f32()
        p.f32()

    for _ in range(p.varint()):  # physics（写 0，兜底走读）
        p.string()
        p.varint()
        p.varint()
        fl = p.byte()
        for bit in (2, 4, 8, 16, 32):
            if fl & bit:
                p.f32()
        if fl & 64:
            p.f32()
        p.byte()
        p.f32()
        p.f32()
        p.f32()
        if fl & 128:
            p.f32()
        p.f32()
        p.f32()
        fl = p.byte()
        if fl & 128:
            p.f32()

    ds = _read_fork_skin(p, True, skel, noness)
    skin_attachments = sum(len(atts) for _, atts in ds[1]) if ds else 0
    nsk = p.varint()
    for _ in range(nsk):
        _read_fork_skin(p, False, skel, noness)

    ne = p.varint()
    ev_audio = []
    for _ in range(ne):
        p.string()
        p.svarint()
        p.f32()
        p.string()
        ap = p.string()
        ev_audio.append(bool(ap))
        if ev_audio[-1]:
            p.f32()
            p.f32()
    skel["ev_audio"] = ev_audio

    na = p.varint()
    anim_names = []
    for _ in range(na):
        anim_names.append(p.string())
        _read_fork_animation(p, skel)

    return {"version": version, "bones": nb, "slots": ns,
            "skin_attachments": skin_attachments, "skins": nsk,
            "events": ne, "animations": anim_names,
            "remaining": len(data) - p.o}


def _read_fork_skin(p, default, skel, noness):
    """fork_read.read_skin 的无打印副本（返回 (name, slots) 或 None）。"""
    if default:
        slot_count = p.varint()
        if slot_count == 0:
            return None
        name = "default"
    else:
        name = p.string()
        if noness:
            for _ in range(4):
                p.byte()
        for _ in range(5):  # bones, ik, transform, path, physics
            n = p.varint()
            for _ in range(n):
                p.varint()
        slot_count = p.varint()
    slots = []
    for _ in range(slot_count):
        slot_index = p.varint()
        atts = []
        for _ in range(p.varint()):
            aname = p.string_ref(skel["pool"])
            _read_fork_attachment(p, skel, noness)
            atts.append(aname)
        slots.append((slot_index, atts))
    return (name, slots)


def _read_fork_attachment(p, skel, noness):
    """fork_read.read_attachment 的无打印副本（只走读，不返回数据）。"""
    flags = p.byte()
    if flags & 8:
        p.string_ref(skel["pool"])
    atype = flags & 0x7
    if atype == 0:  # region
        if flags & 16:
            p.string_ref(skel["pool"])
        if flags & 32:
            for _ in range(4):
                p.byte()
        if flags & 64:
            p.varint()
            p.varint()
            p.varint()
            p.varint()
        if flags & 128:
            p.f32()
        for _ in range(6):
            p.f32()
    elif atype == 1:  # bbox
        _read_fork_vertices(p, (flags & 16) != 0)
        if noness:
            for _ in range(4):
                p.byte()
    elif atype == 2:  # mesh
        if flags & 16:
            p.string_ref(skel["pool"])
        if flags & 32:
            for _ in range(4):
                p.byte()
        if flags & 64:
            p.varint()
            p.varint()
            p.varint()
            p.varint()
        hull = p.varint()
        vl = _read_fork_vertices(p, (flags & 128) != 0)
        for _ in range(vl):
            p.f32()
        tri_count = (vl - hull - 2) * 3
        for _ in range(tri_count):
            p.varint()
        if noness:
            ec = p.varint()
            for _ in range(ec):
                p.varint()
            p.f32()
            p.f32()
    elif atype == 3:  # linkedmesh
        if flags & 16:
            p.string_ref(skel["pool"])
        if flags & 32:
            for _ in range(4):
                p.byte()
        if flags & 64:
            p.varint()
            p.varint()
            p.varint()
            p.varint()
        p.varint()
        p.string_ref(skel["pool"])
        if noness:
            p.f32()
            p.f32()
    elif atype == 4:  # path
        vl = _read_fork_vertices(p, (flags & 64) != 0)
        for _ in range(vl // 6):
            p.f32()
        if noness:
            for _ in range(4):
                p.byte()
    elif atype == 5:  # point
        p.f32()
        p.f32()
        p.f32()
        if noness:
            for _ in range(4):
                p.byte()
    elif atype == 6:  # clipping
        p.varint()
        _read_fork_vertices(p, (flags & 16) != 0)
        if noness:
            for _ in range(4):
                p.byte()
    else:
        raise ValueError("bad attachment type %d at %d" % (atype, p.o))


def _read_fork_vertices(p, weighted) -> int:
    """fork_read.read_vertices 的无打印副本（返回 vl=顶点数×2）。"""
    vc = p.varint()
    vl = vc * 2
    if not weighted:
        for _ in range(vl):
            p.f32()
        return vl
    for _ in range(vc):
        bc = p.varint()
        for _ in range(bc):
            p.varint()
            p.f32()
            p.f32()
            p.f32()
    return vl


def _read_fork_animation(p, skel) -> None:
    p.varint()  # numTimelines（读端忽略）
    # slot timelines
    for _ in range(p.varint()):
        p.varint()  # slot index
        for _ in range(p.varint()):
            ttype = p.byte()
            fc = p.varint()
            if ttype == 0:
                for _ in range(fc):
                    p.f32()
                    p.string_ref(skel["pool"])
            elif ttype in (1, 2, 3, 4, 5):
                p.varint()  # bezier count
                nvals = {1: 4, 2: 3, 3: 7, 4: 6, 5: 1}[ttype]
                p.f32()
                for _ in range(nvals):
                    p.byte()
                for _ in range(fc - 1):
                    p.f32()
                    for _ in range(nvals):
                        p.byte()
                    c = p.byte()
                    if c == 2:
                        for _ in range(nvals * 4):
                            p.f32()
            else:
                raise ValueError("bad slot timeline type %d at %d" % (ttype, p.o))
    # bone timelines
    for _ in range(p.varint()):
        p.varint()
        for _ in range(p.varint()):
            ttype = p.byte()
            fc = p.varint()
            if ttype == 10:
                for _ in range(fc):
                    p.f32()
                    p.byte()
                continue
            p.varint()  # bezier count
            if ttype in (1, 4, 7):
                p.f32()
                p.f32()
                p.f32()
                for _ in range(fc - 1):
                    p.f32()
                    p.f32()
                    p.f32()
                    c = p.byte()
                    if c == 2:
                        for _ in range(8):
                            p.f32()
            elif ttype in (0, 2, 3, 5, 6, 8, 9):
                p.f32()
                p.f32()
                for _ in range(fc - 1):
                    p.f32()
                    p.f32()
                    c = p.byte()
                    if c == 2:
                        for _ in range(4):
                            p.f32()
            else:
                raise ValueError("bad bone timeline type %d at %d" % (ttype, p.o))
    # ik timelines
    for _ in range(p.varint()):
        p.varint()
        fc = p.varint()
        p.varint()  # bezier count
        flags = p.byte()
        p.f32()
        if (flags & 1) and (flags & 2):
            p.f32()
        if flags & 4:
            p.f32()
        for _ in range(fc - 1):
            flags = p.byte()
            p.f32()
            if (flags & 1) and (flags & 2):
                p.f32()
            if flags & 4:
                p.f32()
            if flags & 128:
                for _ in range(8):
                    p.f32()
    # transform timelines
    for _ in range(p.varint()):
        p.varint()
        fc = p.varint()
        p.varint()  # bezier count
        p.f32()
        for _ in range(6):
            p.f32()
        for _ in range(fc - 1):
            p.f32()
            for _ in range(6):
                p.f32()
            c = p.byte()
            if c == 2:
                for _ in range(24):
                    p.f32()
    # path timelines
    for _ in range(p.varint()):
        p.varint()
        for _ in range(p.varint()):
            ttype = p.byte()
            fc = p.varint()
            p.varint()  # bezier count
            if ttype in (0, 1):
                p.f32()
                p.f32()
                for _ in range(fc - 1):
                    p.f32()
                    p.f32()
                    c = p.byte()
                    if c == 2:
                        for _ in range(4):
                            p.f32()
            elif ttype == 2:
                p.f32()
                p.f32()
                p.f32()
                p.f32()
                for _ in range(fc - 1):
                    p.f32()
                    p.f32()
                    p.f32()
                    p.f32()
                    c = p.byte()
                    if c == 2:
                        for _ in range(12):
                            p.f32()
    # physics timelines
    for _ in range(p.varint()):
        p.varint()
        for _ in range(p.varint()):
            ttype = p.byte()
            fc = p.varint()
            if ttype == 8:
                for _ in range(fc):
                    p.f32()
                continue
            p.varint()
            p.f32()
            p.f32()
            for _ in range(fc - 1):
                p.f32()
                p.f32()
                c = p.byte()
                if c == 2:
                    for _ in range(4):
                        p.f32()
    # attachment timelines (deform/sequence)
    for _ in range(p.varint()):
        p.varint()  # skin index
        for _ in range(p.varint()):
            p.varint()  # slot index
            for _ in range(p.varint()):
                p.string_ref(skel["pool"])
                ttype = p.byte()
                fc = p.varint()
                if ttype == 0:  # deform
                    p.varint()  # bezier count
                    p.f32()  # 首帧 time（循环外）
                    for f in range(fc):
                        end = p.varint()
                        if end:
                            p.varint()  # start
                            for _ in range(end):
                                p.f32()
                        if f < fc - 1:
                            p.f32()
                            c = p.byte()
                            if c == 2:
                                for _ in range(4):
                                    p.f32()
                elif ttype == 1:  # sequence
                    for _ in range(fc):
                        p.f32()
                        p.int_be()
                        p.f32()
    # draw order
    dc = p.varint()
    if dc > 0:
        for _ in range(dc):
            p.f32()
            oc = p.varint()
            for _ in range(oc):
                p.varint()
                p.varint()
    # events
    ec = p.varint()
    if ec > 0:
        for _ in range(ec):
            p.f32()
            eidx = p.varint()
            p.svarint()
            p.f32()
            p.string()
            if skel["ev_audio"][eidx]:
                p.f32()
                p.f32()


# ---------------------------------------------------------------------------
# CLI
# ---------------------------------------------------------------------------
def main(argv):
    try:
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    except Exception:
        pass
    if len(argv) != 2:
        print("usage: py -3 tools\\spine_binwriter.py <input_arknights.skel> <output.skel>")
        return 2
    src, dst = argv

    with open(src, "rb") as f:
        data = f.read()

    # 阿基源文件浮点/16位/32位整数 = 大端（spine-asset 默认小端会解析出垃圾坐标）。
    # 实测验证：region x/y/width/height/rotation、mesh uv、三角形索引全部大端才正常。
    _patch_source_reader_big_endian()

    sd = SkeletonBinary().read_skeleton_data(data)
    print("源: bones=%d slots=%d skins=%d events=%d animations=%s" % (
        len(sd.bones), len(sd.slots), len(sd.skins), len(sd.events),
        [a.name for a in sd.animations]))

    final_names = apply_animation_mapping(sd)
    out = write_skeleton_data(sd)
    with open(dst, "wb") as f:
        f.write(out)

    info = _read_fork_info(out)
    header = out[:16]
    print("输出大小: %d bytes -> %s" % (len(out), dst))
    print("前 16 字节: %s" % " ".join("%02X" % b for b in header))
    print("自检读取: version=%r bones=%d slots=%d skin_attachments=%d skins=%d events=%d remaining=%d" % (
        info["version"], info["bones"], info["slots"], info["skin_attachments"],
        info["skins"], info["events"], info["remaining"]))
    print("自检读取动画 (%d):" % len(info["animations"]))
    for i, name in enumerate(info["animations"]):
        print("  [%d] %s" % (i, name))
    print("最终动画列表 (n=%d): %s" % (len(final_names), final_names))

    default_entries = 0
    ordered_skins = _ordered_skins(sd)
    if ordered_skins:
        default_entries = len(ordered_skins[0].attachments)

    ok = True
    if info["version"] != VERSION:
        print("FAIL: version=%r != %r" % (info["version"], VERSION))
        ok = False
    if header[:8] != b"\x00" * 8 or header[8:15] != b"\x07" + VERSION.encode("ascii"):
        print("FAIL: header bytes != 00*8 + 07 34 2E 32 2E 34 33")
        ok = False
    if info["bones"] != len(sd.bones):
        print("FAIL: bone count %d != source %d" % (info["bones"], len(sd.bones)))
        ok = False
    if info["slots"] != len(sd.slots):
        print("FAIL: slot count %d != source %d" % (info["slots"], len(sd.slots)))
        ok = False
    if info["skin_attachments"] != default_entries:
        print("FAIL: skin attachments %d != source %d" % (info["skin_attachments"], default_entries))
        ok = False
    if info["animations"] != final_names:
        print("FAIL: animation names mismatch")
        ok = False
    if info["remaining"] != 0:
        print("FAIL: trailing bytes %d" % info["remaining"])
        ok = False
    print("自检: %s" % ("PASS" if ok else "FAIL"))
    return 0 if ok else 1


if __name__ == "__main__":
    try:
        sys.exit(main(sys.argv[1:]))
    except Exception:  # noqa: BLE001
        import traceback
        traceback.print_exc()
        sys.exit(1)
