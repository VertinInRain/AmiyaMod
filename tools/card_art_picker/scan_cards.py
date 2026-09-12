#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""扫描 AmiyaMod/src/Cards/*.cs，生成卡牌清单 JSON（供卡图选择器使用）。
用法: py -3 tools/card_art_picker/scan_cards.py [输出路径]
"""
import json
import os
import re
import sys

SRC_DIR = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "AmiyaMod", "src", "Cards"))
OUT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "cards.json"))

RARITY_CN = {
    "Basic": "基础", "Common": "普通", "Uncommon": "罕见", "Rare": "稀有",
    "Ancient": "远古", "Token": "衍生", "Special": "特殊", "Curse": "诅咒",
}
RARITY_ORDER = {"Basic": 0, "Common": 1, "Uncommon": 2, "Rare": 3, "Special": 4, "Token": 5, "Ancient": 6, "Curse": 7}
TYPE_CN = {"Attack": "攻击", "Skill": "技能", "Power": "能力", "Status": "状态", "Curse": "诅咒"}
TARGET_CN = {"AnyEnemy": "敌人", "Self": "自身", "AllEnemies": "全体敌人", "None": "无目标", "AnyEnemyOrSelf": "敌人或自身"}
TAG_CN = {"DemonLord": "魔王", "Infection": "感染", "Leader": "领袖", "Graveyard": "墓园", "None": ""}

SKIP_FILES = {"BaseAmiyaCard.cs", "AmiyaTags.cs", "PlaceholderArt.cs", "UpgradeLevelVar.cs"}


def extract_class_chunk(src, cls):
    m = re.search(r"(?:public|internal) (?:sealed|abstract|partial)*\s*class\s+" + cls + r"\b", src)
    if not m:
        return src
    start = m.start()
    # 类体：括号配对
    i = src.find("{", start)
    depth = 0
    for j in range(i, len(src)):
        if src[j] == "{":
            depth += 1
        elif src[j] == "}":
            depth -= 1
            if depth == 0:
                return src[start:j + 1]
    return src[start:]


def parse_file(path):
    with open(path, "r", encoding="utf-8-sig") as f:
        src = f.read()
    out = []
    for cm in re.finditer(r"public sealed class (\w+)\s*:\s*BaseAmiyaCard", src):
        cls = cm.group(1)
        chunk = extract_class_chunk(src, cls)
        ctor = re.search(r":\s*base\(\s*(\d+)\s*,\s*CardType\.(\w+)\s*,\s*CardRarity\.(\w+)\s*,\s*TargetType\.(\w+)", chunk)
        if not ctor:
            continue
        cost, ctype, rarity, target = ctor.group(1), ctor.group(2), ctor.group(3), ctor.group(4)
        title = ""
        m = re.search(r'"title"\s*,\s*"([^"]*)"', chunk)
        if m:
            title = m.group(1)
        desc_raw = ""
        m = re.search(r'"description"\s*,\s*"((?:[^"\\]|\\.)*)"', chunk)
        if m:
            desc_raw = m.group(1)
        tags = []
        m = re.search(r"public override AmiyaTag AmiyaTags => ([\w.|]+);", chunk)
        if m:
            for part in m.group(1).split("|"):
                part = part.strip().replace("AmiyaTag.", "")
                if part and part != "None":
                    tags.append(TAG_CN.get(part, part))
        # 基础数值
        bases = {}
        m = re.search(r"new DamageVar\((\d+)m", chunk)
        if m:
            bases["D"] = m.group(1)
        m = re.search(r"new BlockVar\((\d+)m", chunk)
        if m:
            bases["B"] = m.group(1)
        for vm in re.finditer(r'MakeCalculatedVar\("(\w+)",\s*(\d+)', chunk):
            bases[vm.group(1)] = vm.group(2)
        for vm in re.finditer(r'UpgradeLevelVar\("(\w+)",\s*card\s*=>\s*(\d+)m', chunk):
            bases[vm.group(1)] = vm.group(2)
        desc = clean_description(desc_raw, bases)
        out.append({
            "id": cls,
            "file": os.path.basename(path),
            "title": title,
            "cost": int(cost),
            "type": ctype,
            "type_cn": TYPE_CN.get(ctype, ctype),
            "rarity": rarity,
            "rarity_cn": RARITY_CN.get(rarity, rarity),
            "rarity_order": RARITY_ORDER.get(rarity, 99),
            "target": target,
            "target_cn": TARGET_CN.get(target, target),
            "tags": tags,
            "desc": desc,
            "desc_raw": desc_raw,
        })
    return out


def clean_description(s, bases):
    s = s.replace("\\n", "\n").replace("\\\"", "\"")
    # 升级交换 -x-+y+
    s = re.sub(r"-([^-+]+)-\+([^+]+)\+", lambda m: "%s（升级后：%s）" % (m.group(1), m.group(2)), s)
    # {IfUpgraded:show:a|b}
    s = re.sub(r"\{IfUpgraded:show:([^|}]+)\|([^}]+)\}", lambda m: "%s（升级后：%s）" % (m.group(1), m.group(2)), s)
    # 其他 {xxx} → 取内部
    s = re.sub(r"\{([^{}]+)\}", lambda m: m.group(1).split(":")[-1], s)
    # 动态变量 !X! → 基础值
    def repl(m):
        v = bases.get(m.group(1))
        return v if v is not None else m.group(0)
    s = re.sub(r"!(\w+)!", repl, s)
    s = s.replace("#", "").replace("*", "")
    s = re.sub(r"[ \t]+", " ", s)
    return s.strip()


def main():
    out_path = sys.argv[1] if len(sys.argv) > 1 else OUT
    cards = []
    for fn in sorted(os.listdir(SRC_DIR)):
        if not fn.endswith(".cs") or fn in SKIP_FILES:
            continue
        cards.extend(parse_file(os.path.join(SRC_DIR, fn)))
    cards.sort(key=lambda c: (c["rarity_order"], c["type"], c["id"]))
    with open(out_path, "w", encoding="utf-8") as f:
        json.dump(cards, f, ensure_ascii=False, indent=1)
    print("共扫描到 %d 张卡牌 -> %s" % (len(cards), out_path))
    for c in cards[:6]:
        print("  [%s] %s (%s, %s费) %s" % (c["rarity_cn"], c["title"], c["type_cn"], c["cost"], c["desc"][:38]))
    return 0


if __name__ == "__main__":
    sys.exit(main())
