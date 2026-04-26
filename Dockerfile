FROM node:20-alpine AS vue-builder
WORKDIR /src
COPY VideoAnonymizer.Web.Modules/ClientApp/video-editor/package.json \
     VideoAnonymizer.Web.Modules/ClientApp/video-editor/package-lock.json ./
RUN npm ci
COPY VideoAnonymizer.Web.Modules/ClientApp/video-editor/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS opencvsharp-bridge
RUN apt-get update && apt-get install -y \
    build-essential cmake git ca-certificates \
    libopencv-dev libopencv-contrib-dev \
    libavcodec-dev libavformat-dev libswscale-dev \
    libdc1394-dev libtiff-dev libopenjp2-7-dev \
    && rm -rf /var/lib/apt/lists/*

RUN git clone --depth 1 --branch 4.13.0.20260317 \
    https://github.com/shimat/opencvsharp.git /src/opencvsharp
RUN cd /src/opencvsharp/src/OpenCvSharpExtern && \
    rm -f barcode.cpp barcode.h dnn_superres.cpp dnn_superres.h \
          wechat_qrcode.cpp wechat_qrcode.h
RUN mkdir -p /src/opencvsharp/src/build && cd /src/opencvsharp/src/build && \
    cmake -DCMAKE_BUILD_TYPE=Release \
          -DNO_CONTRIB=ON \
          -DNO_HIGHGUI=ON \
          -DCMAKE_INSTALL_PREFIX=/opt/opencvsharp \
          .. && \
    make -j$(nproc) && \
    make install

FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS dotnet-build
WORKDIR /src

RUN apt-get update && apt-get install -y \
    python3 python3-pip python3-venv \
    && rm -rf /var/lib/apt/lists/*

COPY VideoAnonymizer.slnx ./
COPY VideoAnonymizer.ServiceDefaults/ VideoAnonymizer.ServiceDefaults/
COPY VideoAnonymizer.Contracts/ VideoAnonymizer.Contracts/
COPY VideoAnonymizer.Web.Contracts/ VideoAnonymizer.Web.Contracts/
COPY VideoAnonymizer.Database/ VideoAnonymizer.Database/
COPY VideoAnonymizer.ObjectDetectionClient/ VideoAnonymizer.ObjectDetectionClient/
COPY VideoAnonymizer.Web.Modules/ VideoAnonymizer.Web.Modules/
COPY VideoAnonymizer.Web/ VideoAnonymizer.Web/
COPY VideoAnonymizer.ApiService/ VideoAnonymizer.ApiService/
COPY VideoAnonymizer.VideoProcessor/ VideoAnonymizer.VideoProcessor/
COPY VideoAnonymizer.ModelDownloader/ VideoAnonymizer.ModelDownloader/
COPY VideoAnonymizer.StandaloneHost/ VideoAnonymizer.StandaloneHost/

COPY --from=vue-builder /wwwroot/vue/video-editor /src/VideoAnonymizer.Web.Modules/wwwroot/vue/video-editor

RUN sed -i '/OpenCvSharp4.runtime.win/d' \
    VideoAnonymizer.VideoProcessor/VideoAnonymizer.VideoProcessor.csproj

RUN dotnet restore VideoAnonymizer.StandaloneHost/VideoAnonymizer.StandaloneHost.csproj -p:NuGetAudit=false
RUN dotnet publish VideoAnonymizer.StandaloneHost/VideoAnonymizer.StandaloneHost.csproj \
    -c Release -r linux-x64 --self-contained false -o /publish -p:NuGetAudit=false

# Resolve the Blazor WebAssembly #[.{fingerprint}] placeholder in index.html
# (same workaround as publish-standalone.ps1)
RUN BLAZOR_JS=$(ls /publish/wwwroot/_framework/blazor.webassembly.*.js 2>/dev/null | head -1) && \
    if [ -n "$BLAZOR_JS" ]; then \
        BLAZOR_NAME=$(basename "$BLAZOR_JS") && \
        sed -i "s|_framework/blazor.webassembly#\[\.{fingerprint}\]\.js|_framework/$BLAZOR_NAME|" /publish/wwwroot/index.html && \
        echo "Fixed index.html: blazor.webassembly placeholder -> $BLAZOR_NAME"; \
    else \
        echo "WARNING: blazor.webassembly*.js not found in publish output"; \
    fi && \
    for pair in "dotnet.*.js:dotnet.js" "dotnet.native.*.js:dotnet.native.js" "dotnet.runtime.*.js:dotnet.runtime.js"; do \
        pattern=$(echo "$pair" | cut -d: -f1); \
        name=$(echo "$pair" | cut -d: -f2); \
        script=$(ls /publish/wwwroot/_framework/$pattern 2>/dev/null | head -1); \
        if [ -n "$script" ]; then \
            cp "$script" "/publish/wwwroot/_framework/$name" && \
            echo "Copied $(basename $script) -> $name"; \
        else \
            echo "WARNING: $pattern not found"; \
        fi; \
    done && \
    rm -rf /publish/BlazorDebugProxy 2>/dev/null; \
    rm -rf /publish/App_Data 2>/dev/null; \
    echo "Cleaned up publish-only payload directories"

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble AS runtime

RUN apt-get update && apt-get install -y \
    python3 python3-pip python3-venv \
    libopencv-dev libopencv-contrib-dev \
    libavcodec-dev libavformat-dev libswscale-dev \
    libdc1394-dev libtiff-dev libopenjp2-7-dev \
    libgtk2.0-0 libcanberra0 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=opencvsharp-bridge /opt/opencvsharp/lib/libOpenCvSharpExtern.so \
    /usr/lib/libOpenCvSharpExtern.so

RUN python3 -m venv /opt/venv && \
    /opt/venv/bin/pip install --no-cache-dir \
    fastapi uvicorn[standard] onnxruntime opencv-python-headless \
    numpy pydantic supervision

COPY --from=dotnet-build /publish /app
COPY VideoAnonymizer.ObjectDetection/ /opt/object-detection/
COPY docker/appsettings.Docker.json /app/appsettings.Docker.json
COPY docker/docker-entrypoint.sh /app/docker-entrypoint.sh
COPY docker/object-detection-wrapper.sh /app/object-detection-wrapper.sh

RUN chmod +x /app/docker-entrypoint.sh /app/object-detection-wrapper.sh

RUN ln -sf /usr/bin/python3 /usr/bin/python

ENV ASPNETCORE_ENVIRONMENT=Docker
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1

EXPOSE 5117

VOLUME ["/data"]

ENTRYPOINT ["/app/docker-entrypoint.sh"]
