# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Triso.Domain/Triso.Domain.csproj Triso.Domain/
COPY Triso.Application/Triso.Application.csproj Triso.Application/
COPY Triso.Infrastructure/Triso.Infrastructure.csproj Triso.Infrastructure/
COPY Triso.Api/Triso.Api.csproj Triso.Api/
RUN dotnet restore Triso.Api/Triso.Api.csproj

COPY Triso.Domain/ Triso.Domain/
COPY Triso.Application/ Triso.Application/
COPY Triso.Infrastructure/ Triso.Infrastructure/
COPY Triso.Api/ Triso.Api/
RUN dotnet publish Triso.Api/Triso.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_EnableDiagnostics=0

EXPOSE 8080
USER $APP_UID

ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} exec dotnet Triso.Api.dll"]
