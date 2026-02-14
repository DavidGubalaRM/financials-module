# Memory: Financials Module Documentation Project

## Project Context

- **Repo**: `financials_module` (Git branch: `main`)
- **Module path**: repo root (`/`)
- **Docs path**: `docs/` (Docusaurus 3 site)
- **Platform**: mCase Custom Events (C# / .NET Framework), MCaseEventsSDK
- **User**: dgubala, Windows 10, PowerShell

## Docusaurus Setup Notes

- **Docusaurus 3.4.0** with `@docusaurus/preset-classic` and `@docusaurus/theme-mermaid`
- Mermaid requires BOTH `markdown: { mermaid: true }` AND `themes: ['@docusaurus/theme-mermaid']` in config
- MDX compilation: angle brackets (`<`, `<=`, `>`) in prose are interpreted as JSX tags. Use plain English instead (e.g., "is $1,000 or less" not "<= $1,000")
- `routeBasePath: '/'` makes docs the homepage (no blog)
- Prism `additionalLanguages: ['csharp']` for C# syntax highlighting
- GitHub Pages deployment configured via `.github/workflows/deploy-docs.yml`
  - Triggers on push to `main` when docs files change
  - `baseUrl: '/'` — site is served from the root (not a subdirectory)
  - Uses `actions/deploy-pages@v4` (GitHub Pages source must be set to "GitHub Actions" in repo settings)
  - `npm ci` requires `package-lock.json` to be committed
- PowerShell does not support `&&` for chaining commands; use `;` instead
- npm install had EPERM issues in sandbox; user needs to run `npm install` locally

## Module Architecture

### Event Model
- All 50+ events extend `AMCaseValidateCustomEvent`
- Prefix: `[NMImpact] Financials` (except `CreateInoviceToUtilizationLink` which uses `[NMImpact] Provider Service Invoice`)
- Trigger types: PostCreate, PostUpdate, PreUpdate, Button, OnSchedule
- Core method: `ProcessEventSpecificLogic(eventHelper, triggeringUser, workflow, recordInsData, preSaveRecordData, ...)`
- Return: `EventReturnObject` with `EventStatusCode.Success` or `Failure`

### Finance Gateway
- REST API called via `FinanceServices.MakePostRestCall()`
- Config: `FINANCEGATEWAY_URL` (endpoint), `FINANCEGATEWAY_KEY` (optional Azure Functions key appended as `?code=`)
- 7 action types: CreateAccount, DepositFunds, CommitFunds, ActualFunds, OverUnderPayments, StopPayment, StartPayment
- Message types: AMessage (wrapper), AccountMessage, ManageFundsMessage, OverUnderMessage
- Uses singleton HttpClient via `HttpClientService.Instance.GetHttpClient()`

### Key Entity Relationships (ORM classes)
- `Cases` / `Investigations` → `Placements` → `Service Authorization (F_serviceline)` → `Service Utilization (F_serviceplanutilization)` → `Invoice (F_invoice)`
- `F_servicecatalog` → `F_servicecatalogfundingmodel` → `F_fundingmodel` → `F_fundallocation` → `F_fund` → `F_fundbalances`
- `F_fundbalances` → `F_deposits` (parent relationship)
- `F_serviceplanutilization` → `F_initialfunddistributions` (created during fund distribution)
- `Providers` → `F_providerservice` → `F_servicecatalog`
- `Providers` → `Providerschildofservicecatalog (PCOSC)` → `F_servicecatalog`
- `Providers` → `F_contract (SRA)` → `F_servicerate` → `F_serviceratesagebased`
- `F_standardservicerateagreement` → `F_servicerate`
- `Placements` → `Rateoverride`
- `Cases` → `Adoptionassistanceagreement` / `Guardianshipassistanceagreement` → `Caseparticipants` → `Persons`

### NMFinancialUtils.cs (~2,000 lines)
- Central utility class with static methods
- Record lookups: GetPlacement, GetParentRecord, GetServiceAuthorization, GetServiceCatalogByUniqueCode, GetStandardSRA, etc.
- Rate calculation: CalculateRateForAge (age range matching), CalculateRateBasedOnAge (birthday proration), CalculateRateForAdoptionAndGuardianship (weighted average from sorted agreements)
- Fund distribution: GetFundAllocations (ordered by priority), HandleFundAllocations (deduct balance, create IFD, call gateway), IsChildIVEEligible, GetCTFMAPRateRecord
- Validation: ValidateProviderOffersRequiredService, GetProviderService
- Notifications: SendServiceRateAgreementEndingNotification (30-day advance), SendServiceRateEndingNotification, SendHolidayRunApproachingNotification, SendBackToSchoolRunApproachingNotification

### NMFinancialConstants
- 35+ ServiceCatalogServices codes (SC1-SC35, SC210, SC211)
- requiredServiceMap: Dictionary<string, List<(List<string> PlacementTypes, string RequiredService)>> mapping 7 placement settings
- ErrorMessages: 7 validation message templates
- ActionTypes, TransactionTypes, AccountingAPI constants

### Key Business Logic

#### Placement Approval Flow
1. Placement status → Active
2. Validate provider offers required service (via requiredServiceMap)
3. Check LOC (Level 1/2/3); Level 2/3 requires training completion
4. Handle TFC (agency is payee, not home)
5. Handle Extended Foster Care pregnant/parenting (may create 2 Service Auths)
6. Create Service Authorization
7. If start date in previous month → call gateway StartPayment

#### Rate Calculation (CalculateRateAndTotalAmount)
- 6 rate determination types from Service Catalog's Ratedetermination field
- Rate Override: monthly amount / days in month = daily rate
- SRA/Standard SRA: filter by LOC, date range; age-based or LOC-score rates
- Adoption/Guardianship: weighted average from sorted agreements
- Pregnant/parenting youth: additional Level 1 amount using delivery date as DOB

#### Fund Distribution (PopulateInitialFundDistribution)
1. Get total billable from Service Utilization
2. Get Fund Allocations ordered by priority
3. For each allocation: check IVE eligibility, FMAP rates, VSSA
4. Find Fund Balance, deduct (capped/child-specific), create IFD, call CommitFunds
5. Remaining balance → error record for manual resolution

#### Service Request Approval
- Multi-level: Level 1 → 2 → 3 → Final (configurable per Service Catalog)
- OTA services: amount > $1,000 triggers 2-level (QA Manager + Deputy Director)
- Approvers resolved from supervisor chain or Event Value Mapping (EVM)
- On final approval: creates Service Auth + Service Utilization(s) based on recurrence

#### Adoption/Guardianship Lifecycle
- Termination: end-date Service Auth, mark terminated, call StopPayment
- Suspension: end-date Service Auth, call StopPayment
- Reinstatement: create new Service Auth, call StartPayment

### Disabled Events (as of 11/10/2025)
- `CheckEndDateOnServiceRateAgreementBatch` - updates fields no longer in use
- `CheckEndDateOnServiceRateBatch` - updates fields no longer in use

### File Naming Quirks
- `CreateInoviceToUtilizationLink.cs` has a typo ("Inovice" not "Invoice") - this is the actual filename in the codebase
- `ServiceRateEndingNotificationcs.cs` has "cs" appended to the name (not a file extension issue, it's part of the class name)

## Documentation Structure

```
docs/
├── docs/
│   ├── intro.md                          # Module overview, structure, getting started
│   ├── architecture/
│   │   ├── overview.md                   # Event-driven architecture, high-level Mermaid diagram
│   │   ├── data-model.md                 # Entity relationships, fund hierarchy, all data flows
│   │   ├── core-components.md            # NMFinancialUtils, FinanceServices, FinanceModels, Constants
│   │   └── event-model.md               # Base class pattern, EventReturnObject, execution flow
│   ├── finance-gateway/
│   │   ├── overview.md                   # Configuration, integration pattern, when called
│   │   ├── actions.md                    # 7 action types with usage-by-event table
│   │   └── messages.md                   # Message classes with examples
│   ├── modules/
│   │   ├── placements.md                 # 6 events, placement-to-service mapping table
│   │   ├── rates.md                      # 7 rate events, 6 rate types, birthday proration
│   │   ├── service-request.md            # 5 events, multi-level approval, ceiling validation
│   │   ├── deposits-funds.md             # 8 events, fund distribution flow, IVE eligibility
│   │   ├── invoice.md                    # 3 events, gateway call, utilization linking
│   │   ├── approvals.md                  # 8 approval history + training/pregnancy/catalog/underpayment events
│   │   └── adoptions-guardianship.md     # 4 events, termination/suspension/reinstatement
│   └── reference/
│       ├── service-catalog.md            # 35+ service codes grouped by category
│       ├── constants.md                  # ErrorMessages, ActionTypes, TransactionTypes, requiredServiceMap
│       └── event-index.md               # Complete index: class, exact name, file, triggers
├── docusaurus.config.js
├── sidebars.js
├── package.json
├── src/css/custom.css
├── static/img/logo.svg
└── README.md
```

## Issues Encountered & Resolved
1. Mermaid not rendering → needed `@docusaurus/theme-mermaid` + both config entries
2. MDX `<=` error → replaced angle brackets with plain English in prose
3. `flowdocs` typo → corrected to `flowchart` in Mermaid diagram
4. PowerShell `&&` not supported → use `;` for command chaining
5. npm EPERM in sandbox → user runs npm install outside sandbox
6. 15+ typos in event-index.md (wrong class names, file paths) → all corrected from source code
