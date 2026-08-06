# Echo

A fully local, offline semantic image search tool for Windows. Describe what you're looking for in plain English — "sunset over water," "my dog on the couch," "receipt from last week" — and Echo finds matching photos instantly, without ever sending anything to the cloud.

No account. No internet required after setup. No cloud storage of your photos or their embeddings. Everything runs on your machine.

![Echo search demo](screenshots/search-demo.gif)
## How it works

Echo indexes your photos locally using [CLIP](https://github.com/openai/CLIP) (`openai/clip-vit-base-patch32`), which turns both images and text descriptions into vectors in the same embedding space. Searching just means turning your query into a vector and finding the closest image vectors with [FAISS](https://github.com/facebookresearch/faiss). All of this happens on-device — the model, the index, and your photos never leave your computer.

## Features

- **Global hotkey** (Win+Shift+F) summons a floating search panel from anywhere in Windows
- **Instant, as-you-type search** across all your indexed folders
- **Automatic first-run setup** — indexes your Pictures folder automatically on first launch
- **Live folder management** — add/remove indexed folders, with real-time progress
- **File system watching** — new photos dropped into an indexed folder get picked up automatically
- **Tray notifications** for indexing start/completion
- **Frosted-glass UI** that feels like a native Windows feature, not a third-party app
- **Fully offline** — after initial setup, no internet connection is required

## Tech stack

**Backend** — Python 3.11, FastAPI, CLIP via HuggingFace Transformers, FAISS, SQLite
**Frontend** — C# / .NET 8, WinForms, WebView2 (HTML/CSS/JS UI), DWM APIs for acrylic blur

## Architecture

```
echo/
├── backend/
│   ├── main.py            — FastAPI app + all endpoints
│   ├── model.py            — CLIP model loading
│   ├── indexer.py          — image embedding + FAISS indexing
│   ├── searcher.py         — text query embedding + search
│   ├── database.py         — SQLite operations
│   ├── watcher.py           — filesystem watcher for auto-indexing
│   ├── paths.py            — resolves where user data vs. bundled resources live
│   └── echo-backend.spec   — PyInstaller build spec
├── frontend/EchoApp/
│   ├── Program.cs                — entry point
│   ├── BootstrapAppContext.cs    — first-run backend setup, inside the message loop
│   ├── AppContext.cs             — tray icon, hotkey, first-run auto-indexing
│   ├── BackendManager.cs         — spawns/manages the Python backend process
│   ├── BackendDownloader.cs      — downloads the backend package on first run
│   ├── IndexingWatcher.cs        — polls indexing progress, tray notifications
│   ├── SearchWindow.cs           — floating search UI
│   ├── FolderManagerWindow.cs    — folder management UI
│   ├── EchoBridge.cs             — JS ↔ C# bridge for the WebView2 UI
│   └── ui/                       — HTML/CSS/JS frontend
└── echo-setup.iss          — Inno Setup installer script
```

### Why the installer is shaped the way it is

The backend bundles CLIP, PyTorch, Transformers, and FAISS — 600MB+ once packaged, which is too large for a lightweight installer. So the installer ships only the (small) frontend; on first launch, the app downloads the backend package from this repo's [Releases](https://github.com/kelvinolabanji/echo/releases/download/v1.0.1/echo-backend.zip), verifies it with a SHA-256 checksum, and extracts it — a one-time setup step with its own progress window.

User data (the SQLite database, FAISS index, and thumbnail cache) lives in `%LOCALAPPDATA%\Echo`, independent of wherever the app itself is installed.

## Installation

Download the latest installer from the [Releases page](https://github.com/kelvinolabanji/echo/releases/download/v1.0.1/EchoSetup-1.0.0.exe) and run it. On first launch, Echo will download and set up its search engine (a few hundred MB, one-time) before it's ready to use.

## Running from source

**Backend:**
```powershell
cd backend
python -m venv .venv
.venv\Scripts\Activate.ps1
pip install -r requirements.txt
uvicorn main:app --reload
```

**Frontend:**
```powershell
cd frontend/EchoApp
dotnet run
```

## Building the installer yourself

```powershell
# 1. Build the backend
cd backend
pyinstaller echo-backend.spec

# 2. Zip and host it (e.g. as a GitHub Release asset), then update the
#    BackendDownloadUrl and ExpectedSha256 constants in BackendDownloader.cs

# 3. Publish the frontend
cd frontend/EchoApp
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

# 4. Compile echo-setup.iss with Inno Setup
```

## Known limitations

- CLIP performs noticeably better on real photos than on stylized/animated content (wallpapers, posters, cartoon screenshots)
- Windows only (WinForms + WebView2)

## Roadmap

- ONNX-quantized model for faster CPU indexing
- OCR layer for finding photos by text they contain (screenshots, receipts, signs, documents)
- Video support — extracting and indexing frames so video files become searchable the same way photos are

## License

MIT — see [LICENSE](LICENSE) for details.
