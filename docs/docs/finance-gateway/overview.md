# Finance Gateway Overview

The Finance Gateway is an external REST API that handles accounting and fund management operations. The Financials module integrates with this gateway to synchronize financial data (deposits, commits, actual payments) with the accounting system.

## Configuration

Configure the gateway in `Web.config` (or equivalent):

```xml
<appSettings>
  <add key="FINANCEGATEWAY_URL" value="https://your-gateway-url/api/endpoint" />
  <add key="FINANCEGATEWAY_KEY" value="your-function-key" />
</appSettings>
```

- **FINANCEGATEWAY_URL** — Required. The base URL of the Finance Gateway API.
- **FINANCEGATEWAY_KEY** — Optional. Used as `?code=` query parameter for deployed environments (e.g., Azure Functions).

## Integration Pattern

All gateway calls use `FinanceServices.MakePostRestCall()`:

```csharp
var aMessage = new AMessage()
{
    Action = NMFinancialConstants.ActionTypes.ActualFunds,
    Data = fundMessage
};

var result = await FinanceServices.MakePostRestCall(aMessage, eventHelper);
```

- **Method**: HTTP POST
- **Content-Type**: `application/json`
- **Response**: Parsed and returned in `EventReturnObject`

## When Gateway is Called

| Event | Action | Trigger |
|-------|--------|---------|
| `SentInitialFundDistToGateway` | CommitFunds | Post-create on Initial Fund Distribution |
| `SendActualFundsToGateway` | ActualFunds | Post-update when Invoice status = Paid |
| `PlacementApprovalServiceAuth` | StartPayment | Placement approved, start date in previous month |
| `PlacementEndDated` | StopPayment | Placement end-dated |
| `PauseReInstatePaymentFromStaffingApprovals` | StopPayment / StartPayment | Staffing approval pause/reinstate |
| `DepositsPostUpdate` | DepositFunds | Post-update on Deposits (when applicable) |

## Error Handling

- Non-success HTTP status codes throw an exception with response details
- Exceptions are caught and added to `EventReturnObject` errors
- Debug logging includes URL, payload, and response for troubleshooting
