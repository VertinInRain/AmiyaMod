# spine-godot 4.2 fork 二进制格式 —— 写出规格（已实证）

游戏 `libspine_godot.windows.template_release.x86_64.dll` = spine-godot 4.2.43 定制 fork。
读端源码 = spine-runtimes 4.2 分支 `spine-cpp/spine-cpp/src/spine/SkeletonBinary.cpp`（与游戏 DLL 反汇编逐条吻合）。
实证验证：`tools/fork_read.py`（本格式的完整走读器）已完整走通游戏自带 `ironclad.spskel`（86骨骼/73槽位/68皮肤槽位）与 Typhon `char_2012_typhon.spskel`（216骨骼/105槽位/25+动画），逐字段一致。

# 0. 总体约定（与标准 3.8 完全不同，逐条落实）

- **整数 int（4 字节，如 hash）与所有 float：大端**（`struct.pack(">f")`）。varint 不变（7bit 小端组）。
- **字符串**：varint(UTF8字节数 + 1) + 字节（无 NUL，读端读 len-1）。空串 → varint 0？否——读端 readString：length==0 → NULL；要写空串必须 varint 1 + 0 字节。事件 stringValue 无值写 varint 0（NULL）。
- **颜色**：4 字节 R,G,B,A（0-255）。
- 版本门：`load_from_file` 的 checkBinary = 跳过 8 字节 hash → 读版本串 → **startsWith("4.2")** 才通过，否则 ERR_INVALID_DATA(30)。**版本串必须写 "4.2.43"**（varint 7 + 6 字符）。写 "3.8.99" 必被拒——这就是之前 err 30 的根因。
- 头 8 字节 hash：读端只存作元数据字符串，**内容任意**（写 8 个 0x00）。

# 1. 骨架整体布局（readSkeletonData 顺序）

1. `int lowHash` + `int highHash`：8 字节（写 0）
2. 版本串 "4.2.43"
3. **5 个大端 float**：x, y, width, height, referenceScale（写 0.0, 0.0, 0.0, 0.0, 1.0）
4. nonessential 1 字节：**写 0**（False）→ 之后所有 nonessential 分支全跳过（不写 fps/imagesPath/audioPath、骨骼颜色/图标/可见、槽位可见、皮肤颜色、bbox/path/point/clipping 颜色、mesh edges/width/height、linkedmesh width/height）
5. varint 字符串池数量 + 池内字符串（顺序 = 首次引用顺序，去重；索引 = 位置+1）。**只有以下场合用池引用（readStringRef）**：槽位默认附件名、皮肤附件名、readAttachment 的 name/path（当用 flag 时）、动画附件时间线名、linkedmesh 父名。骨骼名/槽位名/皮肤名/事件名/约束名/动画名 = **内联 readString，不进池**
6. 骨骼（见 §2）
7. 槽位（见 §3）
8. IK 约束（§4）→ 变换约束（§5）→ 路径约束（§6）
9. 物理约束：varint 0
10. **默认皮肤**（readSkin(default=true)，§7）：varint slotCount（=有附件的槽位数；>0）+ 每槽 [varint 槽索引][varint 附件数][附件×N]——**无皮肤名、无骨骼/约束表**。slotCount 写 0 会让游戏得到 NULL 默认皮肤（附件的槽位无渲染）——源有 default skin 就必须写全
11. 其余皮肤：varint 数量 + 每个 readSkin(default=false)（§7）——阿米娅 1 皮肤 → 写 0
12. linkedmesh 解析段：无数据
13. 事件（§8）
14. 动画（§9）

# 2. 骨骼（每根）

`[名称串][若 i>0: varint 父索引][8×大端float: rotation, x, y, scaleX, scaleY, shearX, shearY, length][varint inherit][byte skinRequired]`
- inherit = spine-asset 的 transform_mode 映射：NORMAL=0, ONLY_TRANSLATION=1, NO_ROTATION_OR_REFLECTION=2, NO_SCALE=3, NO_SCALE_OR_REFLECTION=4（以 spine_asset/v38/Enums.py 实际枚举为准）
- skinRequired = 源骨骼的 skin_required（无该字段写 0）

# 3. 槽位（每槽）

`[名称串][varint 骨骼索引][颜色 4B R,G,B,A×255][4B: a,r,g,b 全 0xFF = 无暗色][varint 附件名池索引(0=空)][varint blendMode]`
- blendMode：Normal=0, Additive=1, Multiply=2, Screen=3
- 源无 dark_color → 写 FF FF FF FF

# 4. IK 约束

`[名称][varint order][varint 骨骼数][各 varint 骨骼索引][varint 目标骨骼索引][byte flags][flags&32: (flags&64: float mix)][flags&128: float softness]`
- flags：1=skinRequired, 2=bendDir>0, 4=compress, 8=stretch, 16=uniform, 32=有mix字段, 64=mix值显式, 128=有softness
- 写策略：mix 非默认(≠1)→置 32|64 并写值；softness≠0→置 128 并写值；bend_direction>0→置 2（源 255 表示 -1 → 不置）

# 5. 变换约束

`[名称][varint order][骨骼列表][目标][flags1][flags1 置位的值按位序 8,16,32,64,128 顺序写 float][flags2][flags2 置位的值按位序 1,2,4,8,16,32,64 顺序写 float]`
- flags1：1=skinRequired, 2=local, 4=relative, 8=offsetRotation, 16=offsetX, 32=offsetY, 64=offsetScaleX, 128=offsetScaleY
- flags2：1=offsetShearY, 2=mixRotate, 4=mixX, 8=mixY, 16=mixScaleX, 32=mixScaleY, 64=mixShearY
- 只写非零/非默认的字段并置对应位；local/relative/skinRequired 按源布尔置位

# 6. 路径约束

`[名称][varint order][byte skinRequired(0/1)][骨骼列表][varint 目标槽位索引][byte flags][flags&128: float offsetRotation][float position][float spacing][float mixRotate][float mixX][float mixY]`
- flags：bit0-1=positionMode, bit2-3=spacingMode, bit4-5=rotateMode, bit7=offsetRotation
- 枚举：PositionMode Fixed=0/Percent=1；SpacingMode Length=0/Fixed=1/Percent=2；RotateMode Tangent=0/Chain=1/ChainScale=2

# 7. 皮肤附件

readAttachment = `[byte flags][flags&8: varint 名称池索引][按类型][flags&16: varint 路径池索引][flags&32: 颜色4B][flags&64: sequence(4×varint)][...]`
- flags：bit0-2=类型(0=region,1=bbox,2=mesh,3=linkedmesh,4=path,5=point,6=clipping), bit3=显式名, bit4=显式路径, bit5=有颜色, bit6=有sequence, bit7=region的rotation/mesh的weighted/path的weighted/bbox|clipping的weighted
- **region**(0)：flags(bit3 写显式名=池索引、bit4 写路径=池索引、bit5 非白写颜色、bit6 sequence 全无→不置、bit7 rotation≠0 写值) + [rotation f32 若置位] + x,y,scaleX,scaleY,width,height 6×f32。顺序：rotation 先于 x,y。
- **bbox**(1)：flags(bit3/bit4 同上、bit7=weighted) + readVertices + 无颜色(nonessential=0)。readVertices：`[varint vertexCount(=len(vertices)//2)][未加权: vertexCount*2 个 f32 坐标；加权: 每顶点 varint boneCount + (varint 骨骼索引 + f32 x + f32 y + f32 weight)×boneCount]`（扁平格式与 spine-asset 的 bones/vertices 一致）
- **mesh**(2)：flags(bit3 名、bit4 路径、bit5 颜色、bit7=weighted) + `[varint hullLength(=源 hull_length//2)]` + readVertices(同上，**vertexCount=全部顶点数**，不是 hull) + uvs f32×(vertexCount*2) + 三角形 varint×((vertexCount*2 − hullLength − 2)×3)（**数量由公式推导，必须精确匹配**；已验证 153/153 网格吻合）+ (nonessential=0 → 无 edges/width/height)
- **linkedmesh**(3)：flags(bit3/4/5、bit7=inheritTimelines) + `[varint 父皮肤索引][varint 父网格名池索引]`（阿米娅 0 个，可不实现）
- **path**(4)：flags(bit3 名、bit4=closed、bit5=constantSpeed、bit6=weighted) + readVertices + lengths f32×(vertexCount*2//6)（无颜色）
- **point**(5)：rotation, x, y 3×f32（无颜色）
- **clipping**(6)：`[varint 结束槽位索引]` + readVertices（无颜色）
- 注意：region/mesh/path 等的**路径**若与附件名相同，读端允许省略（flag 不置）；**为稳妥一律显式写名+路径**（置 bit3|bit4，池里放两个串——同名也放同一个索引即可）

# 8. 事件

`[varint 数量][每事件: 名称串][varint intValue(zigzag=false 的 varint)][float floatValue][stringValue 串(无写 varint 0)][audioPath 串——写空串(varint 1+0字节)则无 volume/balance]`

# 9. 动画（每动画，readAnimation 顺序）

1. 名称串
2. varint numTimelines（读端忽略，写 0 即可）
3. **槽位时间线**（先！）：varint 块数；每块 `[varint 槽索引][varint 时间线数][每时间线: byte 类型 + varint 帧数 + ...]`
   - 类型：0=ATTACHMENT（每帧 f32 time + varint 附件名池索引，无曲线）、1=RGBA（varint bezierCount + 帧: time+4字节色；后续帧: time2+4字节+曲线字节+bezier时 4×4 floats）、2=RGB（3字节、3×4）、3=RGBA2（7字节、7×4）、4=RGB2（6字节、6×4）、5=ALPHA（1字节、1×4）
   - 曲线字节：0=linear 无数据；1=stepped；2=bezier + 每值 4 个 f32(cx1,cy1,cx2,cy2)。后续帧读 time2 在前（与 3.8 相同）
4. **骨骼时间线**：varint 块数；每块 `[varint 骨索引][varint 时间线数][每时间线: byte 类型 + varint 帧数 + ...]`
   - 10=INHERIT：每帧 f32 time + byte inherit，无 bezierCount
   - 其余：varint bezierCount；0=ROTATE(1值) 1=TRANSLATE(2值) 2/3=TRANSLATEX/Y(1值) 4=SCALE(2值) 5/6=SCALEX/Y(1值) 7=SHEAR(2值) 8/9=SHEARX/Y(1值)。帧 = time+值；后续帧 = time2+值+曲线（2值→2 beziers=8 floats）
5. **IK 时间线**：varint 块数；每块 `[varint 约束索引][varint 帧数][varint bezierCount][byte flags][f32 time][flags&1&&flags&2: f32 mix][flags&4: f32 softness]`；后续每帧：`[byte flags][f32 time2][mix2?][softness2?][曲线: flags&64=stepped；flags&128=bezier(2×4 floats)]`
   - flags：1=有mix、2=mix值显式、4=有softness、8=bend>0、16=compress、32=stretch（写策略同约束：mix≠1 置 1|2 写值；softness≠0 置 4 写值；bend/compress/stretch 按源）
6. **变换时间线**：每块 `[索引][帧数][bezierCount][f32 time + 6 值(mixRotate,mixX,mixY,mixScaleX,mixScaleY,mixShearY)]`；后续帧：time2+6值+曲线(bezier=6×4 floats)
7. **路径时间线**：每块 `[varint 索引][varint 时间线数][每时间线: byte 类型 + varint 帧数 + varint bezierCount + 帧]`；类型 0=POSITION(1值) 1=SPACING(1值) 2=MIX(3值)。帧结构同骨骼 1/2 值
8. **物理时间线**：varint 0
9. **附件时间线（deform/sequence）**：varint 皮肤块数；每块 `[varint 皮肤索引][varint 槽组数][每组: varint 槽索引][varint 时间线数][每时间线: varint 附件名池索引 + byte 类型 + varint 帧数 + ...]`
   - 0=DEFORM：varint bezierCount + f32 time（循环外一次）+ 每帧：`[varint end][end>0: varint start + end 个 f32 顶点偏移]` + 每帧(除最后)：f32 time2 + 曲线字节(bezier=4 floats)。**写 end=实际偏移数、start=0、值=源 deform 帧的偏移 floats**（源为偏移量，读端对未加权会自动加基准顶点）
   - 1=SEQUENCE：每帧 f32 time + int(4B大端 modeAndIndex) + f32 delay
10. **drawOrder**：varint 帧数；每帧 `[f32 time][varint offsetsCount][offsetsCount × (varint 槽索引, varint offset)]`——(槽索引, 新位置−原位置) 对，**按槽索引升序**，跳过未变槽（-1/原位）
11. **事件时间线**：varint 帧数；每帧 `[f32 time][varint 事件索引][varint intValue(zigzag)][f32 floatValue][stringValue 串(无写 0)][若该事件 audioPath 非空: volume, balance——阿米娅无 audio]`

# 10. 曲线数据源映射

spine-asset 的时间线 curves 数组 = 每曲线 19 项（BEZIER_SIZE=19）：[类型(0=linear/1=stepped/2=bezier)][18 floats]。**fork 的 bezier 只需每值 4 个 f32 (cx1,cy1,cx2,cy2)**——从源 18 个 float 中按每值取前 4 个（先核对 spine_asset/v38/timelines*.py 的 curves 结构与 CurveTimeline 常量，若 18=6×3 之类的布局需相应提取；通常 3.8 二进制曲线 = 每值 4 floats，18 可能是固定缓冲大小，取前 4×N 个即可）。类型字节按源类型写 0/1/2。

# 11. 自检

复用 `tools/fork_read.py` 的解析逻辑（它是本格式的走读器，已验证可完整解析游戏文件）：写完后从 offset 0 解析输出，逐节走到 EOF 无剩余字节，骨骼/槽位/皮肤/动画名与源一致即 PASS。注意 fork_read.py 当前含 .spskel 文本前置自动定位——自检时直接把 payload 起点传 0。

# 12. 参考实现

- 读端参考（权威）：`tools/fork_read.py`（本仓库，已验证）
- 上游源码：spine-runtimes@4.2 spine-cpp/spine-cpp/src/spine/SkeletonBinary.cpp（jsdelivr 可下）
- 枚举值：BONE_ROTATE=0..SHEARY=9, INHERIT=10；SLOT_ATTACHMENT=0,RGBA=1,RGB=2,RGBA2=3,RGB2=4,ALPHA=5；ATTACHMENT_DEFORM=0,SEQUENCE=1；PATH_POSITION=0,SPACING=1,MIX=2；CURVE_LINEAR=0,STEPPED=1,BEZIER=2
