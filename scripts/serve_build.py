#!/usr/bin/env python3
"""
Serve a Unity Web build locally with the right headers.

Unity compresses Web builds to Brotli (.br). A plain static server hands those
back without `Content-Encoding: br`, so the browser sees garbage and the loader
fails with an unhelpful error. This sets the encoding and MIME type per file so
the build actually runs.

    python3 scripts/serve_build.py unity-sim/BuildWeb
    python3 scripts/serve_build.py unity-sim/BuildWeb --port 8080
"""
import argparse
import functools
import http.server
import os
import socketserver

ENCODINGS = {".br": "br", ".gz": "gzip"}
TYPES = {
    ".wasm": "application/wasm",
    ".js": "application/javascript",
    ".json": "application/json",
    ".data": "application/octet-stream",
    ".html": "text/html",
    ".css": "text/css",
}


class UnityHandler(http.server.SimpleHTTPRequestHandler):
    def end_headers(self):
        # Only Content-Encoding is added here. Content-Type is already correct —
        # Python's guess_type strips the .br suffix and reports the inner type —
        # and sending a second one makes the browser discard the response.
        # COOP/COEP are deliberately absent: require-corp blocks the loader's
        # own fetches unless every asset also carries CORP, which stalls the
        # progress bar at 0%.
        _, ext = os.path.splitext(self.translate_path(self.path))
        if ext in ENCODINGS:
            self.send_header("Content-Encoding", ENCODINGS[ext])
        self.send_header("Cache-Control", "no-store")
        super().end_headers()

    def log_message(self, fmt, *args):
        pass  # keep the console readable


def main():
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("directory")
    ap.add_argument("--port", type=int, default=8000)
    args = ap.parse_args()

    if not os.path.isfile(os.path.join(args.directory, "index.html")):
        raise SystemExit(f"no index.html in {args.directory}")

    handler = functools.partial(UnityHandler, directory=args.directory)
    socketserver.TCPServer.allow_reuse_address = True
    with socketserver.TCPServer(("127.0.0.1", args.port), handler) as httpd:
        print(f"serving {args.directory} at http://localhost:{args.port}")
        httpd.serve_forever()


if __name__ == "__main__":
    main()
