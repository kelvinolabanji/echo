"""
Central place for all backend file locations.

Two different kinds of paths, resolved differently:

- BASE_DIR: where bundled resources (the CLIP model) live. This is next to
  the exe when frozen with PyInstaller, or the script folder in dev.

- DATA_DIR: where user data (db, FAISS index, thumbnails) lives. Always
  %LOCALAPPDATA%\\Echo on Windows, regardless of where the app itself is
  installed. This matters because a standard (non-admin) install typically
  places the app under Program Files, which normal users can't write to —
  writing user data there would fail the first time someone indexes a folder.
"""

import os
import sys


def _get_base_dir() -> str:
    if getattr(sys, "frozen", False):
        # PyInstaller onefile extracts to sys._MEIPASS; onedir runs next to the exe.
        return getattr(sys, "_MEIPASS", os.path.dirname(sys.executable))
    return os.path.dirname(os.path.abspath(__file__))


def _get_data_dir() -> str:
    local_appdata = os.environ.get("LOCALAPPDATA")
    if local_appdata:
        data_dir = os.path.join(local_appdata, "Echo")
    else:
        # Fallback for dev on non-Windows machines
        data_dir = os.path.join(os.path.expanduser("~"), ".echo")
    os.makedirs(data_dir, exist_ok=True)
    return data_dir


BASE_DIR = _get_base_dir()
DATA_DIR = _get_data_dir()

DB_PATH = os.path.join(DATA_DIR, "echo.db")
FAISS_INDEX_PATH = os.path.join(DATA_DIR, "echo.index")
THUMBNAIL_DIR = os.path.join(DATA_DIR, "thumbnail_cache")
MODEL_PATH = os.path.join(BASE_DIR, "models", "clip")
