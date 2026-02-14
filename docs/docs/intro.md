---
slug: /
---

# Financials Module

Welcome to the technical documentation for the **Financials** module. This module is part of the mCase Custom Events system and provides comprehensive financial management capabilities for child welfare and foster care services.

## Overview

The Financials module handles:

- **Placement-based payments** -- Service authorizations and rate calculations for foster care placements
- **Fund management** -- Deposits, transfers, fund balances, and allocations with IVE eligibility
- **Service requests** -- Multi-level approval workflows for financial requests (MFD)
- **Invoice processing** -- Integration with the Finance Gateway for actual payment disbursement
- **Adoption & guardianship** -- Subsidy agreements, termination, suspension/reinstatement
- **Rates & agreements** -- Service rate agreements, standard rates, rate overrides, and age-based rate calculations

## Key Features

| Feature | Description |
|---------|-------------|
| **Finance Gateway Integration** | REST API integration for accounting system synchronization (7 action types) |
| **Event-driven Architecture** | 50+ custom events triggered on create/update/button operations |
| **Placement-to-Service Mapping** | Automatic mapping of placement settings/types to 35+ service catalog services |
| **Multi-level Approvals** | Configurable approval workflows (up to 4 levels: Supervisor → Manager → Assoc. Deputy Dir → Deputy Dir) |
| **Rate Determination** | 6 rate types: Standard SRA, Provider SRA, Rate Override, Adoption/Guardianship agreements, No Rate |
| **IVE Eligibility** | Fund allocations check IVE eligibility/reimbursability with FMAP and Tribal FMAP rate support |
| **Batch Notifications** | 30-day advance notifications for expiring rate agreements and service rates |

## Module Structure

```
Financials/
├── Utils/                    # FinanceModels, FinanceServices, NMFinancialConstants
├── NMFinancialUtils.cs       # Core utility functions (~2,000 lines)
├── CreateInoviceToUtilizationLink.cs  # Invoice-to-utilization linking
├── AdoptionsGuardianship/    # Adoption & guardianship lifecycle events (4 events)
├── ApprovalHistory/          # Approval history creation events (8 events)
├── ChildYouthTraining/       # Training completion → LOC update (1 event)
├── Deposits/                 # Deposit validations and post-update (2 events)
├── FundingModel/             # Fund allocation validations (1 event)
├── FundBalances/             # Fund balance creation events (2 events)
├── InitialFundDistributions/ # Initial fund distribution & gateway calls (3 events)
├── Invoice/                  # Send actual funds to gateway (1 event)
├── Placements/               # Placement approval, service auth, end-dating (3 events)
├── PregnancyParentingInformation/  # Service type determination (1 event)
├── ProviderServices/         # Provider service validations & PCOSC (7 events)
├── Rates/                    # Rate calculations, validations, notifications (7 events)
├── ServiceAuthorization/     # Pause/reinstate payment from staffing (1 event)
├── ServiceCatalog/           # Service catalog validations (1 event)
├── ServiceRequest/           # Service request approval flow (5 events)
├── ServiceUtilization/       # Service utilization county setting (1 event)
└── Underpayments/            # Underpayment handling (2 events)
```

## Getting Started

- **[Architecture Overview](architecture/overview)** -- Understand the event-driven architecture
- **[Data Model](architecture/data-model)** -- See how entities relate (Fund → Fund Balance → Deposits, etc.)
- **[Finance Gateway](finance-gateway/overview)** -- Learn about the accounting integration
- **[Placements Module](modules/placements)** -- Start with the placement-based payment flow
- **[Event Index](reference/event-index)** -- Complete index of all 50+ events

## Technology Stack

- **Platform**: mCase Custom Events (C# / .NET Framework)
- **SDK**: MCaseEventsSDK (event framework, data access, ORM entities)
- **Integration**: REST API via `FinanceServices.MakePostRestCall()` (Finance Gateway)
- **Serialization**: Newtonsoft.Json
- **Data**: mCase data lists and generated ORM entities
