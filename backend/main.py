from fastapi import FastAPI, Query, BackgroundTasks
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import FileResponse
from database import get_stats, get_all_images, init_db, get_indexed_folders
from indexer import index_folder, get_progress, cancel_indexing, unindex_folder, get_thumbnail_path, THUMBNAIL_DIR, index_single_file
from searcher import search
from watcher import WatcherService
import os

app = FastAPI()

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_methods=["*"],
    allow_headers=["*"],
)

# Initialize the Watcher Service, pointing its callback to index_single_file
watcher_service = WatcherService(on_file_changed=index_single_file)

@app.on_event("startup")
def startup():
    init_db()
    folders = get_indexed_folders()
    for folder in folders:
        folder_path = None
        if isinstance(folder, dict):
            # Check common key alternatives
            for key in ["path", "folder", "directory", "dir"]:
                if key in folder:
                    folder_path = folder[key]
                    break
            if not folder_path and folder:
                folder_path = list(folder.values())[0]
        elif hasattr(folder, "keys"):
            # Handle sqlite3.Row objects
            for key in ["path", "folder", "directory", "dir"]:
                try:
                    folder_path = folder[key]
                    break
                except KeyError:
                    continue
        else:
            folder_path = folder

        if folder_path and isinstance(folder_path, str) and os.path.isdir(folder_path):
            watcher_service.watch_folder(folder_path)
            
    print(f"[Watcher] Started and monitoring directories.")

@app.on_event("shutdown")
def shutdown():
    watcher_service.stop()

@app.get("/search")
def search_images(q: str = Query(...), top_k: int = 20):
    results = search(q, top_k)
    return {"query": q, "results": results}

@app.post("/index")
def index_images(folder: str, background_tasks: BackgroundTasks):
    if not os.path.isdir(folder):
        return {"error": f"Folder not found: {folder}"}
    
    # Enable live watching for this folder right away
    watcher_service.watch_folder(folder)
    
    background_tasks.add_task(index_folder, folder)
    return {"message": f"Indexing started for: {folder}"}

@app.post("/index/cancel")
def cancel():
    cancel_indexing()
    return {"message": "Cancellation requested"}

@app.get("/index/progress")
def progress():
    return get_progress()

@app.post("/unindex")
def unindex(folder: str, background_tasks: BackgroundTasks):
    # Stop live watching this folder
    watcher_service.unwatch_folder(folder)
    
    background_tasks.add_task(unindex_folder, folder)
    return {"message": f"Unindexing started for: {folder}"}

@app.get("/watcher/status")
def watcher_status():
    return {
        "active_watches": list(watcher_service.watches.keys())
    }

@app.get("/folders")
def folders():
    # get_indexed_folders() only knows about folders with at least one
    # ALREADY-indexed photo. A folder that was just submitted for indexing
    # has zero indexed photos yet (indexing runs in the background and takes
    # time), so it wouldn't show up here at all — even though watch_folder()
    # was already called synchronously in /index before this. Merging in
    # currently-watched folders means the UI shows it immediately, with a
    # count that fills in as indexing actually progresses.
    indexed = get_indexed_folders()
    merged = {entry["folder"]: entry for entry in indexed}

    for watched_folder in watcher_service.watches.keys():
        if watched_folder not in merged:
            merged[watched_folder] = {"folder": watched_folder, "count": 0}

    return list(merged.values())

@app.get("/stats")
def stats():
    return get_stats()

@app.get("/images")
def list_images():
    return get_all_images()

@app.get("/thumbnail")
def get_thumbnail(path: str):
    thumb_path = get_thumbnail_path(path)
    if os.path.exists(thumb_path):
        return FileResponse(thumb_path, media_type="image/jpeg")
    if os.path.exists(path):
        return FileResponse(path)
    return {"error": "File not found"}


if __name__ == "__main__":
    # This is the actual entry point once frozen into echo-backend.exe.
    # In dev, `uvicorn main:app --reload` is what starts the server — uvicorn's
    # own CLI does the invoking, so this block never ran and was never missed.
    # But a frozen exe just executes this module directly, so without this,
    # echo-backend.exe would define the app and immediately exit.
    import multiprocessing
    multiprocessing.freeze_support()  # safe no-op here, but cheap insurance
                                       # against torch/multiprocessing quirks
                                       # inside a frozen onefile/onedir exe

    # A windowed (console=False) frozen exe has no real console, so Windows
    # gives it stdout/stderr of None. Uvicorn's default logging setup calls
    # .isatty() on that stream to decide whether to use colors, which crashes
    # outright when the stream doesn't exist. Give it somewhere harmless to
    # write instead, and skip uvicorn's color-detecting log config entirely.
    import sys
    if sys.stdout is None:
        sys.stdout = open(os.devnull, "w")
    if sys.stderr is None:
        sys.stderr = open(os.devnull, "w")

    import uvicorn
    uvicorn.run(app, host="127.0.0.1", port=8000, log_config=None, use_colors=False)