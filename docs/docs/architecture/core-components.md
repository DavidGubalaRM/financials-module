# Core Components

The Financials module is built around several core components that provide shared functionality across all events.

## NMFinancialUtils

**Location**: `NMFinancialUtils.cs`

A large static utility class (~2,000 lines) providing common operations used throughout the module. This is the central shared logic layer.

### Record Lookup Methods

| Method | Purpose |
|--------|---------|
| `CheckForExistingPCOSC` | Check if provider has a Provider Child of Service Catalog record |
| `GetRunRecord` | Get or create a batch interface run record for batch processes |
| `FailRunRecord` | Mark a batch run record as Failed with error details |
| `GetServiceAuthorization` | Resolve service authorization from utilization (case or investigation) |
| `GetParentRecord` | Get parent Case or Investigation from any record |
| `GetPersonDOB` | Get person date of birth (exact or approximated) |
| `GetPlacement` | Get placement record from service utilization |
| `GetServiceCatalogByUniqueCode` | Look up Service Catalog by unique code (e.g., "SC28") |
| `GetStandardSRA` | Get Standard Service Rate Agreement for a date range |
| `GetServiceAutorizationForPlacement` | Find existing Service Auth for a placement |
| `GetLatestRemovalCounty` | Get the most recent removal county for a child |
| `GetDateTrainingCompleted` | Check if child/youth training is complete |
| `GetTFCAgencyProvider` | Get TFC agency provider for a TFC home |
| `GetChildFromPlacement` | Get child person record from placement (Case or Investigation) |
| `GetFundBalanceRecords` | Get fund balance records for a person and fund |
| `GetDeliveryDate` | Get delivery date for pregnant/parenting youth |

### Rate Calculation Methods

| Method | Purpose |
|--------|---------|
| `CalculateRateForAge` | Find rate from age-based rate records for a given person age |
| `CalculateRateBasedOnAge` | Calculate rate with birthday proration (handles birthday mid-period) |
| `CalculateRate` | Calculate rate from a single age-based rate record (handles monthly-to-daily conversion) |
| `CalculateRateForAdoptionAndGuardianship` | Calculate weighted average rate from adoption/guardianship agreements |
| `CalculateAgeUpToGivenDate` | Calculate age as of a specific date |
| `PersonHasBirthdayDuringPeriod` | Check if person has birthday during a service period |
| `HandleRates` | Process rate records for a date range, handling age-based and LOC-based logic |
| `GetLevelOfCareAndScoreFromHistory` | Determine Level of Care from placement history records |
| `DetermineDailyRate` | Calculate daily rate from service utilization |

### Fund Distribution Methods

| Method | Purpose |
|--------|---------|
| `GetFundAllocations` | Get ordered Fund Allocations for a Service Utilization's date range |
| `HandleFundAllocations` | Core fund distribution logic: find balance, deduct, create Initial Fund Distribution, call gateway |
| `CreateBlankInitialFundDisRecord` | Create an error Initial Fund Distribution record for manual resolution |
| `IsChildIVEEligible` | Check if child has IVE Eligibility on their case |
| `DetermineNumberOfDays` | Calculate number of days for VSSA-based fund allocations |
| `HasSignedVSSA` | Check if placement has a signed Voluntary Services Support Agreement |
| `GetCTFMAPRateRecord` | Get CT FMAP rate record for FMAP-based fund allocations |
| `GetCTTribalFMAPRateRecord` | Get CT Tribal FMAP rate record |

### Validation Methods

| Method | Purpose |
|--------|---------|
| `ValidateProviderOffersRequiredService` | Validate provider offers required service for placement setting/type |
| `GetProviderService` | Get provider service record for a service catalog and provider |

### Gateway and Notification Methods

| Method | Purpose |
|--------|---------|
| `HandleGatewayCall` | Build message and invoke Finance Gateway with appropriate action |
| `SendServiceRateAgreementEndingNotification` | Send notification to System Admin and Contracts Unit when SRA is expiring |
| `SendServiceRateEndingNotification` | Send notification when Service Rate is expiring |
| `SendHolidayRunApproachingNotification` | Send holiday run approaching notification |
| `SendBackToSchoolRunApproachingNotification` | Send back-to-school run approaching notification |
| `GetNextStartDate` | Calculate next notification trigger date for holiday/back-to-school runs |

## FinanceServices

**Location**: `Utils/FinanceServices.cs`

Handles HTTP communication with the Finance Gateway.

```csharp
public static async Task<EventReturnObject> MakePostRestCall(
    object dataObject, AEventHelper eventHelper)
```

### How It Works

1. Reads gateway URL and token from `WebConfigurationManager.AppSettings`
2. If token exists, appends `?code=<token>` to URL (for Azure Functions authentication)
3. Serializes data object to JSON via `JsonConvert.SerializeObject`
4. Sends HTTP POST with `application/json` content type
5. On success: returns `EventReturnObject` with `Success` status and response text
6. On failure: logs error details (URL, payload, response) and returns error

### Configuration

| App Setting | Purpose |
|-------------|---------|
| `FINANCEGATEWAY_URL` | Gateway endpoint URL (required) |
| `FINANCEGATEWAY_KEY` | Function key for deployed environments (optional, not needed locally) |

### HTTP Client

Uses `HttpClientService.Instance.GetHttpClient()` -- a singleton HTTP client to avoid socket exhaustion.

## FinanceModels

**Location**: `Utils/FinanceModels.cs`

Message classes for the middle-tier accounting abstraction layer. These provide a clean contract between mCase and the external accounting system, passing only the minimum required data.

### Class Hierarchy

```
BaseMessage (RecordId, ModifiedBy)
├── AccountMessage (FundName, FundCode)
├── ManageFundsMessage (TransactionType, FromRecordId)
└── OverUnderMessage (StartDate, Previous, New)
    └── OverUnderDetails (ServiceAuthId, ProviderId)

AMessage (Action, Data) — wrapper for all messages
```

See [Finance Gateway Messages](../finance-gateway/messages) for detailed documentation.

## NMFinancialConstants

**Location**: `Utils/NMFinancialConstants.cs`

Centralized constants for the module.

### ErrorMessages

Validation error message templates used across events. See [Constants Reference](../reference/constants) for the full list.

### ServiceCatalogServices

Service catalog unique codes (e.g., `SC8` = Congregate Care Community Home). 35+ service codes covering all placement types. See [Service Catalog Reference](../reference/service-catalog).

### requiredServiceMap

Dictionary mapping placement settings and types to required service catalog codes:

```csharp
Dictionary<string, List<(List<string> PlacementTypes, string RequiredService)>>
```

Covers 7 placement settings with multiple type-to-service mappings each. Used for:
- Placement approval service authorization creation
- Provider service validation on placement

### AccountingAPI, ActionTypes, TransactionTypes

Gateway configuration keys and action/transaction type constants. See [Constants Reference](../reference/constants).
