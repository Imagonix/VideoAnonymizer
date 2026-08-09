# VideoAnonymizer - Agent Guide

## Project Overview

Full-stack video anonymization app: upload a video, detect configured sensitive objects such as faces and license plates via AI, review detections in a visual editor, select which objects to blur, then export and download the anonymized result.

## Build Rules

Do not modify compiled or generated files. Always modify source files and rebuild.
Reqnroll `.feature.cs` code-behind files are generated during a normal build/test, are ignored by Git, and must not be created, edited, or staged manually. Edit the `.feature` source and step definitions instead.

## Naming Conventions

Extension method files and classes are named after the type being extended. For interfaces, drop the `I` prefix (e.g., `IHostApplicationBuilder` → `HostApplicationBuilderExtensions`, `IServiceProvider` → `ServiceProviderExtensions`). For classes, use the class name directly (e.g., `VideoAnonymizerDbContext` → `VideoAnonymizerDbContextExtensions`).

## Tech Stack

- **Frontend**: Blazor WebAssembly (.NET 10) with MudBlazor UI library
- **Video Editor**: Embedded Vue 3 SPA (compiled, served via Blazor JS interop)
- **Backend**: ASP.NET Core minimal API
- **Processing**: OpenCvSharp (OpenCV .NET bindings) background worker
- **Detection**: Python FastAPI with ONNX Runtime and configured ONNX detector models
- **Messaging**: RabbitMQ (distributed) / Direct messaging abstraction (standalone)
- **Database**: PostgreSQL via EF Core (distributed) / SQLite via EF Core (standalone)
- **Real-time**: SignalR for job progress/completion notifications

## Solution Structure

Key projects under `VideoAnonymizer.slnx`:

| Project | Role |
|---|---|
| `VideoAnonymizer.Web` | Blazor WASM frontend, SignalR client, download service |
| `VideoAnonymizer.Web.Contracts` | Shared DTOs (`AnalyzedFrameDto`, `DetectedObjectDto`, etc.) and API route constants |
| `VideoAnonymizer.Web.Modules` | Razor Class Library hosting the Vue video editor component + action classes (`ObjectAddedAction`, `ObjectUpdatedAction`, `ObjectsBulkUpdatedAction`, `UndoAction`, `RedoAction`) |
| `VideoAnonymizer.ApiService` | ASP.NET Core API (analyze, anonymize, video serving endpoints + SignalR hub) |
| `VideoAnonymizer.VideoProcessor` | Background worker: frame extraction, blur processing, export |
| `VideoAnonymizer.ObjectDetection` | Python FastAPI object detection service that loads detector configs from the models folder |
| `VideoAnonymizer.ObjectDetectionClient` | .NET HTTP client for the Python detection API |
| `VideoAnonymizer.Database` | EF Core entities (`Video`, `AnalyzedFrame`, `DetectedObject`, `EditorAction`) |
| `VideoAnonymizer.Database.Postgres` | PostgreSQL provider — migrations + `AddPostgresVideoAnonymizerDbContext[Factory]()` |
| `VideoAnonymizer.Database.SQLite` | SQLite provider — migrations + `AddSqliteVideoAnonymizerDbContext[Factory]()` + design-time factory |
| `VideoAnonymizer.Contracts` | RabbitMQ message types and constants |
| `VideoAnonymizer.AppHost` | .NET Aspire orchestrator |
| `VideoAnonymizer.Web.Tests` | bUnit + Reqnroll web component tests |
| `VideoAnonymizer.ApiService.Tests` | Lightweight non-Docker API/service persistence tests |
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
- Vue app shows the video player with unblurred colored region outlines, collapsed/expanded timelines, a right-side editor toolbar, and a draggable three-panel inspector (`Current occurrence`, `Current segment`, `Entire track`)
- Blur size is configured at video, materialized-track, or occurrence scope; time buffers are configured at video scope or on segment boundaries. There is no track-wide time-buffer control or track entity
- The inspector inclusion checkbox changes only the selected occurrence; timeline track checkboxes bulk-change every occurrence in that track. Excluded occurrences remain reselectable as ghost outlines in the preview

### 3. Anonymize
- User clicks `Export anonymized video` in `ReviewExportTab`
- `ReviewExportTab.OnStartAnonymizationClicked()` calls `_videoEditor.GetFramesAsync()` (JS interop) to get frame/selection state
- `Home.StartAnonymizationAsync()` -> `POST /anonymize/{videoId}` with frames + settings
- API updates frame/object selections in DB, publishes `video.anonymize` RabbitMQ message
- `VideoAnonymizer` worker iterates all frames, applies the configured blur shape to selected object regions

### 4. Download
- On completion: `video.anonymized` RabbitMQ message -> SignalR `videoAnonymized` event
- Blazor receives event -> triggers `DownloadAsync()` automatically
- After a successful export, a compact Download button beside Export can retry/recover the download of the latest completed result; it is unavailable while a newer export is running
- `DownloadService.DownloadFileAsync()` calls JS `triggerFileDownload(fileName, url)` which creates an anchor element and clicks it
- API endpoint `GET /anonymized/{videoId}` streams the processed file

### 2b. Track Forward (Streaming)
- User selects an occurrence and clicks `Track forward` in the Advanced section of `ObjectDetailsPanel`
- Vue calls `trackForward(obj)` — immediately shows a pulsating dot at the next analyzed frame position in the timeline
- `TrackForwardAction` is dispatched → `ReviewExportTab.ApplyTrackForwardAsync()` pre-populates `_pendingTrackForwardJobs`, then POSTs to `POST /analyzed/{videoId}/tracks/track-forward`
- API saves a `TrackForwardJob` to DB, publishes `video.track-forward` RabbitMQ message
- `SingleObjectTracker` worker receives the job, calls Python `POST /trackForward` (now SSE streaming)
- Python opens the video, initializes the OpenCV tracker on the seed frame, then iterates frame-by-frame
- **For each tracked frame**: Python yields a `data: {"type":"detection",...}` SSE event → .NET reads the stream → saves the detected object to DB → publishes `TrackForwardProgress` (RabbitMQ) → API queries new DTOs → pushes `trackForwardProgress` SignalR event
- Blazor receives each progress event:
  - Calls `PushChangesToVue(new DetectedObjectChangeSet { ObjectsToAdd = [...] })` — new object dots appear in the timeline in real-time
  - Calls `PushTrackingProgress(frameTimeMs, frameTimeMs)` — the pulsating dot moves to the next frame position
- On break (end of video, max duration, lost timeout): Python yields `{"type":"complete"}` → worker publishes final `TrackForwardProgress` + `TrackForwardCompleted`
- `ReviewExportTab.OnTrackForwardCompletedAsync` records the action for undo/redo

### 5. Action Persistence (Undo/Redo across page reload)
- Every editor action (add, update, bulk-update, delete, settings, track-forward) is recorded via `POST /video/{videoId}/actions` after the data mutation succeeds. The action's relevant DTOs are serialized to JSON in the `Data` column of the `EditorAction` table.
- On page load, `ReviewExportTab.OnParametersSetAsync` calls `LoadActionHistoryAsync()` which GETs all non-undone actions and reconstructs the `_undoRedoState` stack via `VideoEditorUndoRedoState.DeserializeActions()`.
- Undo/redo toggles the `Undone` flag via `PUT /video/{videoId}/actions/{actionId}/undone` while the `VideoEditorActionPersister.ApplyUndoRedoAsync()` still handles the actual CRUD inversion.
- Track-forward specifically:
  - The `TrackForwardCompletedMessage` carries `List<DetectedObjectDto> CreatedObjects` (populated by the API notification handler querying the DB after completion)
  - These DTOs are stored on `ActionHistoryItem.CreatedObjectDtos` for redo
  - On undo: bulk-delete objects by ID
  - On redo: re-POST each cached DTO to create them again

## Key Code Locations

### Frontend - Main Page
- `VideoAnonymizer.Web/Pages/Home.razor` - Page shell: fixed compact left icon navigation rail (Import / Review & Export) with the CPU/GPU `LocalRuntimeFeedback` pinned below a divider at the rail bottom; active view rendered in a `content-area` that does not shift; `_activeTabIndex` drives the switch. ReviewExportTab binding
- `VideoAnonymizer.Web/Pages/Home.razor.cs` - All event handlers: upload, analyze, anonymize, download. SignalR subscription setup in `OnInitializedAsync`
- `VideoAnonymizer.Web/Pages/Home.razor.js` - `triggerFileDownload()` JS function

### Frontend - Components
- `VideoAnonymizer.Web/Components/ReviewExportTab.razor` - Video-level settings, editor, Export/Download controls, sync status indicator (save icon / spinner tied to channel state), action handler (switch on `VideoEditorAction`), action history persistence
- `VideoAnonymizer.Web/Components/ReviewExport/ActionPersistenceData.cs` - Internal JSON serialization records per action type
- `VideoAnonymizer.Web/Components/ReviewExport/VideoEditorUndoRedoState.cs` - Undo/redo stack with persisted ActionId, DeserializeActions() for page reload recovery
- `VideoAnonymizer.Web/Components/UploadTab.razor` - File upload + detect button + existing videos list with click-to-open; newest-upload-first table sortable by Filename/Uploaded columns; delete requires an explicit "Delete working copy" confirmation dialog
- `VideoAnonymizer.Web/Components/StatusIndicator.razor` - Progress overlay

### Frontend - Services
- `VideoAnonymizer.Web/Services/IDownloadService.cs` / `DownloadService.cs` - JS interop file download
- `VideoAnonymizer.Web/Services/IJobHubClient.cs` / `JobHubClient.cs` - SignalR hub connection

### Vue Editor (within Web.Modules)
- `VideoAnonymizer.Web.Modules/Actions/VideoEditorAction.cs` - Action class hierarchy (`ObjectAddedAction`, `ObjectUpdatedAction`, `ObjectsBulkUpdatedAction`, `UndoAction`, `RedoAction`); single `OnAction` callback dispatched via switch in `ReviewExportTab`; actions carry `OperationType` string (`"toggle"`, `"merge"`, `"split"`, `"reassign"`, `"move"`, `"resize"`) propagated from Vue
- `VideoAnonymizer.Web.Modules/Components/VideoEditor.razor.cs` - Thin Blazor/Vue bridge; JS-invokable methods immediately forward `VideoEditorAction` records via `OnAction`
- `VideoAnonymizer.Web.Modules/ClientApp/video-editor/src/VideoEditorApp.vue` - Main Vue composition root: selection/mode state, inspector placement, preview regions, toolbar and timeline wiring, Blazor delta application, and track-forward progress
- `VideoAnonymizer.Web.Modules/ClientApp/video-editor/src/EditorToolbar.vue` - Fixed right-side icon toolbar for Add, Confirm, Merge, Split, and Discard
- `VideoAnonymizer.Web.Modules/ClientApp/video-editor/src/ObjectDetailsPanel.vue` - Draggable grouped inspector with separate occurrence, consecutive-segment, and entire-track panels plus Advanced operations
- `VideoAnonymizer.Web.Modules/ClientApp/video-editor/src/CollapsedTimelineBar.vue` - Compact single-row playback/timeline presentation used while collapsed and as the aligned ruler/header band when expanded
- `VideoAnonymizer.Web.Modules/ClientApp/video-editor/src/TimelineRow.vue` - Row of occurrence dots; supports split-mode dot clicking with Ctrl/Shift selection; renders a pulsating dot (in the track's color) at the frame being tracked during track forward progress
- `VideoAnonymizer.Web.Modules/ClientApp/video-editor/src/TimelineRowLabel.vue` - Vertically aligned track label with inclusion checkbox, lazy representative thumbnail, and editable Track ID
- `VideoAnonymizer.Web.Modules/ClientApp/video-editor/src/BoundingBoxOverlay.vue` - Unblurred region/ghost overlay with sharp editing handles and Move/Resize/Add interaction support
- `VideoAnonymizer.Web.Modules/ClientApp/video-editor/src/Timeline.vue` - Timeline visualization
- `VideoAnonymizer.Web.Modules/ClientApp/video-editor/src/TrackThumbnail.vue` - Lazy, unblurred representative occurrence crop for a track
- `VideoAnonymizer.Web.Modules/ClientApp/video-editor/src/VideoPlayer.vue` - Video playback bridge; intentionally suppresses programmatic seeks whose difference is at most 50 ms to avoid playback stutter
- `VideoAnonymizer.Web.Modules/ClientApp/video-editor/src/MudLikeCheckbox.vue` - Custom checkbox mimicking MudBlazor style
- `VideoAnonymizer.Web.Modules/ClientApp/video-editor/src/types.ts` - TypeScript DTOs and editor types; `EditorMode = 'select' | 'merge' | 'split' | 'adjust' | 'add'`
- `VideoAnonymizer.Web.Modules/ClientApp/video-editor/src/services/ColorManager.ts` - HSL color assignment per object/track
- `VideoAnonymizer.Web.Modules/ClientApp/video-editor/src/services/TrackThumbnailService.ts` - Shared detached-video thumbnail extraction queue/cache
- `VideoAnonymizer.Web.Modules/ClientApp/video-editor/src/composables/useBlurPreviewObjects.ts` - Resolves stored/interpolated/extrapolated current-frame preview regions and effective blur size
- `VideoAnonymizer.Web.Modules/ClientApp/video-editor/src/composables/useConsecutiveTrackSegment.ts` - Builds/normalizes consecutive same-track segments from the complete ordered analyzed-frame sequence
- `VideoAnonymizer.Web.Modules/ClientApp/video-editor/src/composables/useEditorModes.ts` - Mutually-exclusive mode state machine
- `VideoAnonymizer.Web.Modules/ClientApp/video-editor/src/composables/useMerge.ts` - Merge selection + execution with duplicate trackId prevention
- `VideoAnonymizer.Web.Modules/ClientApp/video-editor/src/composables/useOccurrenceSelection.ts` - Ctrl/Shift dot occurrence selection
- `VideoAnonymizer.Web.Modules/ClientApp/video-editor/src/composables/useSplit.ts` - Split execution assigning new trackIds
- `VideoAnonymizer.Web.Modules/ClientApp/video-editor/src/composables/useTrackSettings.ts` - Applies occurrence/segment/materialized-track settings and mirrored gap-boundary state
- `VideoAnonymizer.Web.Modules/ClientApp/video-editor/src/utils/motionPrediction.ts` - Raw current-frame interpolation/extrapolation matching processor state transitions
- `VideoAnonymizer.Web.Modules/ClientApp/video-editor/src/utils/projectedRegion.ts` - Final frame-intersection and fully-outside checks without mutating raw motion geometry
- `VideoAnonymizer.Web.Modules/ClientApp/video-editor/src/utils/keys.ts` - Timeline key derivation helpers (`buildObjectKey`, `getTimelineKey`, `getObjTimelineKey`)
- `VideoAnonymizer.Web.Modules/wwwroot/js/videoEditorHost.js` - JS bridge for mounting Vue app; exports `updateTrackingProgress()` for per-frame dot positioning during track forward

### Backend
- `VideoAnonymizer.ApiService/Controllers/VideosController.cs` - Video endpoints: analyze, analyzed, video, anonymize, anonymized
- `VideoAnonymizer.ApiService/Controllers/DetectedObjectsController.cs` - Review editor object persistence endpoints
- `VideoAnonymizer.ApiService/Controllers/ActionsController.cs` - Action history CRUD (record, list, toggle undone)
- `VideoAnonymizer.ApiService/DataServices/VideoDataService.cs` - Video DB access
- `VideoAnonymizer.ApiService/DataServices/DetectedObjectDataService.cs` - Detected object DB access
- `VideoAnonymizer.ApiService/DataServices/EditorActionDataService.cs` - Action persistence with auto-incrementing SequenceNumber
- `VideoAnonymizer.ApiService/Notifications/LongRunningJobsHub.cs` - SignalR hub
- `VideoAnonymizer.Database/EditorAction.cs` - Action entity: ActionType, Data (JSON), Undone flag, SequenceNumber
- `VideoAnonymizer.VideoProcessor/Anonymization/VideoAnonymizer.cs` - Core blur engine (OpenCvSharp), applying resolved anonymization regions to exported frames
- `VideoAnonymizer.VideoProcessor/Anonymization/RelevantDetectedObjectSelector.cs` - Resolves stored/interpolated/extrapolated regions for the export's current frame
- `VideoAnonymizer.VideoProcessor/Anonymization/AnonymizationSettingsResolver.cs` - Resolves video/track/occurrence blur, segment buffers, and nullable gap modes
- `VideoAnonymizer.VideoProcessor/Anonymization/ConsecutiveSegmentResolver.cs` - Builds consecutive same-track segments using analyzed-frame adjacency
- `VideoAnonymizer.VideoProcessor/Anonymization/ProjectedRegionClipper.cs` - Clips a copy of the final projected region immediately before rasterization
- `VideoAnonymizer.VideoProcessor/Analysis/Tracking/SingleObjectTracker.cs` - Track forward job consumer, publishes per-frame progress via SSE streaming
- `VideoAnonymizer.VideoProcessor/Analysis/Tracking/ForwardTrackingService.cs` - Resolves seed frame, calls Python tracker, persists detections
- `VideoAnonymizer.ApiService/Notifications/TrackForwardProgressNotificationHandler.cs` - Queries DB for new objects and pushes `trackForwardProgress` SignalR event
- `VideoAnonymizer.ApiService/Notifications/TrackForwardProgressConsumer.cs` - RabbitMQ consumer for per-frame progress

### Shared Constants
- `VideoAnonymizer.Web.Contracts/SharedConstants.cs` - API routes (`analyze`, `anonymize`, `video`, `anonymized`, `actions`, `undone`) and SignalR message keys (`videoAnalyzed`, `videoAnonymized`, `trackForwardCompleted`, `trackForwardProgress`, `jobProgress`)
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
- Add route + handler in the relevant controller under `VideoAnonymizer.ApiService/Controllers/`
- Add path constant in `VideoAnonymizer.Web.Contracts/SharedConstants.cs`
- Add DTO if needed in `VideoAnonymizer.Web.Contracts/DTO/`
- Use `HttpPost` for create, `HttpPut` for single update, `HttpPatch` for bulk update, `HttpDelete` for delete

### Adding a database migration
Run from `VideoAnonymizer.Database/`:
```bash
# Postgres
dotnet ef migrations add <Name> --project ../VideoAnonymizer.Database.Postgres/ --startup-project ../VideoAnonymizer.Database.MigrationService/ --output-dir Migrations
# SQLite
dotnet ef migrations add <Name> --project ../VideoAnonymizer.Database.SQLite/ --output-dir Migrations
```
Or use the script: `.\add-migrations.ps1 -Name "<Name>"` from `VideoAnonymizer.Database/`
Note: SQLite project must be built first (`dotnet build ../VideoAnonymizer.Database.SQLite/`) before running `dotnet ef migrations add` against it, since it acts as its own design-time factory host without a separate startup project.

### Adding a background processing step
- Add a new consumer/handler pair in `VideoAnonymizer.VideoProcessor/`
- Register RabbitMQ bindings in the consumer
- Publish messages from the API service

### Running tests
- Web tests: `dotnet test VideoAnonymizer.Web.Tests/` (bUnit + Reqnroll)
- API service tests: `dotnet test VideoAnonymizer.ApiService.Tests/`
- API integration tests: `dotnet test VideoAnonymizer.ApiService.IntegrationTests/`
- Python detection tests: in `VideoAnonymizer.ObjectDetectionTests/`
- Vue editor integration tests: `npm test` from `VideoAnonymizer.Web.Modules/ClientApp/video-editor/` (vitest + jsdom)

### Test Conventions
- Gherkin scenarios should describe user stories in human-readable language.
- New .NET behavior and regression tests should be written as `.feature` scenarios with Reqnroll step definitions, so the tested behavior is readable in Gherkin. Avoid direct NUnit test classes unless the test is a very small technical helper test where Gherkin would make the intent less clear.
- Prefer one `When` per scenario. Split scenarios when multiple user actions would otherwise require multiple `When` steps.
- Reqnroll step definitions should store scenario state in `ScenarioContext`, following the pattern in `HomeStepDefinitions`, instead of keeping mutable instance fields.
- Reqnroll `.feature.cs` files are not versioned. A normal `dotnet build` or `dotnet test` generates them from `.feature` files; `--no-build` assumes the test assembly was already built. Never edit or stage generated code-behind files.
- Vue `.feature` tests are executed by the Vitest feature runner, not by Reqnroll, so Visual Studio Reqnroll navigation does not apply to those files.
- When fixing build or test failures, fix the root cause at the failing dependency, configuration, or behavior boundary first. Do not add defensive cleanup, null checks, retries, or other robustness changes merely to suppress follow-on failures unless the user explicitly asks for that hardening or the follow-on failure is itself the root defect being addressed.

### Refactoring Guidance
- Do not extract single-use helper methods unless the surrounding method is becoming hard to read.
- Prefer extracting cohesive static logic into a named helper class when it represents a real concept.
- Keep worker/orchestration classes focused on workflow; move reusable selection, mapping, or geometry logic out when it becomes independently testable.

### CI Notes
- The standalone workflow should run lightweight API tests with `dotnet test VideoAnonymizer.ApiService.Tests/...`.
- Keep Dockerfile and `.dockerignore` focused on runtime/build projects. Do not add test projects to the Docker build context unless Docker builds start running tests.

## Docker Setup

### Files

| File | Purpose |
|---|---|
| `Dockerfile` | Multi-stage build: Vue editor → OpenCvSharpExtern native bridge → .NET publish → runtime |
| `docker-compose.yml` | Single service, mounts `./docker-data:/data`, exposes port 5117 |
| `docker/docker-entrypoint.sh` | Seeds bundled detector models, starts .NET app, then waits for .NET process |
| `docker/appsettings.Docker.json` | Docker config: headless, absolute paths on `/data` volume |
| `docker/object-detection-wrapper.sh` | Prevents port conflict when .NET app's `ObjectDetectionProcessHostedService` runs |
| `.dockerignore` | Optimized build context (only source files + project dirs) |

### Build & Run

```bash
docker compose build     # first build: ~30-60 min (native bridge compilation)
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
  models/              # bundled model copy + additional local detector models
```

Symlinked into `/app/` so existing code finds paths without changes. To reset, delete files in `./docker-data/`.

### Architecture Differences from Standalone

- **OpenCvSharp**: Native bridge (`libOpenCvSharpExtern.so`) built from source via CMake against system OpenCV from apt (not the NuGet `runtime.win` package, which is removed via `sed` in the Dockerfile)
- **Detection Service**: Launched by .NET's `ObjectDetectionProcessHostedService` (via the wrapper script) after bundled model files are available
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
2. Entrypoint copies bundled detector model/config files to `/data/models/` when missing; user-added detector pairs in that folder are preserved
3. `.NET` app starts in background
4. `.NET` app's `ObjectDetectionProcessHostedService` starts Python detection service via wrapper script on port 8765
5. App is ready at `http://localhost:5117`

## Important Notes

- The solution has a **standalone mode**, a **distributed mode** (Aspire), and a **Docker mode** sharing the same app concepts
- There is no standalone `Video Anonymizer` heading/header row on the Home page; navigation uses the fixed left icon rail, and the CPU/GPU mode indicator lives in the same rail's bottom slot (`LocalRuntimeFeedbackMode.Compact`).
- `Home.razor.cs` manages all SignalR subscriptions in `OnInitializedAsync()` and implements `IAsyncDisposable` for cleanup
- The `videoAnonymized` SignalR handler calls `DownloadAsync()` automatically. `ReviewExportTab` also exposes a compact Download recovery button beside Export after a successful result; it always targets the latest completed export and is blocked while a newer export is running.
- `SelectedFileName` is preserved from the initial file selection (not nullified after analysis) to ensure correct download filename
- Video files are stored on disk; the API serves them via `PhysicalFile()` with range processing support
- The Vue editor communicates with Blazor via JS interop (`GetFramesAsync()` / property updates on the mounted Vue app)
- The `trackForwardCompleted` SignalR message carries `List<DetectedObjectDto> CreatedObjects` (full DTOs of newly tracked objects, queried from DB by the API notification handler) for redo support
- Track forward uses **SSE streaming** from the Python API (`POST /trackForward` returns `text/event-stream`). Per-frame `TrackForwardProgress` messages flow through RabbitMQ → SignalR → Blazor, driving real-time pulsating dots in the timeline and incremental object appearance. The `TrackForwardProgress` contract, consumer, and SignalR handler follow the same pattern as `TrackForwardCompleted`.
- When initiating track forward, `_pendingTrackForwardJobs` is pre-populated **before** the HTTP call to avoid dropping early progress messages.
- Video-level blur size and time buffer are persisted on change via `PUT /video/{videoId}/settings` and go through the `VideoEditor` operation channel.
- **Anonymization scope hierarchy** (materialized-track model, no track entity): effective blur = `OccurrenceBlurSizePercentOverride ?? BlurSizePercentOverride ?? Video.BlurSizePercent`; segment pre/post = `first.PreBufferMsOverride/last.PostBufferMsOverride ?? Video.TimeBufferMs`. `BlurSizePercentOverride` and blur shape are materialized on every occurrence in a track; occurrence-level blur lives only on one stored occurrence. Track-wide edits bulk-update every occurrence through the action queue; server/streamed objects arrive normalized. There is intentionally no materialized or UI track-wide time-buffer value and no track-level `Mixed` time state.
- **Consecutive segment semantics**: order the complete analyzed-frame sequence by `FrameIndex` (falling back to time only where the frontend DTO requires it). Same-track occurrences are consecutive when they appear in successive analyzed-frame entries. An intervening analyzed frame without that track breaks the segment; a numeric `FrameIndex` jump alone does not. Only the segment's first occurrence may own `PreBufferMsOverride`; only its last may own `PostBufferMsOverride` and following-gap state.
- **Gap handling boundary**: `DetectedObject.NextGapHandlingMode` is nullable `GapHandlingMode` in the EF model and is persisted as its string name. The DTO/TypeScript boundary remains nullable `"Interpolate"`/`"UseBuffers"`; `null` resolves to Interpolate at a real same-track gap. The mode is canonical only on the last occurrence before that gap. `Interpolate gap after` on the preceding segment and `Interpolate gap before` on the following segment edit the same boundary. Buffer values are retained while interpolation makes them inactive.
- **Current-frame region geometry**: interpolation/extrapolation keeps raw x/y/width/height unbounded. Crossing a frame edge must not clamp the center, shrink the raw box, snap it back, or feed clipped geometry into later motion. Preview overflow and export rasterization clip only the final visible intersection; the region disappears when fully outside or after its active buffer. The preview remains an unblurred colored outline, but its effective shape, blur-size enlargement, presence, and visible intersection must match the region selected for export.
- **Timeline seek tolerance**: Previous/Next and occurrence dots request stored occurrence times, but `VideoPlayer.vue` intentionally ignores a programmatic time change when its absolute difference from the element's current time is at most `0.05` seconds. This tolerance prevents video stutter and is accepted even when two occurrences are closer together; do not lower or remove it without an explicit product decision.
- VideoEditor operations use a command pattern: `VideoEditorAction` records dispatched through a single `OnAction` callback with a switch in `ReviewExportTab`
- **Undo/Redo**: Blazor owns the authoritative undo/redo stack and the action queue in `ReviewExportTab`. Every action is recorded via `POST /video/{videoId}/actions` and persisted to the `EditorAction` DB table. On page reload, `VideoEditorUndoRedoState.DeserializeActions()` reconstructs the stack from the API. Object update actions carry `BeforeState` plus the updated object payload, while settings actions carry `BeforeState` and `AfterState`. Vue sends Ctrl+Z/Y as `onUndo`/`onRedo` signals (no payload). Blazor serializes pending saves before undo/redo, applies the inverse HTTP call, toggles the `Undone` flag via `PUT /video/{videoId}/actions/{actionId}/undone`, then pushes a `DetectedObjectChangeSet` delta to Vue via `applyDetectedObjectChanges` JS bridge. A Blazor overlay blocks editor input when undo/redo is requested while earlier actions are pending. New actions clear any redo history (actions after current index).
- **Blazor → Vue state propagation**: Blazor pushes state to Vue via dedicated JS bridge functions (`updateVideoEditorSettings`, `applyDetectedObjectChanges`). These are defined in `videoEditorHost.js` and exposed as `AppHandle` methods in `main.ts`, updating the reactive `state` proxy.
- HTTP execution and queueing logic lives in `ReviewExportTab` and its `ReviewExport/` helper classes. `VideoEditor` only bridges Vue events and JS interop calls.
- The upload tab shows a list of existing videos loaded from `GET /videos`; clicking a row opens the video directly (no separate button)
- Every `Video` stores a required `UploadedAtUtc` (set from the server UTC clock on upload); the imported-video table renders it in local time and orders newest-first by default with deterministic filename/ID tie-breaking. Deleting a working copy always requires an explicit confirmation dialog.

## API data boundary

- Controllers must not receive, return, or otherwise depend on EF Core entity types from `VideoAnonymizer.Database`.
- Data services own EF entity access and map query/create results to DTOs before returning them.
- Prefer the existing `Mapper` extensions for entity-to-DTO conversion.
- Data-service parameters may use DTOs, scalar values, or appropriate non-entity types such as `IFormFile` when required by the operation.
