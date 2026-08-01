# echo-backend.spec
# Build with: pyinstaller echo-backend.spec
#
# Produces a single standalone echo-backend.exe that bundles Python,
# torch, transformers, faiss, uvicorn, and the local CLIP weights folder.
# No Python install required on the target machine.

# ---------------------------------------------------------------------------
# NOTES BEFORE YOU RUN THIS
# ---------------------------------------------------------------------------
# 1. Run this from inside backend/, with your .venv (Python 3.11) activated.
# 2. torch + transformers are notorious for PyInstaller missing hidden imports
#    and dynamically-loaded submodules. If the exe crashes on first run with
#    a ModuleNotFoundError, add the missing module to hiddenimports below.
# 3. Test the exe in isolation first:
#      dist\echo-backend\echo-backend.exe
#    then curl http://127.0.0.1:8000/stats before wiring it into EchoApp.
# 4. --add-data paths use ';' as separator on Windows, ':' on Mac/Linux.
# ---------------------------------------------------------------------------

import sys
from PyInstaller.utils.hooks import collect_all

block_cipher = None

# collect_all pulls in submodules, data files, and binaries these packages
# need that PyInstaller's static analysis misses on its own
datas = []
binaries = []
hiddenimports = []

for pkg in ('torch', 'transformers', 'faiss'):
    pkg_datas, pkg_binaries, pkg_hiddenimports = collect_all(pkg)
    datas += pkg_datas
    binaries += pkg_binaries
    hiddenimports += pkg_hiddenimports

# Bundle the local CLIP model weights folder so no download happens at runtime.
# Source path is relative to this spec file; adjust if your layout differs.
datas += [('models/clip', 'models/clip')]

hiddenimports += [
    'uvicorn.logging',
    'uvicorn.loops',
    'uvicorn.loops.auto',
    'uvicorn.protocols',
    'uvicorn.protocols.http',
    'uvicorn.protocols.http.auto',
    'uvicorn.protocols.websockets',
    'uvicorn.protocols.websockets.auto',
    'uvicorn.lifespan',
    'uvicorn.lifespan.on',
]

a = Analysis(
    ['main.py'],
    pathex=[],
    binaries=binaries,
    datas=datas,
    hiddenimports=hiddenimports,
    hookspath=[],
    hooksconfig={},
    runtime_hooks=[],
    excludes=[],
    win_no_prefer_redirects=False,
    win_private_assemblies=False,
    cipher=block_cipher,
    noarchive=False,
)

pyz = PYZ(a.pure, a.zipped_data, cipher=block_cipher)

exe = EXE(
    pyz,
    a.scripts,
    [],
    exclude_binaries=True,
    name='echo-backend',
    debug=False,
    bootloader_ignore_signals=False,
    strip=False,
    upx=True,
    console=False,   # no visible console window when EchoApp spawns this
    disable_windowed_traceback=False,
    target_arch=None,
    codesign_identity=None,
    entitlements_file=None,
)

coll = COLLECT(
    exe,
    a.binaries,
    a.zipfiles,
    a.datas,
    strip=False,
    upx=True,
    upx_exclude=[],
    name='echo-backend',
)
