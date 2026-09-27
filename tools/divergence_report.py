"""阅读 RitsuLib 的联机状态分歧报告（ritsulib_state_divergence_*.zip）。

用法：
    python tools/divergence_report.py                     # 自动取最新的一份
    python tools/divergence_report.py <zip 路径>
    python tools/divergence_report.py --all               # 汇总所有分歧包（只看牌堆张数差）

输出内容：
  1. 身份/校验和/触发时机（Checksum ID、Context、Role）
  2. 所有 local/remote 不同的字段
  3. 每个牌堆的两端张数对比（手牌/抽牌堆/弃牌堆/消耗堆）—— 分歧最常见的地方
"""
import glob
import os
import re
import sys
import zipfile

LOG_DIR = os.path.join(os.environ.get("APPDATA", ""), "SlayTheSpire2", "logs")
PILE_RE = re.compile(r"^(?P<sec>[A-Za-z][\w\.\[\]]*)\s*$")
KV_RE = re.compile(r"^(?P<key>[^:]+):\s*local=(?P<l>.*?);\s*remote=(?P<r>.*)$")
COUNT_RE = re.compile(r"^Local:\s*(?P<n>\d+)\s*card\(s\);\s*remote:\s*(?P<m>\d+)\s*card\(s\)")


def read_report(path):
    with zipfile.ZipFile(path) as zf:
        return zf.read("state-divergence-report.txt").decode("utf-8", "replace")


def summarize(path, verbose=True):
    text = read_report(path)
    lines = text.splitlines()
    print("=" * 100)
    print(os.path.basename(path))
    print("=" * 100)
    for ln in lines[:18]:
        if ln.strip():
            print("   ", ln.strip()[:180])

    if not verbose:
        return

    section = None
    diffs = []
    piles = []
    for ln in lines:
        s = ln.strip()
        if s and not s.startswith(("Local:", "Remote:", "local:", "remote:")) and ":" not in s[:1] and re.match(r"^[\w\.\[\]]+$", s):
            section = s
        m = COUNT_RE.match(s)
        if m and m.group("n") != m.group("m"):
            piles.append((section, int(m.group("n")), int(m.group("m"))))
        kv = KV_RE.match(s)
        if kv and kv.group("l").strip() != kv.group("r").strip():
            diffs.append((section, kv.group("key"), kv.group("l"), kv.group("r")))

    print("\n-- 牌堆张数不同（section / local / remote）--")
    if not piles:
        print("   （没有）")
    for sec, n, m in piles:
        print("   %-42s local=%s  remote=%s  (差 %+d)" % (sec, n, m, n - m))

    print("\n-- local/remote 不同的字段 --")
    for sec, key, l, r in diffs[:80]:
        print("   [%s] %s" % (sec or "-", key))
        print("        local : %s" % l[:200])
        print("        remote: %s" % r[:200])


def main():
    args = [a for a in sys.argv[1:] if a != "--all"]
    dumps = sorted(glob.glob(os.path.join(LOG_DIR, "ritsulib_state_divergence_*.zip")))
    if not dumps:
        print("没有找到分歧报告（%s）" % LOG_DIR)
        return 1
    show_all = "--all" in sys.argv
    targets = args or ([dumps[-1]] if not show_all else dumps)
    for t in targets:
        summarize(t, verbose=not show_all)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
