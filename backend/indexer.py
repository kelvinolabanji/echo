import os
import torch
import numpy as np
import faiss
import threading
from PIL import Image
from database import (
    init_db, image_already_indexed, save_image,
    get_all_images_with_embeddings, delete_images_by_folder
)
from model import model, processor

SUPPORTED_EXTENSIONS = {".jpg", ".jpeg", ".png", ".webp", ".bmp"}
FAISS_INDEX_PATH = "echo.index"

# Global state
_progress = {
    "running": False,
    "total": 0,
    "indexed": 0,
    "skipped": 0,
    "current_file": "",
    "folder": ""
}
_cancel_event = threading.Event()
_lock = threading.Lock()

def get_progress() -> dict:
    with _lock:
        return dict(_progress)

def cancel_indexing():
    _cancel_event.set()

def get_or_create_index(dimension: int = 512) -> faiss.Index:
    if os.path.exists(FAISS_INDEX_PATH):
        return faiss.read_index(FAISS_INDEX_PATH)
    return faiss.IndexFlatIP(dimension)

def embed_image(path: str) -> np.ndarray | None:
    try:
        image = Image.open(path).convert("RGB")
        inputs = processor(images=image, return_tensors="pt")
        outputs = model.get_image_features(**inputs)
        if isinstance(outputs, torch.Tensor):
            embedding = outputs.detach().numpy()
        else:
            embedding = outputs.pooler_output.detach().numpy()
        faiss.normalize_L2(embedding)
        return embedding
    except Exception as e:
        print(f"Failed to embed {path}: {e}")
        return None

def rebuild_index():
    records = get_all_images_with_embeddings()
    if not records:
        if os.path.exists(FAISS_INDEX_PATH):
            os.remove(FAISS_INDEX_PATH)
        return

    index = faiss.IndexFlatIP(512)
    new_faiss_id = 0

    conn = __import__('database').get_connection()
    cursor = conn.cursor()

    for record in records:
        embedding = embed_image(record["path"])
        if embedding is None:
            continue
        index.add(embedding)
        cursor.execute(
            "UPDATE images SET faiss_id = ? WHERE path = ?",
            (new_faiss_id, record["path"])
        )
        new_faiss_id += 1

    conn.commit()
    conn.close()
    faiss.write_index(index, FAISS_INDEX_PATH)
    print(f"Index rebuilt with {new_faiss_id} images.")

def unindex_folder(folder_path: str):
    deleted = delete_images_by_folder(folder_path)
    print(f"Deleted {deleted} images from {folder_path}, rebuilding index...")
    rebuild_index()

def index_folder(folder_path: str):
    global _progress
    _cancel_event.clear()

    with _lock:
        _progress = {
            "running": True,
            "total": 0,
            "indexed": 0,
            "skipped": 0,
            "current_file": "",
            "folder": folder_path
        }

    init_db()
    index = get_or_create_index()

    image_paths = []
    for root, _, files in os.walk(folder_path):
        for file in files:
            if os.path.splitext(file)[1].lower() in SUPPORTED_EXTENSIONS:
                image_paths.append(os.path.join(root, file))

    with _lock:
        _progress["total"] = len(image_paths)

    print(f"Found {len(image_paths)} images.")

    indexed = 0
    skipped = 0

    for path in image_paths:
        if _cancel_event.is_set():
            print("Indexing cancelled.")
            break

        with _lock:
            _progress["current_file"] = os.path.basename(path)

        modified_at = os.path.getmtime(path)
        if image_already_indexed(path, modified_at):
            skipped += 1
            with _lock:
                _progress["skipped"] = skipped
            continue

        embedding = embed_image(path)
        if embedding is None:
            continue

        faiss_id = index.ntotal
        index.add(embedding)

        file_size = os.path.getsize(path)
        save_image(path, faiss_id, file_size, modified_at)
        indexed += 1

        with _lock:
            _progress["indexed"] = indexed

        print(f"[{indexed}] Indexed: {path}")

    faiss.write_index(index, FAISS_INDEX_PATH)

    with _lock:
        _progress["running"] = False

    print(f"Done. Indexed {indexed} new images, skipped {skipped}.")