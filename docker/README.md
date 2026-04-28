# VideoAnonymizer – Docker

```bash
docker pull ghcr.io/imagonix/videoanonymizer-local:latest
docker run -d --name video-anonymizer -p 5117:5117 -v ./docker-data:/data --gpus all --restart unless-stopped ghcr.io/imagonix/videoanonymizer-local:latest
```

Open http://localhost:5117.

### Your data stays on your host

The `-v ./docker-data:/data` mount means everything lives in `./docker-data/` on your machine:

| Path | Contents |
|---|---|
| `docker-data/App_Data/Uploads/` | Your analyzed videos + working copy |
| `docker-data/models/` | AI model cache (downloaded once) |

**To clean up:** stop the container and delete `./docker-data/`

**Tags:** `latest`, `<short-sha>` (every build), `v*` (releases).

**Without GPU:** omit `--gpus all` — falls back to CPU (slower).