# Approvals Module

The Approvals module creates approval history records for various financial entities. Each event follows the same pattern: on PostUpdate, check the `Approvalhistorytype` field and create the corresponding history record.

## Approval History Events

All events follow this pattern:
1. Parse record, check `Approvalhistorytype` (or equivalent) field
2. Based on the type (Submission, Approval, Temporary Rejection, Rejection), create an `ApprovalHistory` record with:
   - Entity type (e.g., Deposits, Invoice, Service Request)
   - Action type (e.g., Level1approval, Finallevelrejection)
   - Approver/submitter user
   - Date of action
   - Comments
   - Reference to parent record
3. Clear the `Approvalhistorytype` field to prevent duplicate history creation

| Event | Entity | Approval Types |
|-------|--------|----------------|
| `CreateDepositsApprovalHistory` | Deposits | Submission, Approval, Temporary Rejection, Rejection |
| `CreateInvoiceApprovalHistory` | Invoice | Submission, Approval, Temporary Rejection, Rejection |
| `CreateOverpaymentApprovalHistory` | Overpayment | Submission, Approval, Temporary Rejection, Rejection |
| `CreateRateOverrideApprovalHistory` | Rate Override | Submission, Approval, Temporary Rejection, Rejection |
| `CreateServiceRequestApprovalHistory` | Service Request | Level 2/3/Final submission (for multi-level auto-advance) |
| `CreateServiceUtilApprovalHistory` | Service Utilization | Submission, Approval, Rejection |
| `CreateUnderpaymentApprovalHistory` | Underpayment | Submission, Approval, Temporary Rejection, Rejection |
| `CreateWarrantCancellationApprovalHistory` | Warrant Cancellation | Submission, Approval, Temporary Rejection, Rejection |

:::note
Service Request approval history is special. Most history is created inline by `ServiceRequestApproval`. The separate `CreateServiceRequestApprovalHistory` event handles submission records for Level 2, Level 3, and Final Level when the request auto-advances through levels.
:::

## Service Authorization: Pause/Reinstate

### PauseReInstatePaymentFromStaffingApprovals

**Trigger**: PostUpdate on Staffings record

**Purpose**: When a staffing is approved, check the staffing type to determine action:

**Pause Payment** (staffing type = Voluntary Services Support Agreement Pause Payment):
- For each child participant, find their active Service Authorization
- End-date the Service Auth on the staffing approval date
- Call gateway with **StopPayment**

**Reinstate Payment** (staffing type = Voluntary Services Support Agreement Reinstate Payment):
- For each child participant, create a new Service Authorization starting on the approval date
- Call gateway with **StartPayment**

## Other Training/Lifecycle Events

### ChildYouthTrainingCompleted

**Trigger**: PostCreate on Child/Youth Specific Training

**Purpose**: When all training is complete and the placement is active at Level 2 or Level 3, check if the Level of Care has changed. If so:
1. End-date the existing Service Authorization
2. Create a new Service Authorization with the updated service type, service catalog, and level of care

### DeterminePregnancyParentingServiceType

**Trigger**: PostCreate on Pregnancy and Parenting Information

**Purpose**: When pregnancy/parenting info is added for a child in an active Extended Foster Care placement, check if the Service Authorization needs to change service type:
1. If not already the "Extended Foster Care Pregnant and Parenting Youth" service, end the current auth
2. Create a new auth with the appropriate pregnant/parenting service type and provider service

### ServiceCatalogValidations

**Trigger**: PostCreate, PreUpdate on Service Catalog

**Purpose**: Validates ceiling frequency restrictions:
- "Per Foster Parent (lifetime)" can only be used for "Foster Care Parent Incidentals"
- Per-child ceiling frequencies can only be used for "Foster Care Child Incidentals"

## Underpayments

### UnderpaymentPostInsert

**Trigger**: PostCreate on Underpayments

**Purpose**: Mirror the Service Catalog type from the parent Service Utilization to the underpayment's `Originalfund` field.

### UnderpaymentValidations

**Trigger**: PostCreate, PreUpdate on Underpayments

**Purpose**: Validate that the corrected amount is less than the original amount.
