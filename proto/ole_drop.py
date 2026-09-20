"""Explorer file drop via OLE IDropTarget (stdlib ctypes)."""
from __future__ import annotations

import ctypes
from ctypes import HRESULT, POINTER, WINFUNCTYPE, Structure, addressof, byref, cast, c_ubyte
from ctypes import c_uint, c_ulong, c_long, c_ushort, c_void_p

ole32 = ctypes.WinDLL("ole32")
shell32 = ctypes.WinDLL("shell32")
ole32.OleInitialize.argtypes = [c_void_p]
ole32.OleInitialize.restype = HRESULT
ole32.RegisterDragDrop.argtypes = [c_void_p, c_void_p]
ole32.RegisterDragDrop.restype = HRESULT
ole32.RevokeDragDrop.argtypes = [c_void_p]
ole32.RevokeDragDrop.restype = HRESULT
ole32.ReleaseStgMedium.argtypes = [c_void_p]

S_OK = 0
E_NOINTERFACE = 0x80004002
CF_HDROP = 15
TYMED_HGLOBAL = 1
DVASPECT_CONTENT = 1
DROPEFFECT_COPY = 1


class GUID(Structure):
    _fields_ = [
        ("Data1", c_ulong),
        ("Data2", c_ushort),
        ("Data3", c_ushort),
        ("Data4", c_ubyte * 8),
    ]


def _guid(text: str) -> GUID:
    body = text.replace("{", "").replace("}", "")
    a, b, c, d, e = body.split("-")
    rest = d + e
    data4 = (c_ubyte * 8)(*[int(rest[i : i + 2], 16) for i in range(0, 16, 2)])
    return GUID(int(a, 16), int(b, 16), int(c, 16), data4)


IID_IUnknown = _guid("00000000-0000-0000-C000-000000000046")
IID_IDropTarget = _guid("00000122-0000-0000-C000-000000000046")


def _iid_eq(ptr, guid: GUID) -> bool:
    return ctypes.string_at(ptr, 16) == bytes(guid)


class POINTL(Structure):
    _fields_ = [("x", c_long), ("y", c_long)]


class FORMATETC(Structure):
    _fields_ = [
        ("cfFormat", c_ushort),
        ("ptd", c_void_p),
        ("dwAspect", c_ulong),
        ("lindex", c_long),
        ("tymed", c_ulong),
    ]


class STGMEDIUM(Structure):
    _fields_ = [
        ("tymed", c_uint),
        ("_pad", c_uint),
        ("hGlobal", c_void_p),
        ("pUnkForRelease", c_void_p),
    ]


class COMObject(Structure):
    _fields_ = [("lpVtbl", POINTER(c_void_p))]


QI_FN = WINFUNCTYPE(HRESULT, c_void_p, c_void_p, POINTER(c_void_p))
REF_FN = WINFUNCTYPE(c_ulong, c_void_p)
ENTER_FN = WINFUNCTYPE(HRESULT, c_void_p, c_void_p, c_ulong, POINTL, POINTER(c_ulong))
OVER_FN = WINFUNCTYPE(HRESULT, c_void_p, c_ulong, POINTL, POINTER(c_ulong))
LEAVE_FN = WINFUNCTYPE(HRESULT, c_void_p)
DROP_FN = WINFUNCTYPE(HRESULT, c_void_p, c_void_p, c_ulong, POINTL, POINTER(c_ulong))
GETDATA_FN = WINFUNCTYPE(HRESULT, c_void_p, POINTER(FORMATETC), POINTER(STGMEDIUM))


def _paths_from_dataobject(p_data) -> list[str]:
    if not p_data:
        return []
    vtbl = cast(cast(p_data, POINTER(c_void_p)).contents, POINTER(c_void_p))
    get_data = GETDATA_FN(vtbl[3])
    fmt = FORMATETC()
    fmt.cfFormat = CF_HDROP
    fmt.dwAspect = DVASPECT_CONTENT
    fmt.lindex = -1
    fmt.tymed = TYMED_HGLOBAL
    medium = STGMEDIUM()
    hr = get_data(p_data, byref(fmt), byref(medium))
    if hr != S_OK or not medium.hGlobal:
        return []
    paths: list[str] = []
    try:
        count = shell32.DragQueryFileW(medium.hGlobal, 0xFFFFFFFF, None, 0)
        buf = ctypes.create_unicode_buffer(32768)
        for i in range(count):
            shell32.DragQueryFileW(medium.hGlobal, i, buf, 32768)
            paths.append(buf.value)
    finally:
        ole32.ReleaseStgMedium(byref(medium))
    return paths


class OleDropTarget:
    def __init__(self, hwnd: int, on_paths) -> None:
        self.hwnd = hwnd
        self.on_paths = on_paths
        self.refcount = 1
        self._keep = []

        def query_interface(this, riid, ppv):
            if not ppv:
                return E_NOINTERFACE
            if _iid_eq(riid, IID_IUnknown) or _iid_eq(riid, IID_IDropTarget):
                ppv[0] = this
                ref_add(this)
                return S_OK
            ppv[0] = None
            return E_NOINTERFACE

        def ref_add(this):
            self.refcount += 1
            return self.refcount

        def ref_rel(this):
            self.refcount -= 1
            return self.refcount

        def drag_enter(this, p_data, key, pt, effect):
            if effect:
                effect[0] = DROPEFFECT_COPY
            return S_OK

        def drag_over(this, key, pt, effect):
            if effect:
                effect[0] = DROPEFFECT_COPY
            return S_OK

        def drag_leave(this):
            return S_OK

        def drop(this, p_data, key, pt, effect):
            if effect:
                effect[0] = DROPEFFECT_COPY
            paths = _paths_from_dataobject(p_data)
            self.on_paths(paths)
            return S_OK

        fns = [
            QI_FN(query_interface),
            REF_FN(ref_add),
            REF_FN(ref_rel),
            ENTER_FN(drag_enter),
            OVER_FN(drag_over),
            LEAVE_FN(drag_leave),
            DROP_FN(drop),
        ]
        self._keep.extend(fns)
        self._vtbl = (c_void_p * 7)(*[cast(fn, c_void_p) for fn in fns])
        self._obj = COMObject()
        self._obj.lpVtbl = cast(addressof(self._vtbl), POINTER(c_void_p))
        ole32.OleInitialize(None)
        hr = ole32.RegisterDragDrop(c_void_p(hwnd), byref(self._obj))
        self.hr = hr & 0xFFFFFFFF

    def close(self) -> None:
        ole32.RevokeDragDrop(c_void_p(self.hwnd))
