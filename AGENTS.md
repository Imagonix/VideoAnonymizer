# VideoAnonymizer - Agent Guide

## Project Overview

Full-stack video anonymization app: upload a video, detect faces via AI, review detections in a visual editor, select which objects to blur, then export and download the anonymized result.

## Build Rules

Do not modify compiled or generated files. Always modify source files and rebuild.

## Tech Stack

- **Frontend**: Blazor WebAssembly (.NET 10) with MudBlazor UI library
- **Video Editor**: Embedded Vue 3 SPA (compiled, served via Blazor JS interop)
- **Backend**: ASP.NET Core minimal API
- **Processing**: OpenCvSharp (OpenCV .NET bindings) background worker
- **Detection**: Python FastAPI with ONNX Runtime / RetinaFace model
- **Messaging**: RabbitMQ (distributed) / Direct messaging abstraction (standalone)
- **Database**: PostgreSQL via EF Core (distributed) / In-memory (standalone)
- **Real-time**: SignalR for job progress/completion notifications

## Solution Structure

Key projects under `VideoAnonymizer.slnx`:

| Project | Role |
|---|---|
| `VideoAnonymizer.Web` | Blazor WASM frontend, SignalR client, download service |
| `VideoAnonymizer.Web.Contracts` | Shared DTOs (`AnalyzedFrameDto`, `DetectedObjectDto`, etc.) and API route constants |
| `VideoAnonymizer.Web.Modules` | Razor Class Library hosting the Vue video editor component |
| `VideoAnonymizer.ApiService` | ASP.NET Core API (analyze, anonymize, video serving endpoints + SignalR hub) |
| `VideoAnonymizer.VideoProcessor` | Background worker: frame extraction, blur processing, export |
| `VideoAnonymizer.ObjectDetection` | Python FastAPI face detection service |
| `VideoAnonymizer.ObjectDetectionClient` | .NET HTTP client for the Python detection API |
| `VideoAnonymizer.Database` | EF Core entities (`Video`, `AnalyzedFrame`, `DetectedObject`) |
| `VideoAnonymizer.Contracts` | RabbitMQ message types and constants |
| `VideoAnonymizer.AppHost` | .NET Aspire orchestrator |
| `VideoAnonymizer.Web.Tests` | bUnit + SpecFlow web component tests |
| `VideoAnonymizer.ApiService.IntegrationTests` | API integration tests |

## Data Flow (End-to-End)

### 1. Upload & Analyze
- `UploadTab.razor` -> user selects video, clicks "Detect Objects"
- `Home.DetectObjectsAsync()` -> `POST /analyze?detectionIntervalMs={ms}` (multipart)
- API saves video to disk + DB, publishes `video.analyze` RabbitMQ message
- `VideoAnalyzer` worker reads frames via OpenCvSharp, sends to Python detection API
- Detected objects stored with `Selected = true` (all selected by default)
- On completion: `video.analyzed` RabbitMQ message -> SignalR `videoAnalyzed` event
- Blazor receives event, calls `LoadAnalyzedFramesAsync()` (GET `/analyzed/{videoId}`), switches to Review tab

### 2. Review & Configure
- `ReviewExportTab.razor` renders `VideoEditor` (Blazor wrapper) -> Vue 3 editor
- Vue app shows: video player, bounding box previews, object list with toggle checkboxes, timeline
- User can adjust blur size (100-300%) and time buffer (0-1000ms)
- User can deselect objects to skip them during anonymization

### 3. Anonymize
- User clicks "Anonymize selected objects" button in `ReviewExportTab`
- `ReviewExportTab.OnStartAnonymizationClicked()` calls `_videoEditor.GetFramesAsync()` (JS interop) to get frame/selection state
- `Home.StartAnonymizationAsync()` -> `POST /anonymize/{videoId}` with frames + settings
- API updates frame/object selections in DB, publishes `video.anonymize` RabbitMQ message
- `VideoAnonymizer` worker iterates all frames, applies elliptical Gaussian blur to selected object regions

### 4. Download
- On completion: `video.anonymized` RabbitMQ message -> SignalR `videoAnonymized` event
- Blazor receives event -> triggers `DownloadAsync()` automatically
- `DownloadService.DownloadFileAsync()` calls JS `triggerFileDownload(fileName, url)` which creates an anchor element and clicks it
- API endpoint `GET /anonymized/{videoId}` streams the processed file

## Key Code Locations

### Frontend - Main Page
- `VideoAnonymizer.Web/Pages/Home.razor` - Page layout, tab structure, ReviewExportTab binding
- `VideoAnonymizer.Web/Pages/Home.razor.cs` - All event handlers: upload, analyze, anonymize, download. SignalR subscription setup in `OnInitializedAsync`
- `VideoAnonymizer.Web/Pages/Home.razor.js` - `triggerFileDownload()` JS function

### Frontend - Components
- `VideoAnonymizer.Web/Components/ReviewExportTab.razor` - Settings (blur size, time buffer), editor, anonymize button
- `VideoAnonymizer.Web/Components/UploadTab.razor` - File upload + detect button
- `VideoAnonymizer.Web/Components/StatusIndicator.razor` - Progress overlay

### Frontend - Services
- `VideoAnonymizer.Web/Services/IDownloadService.cs` / `DownloadService.cs` - JS interop file download
- `VideoAnonymizer.Web/Services/IJobHubClient.cs` / `JobHubClient.cs` - SignalR hub connection

### Vue Editor (within Web.Modules)
- `VideoAnonymizer.Web.Modules/ClientApp/video-editor/src/VideoEditorApp.vue` - Main Vue component
- `VideoAnonymizer.Web.Modules/ClientApp/video-editor/src/BoundingBoxOverlay.vue` - Blur preview boxes
- `VideoAnonymizer.Web.Modules/ClientApp/video-editor/src/ObjectList.vue` - Object toggle list
- `VideoAnonymizer.Web.Modules/ClientApp/video-editor/src/Timeline.vue` - Timeline visualization
- `VideoAnonymizer.Web.Modules/ClientApp/video-editor/src/types.ts` - TypeScript types
- `VideoAnonymizer.Web.Modules/wwwroot/js/videoEditorHost.js` - JS bridge for mounting Vue app

### Backend
- `VideoAnonymizer.ApiService/VideoAnonymizerApi.cs` - API endpoints: analyze, analyzed, video, anonymize, anonymized
- `VideoAnonymizer.ApiService/DataServices/VideoDataService.cs` - DB access
- `VideoAnonymizer.ApiService/Notifications/LongRunningJobsHub.cs` - SignalR hub
- `VideoAnonymizer.VideoProcessor/VideoAnonymizer.cs` - Core blur engine (OpenCvSharp)
- `VideoAnonymizer.VideoProcessor/AnonymizeVideoConsumer.cs` / `AnonymizeVideoHandler.cs` - RabbitMQ consumer

### Shared Constants
- `VideoAnonymizer.Web.Contracts/SharedConstants.cs` - API routes (`analyze`, `anonymize`, `video`, `anonymized`) and SignalR message keys (`videoAnalyzed`, `videoAnonymized`, `jobProgress`)
- `VideoAnonymizer.Web.Contracts/DTO/` - All request/response DTOs

## Common Tasks

### Adding a new UI component
- Place Blazor components in `VideoAnonymizer.Web/Components/`
- Follow MudBlazor patterns from existing components
- Register new DI services in `VideoAnonymizer.Web/Program.cs`

### Modifying the Vue editor
- Edit files in `VideoAnonymizer.Web.Modules/ClientApp/video-editor/src/`
- Rebuild with `npm run build` from the `video-editor` directory
- The compiled output is served as static assets from the RCL

### Adding an API endpoint
- Add route + handler in `VideoAnonymizer.ApiService/VideoAnonymizerApi.cs`
- Add path constant in `VideoAnonymizer.Web.Contracts/SharedConstants.cs`
- Add DTO if needed in `VideoAnonymizer.Web.Contracts/DTO/`

### Adding a background processing step
- Add a new consumer/handler pair in `VideoAnonymizer.VideoProcessor/`
- Register RabbitMQ bindings in the consumer
- Publish messages from the API service

### Running tests
- Web tests: `dotnet test VideoAnonymizer.Web.Tests/` (bUnit + SpecFlow)
- API integration tests: `dotnet test VideoAnonymizer.ApiService.IntegrationTests/`
- Python detection tests: in `VideoAnonymizer.ObjectDetectionTests/`

## Docker Setup

### Files

| File | Purpose |
|---|---|
| `Dockerfile` | Multi-stage build: Vue editor → OpenCvSharpExtern native bridge → .NET publish → runtime |
| `docker-compose.yml` | Single service, mounts `./docker-data:/data`, exposes port 5117 |
| `docker/docker-entrypoint.sh` | Starts .NET app (model download), waits for model, then waits for .NET process |
| `docker/appsettings.Docker.json` | Docker config: headless, absolute paths on `/data` volume |
| `docker/object-detection-wrapper.sh` | Prevents port conflict when .NET app's `ObjectDetectionProcessHostedService` runs |
| `.dockerignore` | Optimized build context (only source files + project dirs) |

### Build & Run

```bash
docker compose build     # first build: ~30-60 min (native bridge compilation + model download)
docker compose up -d     # start container
docker compose logs -f   # follow logs
# Open http://localhost:5117
```

Rebuild with `docker compose build` after code changes. The native bridge layer is cached.

### Data Volume

`./docker-data/` on the host (mounted at `/data` in the container):

```
docker-data/
  App_Data/Uploads/    # uploaded + anonymized videos
  models/              # downloaded FaceDetector.onnx (persistent)
```

Symlinked into `/app/` so existing code finds paths without changes. To reset, delete files in `./docker-data/`.

### Architecture Differences from Standalone

- **OpenCvSharp**: Native bridge (`libOpenCvSharpExtern.so`) built from source via CMake against system OpenCV from apt (not the NuGet `runtime.win` package, which is removed via `sed` in the Dockerfile)
- **Detection Service**: Launched by .NET's `ObjectDetectionProcessHostedService` (via the wrapper script) after model download completes
- **No browser launch**: `Standalone.OpenBrowser` set to `false`
- **GPU acceleration**: Runtime based on `nvidia/cuda:12.8.0-cudnn-runtime-ubuntu24.04` with `onnxruntime-gpu`; requires NVIDIA Container Toolkit and `deploy.resources.reservations.devices` with GPU capabilities in docker-compose
- **Data directory**: Absolutized to `/data` via `appsettings.Docker.json`; symlinks bridge into `/app/App_Data` and `/app/data`

### Common Build Failures

| Error | Fix |
|---|---|
| `opencv2/xfeatures2d.hpp` not found | Install `libopencv-contrib-dev`; add `-DNO_CONTRIB=ON` to cmake |
| `cv::barcode` not a member of `cv` | Delete `barcode.cpp` / `barcode.h` from OpenCvSharpExtern source before build |
| NuGet audit treats vulnerability as error | Pass `-p:NuGetAudit=false` to restore/publish |
| Blazor WASM Mono runtime not found for linux-x64 | Don't pass `-r linux-x64` to `dotnet restore` (only needed for publish) |

### Start-up Order (Container Runtime)

1. Entrypoint creates symlinks: `/app/App_Data` → `/data/App_Data`, `/app/data` → `/data`
2. `.NET` app starts in background → `StandaloneModelDownloadHostedService` downloads `FaceDetector.onnx` to `/data/models/`
3. Entrypoint polls for model file (up to 4 min)
4. `.NET` app's `ObjectDetectionProcessHostedService` starts Python detection service via wrapper script on port 8765
5. App is ready at `http://localhost:5117`

## Important Notes

- The solution has a **standalone mode**, a **distributed mode** (Aspire), and a **Docker mode** sharing the same app concepts
- `Home.razor.cs` manages all SignalR subscriptions in `OnInitializedAsync()` and implements `IAsyncDisposable` for cleanup
- The `DownloadAsync()` method is called automatically from the `videoAnonymized` SignalR handler (no download button)
- `SelectedFileName` is preserved from the initial file selection (not nullified after analysis) to ensure correct download filename
- Video files are stored on disk; the API serves them via `PhysicalFile()` with range processing support
- The Vue editor communicates with Blazor via JS interop (`GetFramesAsync()` / property updates on the mounted Vue app)
