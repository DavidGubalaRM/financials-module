# Invoice Module

The Invoice module handles invoice-to-utilization linking and sending actual payments to the Finance Gateway.

## Events

### SendActualFundsToGateway

**Trigger**: PostUpdate on Invoice

**Purpose**: When invoice `Paymentstatus` changes to **Paid**, send actual payment notification to Finance Gateway.

**Logic**:
1. Parse invoice record
2. If `Paymentstatus` is not `Paid`, exit with success (no action needed)
3. Build `ManageFundsMessage` with the invoice's RecordInstanceID and triggering user's name
4. Build `AMessage` with Action = `ActualFunds`
5. Call `FinanceServices.MakePostRestCall(aMessage, eventHelper)`

**Message payload** (JSON):
```json
{
  "Action": "ActualFunds",
  "Data": {
    "RecordId": "<invoice record id>",
    "ModifiedBy": "<username>"
  }
}
```

### CreateInoviceToUtilizationLink

**Location**: `CreateInoviceToUtilizationLink.cs` (root of Financials folder)

**Trigger**: PostCreate on Provider Service Invoice

**Purpose**: Link a Provider Service Invoice record to the correct Service Utilization record.

**Logic**:
1. Read the `ProviderUtilizationDD` and `InvoiceDate` fields from the invoice
2. Search for the Service Utilization record matching the utilization ID
3. Set the `ProviderInvoice` and `ProviderInvoiceDate` fields on the Service Utilization
4. Save the Service Utilization record

:::note
This event uses a different prefix (`[NMImpact] Provider Service Invoice`) and works with a specific Provider Service Invoice data list, not the general `F_invoice` entity.
:::

### CreateInvoiceApprovalHistory

**Trigger**: PostUpdate on Invoice

**Purpose**: Create approval history record when invoice approval status changes. Handles Submission, Approval, Temporary Rejection, and Rejection types.

## Service Utilization County

### ServiceUtilSetCounty

**Trigger**: PostCreate, PostUpdate on Service Utilization

**Purpose**: Set the county field on the Service Utilization record based on provider or placement data.
