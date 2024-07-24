FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

COPY *.sln .
COPY img.birb.cc/*.csproj ./img.birb.cc/
RUN dotnet restore

COPY img.birb.cc/. ./img.birb.cc/
WORKDIR /source/img.birb.cc
RUN dotnet publish -c release -o /app --no-restore --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true

FROM mcr.microsoft.com/dotnet/runtime-deps:8.0-noble-chiseled
WORKDIR /app
COPY --from=build /app ./

ENV ASPNETCORE_HTTP_PORTS=5000
ENV CONFIG_PATH=/app/config
ENV SALT_PATH=/app/config/salt.txt
ENV UPLOADS_PATH=/app/uploads

VOLUME /app/config
VOLUME /app/uploads

ENTRYPOINT ["/app/img.birb.cc"]