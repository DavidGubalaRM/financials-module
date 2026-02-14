# Constants Reference

## NMFinancialConstants

**Namespace**: `MCase.Event.NMImpact.Constants`  
**Location**: `Utils/NMFinancialConstants.cs`

### ErrorMessages

| Constant | Message Template |
|----------|------------------|
| `inValidCombination` | Invalid combination of placement setting {0} and placement type {1}. |
| `noProviderServicesFound` | The selected Provider {0} does not offer any Services. |
| `serviceNotOffered` | Provider {0} does not offer the required service ({1}) based on the selections. |
| `placementNotMapped` | The placement setting/type {0}/{1} could not be mapped to a Service Type. |
| `inActiveServiceFound` | Required Service ({0}) was found, but it is currently Inactive. |
| `serviceCatalogNotFound` | Service Catalog not found for code {0}. |
| `rateOverrideNeeded` | Placement requires an Approved Rate Override Record for Submit for Approval button to show |

### AccountingAPI

| Constant | Config Key | Description |
|----------|------------|-------------|
| `EndPoint` | FINANCEGATEWAY_URL | Finance Gateway API URL |
| `EndPointToken` | FINANCEGATEWAY_KEY | Optional function key for deployed environments |

### ActionTypes

| Constant | Value |
|----------|-------|
| `CreateAccount` | "CreateAccount" |
| `DepositFunds` | "DepositFunds" |
| `CommitFunds` | "CommitFunds" |
| `ActualFunds` | "ActualFunds" |
| `OverUnderPayments` | "OverUnderPayments" |
| `StopPayment` | "StopPayment" |
| `StartPayment` | "StartPayment" |

### TransactionTypes

| Constant | Value |
|----------|-------|
| `DepositFunds` | "D" |
| `TransferFunds` | "T" |

### requiredServiceMap

Dictionary structure:
```csharp
Dictionary<string, List<(List<string> PlacementTypes, string RequiredService)>>
```

Maps placement setting → list of (placement types, service catalog code) pairs. Used for:
- Validating provider offers required service on placement
- Creating Service Authorization with correct service type
