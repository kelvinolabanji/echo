import time
import threading
from pathlib import Path
from watchdog.observers import Observer
from watchdog.events import FileSystemEventHandler

class EchoFileHandler(FileSystemEventHandler):
    def __init__(self, on_file_changed):
        super().__init__()
        self.on_file_changed = on_file_changed
        self.extensions = {".jpg", ".jpeg", ".png", ".webp", ".bmp"}
        self._timers = {}
        self._lock = threading.Lock()

    def _is_valid_image(self, path_str):
        return Path(path_str).suffix.lower() in self.extensions

    def _debounce_index(self, path_str):
        with self._lock:
            if path_str in self._timers:
                self._timers[path_str].cancel()
            
            # Wait 2 seconds for file writes/downloads to completely finish
            timer = threading.Timer(2.0, lambda: self.on_file_changed(path_str))
            self._timers[path_str] = timer
            timer.start()

    def on_created(self, event):
        if not event.is_directory and self._is_valid_image(event.src_path):
            self._debounce_index(event.src_path)

    def on_modified(self, event):
        if not event.is_directory and self._is_valid_image(event.src_path):
            self._debounce_index(event.src_path)

    def on_moved(self, event):
        # Handle file renames or moves into the watched directory
        if not event.is_directory and self._is_valid_image(event.dest_path):
            self._debounce_index(event.dest_path)

class WatcherService:
    def __init__(self, on_file_changed):
        self.observer = Observer()
        self.handler = EchoFileHandler(on_file_changed)
        self.watches = {}
        self.observer.start()

    def watch_folder(self, folder_path):
        abs_path = str(Path(folder_path).resolve())
        if abs_path not in self.watches and Path(abs_path).exists():
            watch = self.observer.schedule(self.handler, abs_path, recursive=True)
            self.watches[abs_path] = watch

    def unwatch_folder(self, folder_path):
        abs_path = str(Path(folder_path).resolve())
        if abs_path in self.watches:
            watch = self.watches.pop(abs_path)
            self.observer.unschedule(watch)

    def stop(self):
        self.observer.stop()
        self.observer.join()