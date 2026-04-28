# 🎥 VideoAnonymizer

**Local-first video anonymization for faces, built as a full-stack engineering project.**

VideoAnonymizer helps detect faces in videos, review the detections, select which detected objects should be anonymized, and export a blurred version of the video. The standalone variant runs on the user's own machine: videos, extracted frames, detection results, and exports stay local instead of being uploaded to a remote service.

The project is also designed as a recruiter-friendly reference implementation: it combines .NET, Blazor, Vue, Python computer vision, background workers, messaging abstractions, packaging automation, and a cloud-ready Aspire architecture.

## Demo

### Workflow Demo

<video src="docs/video/demovideo.mp4" controls muted playsinline title="VideoAnonymizer workflow demo"></video>

The demo shows the current local workflow: select a video, analyze it, review detections in the editor, deselect a face, anonymize selected detections, and play the exported result.

## Quickstart

The currently recommended way to run VideoAnonymizer is locally via Docker, this runs it in standalone mode, all data stays on your device:

```bash
docker pull ghcr.io/imagonix/videoanonymizer-local:latest
```

```bash
docker run -d --name video-anonymizer -p 5117:5117 -v ./docker-data:/data --gpus all --restart unless-stopped ghcr.io/imagonix/videoanonymizer-local:latest
```

Open [http://localhost:5117](http://localhost:5117) after the container has started.

## Current Scope

VideoAnonymizer currently focuses on **face anonymization**.

What works today:

- upload/select a video
- analyze frames with a Python object detection service
- detect faces using an ONNX model
- review detected objects in a video/timeline UI
- deselect objects that should not be anonymized
- blur selected detections
- export and download the anonymized video
- run via Docker on any OS, as a standalone local Windows package, or from source as a distributed Aspire app

What is not there yet:

- manual drawing of new boxes
- moving or resizing detections
- detection of license plates, addresses, labels, or other sensitive objects
- signed Windows releases without SmartScreen friction

## Architecture

VideoAnonymizer supports two execution styles: a standalone local build and a cloud-ready distributed setup.

### Why The Standalone Version Matters

Video anonymization often touches private material. A local desktop workflow is useful when users do not want to upload raw footage to a cloud service just to remove faces.

The standalone version starts the local web UI, API, workers, and object detection service from one launcher. It is intended for privacy-sensitive desktop usage while keeping the same product workflow as the distributed version.

In standalone mode:

- processing happens locally on the user's machine
- user videos are not sent to a remote backend
- the Python object detection API is launched locally as a bundled executable
- the face detection model is downloaded on first start if missing
- CUDA status is shown in the UI, with warnings when CPU fallback is used

### Standalone Mode

The standalone package is built for local Windows usage. It starts:

- local web frontend
- API service
- video processing workers
- Python object detection executable
- local direct messaging
- local/in-memory storage

This variant is optimized for simple desktop usage and privacy-sensitive processing.

### Cloud-Ready Mode

The distributed variant keeps service boundaries explicit and is orchestrated with .NET Aspire:

- `webfrontend` - Blazor WebAssembly frontend
- `apiservice` - ASP.NET Core backend and SignalR notifications
- `video processor` - worker service for frame extraction, anonymization, and export
- `objectDetection` - Python FastAPI computer vision service
- `RabbitMQ` - asynchronous job queue
- `PostgreSQL` - metadata and processing state
- `modeldownloader` - startup model provisioning
- `database migration service` - schema migration on startup

The two modes share the same application concepts while using different infrastructure adapters for persistence and messaging.

## Processing Flow

1. The user selects a video.
2. The API creates an analysis job.
3. The video processor extracts frames.
4. Frames are sent to the Python detection service.
5. Detected faces are stored and displayed in the review UI.
6. The user deselects objects that should remain visible.
7. Selected detections are blurred.
8. The processed video is exported for download.

## Getting Started

### Docker (Cross-Platform)

Requirements: [Docker](https://docs.docker.com/engine/install/) and optionally [NVIDIA Container Toolkit](https://docs.nvidia.com/datacenter/cloud-native/container-toolkit/latest/install-guide.html) for GPU acceleration.

Run the tool on any OS. See [docker/README.md](docker/README.md) for details.

```bash
docker run -d -p 5117:5117 -v ./docker-data:/data --gpus all ghcr.io/imagonix/videoanonymizer-local:latest
```

### From Source Code

Requirements:

- .NET 10
- Python 3.10+
- Node.js/npm
- Docker
- GPU optional, recommended for faster inference
  - CUDA Toolkit 12.x and cuDNN 9.x for CUDA 12

#### Build Standalone Package

```powershell
git clone https://github.com/Imagonix/VideoAnonymizer.git
cd VideoAnonymizer
.\publish-standalone.ps1
```

Output:

```text
artifacts/standalone/VideoAnonymizer.exe
```
The standalone package starts the app locally and opens the browser UI automatically.

#### Distributed Development Setup

```powershell
git clone https://github.com/Imagonix/VideoAnonymizer.git
cd VideoAnonymizer
.\setup-dev.ps1
cd VideoAnonymizer.Web.Modules/ClientApp/video-editor
npm ci
npm run build
cd ../../..
dotnet run --project VideoAnonymizer.AppHost
```

This starts the Aspire application with frontend, API, RabbitMQ, PostgreSQL, workers, model downloader, and Python object detection.

## Tech Stack

### Backend

- .NET 10
- ASP.NET Core
- Entity Framework Core
- SignalR
- RabbitMQ for distributed messaging
- direct messaging abstraction for standalone mode

### Frontend

- Blazor WebAssembly
- Vue for the video review/editor component
- MudBlazor

### AI and Processing

- Python
- FastAPI
- ONNX Runtime
- RetinaFace-style face detection model
- OpenCV / OpenCvSharp

### Infrastructure and Delivery

- .NET Aspire
- PostgreSQL
- RabbitMQ
- Docker
- PyInstaller for the Python object detection executable
- GitHub Actions for standalone packaging and prerelease artifacts

## AI Model

- Face detection uses an ONNX model.
- The model file is not included in the repository.
- It is downloaded automatically on startup when missing.
- In standalone mode the model is stored locally under the standalone data folder.

## Testing and Quality

The project includes:

- Python tests for the object detection API
- Web component tests for the Blazor UI
- API integration tests for distributed workflows

The standalone release pipeline currently runs Python tests, builds Vue assets, builds the .NET solution, runs web tests, publishes the local Windows package, uploads an artifact, and creates a GitHub prerelease.

## Roadmap

In progress:

- manual bounding box editing

Planned:

- license plate detection
- additional sensitive object categories
- improved object tracking across frames

## Project Purpose

VideoAnonymizer is built as a practical product-style reference project. It demonstrates how AI inference can be turned into a usable workflow with review, correction points, background processing, and export.

It is intended to show:

- privacy-aware product thinking
- full-stack .NET application development
- Python AI service integration
- distributed architecture with Aspire
- local standalone packaging
- pragmatic CI/CD and release automation

## License

MIT License
