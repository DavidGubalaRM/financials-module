# Event Index

Complete index of all custom events in the Financials module. All events use prefix `[NMImpact] Financials` unless otherwise noted.

## Adoptions & Guardianship

| Class | Exact Name | File | Triggers |
|-------|-----------|------|----------|
| `AdoptionSuspensionReInstatement` | Adoption Assistance Agreement Suspension and ReInstatement | AdoptionsGuardianship/AdoptionSuspensionReInstatement.cs | PostUpdate |
| `AdoptionTermination` | Adoption Assistance Agreement Termination | AdoptionsGuardianship/AdoptionTermination.cs | PostUpdate |
| `GuardianshipSuspensionReInstatement` | Guardianship Suspension and ReInstatement | AdoptionsGuardianship/GuardianshipSuspensionReInstatement.cs | PostUpdate |
| `GuardianshipTermination` | Guardianship Assistance Termination | AdoptionsGuardianship/GuardianshipTermination.cs | PostUpdate |

## Approval History

| Class | Exact Name | File | Triggers |
|-------|-----------|------|----------|
| `CreateDepositsApprovalHistory` | Create Approval History | ApprovalHistory/CreateDepositsApprovalHistory.cs | PostUpdate |
| `CreateInvoiceApprovalHistory` | Create Approval History | ApprovalHistory/CreateInvoiceApprovalHistory.cs | PostUpdate |
| `CreateOverpaymentApprovalHistory` | Create Approval History | ApprovalHistory/CreateOverpaymentApprovalHistory.cs | PostUpdate |
| `CreateRateOverrideApprovalHistory` | Create Approval History | ApprovalHistory/CreateRateOverrideApprovalHistory.cs | PostUpdate |
| `CreateServiceRequestApprovalHistory` | Create Approval History | ApprovalHistory/CreateServiceRequestApprovalHistory.cs | PostUpdate |
| `CreateServiceUtilApprovalHistory` | Service Utilization Create Approval History | ApprovalHistory/CreateServiceUtilApprovalHistory.cs | PostUpdate |
| `CreateUnderpaymentApprovalHistory` | Create Approval History | ApprovalHistory/CreateUnderpaymentApprovalHistory.cs | PostUpdate |
| `CreateWarrantCancellationApprovalHistory` | Create Approval History | ApprovalHistory/CreateWarrantCancellationApprovalHistory.cs | PostUpdate |

## Child/Youth Training

| Class | Exact Name | File | Triggers |
|-------|-----------|------|----------|
| `ChildYouthTrainingCompleted` | Child/Youth Specific Training Completed | ChildYouthTraining/ChildYouthTrainingCompleted.cs | PostCreate |

## Deposits

| Class | Exact Name | File | Triggers |
|-------|-----------|------|----------|
| `DepositsPostUpdate` | Deposits Post Update | Deposits/DepositsPostUpdate.cs | PostUpdate |
| `DepositsValidations` | Deposits Validations | Deposits/DepositsValidations.cs | PostCreate, PreUpdate |

## Funding Model

| Class | Exact Name | File | Triggers |
|-------|-----------|------|----------|
| `FundAllocationValidation` | Fund Allocation Validations | FundingModel/FundAllocationValidation.cs | PostCreate, PreUpdate |

## Fund Balances

| Class | Exact Name | File | Triggers |
|-------|-----------|------|----------|
| `CreateFundBalanceNewFundAdded` | Create Fund Balances for Added Capped Fund | FundBalances/CreateFundBalanceNewFundAdded.cs | PostCreate |
| `CreateFundBalancePlacementApproval` | Create Fund Balances on Placement Approval | FundBalances/CreateFundBalancePlacementApproval.cs | PostUpdate, Button |

## Initial Fund Distributions

| Class | Exact Name | File | Triggers |
|-------|-----------|------|----------|
| `PopulateInitialFundDistribution` | Populate Initial Fund Distribution | InitialFundDistributions/PopulateInitialFundDistribution.cs | PostCreate, PostUpdate |
| `PopulateInitialFundDistUnderpayments` | Populate Initial Fund Dist Underpayments | InitialFundDistributions/PopulateInitialFundDistUnderpayments.cs | PostCreate, PostUpdate |
| `SentInitialFundDistToGateway` | Send Initial Funds Distribution to Gateway | InitialFundDistributions/SentInitialFundDistToGateway.cs | PostCreate |

## Invoice

| Class | Exact Name | File | Triggers |
|-------|-----------|------|----------|
| `SendActualFundsToGateway` | Send Actual Payment to Gateway | Invoice/SendActualFundsToGateway.cs | PostUpdate |

## Placements

| Class | Exact Name | File | Triggers |
|-------|-----------|------|----------|
| `PlacementApprovalServiceAuth` | Placement Approval Create Service Auth | Placements/PlacementApprovalServiceAuth.cs | PostUpdate |
| `PlacementEndDated` | Placement End Dated | Placements/PlacementEndDated.cs | Button |
| `TemporaryPlacementApprovalServiceUtil` | Temporary Placement Approval Create Service Util | Placements/TemporaryPlacementApprovalServiceUtil.cs | PostUpdate |

## Pregnancy/Parenting

| Class | Exact Name | File | Triggers |
|-------|-----------|------|----------|
| `DeterminePregnancyParentingServiceType` | Determine Pregnancy & Parenting Service Type | PregnancyParentingInformation/DeterminePregnancyParentingServiceType.cs | PostCreate |

## Provider Services

| Class | Exact Name | File | Triggers |
|-------|-----------|------|----------|
| `CheckEndDateOnServiceRateAgreementBatch` | Check End Date on Service Rate Agreement Batch | ProviderServices/CheckEndDateOnServiceRateAgreementBatch.cs | OnSchedule, Button |
| `CheckEndDateOnServiceRateBatch` | Check End Date on Service Rate Batch | ProviderServices/CheckEndDateOnServiceRateBatch.cs | OnSchedule, Button |
| `PlacementApprovalCreateResourceFamilyProvider` | Create Unique Resource Family Provider | ProviderServices/PlacementApprovalCreateResourceFamilyProvider.cs | PostUpdate |
| `ProviderServiceCreatePCOSC` | Create Provider copy under Service Catalog | ProviderServices/ProviderServiceCreatePCOSC.cs | PostCreate, PostUpdate |
| `RateOverrideValidation` | Rate Override Validation | ProviderServices/RateOverrideValidation.cs | PostUpdate |
| `ServiceRatesUpdatePCOSC` | Service Rates Update PCOSC | ProviderServices/ServiceRatesUpdatePCOSC.cs | PostCreate, PostUpdate |
| `ValidateProviderServiceOnPlacement` | Placements Validate Provider Service | ProviderServices/ValidateProviderServiceOnPlacement.cs | PostCreate, PreUpdate |

## Rates

| Class | Exact Name | File | Triggers |
|-------|-----------|------|----------|
| `AgeBasedRatesValidations` | Service Rate Age-Based Rates Validations | Rates/AgeBasedRatesValidations.cs | PostCreate, PostUpdate |
| `CalculateRateAndTotalAmount` | Calculate Daily Rate and Total Amount | Rates/CalculateRateAndTotalAmount.cs | PostCreate, PostUpdate |
| `RateOverridePostUpdate` | Rate Override Approval | Rates/RateOverridePostUpdate.cs | PostUpdate |
| `RatesValidation` | Code Table Rate Validations | Rates/RatesValidation.cs | PostCreate, PreUpdate |
| `ServiceRateAgreementEndingNotification` | Service Rate Agreement Ends in 30 days Notification | Rates/ServiceRateAgreementEndingNotification.cs | PostCreate, PostUpdate |
| `ServiceRateEndingNotificationcs` | Service Rate Ends in 30 days Notification | Rates/ServiceRateEndingNotificationcs.cs | PostCreate, PostUpdate |
| `ServiceRateValidation` | Service Rate Validations | Rates/ServiceRateValidation.cs | PreUpdate |

## Service Authorization

| Class | Exact Name | File | Triggers |
|-------|-----------|------|----------|
| `PauseReInstatePaymentFromStaffingApprovals` | Staffing Approval Pause or ReInstate Service Auth | ServiceAuthorization/PauseReInstatePaymentFromStaffingApprovals.cs | PostUpdate |

## Service Catalog

| Class | Exact Name | File | Triggers |
|-------|-----------|------|----------|
| `ServiceCatalogValidations` | Service Catalog Validations | ServiceCatalog/ServiceCatalogValidations.cs | PostCreate, PreUpdate |

## Service Request

| Class | Exact Name | File | Triggers |
|-------|-----------|------|----------|
| `ServiceRequestApproval` | Service Request Approval | ServiceRequest/ServiceRequestApproval.cs | PostCreate, PostUpdate |
| `ServiceRequestApprovalServiceAuthUtil` | Service Request Approval Create Service Auth | ServiceRequest/ServiceRequestApprovalServiceAuthUtil.cs | PostUpdate |
| `ServiceRequestMirrorFields` | Service Request Post Insert Mirroring | ServiceRequest/ServiceRequestMirrorFields.cs | PostCreate |
| `ServiceRequestReminderNotifications` | Service Request Reminder Notifications | ServiceRequest/ServiceRequestReminderNotifications.cs | PostCreate |
| `ServiceRequestValidations` | Service Request Validations | ServiceRequest/ServiceRequestValidations.cs | PostCreate, PreUpdate |

## Service Utilization

| Class | Exact Name | File | Triggers |
|-------|-----------|------|----------|
| `CreateInoviceToUtilizationLink` | Create Invoice to Utilization Link | CreateInoviceToUtilizationLink.cs | PostCreate |
| `ServiceUtilSetCounty` | Service Util Set County | ServiceUtilization/ServiceUtilSetCounty.cs | PostCreate, PostUpdate |

## Underpayments

| Class | Exact Name | File | Triggers |
|-------|-----------|------|----------|
| `UnderpaymentPostInsert` | Underpayments Post Insert | Underpayments/UnderpaymentPostInsert.cs | PostCreate |
| `UnderpaymentValidations` | Underpayment Validations | Underpayments/UnderpaymentValidations.cs | PostCreate, PreUpdate |
