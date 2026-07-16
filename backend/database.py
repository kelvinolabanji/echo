import os
import time
import numpy as np
import faiss
from PIL import Image
from transformers import CLIPProcessor, CLIPModel
from database import init_db, image_already_indexed, save_image

SUPPORTED_EXTENSIONS = {".jpg", ".jpeg", ".png", ".webp", ".bmp"}
FAISS_INDEX_PATH = "echo.index"
MODEL_NAME = "openai/clip-vit-base-patch32"

# Load model once globally
print("Loading CLIP model...")
model = CLIPModel.from_pretrained(MODEL_NAME)
processor = CLIPProcessor.from_pretrained(MODEL_NAME)
model.eval()
print("Model loaded.")

def get_or_create_index(dimension: int = 512) -> faiss.Index:
    if os.path.exists(FAISS_INDEX_PATH):
        return faiss.read_index(FAISS_INDEX_PATH)
    return faiss.IndexFlatIP(dimension)

def embed_image(path: str) -> np.ndarray | None:
    try:
        image = Image.open(path).convert("RGB")
        inputs = processor(images=image, return_tensors="pt")
        outputs = model.get_image_features(**inputs)
        embedding = outputs.detach().numpy()
        faiss.normalize_L2(embedding)
        return embedding
    except Exception as e:
        print(f"Failed to embed {path}: {e}")
        return None

def index_folder(folder_path: str):
    init_db()
    index = get_or_create_index()

    image_paths = []
    for root, _, files in os.walk(folder_path):
        for file in files:
            if os.path.splitext(file)[1].lower() in SUPPORTED_EXTENSIONS:
                image_paths.append(os.path.join(root, file))

    print(f"Found {len(image_paths)} images.")

    indexed = 0
    skipped = 0

    for path in image_paths:
        modified_at = os.path.getmtime(path)
        if image_already_indexed(path, modified_at):
            skipped += 1
            continue

        embedding = embed_image(path)
        if embedding is None:
            continue

        faiss_id = index.ntotal
        index.add(embedding)

        file_size = os.path.getsize(path)
        save_image(path, faiss_id, file_size, modified_at)
        indexed += 1
        print(f"[{indexed}] Indexed: {path}")

    faiss.write_index(index, FAISS_INDEX_PATH)
    print(f"Done. Indexed {indexed} new images, skipped {skipped} already indexed.")