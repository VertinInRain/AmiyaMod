#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""阿米娅卡图裁剪器：
按 card_art_map.json 逐张显示已选卡图，提供"拖动取景框 + 缩放图片"的交互：
  - 普通卡取景框 = 250:190（游戏卡图区域实测比例），输出 250x190 PNG；
  - 远古卡（先古牌）取景框 = 674:427（ancient_portrait_mask_large 实测），输出 674x427；
  - 左键拖动移动图片、滚轮或按钮缩放、双击或"确定"确认裁剪；
  - 进度自动保存（crop_progress.json），支持撤销上一张与中途退出续做。
用法: py -3 tools/card_art_picker/cropper.py
"""
import json
import os
import sys
import tkinter as tk
from tkinter import messagebox

from PIL import Image, ImageOps, ImageTk

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, "..", ".."))
CARDS_JSON = os.path.join(HERE, "cards.json")
MAP_JSON = os.path.join(HERE, "card_art_map.json")
PROGRESS_JSON = os.path.join(HERE, "crop_progress.json")
OUT_DIR = os.path.join(HERE, "cropped")
MANIFEST_JSON = os.path.join(HERE, "crop_done.json")

# 输出尺寸（按用户参照图实测：普通卡=1.png 2349x1785，远古卡=7.png 1728x2425）
NORMAL_W, NORMAL_H = 250, 190        # 普通卡图区域（1.316）
ANCIENT_W, ANCIENT_H = 712, 1000     # 远古卡立绘（竖版 0.712，同 7.png）

FRAME_DISPLAY_H = 560                # 取景框显示高度（宽度按比例）
CANVAS_W, CANVAS_H = 1100, 700


def load_cards():
    with open(CARDS_JSON, "r", encoding="utf-8") as f:
        return json.load(f)


def load_mapping():
    with open(MAP_JSON, "r", encoding="utf-8") as f:
        return json.load(f)["mapping"]


def load_progress():
    if os.path.exists(PROGRESS_JSON):
        with open(PROGRESS_JSON, "r", encoding="utf-8") as f:
            p = json.load(f)
            return {"done": p.get("done", {}), "order": p.get("order", [])}
    return {"done": {}, "order": []}


class Cropper:
    def __init__(self, root):
        self.root = root
        root.title("阿米娅卡图裁剪器")
        root.geometry("1280x860")

        self.cards = {c["id"]: c for c in load_cards()}
        self.mapping = load_mapping()
        self.progress = load_progress()
        # 队列 = 未完成的映射条目；支持多级卡图变体（如 SurvivalOmen_0/_1/_2，对应同名卡的多级立绘）
        self.queue = [cid for cid in self.mapping
                      if cid not in self.progress["done"]
                      and (cid in self.cards
                           or (cid.rsplit("_", 1)[0] in self.cards and cid.rsplit("_", 1)[1].isdigit()))]
        self.current = None
        self.img = None
        self.photo = None
        self.s = 1.0
        self.ox = self.oy = 0.0
        self.out_w = self.out_h = 0
        self.frame_w = self.frame_h = 0
        self.mouse_anchor = None

        top = tk.Frame(root)
        top.pack(fill="x", padx=10, pady=6)
        self.info_label = tk.Label(top, text="", font=("Microsoft YaHei", 13), anchor="w")
        self.info_label.pack(side="left")
        self.pos_label = tk.Label(top, text="", font=("Microsoft YaHei", 10), fg="#888")
        self.pos_label.pack(side="right")

        self.canvas = tk.Canvas(root, width=CANVAS_W, height=CANVAS_H, bg="#202020",
                                highlightthickness=1, highlightbackground="#555")
        self.canvas.pack(padx=10, pady=4)
        self.canvas.bind("<Button-1>", self.on_down)
        self.canvas.bind("<B1-Motion>", self.on_drag)
        self.canvas.bind("<MouseWheel>", self.on_wheel)
        self.canvas.bind("<Double-Button-1>", lambda e: self.confirm())

        bar = tk.Frame(root)
        bar.pack(fill="x", padx=10, pady=6)
        tk.Button(bar, text="缩小 -", command=lambda: self.zoom(0.85), width=10).pack(side="left", padx=4)
        tk.Button(bar, text="放大 +", command=lambda: self.zoom(1.18), width=10).pack(side="left", padx=4)
        tk.Button(bar, text="重置视图", command=self.reset_view, width=12).pack(side="left", padx=4)
        tk.Button(bar, text="撤销上一张", command=self.undo, width=12).pack(side="left", padx=16)
        tk.Button(bar, text="保存并退出", command=self.quit_save, width=12).pack(side="left", padx=4)
        tk.Button(bar, text="确定 → 下一张", command=self.confirm, width=16,
                  bg="#2e7d32", fg="white", font=("Microsoft YaHei", 11, "bold")).pack(side="right", padx=8)

        self.next_card()

    def save_progress(self):
        with open(PROGRESS_JSON, "w", encoding="utf-8") as f:
            json.dump(self.progress, f, ensure_ascii=False, indent=1)

    def next_card(self):
        if self.queue:
            self.current = self.queue[0]
            self.load_card()
        else:
            self.finish()

    def card_of(self, cid):
        if cid in self.cards:
            return self.cards[cid]
        return self.cards.get(cid.rsplit("_", 1)[0])

    def load_card(self):
        c = self.card_of(self.current)
        if c is None:
            self.queue.pop(0)
            self.next_card()
            return
        ancient = c["rarity"] == "Ancient"
        self.out_w, self.out_h = (ANCIENT_W, ANCIENT_H) if ancient else (NORMAL_W, NORMAL_H)
        fscale = FRAME_DISPLAY_H / self.out_h
        self.frame_w = int(self.out_w * fscale)
        self.frame_h = FRAME_DISPLAY_H
        label = c["title"]
        if self.current not in self.cards:
            label += "（等级%s）" % self.current.rsplit("_", 1)[1]
        self.info_label.config(text="%s · %s%s（输出 %dx%d）· 已完成 %d/%d" % (
            label, c["rarity_cn"], " · 远古立绘窗口" if ancient else "",
            self.out_w, self.out_h, len(self.progress["done"]), len(self.mapping)))
        path = os.path.join(ROOT, self.mapping[self.current])
        img = Image.open(path)
        img = ImageOps.exif_transpose(img).convert("RGB")
        self.img = img
        state = self.progress["done"].get(self.current)
        if state and state.get("ow") == self.out_w:
            self.s = state["s"]
            self.ox = state["ox"]
            self.oy = state["oy"]
        else:
            self.reset_view()
        self.redraw()

    def frame_rect(self):
        fx = (CANVAS_W - self.frame_w) / 2
        fy = (CANVAS_H - self.frame_h) / 2
        return fx, fy

    def reset_view(self):
        if self.img is None:
            return
        w, h = self.img.size
        self.s = (self.frame_h * 1.6) / h
        if w * self.s < self.frame_w * 1.1:
            self.s = (self.frame_w * 1.1) / w
        fx, fy = self.frame_rect()
        self.ox = fx + self.frame_w / 2 - (w * self.s) / 2
        self.oy = fy + self.frame_h / 2 - (h * self.s) / 2
        self.clamp_pos()

    def clamp_pos(self):
        if self.img is None:
            return
        w, h = self.img.size
        fx, fy = self.frame_rect()
        self.ox = min(self.ox, fx)
        self.oy = min(self.oy, fy)
        self.ox = max(self.ox, fx + self.frame_w - w * self.s)
        self.oy = max(self.oy, fy + self.frame_h - h * self.s)

    def zoom(self, factor):
        if self.img is None:
            return
        w, h = self.img.size
        fx, fy = self.frame_rect()
        cx = (fx + self.frame_w / 2 - self.ox) / self.s
        cy = (fy + self.frame_h / 2 - self.oy) / self.s
        ns = self.s * factor
        ns = max(ns, self.frame_w / w, self.frame_h / h)
        ns = min(ns, 4.0)
        self.s = ns
        self.ox = fx + self.frame_w / 2 - cx * ns
        self.oy = fy + self.frame_h / 2 - cy * ns
        self.clamp_pos()
        self.redraw()

    def on_wheel(self, event):
        self.zoom(1.12 if event.delta > 0 else 0.9)

    def on_down(self, event):
        self.mouse_anchor = (event.x, event.y, self.ox, self.oy)

    def on_drag(self, event):
        if self.mouse_anchor is None or self.img is None:
            return
        x0, y0, ox0, oy0 = self.mouse_anchor
        self.ox = ox0 + (event.x - x0)
        self.oy = oy0 + (event.y - y0)
        self.clamp_pos()
        self.redraw()

    def redraw(self):
        if self.img is None:
            return
        w, h = self.img.size
        dw = max(1, int(w * self.s))
        dh = max(1, int(h * self.s))
        disp = self.img.resize((dw, dh), Image.LANCZOS)
        self.photo = ImageTk.PhotoImage(disp)
        self.canvas.delete("img")
        self.canvas.create_image(int(self.ox), int(self.oy), image=self.photo, anchor="nw", tags="img")
        self.canvas.tag_lower("img")
        self.canvas.delete("frame")
        fx, fy = self.frame_rect()
        self.canvas.create_rectangle(fx, fy, fx + self.frame_w, fy + self.frame_h,
                                     outline="#ff4444", width=3, tags="frame")
        self.canvas.tag_raise("frame")
        sx = (fx - self.ox) / self.s
        sy = (fy - self.oy) / self.s
        self.pos_label.config(text="原图取景: x=%d y=%d 宽=%d 高=%d（原图 %dx%d）" % (
            max(0, int(sx)), max(0, int(sy)), int(self.frame_w / self.s), int(self.frame_h / self.s), w, h))

    def confirm(self):
        if self.img is None or self.current is None:
            return
        fx, fy = self.frame_rect()
        sx = (fx - self.ox) / self.s
        sy = (fy - self.oy) / self.s
        sw = self.frame_w / self.s
        sh = self.frame_h / self.s
        w, h = self.img.size
        left = max(0, min(w, sx))
        top = max(0, min(h, sy))
        right = max(0, min(w, sx + sw))
        bottom = max(0, min(h, sy + sh))
        crop = self.img.crop((int(left), int(top), int(right), int(bottom)))
        out = crop.resize((self.out_w, self.out_h), Image.LANCZOS)
        os.makedirs(OUT_DIR, exist_ok=True)
        out_path = os.path.join(OUT_DIR, self.current + ".png")
        out.save(out_path)
        self.progress["done"][self.current] = {"s": self.s, "ox": self.ox, "oy": self.oy,
                                               "ow": self.out_w, "file": out_path}
        if self.current not in self.progress["order"]:
            self.progress["order"].append(self.current)
        self.save_progress()
        self.queue.pop(0)
        self.next_card()

    def undo(self):
        if not self.progress["order"]:
            messagebox.showinfo("撤销", "没有可撤销的裁剪")
            return
        last = self.progress["order"].pop()
        self.progress["done"].pop(last, None)
        self.save_progress()
        self.queue.insert(0, last)
        self.next_card()

    def finish(self):
        manifest = {}
        for cid, st in self.progress["done"].items():
            c = self.card_of(cid)
            if c is None:
                continue
            ancient = c["rarity"] == "Ancient"
            manifest[cid] = {"file": st["file"], "size": [ANCIENT_W, ANCIENT_H] if ancient else [NORMAL_W, NORMAL_H]}
        with open(MANIFEST_JSON, "w", encoding="utf-8") as f:
            json.dump(manifest, f, ensure_ascii=False, indent=1)
        self.info_label.config(text="全部完成！共 %d 张，已写入 crop_done.json" % len(self.progress["done"]))
        messagebox.showinfo("完成", "全部 %d 张卡图裁剪完成！\n输出目录: cropped/\n清单: crop_done.json" % len(self.progress["done"]))

    def quit_save(self):
        self.save_progress()
        self.root.destroy()


def main():
    root = tk.Tk()
    root.tk.call("tk", "scaling", 1.2)
    Cropper(root)
    root.mainloop()
    return 0


if __name__ == "__main__":
    sys.exit(main())
