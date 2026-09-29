# echo-backend.spec
# Run: pyinstaller echo-backend.spec
#
# Packages Python, PyTorch, Transformers, FAISS, Uvicorn, and our local CLIP model
# into a standalone executable—no Python installation needed on the target machine.

import sys
from PyInstaller.utils.hooks import collect_all

block_cipher = None

# Heavy ML libraries often load dynamic modules at runtime that PyInstaller’s
# static scanner misses, so we grab their binaries, data, and hidden imports explicitly.
datas = []
binaries = []
hiddenimports = []

for pkg in ('torch', 'transformers', 'faiss'):
    pkg_datas, pkg_binaries, pkg_hiddenimports = collect_all(pkg)
    datas += pkg_datas
    binaries += pkg_binaries
    hiddenimports += pkg_hiddenimports

# Copy local CLIP weights so the app works completely offline
datas += [('models/clip', 'models/clip')]

# Standard Uvicorn internals needed when running as an executable
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
    console=False,  # Runs silently in the background when launched by EchoApp
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
