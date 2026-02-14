# Rates Module

The Rates module handles rate calculation, validation, and notifications for service utilization and rate agreements.

## Rate Calculation: CalculateRateAndTotalAmount

**Trigger**: PostCreate, PostUpdate on Service Utilization (when Request Type = Placement Services)

**Purpose**: Calculate daily rate and total billable amount for placement-based services.

### Rate Determination Types

The Service Catalog's `Ratedetermination` field drives which rate source is used:

| Type | Source | Logic |
|------|--------|-------|
| **Rate Override** | Approved Rate Override on placement | Monthly amount / days in month = daily rate; total = daily rate x placement days |
| **Service Rate Agreement** | Provider's SRA | Use provider SRA rates filtered by Level of Care and date range; age-based or LOC score |
| **Standard Service Rate Agreement** | Standard SRA | Same logic as SRA, from system-wide standard rates |
| **Adoption Assistance Agreement** | Case adoption agreement | Monthly rate prorated for days in period; supports multiple agreements with different effective dates |
| **Guardianship Assistance Agreement** | Case guardianship agreement | Same as adoption, using guardianship agreement fields |
| **No Rate** | -- | Return success without creating utilization amounts |

### Level of Care

- **Level 1, 2**: Determined from placement `Placementslevoffcdd`
- **Level 2/3**: Requires child/youth training completion; defaults to Level 1 until training is done
- **Level 3**: Also uses LOC assessment score (`Locassessscore`) for LOC-score-based rates
- LOC history is checked via `GetLevelOfCareAndScoreFromHistory` to handle mid-period changes

### Age-Based Rates

`CalculateRateForAge()` finds the rate where `personAge` falls within the `StartingAge`--`EndingAge` range. When a person's birthday falls within the service period, `CalculateRateBasedOnAge()` prorates:
- Days before birthday at pre-birthday rate
- Days after birthday at post-birthday rate
- Handles both daily and monthly rate occurrences

### Birthday Proration Example

If a child turns 6 on the 15th of the month:
- Days 1-14: Rate for age 5
- Days 15-30: Rate for age 6
- Monthly rates are prorated; daily rates are summed

### Pregnant and Parenting Youth

For **Non-relative foster home** placements, if the child has a delivery date within the service period:
- Additional amount calculated using **Level 1** rate from Standard SRA (using delivery date as the DOB for the rate lookup)
- Populates: `Pregnantparentingyouth`, `Actualdeliverydate`, `Pregnantparentingrate`, `Pregnantparentingamount`
- Total billable amount = original amount + additional parenting amount

## Rate Validations

### RatesValidation

**Trigger**: PostCreate, PreUpdate on CT FMAP and CT Tribal FMAP rate tables

Validates:
- Start date must be first day of month
- End date must be last day of month
- No overlapping date ranges with existing records

### AgeBasedRatesValidations

**Trigger**: PostCreate, PostUpdate on Age-Based Rate records

Validates:
- Ending age must be greater than starting age
- No overlapping age ranges (sorted by starting age, checks for overlap)

### ServiceRateValidation

**Trigger**: PreUpdate on Service Rate

Prevents users from changing any fields except the end date (immutable after creation). Skips validation if the record was created on the same day.

### RateOverridePostUpdate

**Trigger**: PostUpdate on Rate Override

When rate override status becomes **Approved**, checks if the parent placement record needs a rate override and enables the "Submit for Approval" button on the placement.

## Notifications

### ServiceRateAgreementEndingNotification

**Trigger**: PostCreate, PostUpdate on Service Rate Agreement

Creates a batch notification trigger for **30 days** before the end date. Sends notifications to the System Admin and Contracts Unit work queues with provider names and service details.

### ServiceRateEndingNotificationcs

**Trigger**: PostCreate, PostUpdate on Service Rate

Creates a notification trigger for when the Service Rate is ending (end date + 1 day). Sends notification to the System Admin work queue.

## Provider Service Events

### ServiceRatesUpdatePCOSC

**Trigger**: PostCreate, PostUpdate on Service Rate

For each service linked to the rate, finds all providers with the rate's contract and updates their PCOSC (Provider Child of Service Catalog) records with `HasPrice` and `IsAgeBased` flags.

### CheckEndDateOnServiceRateAgreementBatch

**Trigger**: OnSchedule, Button (batch process)

**Status**: *Disabled as of 11/10/2025* -- updates fields no longer in use.

Processes approved service rate agreements with end dates in the past. Updates PCOSC records for providers linked to the agreements.

### CheckEndDateOnServiceRateBatch

**Trigger**: OnSchedule, Button (batch process)

**Status**: *Disabled as of 11/10/2025* -- updates fields no longer in use.

Processes service rate records with past end dates and updates PCOSC records accordingly.
