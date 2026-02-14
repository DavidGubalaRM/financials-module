using MCase.Core.Event;
using MCase.Event.NMImpact.Constants;
using MCase.Event.NMImpact.Utils.DatalistUtils;
using MCaseCustomEvents.NMImpact.Generated.Entities;
using MCaseEventsSDK;
using MCaseEventsSDK.Util;
using MCaseEventsSDK.Util.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MCase.Event.NMImpact
{
    /// <summary>
    /// Post Insert / Pre Update on Service Request. If Case/Investigation participant is selected with role = Parent/Guardian/Custodian,
    /// then only Service Category 'Foster Care Parent Incidentals' and 'Involuntary Family Services (Parent or Guardian)' are valid selections.
    /// If Case/Investigation participant is selected with role = Child/Youth
    /// then Service Category 'Foster Care Parent Incidentals' and 'Involuntary Family Services (Parent or Guardian)' selections are NOT valid.
    /// 
    /// Multiple participants with role = Child/Youth cannot be selected if Service Type has a funding model with a Fund Allocation using Fund = 
    /// 'Title-IVE' or Child Specific Fund = 'Yes'.
    /// 
    /// Check for Ceiling - if calculated amount plus amount entered > ceiling, return error. 
    /// </summary>
    public class ServiceRequestValidations : AMCaseValidateCustomEvent
    {
        public override string PrefixName => "[NMImpact] Financials";
        public override string ExactName => "Service Request Validations";

        protected override Dictionary<string, List<string>> SpecificFieldSystemNamesByListSystemName => new Dictionary<string, List<string>>()
        {
        };

        protected override List<EventTrigger> ValidEventTriggers => new List<EventTrigger>
        {
            EventTrigger.PostCreate, EventTrigger.PreUpdate
        };

        protected override Dictionary<string, List<string>> NeededRelationships => new Dictionary<string, List<string>>()
        {
        };

        protected override List<string> RecordDatalistType => new List<string>()
        {
        };

        protected string CategoryDoesNotApplyToParentGuardian = "The Selected Service Category does not apply to Participants with Role Parent/Guardian/Custodian";
        protected string CategoryDoesNotApplyToChildYouth = "The Selected Service Category does not apply to Participants with Role Child/Youth";
        protected string MultipleChildYouthFundAllocationIssue = "Multiple participants with role Child/Youth cannot be selected if Service Type has a Funding Model with a Fund Allocation using Title IV-E Fund or Child Spesific Fund";

        private List<string> _errorMessages;

        protected override EventReturnObject ProcessEventSpecificLogic(AEventHelper eventHelper, UserData triggeringUser, WorkFlowData workflow, RecordInstanceData recordInsData,
            RecordInstanceData preSaveRecordData, Dictionary<string, DataListData> datalistsBySystemName, Dictionary<string, Dictionary<string, FieldData>> fieldsBySystemNameByListName, string triggerType)
        {
            _errorMessages = new List<string>();

            // Begin
            eventHelper.AddInfoLog($"{TechName} - Begin");
            if (!recordInsData.TryParseRecord(eventHelper, out Servicerequest serviceRequestRecord))
            {
                eventHelper.AddDebugLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                eventHelper.AddErrorLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                return new EventReturnObject(EventStatusCode.Failure, new List<string> { GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity });
            }

            var record = workflow.TriggerType.Equals(EventTrigger.PreUpdate.GetEnumDescription())
                ? preSaveRecordData : recordInsData;

            var parentRecordInv = serviceRequestRecord.GetParentInvestigations();
            var parentRecordCase = serviceRequestRecord.GetParentCases();

            bool parentRole = false;
            bool childRole = false;
            int childYouthCount = 0;

            var serviceCategory = record.GetFieldValue(ServicerequestStatic.SystemNames.Mfdapprovaltype);

            if (parentRecordInv != null)
            {
                var investigationParticipants = serviceRequestRecord.Investigationsparticipants();
                foreach (var participant in investigationParticipants)
                {
                    var role = participant.Invparticipantrole1;
                    if (role.Contains(InvestigationparticipantsStatic.DefaultValues.Parent_guardian_custodian)
                        || role.Contains(InvestigationparticipantsStatic.DefaultValues.Primarycaretaker))
                    {
                        parentRole = true;
                    }
                    else if (role.Contains(InvestigationparticipantsStatic.DefaultValues.Child_youth))
                    {
                        childRole = true;
                        childYouthCount += 1;
                    }
                }
            }
            if (parentRecordCase != null)
            {
                var caseParticipants = serviceRequestRecord.Mfdpersidname();
                foreach (var participant in caseParticipants)
                {
                    var role = participant.Role;
                    if (role == CaseparticipantsStatic.DefaultValues.Parent_guardian_custodian)
                    {
                        parentRole = true;
                    }
                    else if (role == CaseparticipantsStatic.DefaultValues.Child_youth)
                    {
                        childRole = true;
                        childYouthCount += 1;
                    }
                }
            }

            if (parentRole)
            {
                // 'Foster Care Parent Incidentals' and 'Involuntary Family Services (Parent or Guardian)'
                if (serviceCategory != ServicerequestStatic.DefaultValues.Fostercareparentincidentals
                    && serviceCategory != ServicerequestStatic.DefaultValues.Involuntaryfamilyservices_parentorguardian_)
                {
                    _errorMessages.Add(CategoryDoesNotApplyToParentGuardian);
                    return new EventReturnObject(EventStatusCode.Failure, _errorMessages);
                }
            }

            if (childRole)
            {
                // Service Category 'Foster Care Parent Incidentals' and 'Involuntary Family Services (Parent or Guardian)' selections are NOT valid.
                if (serviceCategory == ServicerequestStatic.DefaultValues.Fostercareparentincidentals
                    || serviceCategory == ServicerequestStatic.DefaultValues.Involuntaryfamilyservices_parentorguardian_)
                {
                    _errorMessages.Add(CategoryDoesNotApplyToChildYouth);
                    return new EventReturnObject(EventStatusCode.Failure, _errorMessages);
                }
            }

            if (childYouthCount > 1)
            {
                ValidateServiceTypeAndFunds(eventHelper, record, ref _errorMessages);
                if (_errorMessages.Any())
                {
                    return new EventReturnObject(EventStatusCode.Failure, _errorMessages);
                }
            }

            #region Validate Ceiling
            var serviceCatalogRecordID = record.GetFieldValue<long>(ServicerequestStatic.SystemNames.Servicetypememo);
            var serviceCatalogRecord = eventHelper.GetActiveRecordById(serviceCatalogRecordID);
            if (serviceCatalogRecord == null)
            {
                _errorMessages.Add("Missing Service Type");
            }
            else
            {
                var ceiling = serviceCatalogRecord.GetFieldValue<double>(F_servicecatalogStatic.SystemNames.Ceiling);
                if (ceiling != 0)
                {
                    var provider = GetProviderRecord(eventHelper, record);
                    var person = GetPersonRecord(eventHelper, record, ref _errorMessages);
                    if (_errorMessages.Any())
                    {
                        return new EventReturnObject(EventStatusCode.Failure, _errorMessages);
                    }

                    if (provider != null && person != null)
                    {
                        var requestDate = record.GetFieldValue<DateTime>(ServicerequestStatic.SystemNames.Requestdate);
                        var requestAmount = record.GetFieldValue<double>(ServicerequestStatic.SystemNames.Mfdamount);
                        var frequency = string.Empty;
                        var units = 1;
                        // if recurring service, calculate total amount for request
                        var isRecurring = record.GetFieldValue(ServicerequestStatic.SystemNames.Recurringservice);
                        if (isRecurring == ServicerequestStatic.DefaultValues.Yes)
                        {
                            frequency = record.GetFieldValue(ServicerequestStatic.SystemNames.Freqreoccur);
                            units = record.GetFieldValue<int>(ServicerequestStatic.SystemNames.Unitsreccuring);
                        }
                        ValidateCeiling(eventHelper, serviceCatalogRecord, provider, person, requestDate, ceiling, requestAmount, ref _errorMessages, frequency, units);
                    }
                }
            }

            #endregion
            if (_errorMessages.Any())
                return new EventReturnObject(EventStatusCode.Failure, _errorMessages);

            return new EventReturnObject(EventStatusCode.Success);

        }

        /// Multiple participants with role = Child/Youth cannot be selected if Service Type has a funding model with a Fund Allocation using Fund = 
        /// 'Title-IVE' or Child Specific Fund = 'Yes'.
        private void ValidateServiceTypeAndFunds(AEventHelper eventHelper, RecordInstanceData record, ref List<string> _errorMessages)
        {
            var serviceType = record.GetFieldValue<long>(ServicerequestStatic.SystemNames.Servicetypememo);
            var serviceCatalogRecord = eventHelper.GetActiveRecordById(serviceType);
            if (serviceCatalogRecord == null)
                return;

            var availableFundSources = serviceCatalogRecord.GetFieldValue<long>(F_servicecatalogStatic.SystemNames.Availablefundsourcesel);
            var fundingModelRecord = eventHelper.GetActiveRecordById(availableFundSources);
            if (fundingModelRecord == null)
                return;

            var fundListID = eventHelper.GetDataListID(F_fundallocationStatic.SystemName);
            var fundAllocationRecords = eventHelper.GetActiveChildRecordsByListId(availableFundSources, fundListID.Value);

            foreach (var fundAllocation in fundAllocationRecords)
            {
                var fundField = fundAllocation.GetFieldValue<long>(F_fundallocationStatic.SystemNames.Fund);
                var fundRecord = eventHelper.GetActiveRecordById(fundField);
                if (fundRecord == null) continue;

                var fundName = fundRecord.GetFieldValue(F_fundStatic.SystemNames.Fundname);
                var childSpecific = fundRecord.GetFieldValue(F_fundStatic.SystemNames.Childspecificfund);

                if (childSpecific == F_fundStatic.DefaultValues.Yes || fundName == F_fundStatic.DefaultValues.Titleiv_e)
                {
                    _errorMessages.Add(MultipleChildYouthFundAllocationIssue);
                    return;
                }
            }
            return;
        }

        private void ValidateCeiling(AEventHelper eventHelper, RecordInstanceData serviceCatalogRecord, RecordInstanceData provider,
            RecordInstanceData person, DateTime requestDate, double ceiling, double requestAmount, ref List<string> _errorMessages, string frequency, int units)
        {
            var ceilingFreq = serviceCatalogRecord.GetFieldValue(F_servicecatalogStatic.SystemNames.Ceilingfrequency);
            double calculatedAmount = 0;
            DateTime? startDate = null;
            DateTime? endDate = null;
            bool getUtilRecords = false;
            var serviceUtilRecords = new List<RecordInstanceData>();

            if (ceilingFreq == F_servicecatalogStatic.DefaultValues.Perincident)
            {
                // don't get any utils, immediate check.
                if (requestAmount > ceiling)
                    _errorMessages.Add($"The amount ${requestAmount} for this request exceeds the ceiling of ${ceiling} for the selected Service Type.");
                return;
            }

            else if (ceilingFreq == F_servicecatalogStatic.DefaultValues.Perchildpercalendaryear)
            {
                getUtilRecords = true;
                // Dates spanning from beginning to end of request year
                startDate = new DateTime(requestDate.Year, 1, 1);
                endDate = new DateTime(requestDate.Year, 12, 31);

                // Check frequency to calculate total amount
                if (frequency == ServicerequestStatic.DefaultValues.Weekly)
                {
                    // Calculate remaining number of weeks in the year from requestDate
                    var remainingDays = (endDate.Value - requestDate).TotalDays + 1;
                    var remainingWeeks = (int)Math.Ceiling(remainingDays / 7);
                    if (units > remainingWeeks)
                    {
                        requestAmount = requestAmount * remainingWeeks;
                    }
                    else
                    {
                        requestAmount = requestAmount * units;
                    }
                }
                else if (frequency == ServicerequestStatic.DefaultValues.Monthly)
                {
                    // Calculate remaining number of months in the year from requestDate
                    var remainingMonths = 12 - requestDate.Month + 1; // include current month
                    if (units > remainingMonths)
                    {
                        requestAmount = requestAmount * remainingMonths;
                    }
                    else
                    {
                        requestAmount = requestAmount * units;
                    }
                }
            }
            else if (ceilingFreq == F_servicecatalogStatic.DefaultValues.Perchildpermonth)
            {
                getUtilRecords = true;
                // Dates spanning from beginning to end of request month
                startDate = new DateTime(requestDate.Year, requestDate.Month, 1);
                endDate = startDate?.AddMonths(1).AddDays(-1);

                // Check frequency to calculate total amount
                if (frequency == ServicerequestStatic.DefaultValues.Weekly)
                {
                    // Calculate remaining number of weeks in the month from requestDate
                    var remainingDays = (endDate.Value - requestDate).TotalDays + 1;
                    var remainingWeeks = (int)Math.Ceiling(remainingDays / 7);
                    if (units > remainingWeeks)
                    {
                        requestAmount = requestAmount * remainingWeeks;
                    }
                    else
                    {
                        requestAmount = requestAmount * units;
                    }
                }
                else if (frequency == ServicerequestStatic.DefaultValues.Monthly)
                {
                    // Only one month, include current month
                    requestAmount = requestAmount * 1;
                }
            }
            else if (ceilingFreq == F_servicecatalogStatic.DefaultValues.Perchild_lifetime_ || ceilingFreq == F_servicecatalogStatic.DefaultValues.Perfosterparent_lifetime_)
            {
                // get all service utils, no start/end date
                getUtilRecords = true;
            }

            if (getUtilRecords == true)
            {
                serviceUtilRecords = GetServiceUtilRecords(eventHelper, serviceCatalogRecord, person, provider, startDate, endDate);
            }

            foreach (var utilRecord in serviceUtilRecords)
            {
                calculatedAmount += utilRecord.GetFieldValue<double>(F_serviceplanutilizationStatic.SystemNames.Totalbillableamount);
            }

            // add amount from this request
            calculatedAmount += requestAmount;
            if (calculatedAmount > ceiling)
                _errorMessages.Add($"The total amount ${calculatedAmount} including this {frequency} request for ${requestAmount} plus previously used amount ${calculatedAmount - requestAmount} exceeds the ceiling of ${ceiling} for the selected Service Type.");
        }

        private RecordInstanceData GetProviderRecord(AEventHelper eventHelper, RecordInstanceData serviceRequestRecord)
        {
            RecordInstanceData provider = null;
            var pcoscRecordID = serviceRequestRecord.GetFieldValue(ServicerequestStatic.SystemNames.Mfdprovidname);
            var resourceFamilyProvRecordID = serviceRequestRecord.GetFieldValue(ServicerequestStatic.SystemNames.Resourceprovidname);
            if (!string.IsNullOrWhiteSpace(pcoscRecordID))
            {
                var pcoscRecord = eventHelper.GetActiveRecordById(long.Parse(pcoscRecordID));
                var providerID = pcoscRecord.GetFieldValue(ProviderschildofservicecatalogStatic.SystemNames.Provider);
                provider = eventHelper.GetActiveRecordById(long.Parse(providerID));
                return provider;
            }

            else if (!string.IsNullOrWhiteSpace(resourceFamilyProvRecordID))
            {
                var resourceFamilyProvider = eventHelper.GetActiveRecordById(long.Parse(resourceFamilyProvRecordID));
                provider = eventHelper.GetDynamicDropdownRecords(resourceFamilyProvider.RecordInstanceID, ResourcefamilyStatic.SystemNames.Provider)
                    .FirstOrDefault();
                return provider;
            }

            return provider;
        }

        private static RecordInstanceData GetPersonRecord(AEventHelper eventHelper, RecordInstanceData serviceRequestRecord, ref List<string> _errorMessages)
        {
            RecordInstanceData personRecord = null;
            var caseParticipantRecordID = serviceRequestRecord.GetFieldValue(ServicerequestStatic.SystemNames.Mfdpersidname);
            var investigationParticipantRecordID = serviceRequestRecord.GetFieldValue(ServicerequestStatic.SystemNames.Investigationsparticipants);

            if (!string.IsNullOrWhiteSpace(caseParticipantRecordID) && caseParticipantRecordID != null)
            {
                if (caseParticipantRecordID.Contains(MCaseEventConstants.MultiDropDownDelimiter))
                {
                    _errorMessages.Add("Service type has ceiling per child and multiple participants selected. Please select only one participant.");
                    return null;
                }
                var caseParticipantRecord = eventHelper.GetActiveRecordById(long.Parse(caseParticipantRecordID));
                personRecord = eventHelper.GetDynamicDropdownRecords(caseParticipantRecord.RecordInstanceID, CaseparticipantsStatic.SystemNames.Caseparticipantname)
                    .FirstOrDefault();
                return personRecord;
            }

            else if (!string.IsNullOrWhiteSpace(investigationParticipantRecordID) && investigationParticipantRecordID != null)
            {
                if (investigationParticipantRecordID.Contains(MCaseEventConstants.MultiDropDownDelimiter))
                {
                    _errorMessages.Add("Service type has ceiling per child and multiple participants selected. Please select only one participant.");
                    return null;
                }
                var investigationParticipantRecord = eventHelper.GetActiveRecordById(long.Parse(investigationParticipantRecordID));
                personRecord = eventHelper.GetDynamicDropdownRecords(investigationParticipantRecord.RecordInstanceID, InvestigationparticipantsStatic.SystemNames.Invparticipantname)
                    .FirstOrDefault();
                return personRecord;
            }

            return personRecord;
        }

        private List<RecordInstanceData> GetServiceUtilRecords(AEventHelper eventHelper, RecordInstanceData serviceCatalogRecord, RecordInstanceData person,
            RecordInstanceData provider, DateTime? startDate, DateTime? endDate)
        {
            var serviceUtilizationInfo = new F_serviceplanutilizationInfo(eventHelper);
            var serviceUtilDataListID = eventHelper.GetDataListID(F_serviceplanutilizationStatic.SystemName);
            var utilFilters = new List<DirectSQLFieldFilterData>
            {
                serviceUtilizationInfo.CreateFilter(F_serviceplanutilizationStatic.SystemNames.O_status,
                    new List<string> { F_serviceplanutilizationStatic.DefaultValues.Pendingpayment_approved_, F_serviceplanutilizationStatic.DefaultValues.Invoicecreated,
                    F_serviceplanutilizationStatic.DefaultValues.Paid }),
                serviceUtilizationInfo.CreateFilter(F_serviceplanutilizationStatic.SystemNames.Servicecatalogtype, new List<string> { serviceCatalogRecord.RecordInstanceID.ToString() }),
                serviceUtilizationInfo.CreateFilter(F_serviceplanutilizationStatic.SystemNames.Participant, new List<string> { person.RecordInstanceID.ToString() })
            };
            var serviceUtilRecords = eventHelper.SearchSingleDataListSQLProcess(serviceUtilDataListID.Value, utilFilters, provider.RecordInstanceID);

            if (startDate != null && endDate != null)
            {
                serviceUtilRecords = serviceUtilRecords
                    .Where(r => r.GetFieldValue<DateTime>(F_serviceplanutilizationStatic.SystemNames.Startdate) >= startDate &&
                    r.GetFieldValue<DateTime>(F_serviceplanutilizationStatic.SystemNames.Startdate) <= endDate &&
                    r.GetFieldValue<DateTime>(F_serviceplanutilizationStatic.SystemNames.Enddate) >= startDate &&
                    r.GetFieldValue<DateTime>(F_serviceplanutilizationStatic.SystemNames.Enddate) <= endDate)
                    .ToList();
            }

            return serviceUtilRecords;
        }
    }
}