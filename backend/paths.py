"""
Central path resolution for backend resources and persistent user data:

- BASE_DIR: Bundled app assets (like local CLIP model weights).
- DATA_DIR: Writeable user storage (%LOCALAPPDATA%\Echo on Windows). 
  Stores runtime data outside Program Files to avoid permission issues.
"""

import os
import sys


def _get_base_dir() -> str:
    if getattr(sys, "frozen", False):
        # PyInstaller unpacks temporary assets to _MEIPASS, or alongside the exe
        return getattr(sys, "_MEIPASS", os.path.dirname(sys.executable))
    return os.path.dirname(os.path.abspath(__file__))


def _get_data_dir() -> str:
    local_appdata = os.environ.get("LOCALAPPDATA")
    if local_appdata:
        data_dir = os.path.join(local_appdata, "Echo")
    else:
        # Fallback directory for non-Windows dev environments
        data_dir = os.path.join(os.path.expanduser("~"), ".echo")
    os.makedirs(data_dir, exist_ok=True)
    return data_dir


BASE_DIR = _get_base_dir()
DATA_DIR = _get_data_dir()

DB_PATH = os.path.join(DATA_DIR, "echo.db")
FAISS_INDEX_PATH = os.path.join(DATA_DIR, "echo.index")
THUMBNAIL_DIR = os.path.join(DATA_DIR, "thumbnail_cache")
MODEL_PATH = os.path.join(BASE_DIR, "models", "clip")
