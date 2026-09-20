# QuickDock proto: tkinterdnd2 (Explorer D&D that actually works).
from __future__ import annotations

import json
import os
from tkinter import Frame, Label, Button, Listbox, END, SINGLE, filedialog
from tkinterdnd2 import DND_FILES, TkinterDnD

APP_NAME = "QuickDock"
CONFIG_DIR = os.path.join(os.environ.get("APPDATA", "."), "QuickDock")
CONFIG_PATH = os.path.join(CONFIG_DIR, "items.json")
BG = "#0a0c10"
PANEL = "#12161f"
ACCENT = "#00f0ff"
FG = "#e6edf3"


def log(msg: str) -> None:
    os.makedirs(CONFIG_DIR, exist_ok=True)
    with open(os.path.join(CONFIG_DIR, "proto.log"), "a", encoding="utf-8") as f:
        f.write(msg + "\n")


def load_items() -> list[str]:
    os.makedirs(CONFIG_DIR, exist_ok=True)
    if not os.path.isfile(CONFIG_PATH):
        return []
    try:
        data = json.loads(open(CONFIG_PATH, encoding="utf-8").read())
        return [p for p in data.get("items") or [] if isinstance(p, str) and os.path.exists(p)]
    except (OSError, json.JSONDecodeError, TypeError):
        return []


def save_items(items: list[str]) -> None:
    os.makedirs(CONFIG_DIR, exist_ok=True)
    with open(CONFIG_PATH, "w", encoding="utf-8") as f:
        json.dump({"items": items}, f, ensure_ascii=False, indent=2)


class Dock:
    def __init__(self) -> None:
        self.items = load_items()
        self.root = TkinterDnD.Tk()
        self.root.title("QuickDock")
        self.root.configure(bg=BG)
        self.root.attributes("-topmost", True)
        self.root.minsize(400, 320)

        wrap = Frame(self.root, bg=PANEL, padx=14, pady=14)
        wrap.pack(fill="both", expand=True)

        Label(wrap, text="QuickDock", fg=ACCENT, bg=PANEL, font=("Segoe UI", 16, "bold")).pack(anchor="w")
        Label(wrap, text="ショートカットや exe を下の枠へドロップ", fg="#9bb4c8", bg=PANEL).pack(anchor="w", pady=(0, 8))

        self.drop = Label(
            wrap,
            text="ここにドロップ",
            fg=FG,
            bg="#243044",
            font=("Segoe UI", 16, "bold"),
            height=4,
        )
        self.drop.pack(fill="both", expand=True)

        row = Frame(wrap, bg=PANEL)
        row.pack(fill="x", pady=8)
        Button(row, text="ファイルを追加", command=self._pick).pack(side="left", padx=(0, 6))
        Button(row, text="起動", command=self._launch).pack(side="left", padx=(0, 6))
        Button(row, text="削除", command=self._remove).pack(side="left")

        self.listbox = Listbox(wrap, bg="#0d1118", fg=FG, selectmode=SINGLE, height=8)
        self.listbox.pack(fill="both", expand=True)
        self.listbox.bind("<Double-Button-1>", lambda e: self._launch())

        self.status = Label(wrap, text="", fg="#9bb4c8", bg=PANEL, anchor="w")
        self.status.pack(fill="x", pady=(8, 0))

        for w in (self.root, wrap, self.drop, self.listbox):
            w.drop_target_register(DND_FILES)
            w.dnd_bind("<<Drop>>", self._on_drop)

        self._refresh()
        self.root.after(100, self._place)

    def _place(self) -> None:
        w, h = 440, 380
        sw = self.root.winfo_screenwidth()
        sh = self.root.winfo_screenheight()
        self.root.geometry(f"{w}x{h}+{max(20, sw - w - 30)}+{max(20, (sh - h) // 2)}")
        self.root.lift()
        log("dnd window shown")

    def _set(self, msg: str) -> None:
        self.status.configure(text=msg)
        log(msg)

    def _refresh(self) -> None:
        self.listbox.delete(0, END)
        for p in self.items:
            self.listbox.insert(END, os.path.basename(p))
        self._set("登録 %d 件" % len(self.items))

    def _parse_drop(self, data: str) -> list[str]:
        try:
            return list(self.root.tk.splitlist(data))
        except Exception:
            return [data.strip().strip("{}")]

    def _on_drop(self, event) -> None:
        paths = self._parse_drop(event.data)
        log("drop " + repr(paths))
        for path in paths:
            self._add(path)

    def _add(self, path: str) -> None:
        path = os.path.normpath(str(path).strip().strip('"'))
        log("add try " + path)
        if not os.path.exists(path):
            self._set("見つからない: " + path)
            return
        if path in self.items:
            self._set("既に登録済み: " + os.path.basename(path))
            return
        self.items.append(path)
        save_items(self.items)
        self._refresh()
        self._set("追加: " + os.path.basename(path))

    def _pick(self) -> None:
        path = filedialog.askopenfilename(
            parent=self.root,
            title="QuickDock に追加",
            filetypes=[("Programs", "*.lnk *.exe *.bat *.cmd"), ("All", "*.*")],
        )
        if path:
            self._add(path)

    def _sel(self) -> int:
        s = self.listbox.curselection()
        return int(s[0]) if s else -1

    def _launch(self) -> None:
        i = self._sel()
        if i < 0:
            self._set("リストから選んでください")
            return
        os.startfile(self.items[i])

    def _remove(self) -> None:
        i = self._sel()
        if i < 0:
            return
        self.items.pop(i)
        save_items(self.items)
        self._refresh()

    def run(self) -> None:
        self.root.mainloop()


if __name__ == "__main__":
    Dock().run()
