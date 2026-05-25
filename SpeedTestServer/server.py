
import argparse
import os
import time
from typing import Annotated

import uvicorn
from fastapi import FastAPI, Query, Request
from fastapi.responses import Response

MAX_BYTES     = 10 * 1024 * 1024
DEFAULT_BYTES =  2 * 1024 * 1024

_RANDOM_POOL = os.urandom(MAX_BYTES)

app = FastAPI(
    title="Speed Test Server",
    description="RN-Technologies / System Info — сервер замера скорости соединения",
    version="1.0.0",
)

@app.get("/ping", summary="Замер задержки")
async def ping():
    return {"t": time.time() * 1000}

@app.get("/download", summary="Входящая скорость (клиент скачивает)")
async def download(
    bytes: Annotated[int, Query(ge=1, le=MAX_BYTES, description="Размер тела ответа")] = DEFAULT_BYTES
):
    payload = _RANDOM_POOL[:bytes]
    return Response(
        content=payload,
        media_type="application/octet-stream",
        headers={
            "Content-Length":      str(len(payload)),
            "Cache-Control":       "no-store",
            "X-Content-Type-Options": "nosniff",
        },
    )

@app.post("/upload", summary="Исходящая скорость (клиент загружает)")
async def upload(request: Request):
    t_start = time.perf_counter()
    body    = await request.body()
    elapsed_ms = (time.perf_counter() - t_start) * 1000

    return {
        "received": len(body),
        "ms":       round(elapsed_ms, 2),
    }

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Speed Test Server")
    parser.add_argument("--host", default="0.0.0.0", help="Адрес для прослушивания")
    parser.add_argument("--port", type=int, default=8000, help="Порт")
    parser.add_argument("--workers", type=int, default=1, help="Число воркеров uvicorn")
    args = parser.parse_args()

    print(f"Speed Test Server запущен на http://{args.host}:{args.port}")
    print(f"  GET  /ping")
    print(f"  GET  /download?bytes=N   (max {MAX_BYTES // 1024} КБ)")
    print(f"  POST /upload")

    uvicorn.run(
        "server:app",
        host=args.host,
        port=args.port,
        workers=args.workers,
        log_level="info",
    )
