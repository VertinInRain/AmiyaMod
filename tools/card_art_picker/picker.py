#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""阿米娅卡图选择器：把五个卡图文件夹的图整合为候选池，逐张显示卡牌
（名字/稀有度/效果），右侧展示所有未使用的卡图缩略图；点选一张即记录配对并进入
下一张。进度自动保存（progress.json），全部完成后输出 card_art_map.json。
用法: py -3 tools/card_art_picker/picker.py
"""
import hashlib
import json
import os
import sys
import tkinter as tk
from tkinter import filedialog, messagebox
from tkinter import font as tkfont

from PIL import Image, ImageTk

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, "..", ".."))
CARDS_JSON = os.path.join(HERE, "cards.json")
PROGRESS_JSON = os.path.join(HERE, "progress.json")
FINAL_JSON = os.path.join(HERE, "card_art_map.json")
THUMB_DIR = os.path.join(HERE, "thumbs")
ART_DIRS = ["阿米娅卡图", "阿米娅卡图候选", "阿米娅卡图第三批", "阿米娅卡图第四批", "阿米娅精选卡图"]
IMG_EXTS = {".png", ".jpg", ".jpeg", ".webp"}
THUMB_SIZE = 200
RARITY_COLOR = {"基础": "#b0b0b0", "普通": "#9e9e9e", "罕见": "#4f9eff", "稀有": "#ffc93c", "远古": "#ff7b4f", "衍生": "#b0b0b0"}


def rel_path(p):
    return os.path.relpath(p, ROOT)


def scan_images():
    paths = []
    for d in ART_DIRS:
        base = os.path.join(ROOT, d)
        if not os.path.isdir(base):
            continue
        for dirpath, _, files in os.walk(base):
            for fn in files:
                if os.path.splitext(fn)[1].lower() in IMG_EXTS:
                    paths.append(os.path.normpath(os.path.join(dirpath, fn)))
    return sorted(set(paths))


def thumb_file(p):
    return os.path.join(THUMB_DIR, hashlib.md5(p.encode("utf-8")).hexdigest() + ".png")


def build_thumbs(paths, status_cb):
    """生成缩略图缓存（一次性）；status_cb(done, total)。"""
    os.makedirs(THUMB_DIR, exist_ok=True)
    todo = [p for p in paths if not os.path.exists(thumb_file(p))]
    total = len(todo)
    for i, p in enumerate(todo):
        try:
            im = Image.open(p).convert("RGB")
            im.thumbnail((THUMB_SIZE, THUMB_SIZE))
            im.save(thumb_file(p))
        except Exception as e:
            print("thumb failed %s: %r" % (p, e))
        status_cb(i + 1, total)


class App:
    def __init__(self, root):
        self.root = root
        root.title("阿米娅卡图选择器")
        root.geometry("1500x900")
        self.cards = self.load_cards()
        self.images = scan_images()
        self.progress = self.load_progress()
        self.done = set(self.progress["order"])
        self.current = None
        self.photos = {}

        # 缩略图缓存（一次性，带进度条）
        self.status_var = tk.StringVar()
        top = tk.Label(root, textvariable=self.status_var, anchor="w", padx=10)
        top.pack(fill="x")
        self.status_var.set("正在生成缩略图缓存…")
        root.update_idletasks()
        build_thumbs(self.images, self.status_cb)
        self.load_photos()

        body = tk.Frame(root)
        body.pack(fill="both", expand=True)

        # 左侧：卡牌信息
        self.left = tk.Frame(body, width=440)
        self.left.pack(side="left", fill="y", padx=12, pady=8)
        self.left.pack_propagate(False)
        self.progress_label = tk.Label(self.left, text="", anchor="w", justify="left",
                                       font=("Microsoft YaHei", 11))
        self.progress_label.pack(fill="x", pady=(0, 8))
        self.title_label = tk.Label(self.left, text="", anchor="w", justify="left",
                                    font=("Microsoft YaHei", 20, "bold"))
        self.title_label.pack(fill="x")
        self.meta_label = tk.Label(self.left, text="", anchor="w", justify="left",
                                   font=("Microsoft YaHei", 12))
        self.meta_label.pack(fill="x", pady=(2, 8))
        self.desc_label = tk.Label(self.left, text="", anchor="nw", justify="left",
                                   wraplength=410, font=("Microsoft YaHei", 12))
        self.desc_label.pack(fill="both", expand=True)

        # 右侧：缩略图网格
        right = tk.Frame(body)
        right.pack(side="left", fill="both", expand=True)
        self.canvas = tk.Canvas(right, highlightthickness=0)
        self.vbar = tk.Scrollbar(right, orient="vertical", command=self.canvas.yview)
        self.canvas.configure(yscrollcommand=self.vbar.set)
        self.vbar.pack(side="right", fill="y")
        self.canvas.pack(side="left", fill="both", expand=True)
        self.grid_frame = tk.Frame(self.canvas)
        self.grid_window = self.canvas.create_window((0, 0), window=self.grid_frame, anchor="nw")
        self.grid_frame.bind("<Configure>", lambda e: self.canvas.configure(scrollregion=self.canvas.bbox("all")))
        self.canvas.bind("<Configure>", lambda e: self.canvas.itemconfigure(self.grid_window, width=e.width))
        self.canvas.bind_all("<MouseWheel>", self.on_wheel)

        # 底部按钮
        bar = tk.Frame(root)
        bar.pack(fill="x", pady=6)
        tk.Button(bar, text="撤销上一张", command=self.undo, width=14).pack(side="left", padx=8)
        tk.Button(bar, text="跳过这张", command=self.skip, width=14).pack(side="left", padx=8)
        tk.Button(bar, text="添加图片文件夹…", command=self.add_folder, width=18).pack(side="left", padx=8)
        tk.Button(bar, text="保存并退出", command=self.quit_save, width=14).pack(side="right", padx=8)

        self.next_card()

    # ---- 数据 ----
    def load_cards(self):
        with open(CARDS_JSON, "r", encoding="utf-8") as f:
            return json.load(f)

    def load_progress(self):
        if os.path.exists(PROGRESS_JSON):
            with open(PROGRESS_JSON, "r", encoding="utf-8") as f:
                p = json.load(f)
                return {"mapping": p.get("mapping", {}), "skipped": list(p.get("skipped", [])),
                        "order": list(p.get("order", []))}
        return {"mapping": {}, "skipped": [], "order": []}

    def save_progress(self):
        with open(PROGRESS_JSON, "w", encoding="utf-8") as f:
            json.dump(self.progress, f, ensure_ascii=False, indent=1)

    def status_cb(self, done, total):
        if total == 0:
            self.status_var.set("缩略图缓存就绪（%d 张图）" % len(self.images))
            return
        self.status_var.set("正在生成缩略图缓存 %d/%d …（首次较慢，之后秒开）" % (done, total))
        self.root.update_idletasks()

    def load_photos(self):
        for p in self.images:
            try:
                self.photos[p] = ImageTk.PhotoImage(Image.open(thumb_file(p)))
            except Exception:
                pass

    # ---- 流程 ----
    def used_images(self):
        return set(self.progress["mapping"].values())

    def next_card(self):
        self.current = None
        for c in self.cards:
            if c["id"] not in self.done:
                self.current = c
                break
        if self.current is None:
            self.finish()
            return
        self.render_card()
        self.render_grid()

    def render_card(self):
        c = self.current
        done_count = len([x for x in self.done if x in self.progress["mapping"]])
        remaining = len(self.cards) - len(self.done)
        self.progress_label.config(text="已配对 %d 张 · 剩余 %d 张（含已跳过 %d 张）" %
                                  (done_count, remaining, len(self.progress["skipped"])))
        self.title_label.config(text=c["title"])
        color = RARITY_COLOR.get(c["rarity_cn"], "#ffffff")
        tag_txt = ("  词条：" + "/".join(c["tags"])) if c["tags"] else ""
        self.meta_label.config(
            text="%s · %s · %d 费 · 目标：%s%s" % (c["rarity_cn"], c["type_cn"], c["cost"], c["target_cn"], tag_txt),
            fg=color)
        self.desc_label.config(text="效果：\n" + c["desc"])

    def pool_images(self):
        used = self.used_images()
        return [p for p in self.images if p not in used]

    def render_grid(self):
        for w in self.grid_frame.winfo_children():
            w.destroy()
        pool = self.pool_images()
        self.status_var.set("候选卡图：%d 张未使用（点击缩略图 = 选给「%s」）" % (len(pool), self.current["title"]))
        cols = max(1, self.canvas.winfo_width() // (THUMB_SIZE + 40)) if self.canvas.winfo_width() > 100 else 4
        for i, p in enumerate(pool):
            cell = tk.Frame(self.grid_frame, padx=6, pady=6)
            cell.grid(row=i // cols, column=i % cols, sticky="nw")
            if p in self.photos:
                lbl = tk.Label(cell, image=self.photos[p], cursor="hand2")
            else:
                lbl = tk.Label(cell, text="(无缩略图)", width=22, height=10, cursor="hand2")
            lbl.pack()
            name = os.path.basename(p)
            folder = os.path.basename(os.path.dirname(p))
            try:
                w, h = Image.open(p).size
                orient = "横版" if w > h else ("方形" if w == h else "竖版")
                size_txt = "%dx%d" % (w, h)
            except Exception:
                orient, size_txt = "", ""
            tk.Label(cell, text="%s\n[%s] %s %s" % (name, folder, size_txt, orient),
                     font=("Microsoft YaHei", 8), anchor="n", justify="center",
                     wraplength=THUMB_SIZE).pack()
            for wgt in (lbl, cell):
                wgt.bind("<Button-1>", lambda e, img=p: self.assign(img))

    def assign(self, img):
        c = self.current
        self.progress["mapping"][c["id"]] = rel_path(img)
        if c["id"] in self.progress["skipped"]:
            self.progress["skipped"].remove(c["id"])
        self.progress["order"].append(c["id"])
        self.done.add(c["id"])
        self.save_progress()
        self.next_card()

    def skip(self):
        c = self.current
        self.progress["skipped"].append(c["id"])
        self.progress["order"].append(c["id"])
        self.done.add(c["id"])
        self.save_progress()
        self.next_card()

    def undo(self):
        if not self.progress["order"]:
            messagebox.showinfo("撤销", "没有可撤销的操作")
            return
        last = self.progress["order"].pop()
        self.progress["mapping"].pop(last, None)
        if last in self.progress["skipped"]:
            self.progress["skipped"].remove(last)
        self.done.discard(last)
        self.save_progress()
        self.current = None
        self.next_card()

    def add_folder(self):
        d = filedialog.askdirectory(title="选择包含卡图的文件夹")
        if not d:
            return
        added = 0
        for dirpath, _, files in os.walk(d):
            for fn in files:
                if os.path.splitext(fn)[1].lower() in IMG_EXTS:
                    p = os.path.normpath(os.path.join(dirpath, fn))
                    if p not in self.images:
                        self.images.append(p)
                        added += 1
        if added:
            build_thumbs(self.images, self.status_cb)
            self.load_photos()
            messagebox.showinfo("已添加", "新增 %d 张图片" % added)
            if self.current:
                self.render_grid()

    def finish(self):
        used = self.used_images()
        skipped = list(self.progress["skipped"])
        with open(FINAL_JSON, "w", encoding="utf-8") as f:
            json.dump({"mapping": self.progress["mapping"]}, f, ensure_ascii=False, indent=1)
        msg = "全部 %d 张卡处理完毕！\n已配对 %d 张，跳过 %d 张。\n映射已写入 card_art_map.json" % (
            len(self.cards), len(self.progress["mapping"]), len(skipped))
        if skipped:
            names = [next(c["title"] for c in self.cards if c["id"] == s) for s in skipped]
            msg += "\n\n跳过的卡：" + "、".join(names)
        self.status_var.set(msg)
        messagebox.showinfo("完成", msg)

    def quit_save(self):
        self.save_progress()
        self.root.destroy()

    def on_wheel(self, event):
        self.canvas.yview_scroll(-1 if event.delta > 0 else 1, "units")


def main():
    root = tk.Tk()
    root.tk.call("tk", "scaling", 1.2)
    App(root)
    root.mainloop()
    return 0


if __name__ == "__main__":
    sys.exit(main())
