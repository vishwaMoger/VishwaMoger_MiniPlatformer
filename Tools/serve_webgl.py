"""Serve a Unity WebGL build locally with the headers Brotli/Gzip builds need.

    python Tools/serve_webgl.py [folder] [port]
    (default: Builds/CrazyGamesRelease on http://localhost:8080)
"""
import http.server
import os
import sys

ROOT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(os.path.dirname(__file__), "..", "Builds", "CrazyGamesRelease")
PORT = int(sys.argv[2]) if len(sys.argv) > 2 else 8080

TYPES = {".js": "application/javascript", ".wasm": "application/wasm", ".data": "application/octet-stream",
         ".json": "application/json", ".html": "text/html", ".css": "text/css", ".png": "image/png"}


class Handler(http.server.SimpleHTTPRequestHandler):
    def __init__(self, *a, **kw):
        super().__init__(*a, directory=ROOT, **kw)

    def end_headers(self):
        path = self.path.split("?")[0]
        for enc, ext in (("br", ".br"), ("gzip", ".gz")):
            if path.endswith(ext):
                self.send_header("Content-Encoding", enc)
        self.send_header("Cache-Control", "no-store")
        super().end_headers()

    def guess_type(self, path):
        base = path[:-3] if path.endswith(".br") else path[:-3] if path.endswith(".gz") else path
        return TYPES.get(os.path.splitext(base)[1], super().guess_type(path))


if __name__ == "__main__":
    print(f"Serving {os.path.abspath(ROOT)} on http://localhost:{PORT}")
    http.server.ThreadingHTTPServer(("127.0.0.1", PORT), Handler).serve_forever()
