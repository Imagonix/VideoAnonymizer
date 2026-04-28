# VideoAnonymizer – Docker

```bash
docker pull ghcr.io/imagonix/videoanonymizer-local:latest
docker run -d --name video-anonymizer -p 5117:5117 -v ./docker-data:/data --gpus all --restart unless-stopped ghcr.io/imagonix/videoanonymizer-local:latest
```

Open http://localhost:5117. Container stores uploads and models in `./docker-data/`.

**Tags:** `latest`, `<short-sha>` (every build), `v*` (releases).

**Without GPU:** omit `--gpus all` — falls back to CPU (slower).