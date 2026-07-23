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
    # Automatically load all existing folders from the database on startup and start watching them
    folders = get_indexed_folders()
    for folder in folders:
        watcher_service.watch_folder(folder)
    print(f"[Watcher] Started and monitoring {len(folders)} directories.")

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
    return get_indexed_folders()

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