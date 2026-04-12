# 🎥 VideoAnonymizer

> Privacy-first video anonymization powered by Computer Vision

Automatically detect and anonymize sensitive information in videos — such as faces, license plates, or other identifiable objects — using a modern, scalable architecture.

---

## ✨ Features

- 🎯 Automatic face detection (powered by RetinaFace)
- 🎬 Video anonymization by blurring sensitive regions
- ⚡ Asynchronous processing with RabbitMQ
- 🌐 Web-based UI built with Blazor
- 🧠 Python AI service based on FastAPI
- 🧩 Modular distributed architecture with .NET Aspire
- 🔌 Designed for local and cloud deployment

---

## 🎞️ Demo

### Before / After

![Before After Demo](docs/img/demo_slider_reveal.gif)

### Editor Preview (Planned / WIP)

- Select detected objects
- Timeline-based editing
- Fine-grained control over anonymization

---

## 🏗️ Architecture Overview

The system is built as a distributed service architecture with the following main components:

- **webfrontend** – Blazor WebAssembly user interface
- **apiservice** – .NET backend exposing a JSON API
- **video processor** – background worker for frame extraction, blurring, and video generation
- **objectDetection** – Python FastAPI service for object detection
- **RabbitMQ** – asynchronous job queue
- **PostgreSQL** – storage for metadata, analysis results, and processing state
- **modeldownloader** – startup component that ensures the required AI model is available
- **database migration service** – applies schema migrations automatically on startup

The architecture is designed to scale individual components (e.g. AI processing) independently.

### Processing Flow

1. The user uploads a video through the frontend.
2. The API stores metadata and creates a processing job.
3. The job is published to RabbitMQ.
4. The video processor consumes the job and extracts frames.
5. The processor sends frames to the Python detection service.
6. Detected regions are returned and blurred in the video.
7. The processed video is reassembled and stored.
8. The user can download the anonymized result.

---

## 🔌 API Overview

The backend exposes a REST-like JSON API for asynchronous video processing workflows.

### Endpoints

```http
POST   /upload
POST   /analyze/{videoId}
POST   /anonymize/{videoId}
GET    /analyzed/{videoId}
```

### Typical Flow

✔ Actual flow (today)
```text
1. Upload a video
2. Start analysis and anonymization
3. Download the processed video
```
🚧 Planned (Editor / Review UI)
```text
1. Upload a video
2. Start analysis
3. Review detected objects
4. Start anonymization
5. Download the processed video
```

---

## 🚀 Getting Started

### Requirements

- .NET 10
- Python 3.10+
- Docker
- GPU (optional, for faster inference)
- A development HTTPS certificate may be required:
```bash
dotnet dev-certs https --trust
```

### Platform Notes
The current development setup is primarily tested on Windows.
The video processing component currently depends on native OpenCvSharp runtime support and is not fully configured for Linux/WSL yet.


### Clone the repository

```bash
git clone https://github.com/Imagonix/VideoAnonymizer.git
cd VideoAnonymizer
```

### First-time setup

Initialize development secrets:

```bash
./setup-dev.ps1
```

### Start the application

```bash
dotnet run --project VideoAnonymizer.AppHost
```

This starts the distributed application, including:

- frontend
- API
- RabbitMQ
- PostgreSQL
- Python object detection service

### Open the web UI

Open the URL shown by the Aspire dashboard or console output.

---

## 🤖 AI Model

- Face detection is based on **RetinaFace (ONNX)**.
- The model file is **not included in the repository**
- It is automatically downloaded on startup via the `modeldownloader` service
- No manual setup is required

> Note: The model is fetched from a public source and stored locally.

---

## 🧪 Tech Stack

### Backend

- .NET 10
- ASP.NET Core
- Entity Framework Core
- RabbitMQ
- SignalR

### Frontend

- Blazor WebAssembly
- MudBlazor

### AI / Processing

- Python
- FastAPI
- RetinaFace (ONNX)
- OpenCV

### Infrastructure

- .NET Aspire
- PostgreSQL
- RabbitMQ
- Docker

---

## 🛣️ Roadmap

- [ ] Bounding box editor
- [ ] Timeline-based object selection
- [ ] Additional object types such as license plates, labels, and addresses
- [ ] Improved tracking between frames
- [ ] Cloud deployment option

---

## 💡 Use Cases

- Privacy-safe video sharing
- Dashcam footage anonymization
- Content creation workflows
- Research datasets
- Internal company documentation and demos

---

## Testing & Quality

The application includes behavior-driven integration tests to verify end-to-end workflows such as:

- Uploading a video
- Running the analysis pipeline
- Receiving completion events via SignalR
- Retrieving the anonymized result

This ensures that the full system behaves correctly across service boundaries.

---

## 📦 Deployment

The architecture is intended to support both:

- **local execution** for simple desktop usage
- **hosted deployment** for browser-based access across devices

---

## 📄 License

MIT License

---

## Project Purpose

This project was created as a reference implementation demonstrating:

- distributed system design with .NET Aspire
- asynchronous processing pipelines
- integration of AI (computer vision) into real-world workflows

Designed and implemented as a full-stack distributed system from scratch.
