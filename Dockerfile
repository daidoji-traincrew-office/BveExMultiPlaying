# Learn about building .NET container images:
# https://github.com/dotnet/dotnet-docker/blob/main/samples/README.md
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG TARGETARCH
WORKDIR /source

# Copy project file and restore as distinct layers
RUN mkdir -p /source/BveExMultiPlaying.Common
COPY --link BveExMultiPlaying.Common/*.csproj BveExMultiPlaying.Common/
RUN mkdir -p /source/BveExMultiPlaying.Server
COPY --link BveExMultiPlaying.Server/*.csproj BveExMultiPlaying.Server/
RUN cd ./BveExMultiPlaying.Server && dotnet restore -a $TARGETARCH 

# Copy source code and publish app
COPY --link BveExMultiPlaying.Common/* BveExMultiPlaying.Common/
COPY --link BveExMultiPlaying.Server/* BveExMultiPlaying.Server/
RUN cd ./BveExMultiPlaying.Server && dotnet publish -a $TARGETARCH --no-restore -o /app


# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0
EXPOSE 8080
WORKDIR /app
COPY --link --from=build /app .
WORKDIR /opt
ENTRYPOINT ["/app/BveExMultiPlaying.Server"]