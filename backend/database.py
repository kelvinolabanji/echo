import sqlite3
import os

DB_PATH = "echo.db"

def get_connection():
    conn = sqlite3.connect(DB_PATH)
    conn.row_factory = sqlite3.Row
    return conn

def init_db():
    conn = get_connection()
    cursor = conn.cursor()

    cursor.execute("""
        CREATE TABLE IF NOT EXISTS images (
            id          INTEGER PRIMARY KEY AUTOINCREMENT,
            path        TEXT UNIQUE NOT NULL,
            faiss_id    INTEGER UNIQUE,
            file_size   INTEGER,
            modified_at REAL,
            indexed_at  REAL
        )
    """)

    conn.commit()
    conn.close()

def image_already_indexed(path: str, modified_at: float) -> bool:
    conn = get_connection()
    cursor = conn.cursor()
    cursor.execute("""
        SELECT id FROM images 
        WHERE path = ? AND modified_at = ?
    """, (path, modified_at))
    result = cursor.fetchone()
    conn.close()
    return result is not None

def save_image(path: str, faiss_id: int, file_size: int, modified_at: float):
    import time
    conn = get_connection()
    cursor = conn.cursor()
    cursor.execute("""
        INSERT OR REPLACE INTO images (path, faiss_id, file_size, modified_at, indexed_at)
        VALUES (?, ?, ?, ?, ?)
    """, (path, faiss_id, file_size, modified_at, time.time()))
    conn.commit()
    conn.close()

def get_path_by_faiss_id(faiss_id: int) -> str | None:
    conn = get_connection()
    cursor = conn.cursor()
    cursor.execute("SELECT path FROM images WHERE faiss_id = ?", (faiss_id,))
    result = cursor.fetchone()
    conn.close()
    return result["path"] if result else None

def get_all_images() -> list:
    conn = get_connection()
    cursor = conn.cursor()
    cursor.execute("SELECT * FROM images ORDER BY indexed_at DESC")
    results = cursor.fetchall()
    conn.close()
    return [dict(row) for row in results]

def get_stats() -> dict:
    conn = get_connection()
    cursor = conn.cursor()
    cursor.execute("SELECT COUNT(*) as total FROM images")
    result = cursor.fetchone()
    conn.close()
    return {"total_indexed": result["total"]}