import os
import torch
import numpy as np
import faiss
from database import get_path_by_faiss_id
from model import model, processor

FAISS_INDEX_PATH = "echo.index"
MIN_SCORE = 0.25

_index = None

def get_index():
    global _index
    if _index is None and os.path.exists(FAISS_INDEX_PATH):
        _index = faiss.read_index(FAISS_INDEX_PATH)
    return _index

def reload_index():
    global _index
    _index = None

def embed_text(query: str) -> np.ndarray:
    inputs = processor(text=[query], return_tensors="pt", padding=True)
    outputs = model.get_text_features(**inputs)
    if isinstance(outputs, torch.Tensor):
        embedding = outputs.detach().numpy()
    else:
        embedding = outputs.pooler_output.detach().numpy()
    faiss.normalize_L2(embedding)
    return embedding

def search(query: str, top_k: int = 20) -> list:
    index = get_index()

    if index is None or index.ntotal == 0:
        return []

    embedding = embed_text(query)
    scores, faiss_ids = index.search(embedding, min(top_k, index.ntotal))

    results = []
    for score, faiss_id in zip(scores[0], faiss_ids[0]):
        if faiss_id == -1:
            continue
        if float(score) < MIN_SCORE:
            continue
        path = get_path_by_faiss_id(int(faiss_id))
        if path:
            results.append({
                "path": path,
                "score": float(score)
            })

    return results