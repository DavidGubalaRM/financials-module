# Finance Gateway Actions

The gateway supports the following action types, defined in `NMFinancialConstants.ActionTypes`.

## Action Types

| Action | Constant | Description |
|--------|----------|-------------|
| Create Account | `CreateAccount` | Create a new fund/account in the accounting system |
| Deposit Funds | `DepositFunds` | Deposit or transfer funds to an account |
| Commit Funds | `CommitFunds` | Commit allocated funds (initial fund distribution) |
| Actual Funds | `ActualFunds` | Record actual payment disbursement (invoice paid) |
| Over/Under Payments | `OverUnderPayments` | Adjust for overpayments or underpayments |
| Stop Payment | `StopPayment` | Pause/stop recurring payment (placement end-dated, suspended, terminated) |
| Start Payment | `StartPayment` | Start/resume recurring payment (placement approved, reinstated) |

## Message Structure

All requests use the `AMessage` wrapper:

```csharp
public class AMessage
{
    public string Action { get; set; }   // One of the action types above
    public object Data { get; set; }     // Action-specific message (see Messages)
}
```

## Usage by Event

| Action | Events Using It | When |
|--------|-----------------|------|
| **CreateAccount** | CreateFundBalanceNewFundAdded | New fund created |
| **CommitFunds** | PopulateInitialFundDistribution, PopulateInitialFundDistUnderpayments, SentInitialFundDistToGateway | Fund distribution records created |
| **ActualFunds** | SendActualFundsToGateway | Invoice status = Paid |
| **DepositFunds** | DepositsPostUpdate | Deposit approved (capped/child-specific funds) |
| **StartPayment** | PlacementApprovalServiceAuth, PauseReInstatePaymentFromStaffingApprovals, AdoptionSuspensionReInstatement, GuardianshipSuspensionReInstatement | Payment started/resumed |
| **StopPayment** | PlacementEndDated, PauseReInstatePaymentFromStaffingApprovals, AdoptionTermination, AdoptionSuspensionReInstatement, GuardianshipTermination, GuardianshipSuspensionReInstatement | Payment paused/stopped |
