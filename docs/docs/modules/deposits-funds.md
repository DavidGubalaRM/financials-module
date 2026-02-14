# Deposits and Funds Module

The Deposits and Funds module handles fund balances, deposits, transfers, fund allocation, and initial fund distributions. See [Data Model Architecture](../architecture/data-model) for entity relationships.

## Deposits

### DepositsValidations

**Trigger**: PostCreate, PreUpdate on Deposits

**Purpose**: Validate deposit/transfer rules before allowing save.

**Validations**:
1. Deposits only allowed for **Child-Specific Funds** or **Capped Funds** (cannot deposit to uncapped, non-child-specific funds)
2. Cannot transfer from same account (`TransferFromAccount` must differ from parent Fund Balance)
3. If transferring: deposit amount must not exceed source account balance

### DepositsPostUpdate

**Trigger**: PostUpdate on Deposits

**Purpose**: When deposit is **Approved**, update Fund Balances and call Finance Gateway.

**Logic**:
1. Exit if status is not Approved or fund is not capped/child-specific
2. Add deposit amount to parent Fund Balance
3. If transfer: subtract amount from source Fund Balance
4. Call Finance Gateway with **DepositFunds** action
5. Set transaction type to `"T"` (Transfer) if `TransferFromAccount` is populated, otherwise `"D"` (Deposit)

## Fund Balances

### CreateFundBalanceNewFundAdded

**Trigger**: PostCreate on Fund (`F_fund`)

**Purpose**: When a new fund is created:
1. If fund is **capped**: create a Fund Balance record with zero balance
2. Call Finance Gateway with **CreateAccount** action (for all new funds)

### CreateFundBalancePlacementApproval

**Trigger**: PostUpdate (and Button for testing) on Placements

**Purpose**: When placement is approved, create child-specific Fund Balance records for:
- Child's Personal Account -- Dedicated SSI Account
- Child's Personal Account -- RSDI
- Child's Personal Account -- SSI

Checks if Fund Balance already exists for the child and fund before creating.

## Funding Model

### FundAllocationValidation

**Trigger**: PostCreate, PreUpdate on Fund Allocation

**Purpose**: Validate fund allocation date rules.

**Validations**:
1. Start date must be the **first day of the month**
2. End date must be the **last day of the month**
3. No overlapping date ranges with other fund allocation records

## Initial Fund Distributions

### PopulateInitialFundDistribution

**Trigger**: PostCreate, PostUpdate on Service Utilization

**Purpose**: The core fund distribution event. Finds Fund Balances for each Fund Allocation and creates Initial Fund Distribution records.

**Logic**:
1. Get total billable amount from Service Utilization; exit if zero
2. Get Service Catalog and its Funding Model for the utilization date
3. Get Fund Allocations ordered by **priority**
4. For each allocation (until remaining amount is zero):
   - Check **IVE eligibility** if required by the fund allocation
   - Determine percentage or specific amount to allocate
   - Handle FMAP / Tribal FMAP rate lookups if applicable
   - Handle signed VSSA requirements if applicable
   - Find Fund Balance for the allocation's Fund (child-specific or shared)
   - For capped/child-specific funds: deduct from balance (take what is available if insufficient)
   - Create Initial Fund Distribution record with amount, percentage, line number
   - Call Finance Gateway with **CommitFunds**
5. If remaining balance > 0 after all allocations: create error record for manual resolution
6. Set Service Utilization to ReadOnly if status is Pending Payment (Approved)

### PopulateInitialFundDistUnderpayments

**Trigger**: PostCreate, PostUpdate on Underpayments

**Purpose**: Same logic as `PopulateInitialFundDistribution` but for underpayment records. Creates Initial Fund Distribution records with the underpayment as the parent record.

### SentInitialFundDistToGateway

**Trigger**: PostCreate on Initial Fund Distribution

**Purpose**: Send committed funds to the Finance Gateway with the **CommitFunds** action.

:::note
Most fund distribution records are committed directly in `PopulateInitialFundDistribution` (which calls the gateway inline). This event handles cases where Initial Fund Distribution records are created through other means.
:::
