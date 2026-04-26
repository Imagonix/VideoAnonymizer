# VideoAnonymizer - Agent Guide

## Project Overview

Full-stack video anonymization app: upload a video, detect faces via AI, review detections in a visual editor, select which objects to blur, then export and download the anonymized result.

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

## Important Notes

- The solution has a **standalone mode** and a **distributed mode** sharing the same app concepts
- `Home.razor.cs` manages all SignalR subscriptions in `OnInitializedAsync()` and implements `IAsyncDisposable` for cleanup
- The `DownloadAsync()` method is called automatically from the `videoAnonymized` SignalR handler (no download button)
- `SelectedFileName` is preserved from the initial file selection (not nullified after analysis) to ensure correct download filename
- Video files are stored on disk; the API serves them via `PhysicalFile()` with range processing support
- The Vue editor communicates with Blazor via JS interop (`GetFramesAsync()` / property updates on the mounted Vue app)
