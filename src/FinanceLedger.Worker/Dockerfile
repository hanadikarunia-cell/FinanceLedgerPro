# Build context is the repo root (see render.yaml's dockerContext) so this can COPY
# the sibling Domain/Application/Infrastructure projects the Worker project references.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/FinanceLedger.Domain/FinanceLedger.Domain.csproj src/FinanceLedger.Domain/
COPY src/FinanceLedger.Application/FinanceLedger.Application.csproj src/FinanceLedger.Application/
COPY src/FinanceLedger.Infrastructure/FinanceLedger.Infrastructure.csproj src/FinanceLedger.Infrastructure/
COPY src/FinanceLedger.Worker/FinanceLedger.Worker.csproj src/FinanceLedger.Worker/
RUN dotnet restore src/FinanceLedger.Worker/FinanceLedger.Worker.csproj

COPY src/FinanceLedger.Domain/ src/FinanceLedger.Domain/
COPY src/FinanceLedger.Application/ src/FinanceLedger.Application/
COPY src/FinanceLedger.Infrastructure/ src/FinanceLedger.Infrastructure/
COPY src/FinanceLedger.Worker/ src/FinanceLedger.Worker/

RUN dotnet publish src/FinanceLedger.Worker/FinanceLedger.Worker.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV DOTNET_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "FinanceLedger.Worker.dll"]
