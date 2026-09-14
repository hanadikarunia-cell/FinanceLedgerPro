# Build context is the repo root (see render.yaml's dockerContext) so this can COPY
# the sibling Domain/Application/Infrastructure projects the API project references.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/FinanceLedger.Domain/FinanceLedger.Domain.csproj src/FinanceLedger.Domain/
COPY src/FinanceLedger.Application/FinanceLedger.Application.csproj src/FinanceLedger.Application/
COPY src/FinanceLedger.Infrastructure/FinanceLedger.Infrastructure.csproj src/FinanceLedger.Infrastructure/
COPY src/FinanceLedger.API/FinanceLedger.API.csproj src/FinanceLedger.API/
RUN dotnet restore src/FinanceLedger.API/FinanceLedger.API.csproj

COPY src/FinanceLedger.Domain/ src/FinanceLedger.Domain/
COPY src/FinanceLedger.Application/ src/FinanceLedger.Application/
COPY src/FinanceLedger.Infrastructure/ src/FinanceLedger.Infrastructure/
COPY src/FinanceLedger.API/ src/FinanceLedger.API/

RUN dotnet publish src/FinanceLedger.API/FinanceLedger.API.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 10000

# Render injects PORT at runtime (not build time), so it must be expanded in a shell,
# not baked in via ENV/exec-form ENTRYPOINT. Falls back to 10000 for local `docker run`.
ENTRYPOINT ["/bin/sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-10000} exec dotnet FinanceLedger.API.dll"]
