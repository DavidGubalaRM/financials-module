# Adoptions & Guardianship Module

The Adoptions and Guardianship module handles subsidy agreements, terminations, and suspension/reinstatement for adoption and guardianship cases. These events manage the lifecycle of assistance payments and their interaction with Service Authorizations and the Finance Gateway.

## Events

### AdoptionTermination

**Trigger**: PostUpdate on Adoption Assistance Agreement Termination

**Purpose**: When termination is approved (and not yet terminated):
1. Find the related Service Authorization for the child
2. Pause payment on the termination date (end-date the Service Auth)
3. Mark the termination record and parent Adoption Assistance Agreement as terminated
4. If termination date is in a previous month, call gateway with **StopPayment**

### AdoptionSuspensionReInstatement

**Trigger**: PostUpdate on Adoption Assistance Agreement Suspension

**Purpose**: Handle both suspension and reinstatement:

**On Suspension (approved)**:
1. Pause payment on the suspension date (end-date the Service Auth)
2. Mark record as suspended
3. Call gateway with **StopPayment** if suspension date is in a previous month

**On Reinstatement (approved)**:
1. Create a new Service Authorization starting on the reinstatement date
2. Call gateway with **StartPayment** if reinstatement date is in a previous month

### GuardianshipTermination

**Trigger**: PostUpdate on Guardianship Assistance Agreement Termination

**Purpose**: Same flow as `AdoptionTermination`:
1. Find related Service Authorization for the child
2. Pause payment on the termination date
3. Mark both the termination and parent Guardianship Assistance Agreement as terminated
4. Set `Reinstated` flag to false
5. Call gateway with **StopPayment** if termination date is in a previous month

### GuardianshipSuspensionReInstatement

**Trigger**: PostUpdate on Guardianship Assistance Agreement Suspension

**Purpose**: Same flow as `AdoptionSuspensionReInstatement`:
- Suspension: end-date Service Auth, call **StopPayment**
- Reinstatement: create new Service Auth, call **StartPayment**

## Rate Integration

Adoption and Guardianship assistance agreements are used in **CalculateRateAndTotalAmount** when the Service Catalog's `Ratedetermination` is:
- `Adoptionassistanceagreement`
- `Guardianshipassistanceagreement`

### Rate Calculation

`CalculateRateForAdoptionAndGuardianship()` processes sorted agreements:
1. Sort agreements by effective date (ascending)
2. For each agreement, determine the effective date range (from its start date to the next agreement's start or the end of the service period)
3. Calculate segment: rate x days in segment
4. Return weighted average rate (total amount / total days)

**Adoption fields**: `Assistancepaymentdate`, `Aaaamount`
**Guardianship fields**: `Gaaeffectivedate`, `Gaaamount`

Rate occurrence is always **Monthly** for both types.

## Service Catalog Services

Relevant service codes for subsidy placements:

| Code | Service |
|------|---------|
| SC25 | IVE Subsidized Adoption Post Decree |
| SC24 | IVE Tribal Subsidized Adoption Post Decree |
| SC32 | State Subsidized Adoption Post Decree |
| SC33 | State Tribal IGA Adoption Post Decree |
| SC19 | Guardianship Subsidy Gap IVE |
| SC20 | Guardianship Subsidy Gap IVE Tribal |
| SC21 | Guardianship Subsidy State |
| SC22 | Guardianship Subsidy State Tribal |
