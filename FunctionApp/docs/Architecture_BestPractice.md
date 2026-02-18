# Architecture Reference (Best Practice)

This document defines the target architecture, layer responsibilities, and project structure for the solution. It is designed to support two hosts:
- Azure Functions (timer + HTTP) for Reference category sync
- App Service (worker/background) for Analytics category sync

The goal is strict separation of concerns, clear dependencies, and shared infrastructure across hosts.

## 1) Target Project Structure

```
/FunctionApp
  /src
    /Functions              (Azure Functions host)
    /AppService             (Worker Service host)
    /Application            (use cases, orchestration, interfaces)
    /Infrastructure         (IO: DB, external APIs, Key Vault, Blob, etc.)
    /Domain                 (entities, domain rules)
    /Shared                 (cross-cutting utilities only)
    /Configuration          (options + binding extensions)
  /tests
```

## 2) Layer Responsibilities

### 2.1 Hosts
**Functions**
- Owns triggers (Timer/HTTP)
- Owns host configuration (appsettings, env vars)
- Calls Application services only
- Registers DI for the host + shared layers

**AppService**
- Runs background jobs for Analytics sync
- Owns its host configuration and scheduling
- Calls Application services only

Hosts do not contain business rules. They only wire and trigger workflows.

### 2.2 Application
- Orchestration of sync operations (Use cases)
- Interfaces/abstractions for infrastructure (repositories, external APIs)
- Handlers per category (e.g., Skill, Group, UserDetails, etc.)
- No direct IO or SDK calls

### 2.3 Infrastructure
- Implements external APIs, DB, Key Vault, Blob, etc.
- EF Core DbContext and repositories
- HTTP clients and token providers
- No host-specific dependencies (Functions/ASP.NET packages)

### 2.4 Domain
- Pure domain entities, enums, value objects
- No DI or external references

### 2.5 Shared
- Truly cross-cutting utilities only
- Extensions, time provider, common constants
- Must not become a dumping ground

### 2.6 Configuration
- Strongly typed Options + binding/validation extensions
- No runtime policy logic; values should come from config

## 3) Allowed Dependency Flow

```
Hosts -> Application -> Domain
Hosts -> Infrastructure -> Domain
Hosts -> Shared
Application -> Domain
Application -> Shared
Infrastructure -> Application (only for interfaces)
Infrastructure -> Domain
Infrastructure -> Shared
Configuration -> no dependencies on other projects
```

**Forbidden:**
- Infrastructure referencing Functions or AppService packages
- Application referencing Infrastructure
- Domain referencing anything

## 4) Required Host Wiring

Each host should register:
- Configuration options
- Application services
- Infrastructure services
- Shared services

Example order:
```
services.AddConfiguration(...)
services.AddApplication(...)
services.AddInfrastructure(...)
services.AddShared(...)
```

## 5) Shared Components Across Hosts

These components must live in Infrastructure and be reusable:
- Key Vault secret provider + caching
- Flurl HTTP client factory + resiliency policies
- Genesys API clients + token provider
- EF Core DbContext + repositories

These must remain host-specific:
- Application Insights and host logging wiring
- Triggers, schedules, and HTTP endpoints

## 6) Sync Execution Model

### 6.1 Categories
- **Reference**: Skills, Groups, Presence, Wrapup Codes (Functions timer)
- **Analytics**: User Details, etc. (AppService background)

### 6.2 Orchestration
- Application layer owns `ISyncOrchestrator` and `ISyncCategoryHandler`
- Hosts only call `ISyncOrchestrator.ExecuteAsync(lob, category)`

### 6.3 LOB Context
- `ILobContextAccessor` scoped, populated per invocation
- `ILobContext` resolves LOB-specific secrets and connection string
- External services and repositories use `ILobContext`

## 7) Concurrency & Locking

- In-process cancellation is OK for single-instance hosts
- If scale-out or multiple instances are possible, use a distributed lock per LOB+Category
- Lock implementation belongs in Infrastructure (DB or Blob lease)
- Application should depend on an interface like `ISyncLock`

## 8) Naming Standards

- Avoid host-specific names in shared layers
  - `FunctionAppDbContext` => `AppDbContext` (or `GenesysSyncDbContext`)
- “Services” in Application = orchestration only
- “Services” in Infrastructure = IO + external calls

## 9) Validation Checklist

A class is in the correct layer if:
- It has no references to host SDKs (Functions/ASP.NET)
- It only uses abstractions for IO from Application
- It can be reused by both hosts without changes

A host is clean if:
- It contains no data logic
- It only wires triggers and calls Application services

## 10) Implementation Roadmap (Minimal Changes)

1. Add `AppService` host project
2. Move host-specific telemetry wiring out of Infrastructure
3. Rename `FunctionAppDbContext`
4. Confirm all shared services are in Infrastructure
5. Introduce optional distributed lock (if scale-out is planned)

## 11) Current File Inventory by Layer

### Functions
src/Functions/Functions.csproj
src/Functions/Http/RecoveryFunction.cs
src/Functions/Program.cs
src/Functions/Properties/launchSettings.json
src/Functions/Timers/References/ReferencesCrcTimer.cs
src/Functions/Timers/References/ReferencesLclTimer.cs
src/Functions/Timers/References/ReferencesNttTimer.cs
src/Functions/Timers/TestTimer.cs
src/Functions/Timers/UserDetails/UserDetailsNttTimer.cs
src/Functions/appsettings.Development.json
src/Functions/appsettings.Production.json
src/Functions/host.json
src/Functions/local.settings.json
src/Functions/sample.appsettings.environment.json

### Application
src/Application/Application.csproj
src/Application/AssemblyMarker.cs
src/Application/Behaviors/ValidationBehavior.cs
src/Application/Common/Abstractions/Context/ILobContext.cs
src/Application/Common/Abstractions/Context/ILobContextAccessor.cs
src/Application/Common/Abstractions/Factories/IHitCountProviderFactory.cs
src/Application/Common/Abstractions/Persistence/IUnitOfWork.cs
src/Application/Common/Abstractions/Providers/IHitCountProvider.cs
src/Application/Common/Abstractions/Providers/ILobSecretsResolver.cs
src/Application/Common/Abstractions/Providers/ISecretProvider.cs
src/Application/Common/Abstractions/Providers/ITokenProvider.cs
src/Application/Common/Abstractions/Services/IIntervalSubdivisionService.cs
src/Application/Common/Abstractions/Services/ISyncCategoryHandler.cs
src/Application/Common/Abstractions/Services/ISyncOrchestrator.cs
src/Application/Common/Enums/SyncCategory.cs
src/Application/Common/Exceptions/ApplicationException.cs
src/Application/Common/Exceptions/IntervalSubdivisionException.cs
src/Application/Common/Extensions/ServiceCollectionExtensions.cs
src/Application/Common/Factories/IntervalFactory.cs
src/Application/Common/Mediator/IPipelineBehavior.cs
src/Application/Common/Mediator/IRequest.cs
src/Application/Common/Mediator/IRequestHandler.cs
src/Application/Common/Mediator/ISimpleMediator.cs
src/Application/Common/Mediator/SimpleMediator.cs
src/Application/Common/Models/Interval.cs
src/Application/Common/Models/IntervalWithPages.cs
src/Application/Common/Models/SyncKey.cs
src/Application/Common/Services/IntervalSubdivisionService.cs
src/Application/Common/Services/SyncOrchestrator.cs
src/Application/Contracts/Enums/GroupType.cs
src/Application/Contracts/Enums/GroupVisibility.cs
src/Application/Contracts/Enums/PresenceType.cs
src/Application/Contracts/Enums/RoutingStatus.cs
src/Application/Contracts/Enums/State.cs
src/Application/Contracts/Enums/SystemPresence.cs
src/Application/Contracts/Recovery/RecoveryRequest.cs
src/Application/Contracts/References/GroupResponse.cs
src/Application/Contracts/References/PagedReferenceResponse.cs
src/Application/Contracts/References/PresenceDefinitionResponse.cs
src/Application/Contracts/References/SkillResponse.cs
src/Application/Contracts/References/WrapupCodeResponse.cs
src/Application/Contracts/UserDetails/PrimaryPresenceResponse.cs
src/Application/Contracts/UserDetails/RoutingStatusResponse.cs
src/Application/Contracts/UserDetails/UserDetailsRequest.cs
src/Application/Contracts/UserDetails/UserDetailsResponse.cs
src/Application/Dtos/UserDetails/PrimaryPresenceDto.cs
src/Application/Dtos/UserDetails/RoutingStatusDto.cs
src/Application/Features/Recovery/CreateRecoveryRequestCommand.cs
src/Application/Features/Recovery/CreateRecoveryRequestCommandValidator.cs
src/Application/Features/Recovery/CreateRecoveryRequestHandler.cs
src/Application/Features/Recovery/CreateRecoveryRequestResponse.cs
src/Application/Normalizers/UserDetails/IUserDetailsNormalizer.cs
src/Application/Normalizers/UserDetails/UserDetailsNormalizer.cs
src/Application/References/Handlers/GroupSyncHandler.cs
src/Application/References/Handlers/PresenceDefinitionSyncHandler.cs
src/Application/References/Handlers/SkillSyncHandler.cs
src/Application/References/Handlers/WrapupCodeSyncHandler.cs
src/Application/References/IReferencesClient.cs
src/Application/References/IReferencesRepository.cs
src/Application/References/IReferencesSyncService.cs
src/Application/UserDetails/IUserDetailsClient.cs
src/Application/UserDetails/IUserDetailsRepository.cs
src/Application/UserDetails/IUserDetailsSyncService.cs
src/Application/UserDetails/UserDetailsIncrementalSyncHandler.cs
src/Application/UserDetails/UserDetailsRecoveryHandler.cs

### Infrastructure
src/Infrastructure/AssemblyMarker.cs
src/Infrastructure/Azure/ApplicationInsights/ApplicationInsightsExtension.cs
src/Infrastructure/Azure/BlobStorage/BlobStorageClient.cs
src/Infrastructure/Azure/BlobStorage/BlobStorageProvider.cs
src/Infrastructure/Azure/KeyVaults/KeyVaultsClientFactory.cs
src/Infrastructure/Azure/KeyVaults/KeyVaultsException.cs
src/Infrastructure/Azure/KeyVaults/KeyVaultsExtension.cs
src/Infrastructure/Azure/KeyVaults/KeyVaultsSecretCache.cs
src/Infrastructure/Azure/KeyVaults/KeyVaultsSecretProvider.cs
src/Infrastructure/Exceptions/InfrastructureException.cs
src/Infrastructure/Extensions/PersistenceExtensions.cs
src/Infrastructure/Extensions/ServiceCollectionExtensions.cs
src/Infrastructure/ExternalServices/ExternalServiceHttpException.cs
src/Infrastructure/ExternalServices/ExternalServicesExtensions.cs
src/Infrastructure/ExternalServices/FlurlHttp/FlurlHttpClient.cs
src/Infrastructure/ExternalServices/FlurlHttp/FlurlHttpClientFactory.cs
src/Infrastructure/ExternalServices/FlurlHttp/IFlurlHttpClient.cs
src/Infrastructure/ExternalServices/FlurlHttp/IFlurlHttpClientFactory.cs
src/Infrastructure/ExternalServices/Genesys/Auth/GenesysTokenClient.cs
src/Infrastructure/ExternalServices/Genesys/Auth/GenesysTokenProvider.cs
src/Infrastructure/ExternalServices/Genesys/Auth/GenesysTokenResponse.cs
src/Infrastructure/ExternalServices/Genesys/Clients/GenesysApiClient.cs
src/Infrastructure/ExternalServices/Genesys/Clients/ReferencesClient.cs
src/Infrastructure/ExternalServices/Genesys/Clients/UserDetailsClient.cs
src/Infrastructure/ExternalServices/Genesys/Providers/HitCountProviderFactory.cs
src/Infrastructure/ExternalServices/Genesys/Providers/UserDetailsHitCountProvider.cs
src/Infrastructure/Infrastructure.csproj
src/Infrastructure/Persistence/Configurations/JobTracking/JobTrackingConfiguration.cs
src/Infrastructure/Persistence/Configurations/References/GroupEntityConfiguration.cs
src/Infrastructure/Persistence/Configurations/References/PresenceDefinitionEntityConfiguration.cs
src/Infrastructure/Persistence/Configurations/References/SkillEntityConfiguration.cs
src/Infrastructure/Persistence/Configurations/References/WrapupCodeEntityConfiguration.cs
src/Infrastructure/Persistence/Configurations/UserDetails/PrimaryPresenceConfiguration.cs
src/Infrastructure/Persistence/Configurations/UserDetails/RoutingStatusConfiguration.cs
src/Infrastructure/Persistence/Entities/Audit.cs
src/Infrastructure/Persistence/Entities/JobTracking/JobTrackingEntity.cs
src/Infrastructure/Persistence/Entities/References/Group.cs
src/Infrastructure/Persistence/Entities/References/PresenceDefinition.cs
src/Infrastructure/Persistence/Entities/References/Skill.cs
src/Infrastructure/Persistence/Entities/References/WrapupCode.cs
src/Infrastructure/Persistence/Entities/UserDetails/PrimaryPresenceEntity.cs
src/Infrastructure/Persistence/Entities/UserDetails/RoutingStatusEntity.cs
src/Infrastructure/Persistence/FunctionAppDbContext/FunctionAppDbContext.cs
src/Infrastructure/Persistence/Interceptors/AuditSaveChangesInterceptor.cs
src/Infrastructure/Persistence/Mappers/ReferencesProfile.cs
src/Infrastructure/Persistence/Mappers/Shared/MappingConverters.cs
src/Infrastructure/Persistence/Mappers/Shared/MappingExtensions.cs
src/Infrastructure/Persistence/Mappers/UserDetailsProfile.cs
src/Infrastructure/Persistence/PersistenceException.cs
src/Infrastructure/Persistence/Repositories/References/ReferencesRepository.cs
src/Infrastructure/Persistence/Repositories/UnitOfWork.cs
src/Infrastructure/Persistence/Repositories/UnitOfWorkCore/CompositeKey.cs
src/Infrastructure/Persistence/Repositories/UnitOfWorkCore/EntityMetadata.cs
src/Infrastructure/Persistence/Repositories/UnitOfWorkCore/EntityQueryBuilder.cs
src/Infrastructure/Persistence/Repositories/UnitOfWorkCore/EntityUpdateHandler.cs
src/Infrastructure/Persistence/Repositories/UnitOfWorkCore/EntityValidator.cs
src/Infrastructure/Persistence/Repositories/UnitOfWorkCore/UpsertResult.cs
src/Infrastructure/Persistence/Repositories/UserDetails/UserDetailsRepository.cs
src/Infrastructure/Persistence/dbScript.sql
src/Infrastructure/Services/References/ReferencesSyncService.cs
src/Infrastructure/Services/UserDetails/UserDetailsIncrementalSyncService.cs
src/Infrastructure/Services/UserDetails/UserDetailsRecoveryService.cs
src/Infrastructure/Services/UserDetails/UserDetailsSyncServiceBase.cs
src/Infrastructure/Shared/Context/LobContext.cs
src/Infrastructure/Shared/Context/LobContextAccessor.cs
src/Infrastructure/Shared/Providers/LobSecretsResolver.cs
src/Infrastructure/WebSockets/GenesysNotificationSubscriber.cs

### Domain
src/Domain/Domain.csproj

### Shared
src/Shared/AssemblyMarker.cs
src/Shared/Constants/CommonConstants.cs
src/Shared/Constants/GenesysConstants.cs
src/Shared/Constants/KeyVaultsConstants.cs
src/Shared/Extensions/EnumConverterExtensions.cs
src/Shared/Extensions/EnumStringExtensions.cs
src/Shared/Extensions/ExceptionExtensions.cs
src/Shared/Extensions/LoggerExtensions.cs
src/Shared/Extensions/ServiceCollectionExtensions.cs
src/Shared/Extensions/StringExtensions.cs
src/Shared/Shared.csproj
src/Shared/Time/DateTimeProvider.cs
src/Shared/Time/DateTimeResolver.cs
src/Shared/Time/IDateTimeProvider.cs

### Configuration
src/Configuration/Configuration.csproj
src/Configuration/ConfigurationExtensions.cs
src/Configuration/Options/ApplicationInsightsOptions.cs
src/Configuration/Options/BlobStorageOptions.cs
src/Configuration/Options/DatabaseOptions.cs
src/Configuration/Options/FlurlClientOptions.cs
src/Configuration/Options/IntervalSubdivisionOptions.cs
src/Configuration/Options/KeyVaultsOptions.cs

