# Placements Module

The Placements module handles service authorization creation, payment start/stop, and placement lifecycle events. It is the core entry point for placement-based financial processing.

## Events

### PlacementApprovalServiceAuth

**Trigger**: PostUpdate on Placements when status changes to **Active**

**Purpose**: Create Service Authorization when placement is approved.

**Logic**:
1. Parse placement record; exit early if status is not Active
2. Get parent Case or Investigation
3. Check if Service Authorization already exists for placement
4. If no existing auth:
   - Validate provider offers required service (via `requiredServiceMap`)
   - Set authorization fields (provider, service catalog, dates, units, LOC)
   - Handle **TFC agency provider** (agency is the payee, not the home)
   - Handle **Level of Care** -- if LOC 2/3, check if child/youth training is complete; default to Level 1 if not
   - Handle **Extended Foster Care** pregnant/parenting youth (may create two Service Auths)
   - Save Service Authorization
   - If start date is in previous month, call gateway with **StartPayment**
5. If existing auth: sync end date with placement end date

**Special Cases**:
- **Extended Foster Care + Supervised Independent Living**: If pregnant/parenting youth with delivery date after placement start, creates two Service Auths -- one before and one after delivery, each with the appropriate service type
- **TFC (Treatment Foster Care)**: The TFC agency is set as the payee provider; the home provider is stored in a separate `Tfchomeprovider` field
- **Removal County**: Uses latest removal county; falls back to parent Case/Investigation county

### PlacementEndDated

**Trigger**: Button event on Placements

**Purpose**: When placement is end-dated, find the related Service Authorization and pause payment by end-dating it. Calls gateway with **StopPayment**.

### TemporaryPlacementApprovalServiceUtil

**Trigger**: PostUpdate on Temporary Placements

**Purpose**: When a temporary placement is approved and the reason is **Respite**, create a Service Utilization record.

**Logic**:
1. Get parent placement (temporary placements are children of regular placements)
2. For TFC respite placements, check that siblings are not in the same TFC home
3. Look up Standard SRA and calculate rate based on person's age
4. Create Service Utilization with appropriate dates, units, rate, and billable amount

### ValidateProviderServiceOnPlacement

**Trigger**: PostCreate, PreUpdate on Placements

**Purpose**: Validate that the selected provider offers the required service for the placement setting/type combination.

**Logic**:
1. Uses `NMFinancialConstants.requiredServiceMap` to map placement setting + type to service
2. Checks for existing Provider Service record
3. Validates Rate Override requirements if applicable
4. Returns warnings if provider does not offer the required service

### PlacementApprovalCreateResourceFamilyProvider

**Trigger**: PostUpdate on Placements

**Purpose**: When placement status becomes Active, check if a Resource Family Provider record already exists under the parent Case/Investigation. If not, create one with provider details copied from the placement provider.

### RateOverrideValidation

**Trigger**: PostUpdate on Placements (when setting/type/provider changes)

**Purpose**: Check if the placement requires a Rate Override. If so, check for an approved Rate Override record and control whether the "Submit for Approval" button is shown on the placement.

## Placement-to-Service Mapping

Placement **setting** + **type** map to a required **Service Catalog** service. The mapping is defined in `NMFinancialConstants.requiredServiceMap`.

| Setting | Type | Service(s) |
|---------|------|------------|
| Out of Home -- Family Home | Non-relative, Relative, Fictive kin, Native American foster home | SC1-SC4 (Adoption Pre-Decree L1-L3, Out of State), SC28-SC31 (RFFC L1-L3, Out of State) |
| Congregate Care | Community homes | SC8 |
| Congregate Care | Multi-service home | SC11 |
| Congregate Care | Pregnant and parenting home | SC12 |
| Congregate Care | Shelter | SC15 |
| Extended Foster Care | Supervised independent living | SC17 (Pregnant/Parenting) or SC18 (Basic Youth) |
| Extended Foster Care | TLYA 18-20 | SC210 |
| Subsidy and Medicaid | Adoption subsidy | SC25, SC24, SC32, SC33 |
| Subsidy and Medicaid | Guardianship subsidy | SC19, SC20, SC21, SC22 |
| Out of Home -- Private Family Home | Child placement agency home | SC5 (ARCA) |
| Out of Home -- Private Family Home | Treatment foster care home | SC34 (TFC Agency) |
| Therapeutic | Residential treatment care | SC13 (QRTP), SC14 (RTC), SC26 (Out of State) |
| Therapeutic | Acute hospital (behavioral) | SC7 |
| Therapeutic | Acute hospital (medical) | SC6 |
| Therapeutic | Group home care | SC9 |
| Out of State | Out of state foster family | SC4 (Adoption OOS), SC31 (RFFC OOS) |

See [Service Catalog Reference](../reference/service-catalog) for the full list of service codes.
