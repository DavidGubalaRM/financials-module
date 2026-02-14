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
using static MCase.Event.NMImpact.NMFinancialUtils;

namespace MCase.Event.NMImpact
{
    /// <summary>
    /// Post Insert Event on Service Utilization for Request Type == "Placement Services".
    /// Placement Providers do not have Service Rate Agreements, get Rate from Standard SRA.
    /// If there is a Rate Override, use amount from Rate override 
    /// (calculate daily rate as amount / number of days between start and end date), otherwise
    /// for Level of Care 1 and 2 (from Placement), use age of child to determine daily rate.
    /// for level of Care 3, use the LOC score (they are supposed to add a LOC score field on the placement) to determine daily rate.
    /// billable Amount = number of days between start and end date* daily rate
    /// </summary>
    public class CalculateRateAndTotalAmount : AMCaseValidateCustomEvent
    {
        public override string PrefixName => "[NMImpact] Financials";
        public override string ExactName => "Calculate Daily Rate and Total Amount";

        protected override Dictionary<string, List<string>> SpecificFieldSystemNamesByListSystemName => new Dictionary<string, List<string>>()
        {

        };

        protected override List<EventTrigger> ValidEventTriggers => new List<EventTrigger>
        {
            EventTrigger.PostCreate, EventTrigger.PostUpdate
        };

        protected override Dictionary<string, List<string>> NeededRelationships => new Dictionary<string, List<string>>()
        {

        };

        protected override List<string> RecordDatalistType => new List<string>()
        {

        };

        protected string inValidStartDate = "Start date is not set or is invalid.";
        protected string inValidEndDate = "End date is not set or is invalid.";
        protected string missingStandardSRA = "No Standard Service Rate Agreement found for the given dates.";
        protected string missingServiceAuthorization = "Service Authorization not found for the Service Utilization.";
        protected string missingPlacement = "Placement Record not found for the Service Utilization.";
        protected string serviceMissingRate = "{0} is not mapped to a Rate type.";

        protected override EventReturnObject ProcessEventSpecificLogic(AEventHelper eventHelper, UserData triggeringUser, WorkFlowData workflow, RecordInstanceData recordInsData,
            RecordInstanceData preSaveRecordData, Dictionary<string, DataListData> datalistsBySystemName, Dictionary<string, Dictionary<string, FieldData>> fieldsBySystemNameByListName, string triggerType)
        {
            var validationMessages = new List<string>();
            // Begin
            eventHelper.AddInfoLog($"{TechName} - Begin");
            if (!recordInsData.TryParseRecord(eventHelper, out F_serviceplanutilization serviceUtilization))
            {
                eventHelper.AddDebugLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                eventHelper.AddErrorLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                return new EventReturnObject(EventStatusCode.Failure, new List<string> { GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity });
            }

            var requestType = serviceUtilization.Sourcerequesttype;
            if (requestType != F_serviceplanutilizationStatic.DefaultValues.Placementservices)
            {
                return new EventReturnObject(EventStatusCode.Success);
            }

            if (!(serviceUtilization.Startdate is DateTime startDate))
            {
                eventHelper.AddErrorLog(inValidStartDate);
                validationMessages.Add(inValidStartDate);
                return new EventReturnObject(EventStatusCode.Failure, validationMessages);
            }

            if (!(serviceUtilization.Enddate is DateTime endDate))
            {
                eventHelper.AddErrorLog(inValidEndDate);
                validationMessages.Add(inValidEndDate);
                return new EventReturnObject(EventStatusCode.Failure, validationMessages);
            }

            double _rate = 0;
            double _totalAmount = 0;
            string _rateOccurrence = string.Empty;

            int days = (endDate - startDate).Days + 1;
            var unitsAuthString = serviceUtilization.Unitsauthorized;
            bool hasValue = int.TryParse(unitsAuthString, out int unitsAuth);
            var serviceCatalogService = serviceUtilization.Servicecatalogtype();
            var serviceName = serviceCatalogService?.Nameofservice;
            var personRecord = serviceUtilization.Participant().FirstOrDefault();

            // Get the Rate Type based on Service Catalog Service
            string rateDetermination = serviceCatalogService.Ratedetermination;

            // Get Related Placement Record
            var placementRecord = GetPlacement(serviceUtilization);
            if (placementRecord == null)
            {
                eventHelper.AddErrorLog(missingPlacement);
                validationMessages.Add(missingPlacement);
                return new EventReturnObject(EventStatusCode.Failure, validationMessages);
            }

            if (string.IsNullOrWhiteSpace(rateDetermination))
            {
                eventHelper.AddErrorLog(string.Format(serviceMissingRate, serviceName));
                validationMessages.Add(string.Format(serviceMissingRate, serviceName));
                return new EventReturnObject(EventStatusCode.Failure, validationMessages);
            }

            switch (rateDetermination)
            {
                case F_servicecatalogStatic.DefaultValues.Rateoverride:
                    HandleRateOverRideServices(eventHelper, startDate, endDate, placementRecord, ref _rateOccurrence, ref _rate, ref _totalAmount);
                    break;
                case F_servicecatalogStatic.DefaultValues.Servicerateagreement:
                    HandleServiceRateAgreement(eventHelper, startDate, endDate, placementRecord, serviceUtilization, personRecord, ref _rateOccurrence, ref _rate, ref _totalAmount);
                    break;
                case F_servicecatalogStatic.DefaultValues.Standardservicerateagreement:
                    HandleStandardServiceRateAgreement(eventHelper, startDate, endDate, placementRecord, serviceUtilization, personRecord, ref _rateOccurrence, ref _rate, ref _totalAmount);
                    break;
                case F_servicecatalogStatic.DefaultValues.Adoptionassistanceagreement:
                    HandleAdoptionAssistanceAgreement(eventHelper, placementRecord, startDate, endDate, personRecord, ref _rateOccurrence, ref _rate, ref _totalAmount);
                    break;
                case F_servicecatalogStatic.DefaultValues.Guardianshipassistanceagreement:
                    HandleGuardianshipAssistanceAgreement(eventHelper, placementRecord, startDate, endDate, personRecord, ref _rateOccurrence, ref _rate, ref _totalAmount);
                    break;
                case F_servicecatalogStatic.DefaultValues.Norate:
                    // No Rate, do not create Service Authorization
                    return new EventReturnObject(EventStatusCode.Success);
            }

            serviceUtilization.Rate = string.Format("{0:N2}", _rate);
            serviceUtilization.Rateoccurrence = _rateOccurrence;
            serviceUtilization.Totalbillableamount = string.Format("{0:N2}", _totalAmount);
            serviceUtilization.Utilizedtotal = string.Format("{0:N2}", _totalAmount);

            // If placement type is Non Relative Foster Home, check if child has a baby and pay extra amount based on the level 1 rate
            if (placementRecord.Placementtype.Equals(PlacementsStatic.DefaultValues.Non_relativefosterhome))
            {
                // check for pregnant and parenting youth
                var placementStartDate = placementRecord.GetFieldValue<DateTime>(PlacementsStatic.SystemNames.Placementdate);
                (DateTime? deliveryDate, _) = GetDeliveryDate(eventHelper, placementRecord, personRecord);

                if (deliveryDate != null && deliveryDate.HasValue && deliveryDate <= endDate)
                {

                    if (deliveryDate > startDate && deliveryDate <= endDate)
                        startDate = (DateTime)deliveryDate;

                    double _addnlrate = 0;
                    double _addnlAmount = 0;
                    HandleStandardServiceRateAgreementAddnlAmount(eventHelper, startDate, endDate, ref _addnlrate, ref _addnlAmount, deliveryDate);

                    serviceUtilization.Pregnantparentingyouth = GeneralConstants.True;
                    serviceUtilization.Actualdeliverydate = deliveryDate;
                    serviceUtilization.Pregnantparentingrate = string.Format("{0:N2}", _addnlrate);
                    serviceUtilization.Pregnantparentingamount = string.Format("{0:N2}", _addnlAmount);
                    serviceUtilization.Totalbillableamount = string.Format("{0:N2}", _totalAmount + _addnlAmount);

                }

            }

            return new EventReturnObject(EventStatusCode.Success);
        }

        private void HandleRateOverRideServices(AEventHelper eventHelper, DateTime startDate, DateTime endDate, Placements placementRecord, ref string _rateOccurrence, ref double _rate, ref double _totalAmount)
        {
            // If there is a Rate Override for the Placement, use amount from Rate override(must be approved)
            // (calculate daily rate as amount / number of days between start and end date)
            var rateOverRideDLID = eventHelper.GetDataListID(RateoverrideStatic.SystemName);
            var rateOverRideInfo = new RateoverrideInfo(eventHelper);
            var rateOverrideFilter = new List<DirectSQLFieldFilterData>
            {
                rateOverRideInfo.CreateFilter(RateoverrideStatic.SystemNames.Rateoverridestatus, new List<string> { RateoverrideStatic.DefaultValues.Approved })
            };

            var approvedRateOverrideRecords = eventHelper.SearchSingleDataListSQLProcess(rateOverRideDLID.Value, rateOverrideFilter, placementRecord.RecordInstanceID);

            if (approvedRateOverrideRecords.Any())
            {
                var overrideRecord = approvedRateOverrideRecords.FirstOrDefault();
                //  var rateOverride = overrideRecord as Rateoverride;
                if (!overrideRecord.TryParseRecord(eventHelper, out Rateoverride rateOverride))
                {
                    // log error that rate override record is missing
                    eventHelper.AddWarningLog(string.Format("Rate Override Record could not be parsed for placement {0}", placementRecord.Label));
                    _rate = 0;
                    _totalAmount = 0;
                    return;
                }
                var rateString = rateOverride.C_rateoverride;
                if (double.TryParse(rateString, out double overrideRate))
                {
                    // calculate number of days in the month of the start date
                    int daysInMonth = DateTime.DaysInMonth(startDate.Year, startDate.Month);

                    // calculate days in placement
                    int placementDays = (endDate - startDate).Days + 1;

                    // calculate daily rate
                    double dailyRate = overrideRate / daysInMonth;

                    _rateOccurrence = F_servicerateStatic.DefaultValues.Monthly;
                    _rate = overrideRate;
                    _totalAmount = Math.Round(dailyRate * placementDays, 2);
                }
            }
            else
            {
                // log error that rate override record is missing
                eventHelper.AddWarningLog(string.Format("Rate Override Record Missing for placement {0}", placementRecord.Label));
                _rate = 0;
                _totalAmount = 0;
            }
        }

        private void HandleServiceRateAgreement(AEventHelper eventHelper, DateTime startDate, DateTime endDate, Placements placementRecord, F_serviceplanutilization serviceUtilization,
            Persons personRecord, ref string _rateOccurrence, ref double _rate, ref double _totalAmount)
        {
            var service = serviceUtilization.Servicecatalogtype();
            var provider = placementRecord.Provider();
            var serviceRateAgreement = provider?.Servicerateagreeid()
                .Where(r => r.O_status == ProvidersStatic.DefaultValues.Approved)
                .Where(r => r.Enddate == null || (r.Enddate >= startDate && r.Enddate <= endDate));

            (var levelOfCare, var locScore) = GetLevelOfCareAndScoreFromHistory(eventHelper, startDate, placementRecord, serviceUtilization);

            double totalAmount = 0;
            int totalDays = 0;
            string rateOccurrenceToUse = null;
            foreach (var agreement in serviceRateAgreement)
            {
                // get the rate records
                var serviceRateRecords = agreement.GetChildrenF_servicerate()
                    .Where(r => r.Levelofcare == levelOfCare)
                    .Where(r => r.Startdate <= endDate && (r.Enddate == null || r.Enddate >= startDate))
                    .Where(r => r.Forservice().Contains(service))
                    .ToList();

                DateTime personDOB = GetPersonDOB(personRecord);
                (double amount, int days, string rateToUse) = HandleRates(eventHelper, personDOB, serviceRateRecords, startDate, endDate, locScore);
                totalAmount += amount;
                totalDays += days;

                // Save the first valid rateOccurrence found
                if (rateOccurrenceToUse == null && !string.IsNullOrEmpty(rateToUse))
                {
                    rateOccurrenceToUse = rateToUse;
                }
            }

            _rateOccurrence = rateOccurrenceToUse ?? "";
            _rate = totalDays > 0 ? totalAmount / totalDays : 0;
            _totalAmount = totalAmount;
        }

        private void HandleStandardServiceRateAgreementAddnlAmount(AEventHelper eventHelper, DateTime startDate, DateTime endDate, 
            ref double _rate, ref double _totalAmount, DateTime? parentingYouthDeliveryDate)
        {
            // for additional payment we pay rate for Level 1
            var serviceCatalogService = GetServiceCatalogByUniqueCode(eventHelper, NMFinancialConstants.ServiceCatalogServices.ResourceFamilyFosterCareLevel1);

            // Get the Standard SRA
            var standardSRARecord = GetStandardSRA(eventHelper, startDate, endDate);
            if (standardSRARecord == null)
            {
                eventHelper.AddErrorLog(missingStandardSRA);
                return;
            }

            // get person's level of care and DOB, for Pregnant and Parenting youth, use delivery date of child.
            var levelOfCare = F_servicerateStatic.DefaultValues.Level1;
            var locScore = "";
            DateTime personDOB = (DateTime)parentingYouthDeliveryDate;

            var rateRecords = GetRateRecords(standardSRARecord, startDate, endDate, levelOfCare, serviceCatalogService.RecordInstanceID);
            if (!rateRecords.Any())
            {
                eventHelper.AddErrorLog("No matching rate records found.");
                return;
            }

            (double totalAmount, int totalDays, string rateOccurenceToUse) = HandleRates(eventHelper, personDOB, rateRecords, startDate, endDate, locScore);

            // Finalize total and average rate
            _totalAmount = totalAmount;
            _rate = totalDays > 0 ? totalAmount / totalDays : 0;
        }

        private F_standardservicerateagreement GetStandardSRA(AEventHelper eventHelper, DateTime startDate, DateTime endDate)
        {
            var standardSRAInfo = new F_standardservicerateagreementInfo(eventHelper);

            var standardSRARecord = standardSRAInfo.CreateQuery(new List<DirectSQLFieldFilterData> { })
                .Where(r => r.Enddate == null || (r.Enddate >= startDate && r.Enddate <= endDate)
                && r.Startdate <= endDate)
                .OrderByDescending(ra => ra.Startdate)
                .FirstOrDefault();

            return standardSRARecord;
        }

        private List<F_servicerate> GetRateRecords( F_standardservicerateagreement standardSRARecord, DateTime startDate, DateTime endDate, string levelOfCare, long scRecordId)
        {
            return standardSRARecord.GetChildrenF_servicerate()
                .Where(ra => ra.Startdate <= endDate && (ra.Enddate == null || ra.Enddate >= startDate))
                .Where(ra => ra.Forservice().Any(s => s.RecordInstanceID == scRecordId))
                .Where(ra => ra.Levelofcare == levelOfCare || string.IsNullOrEmpty(ra.Levelofcare))
                .OrderBy(r => r.Startdate)
                .ToList();
        }

        private void HandleStandardServiceRateAgreement(AEventHelper eventHelper, DateTime startDate, DateTime endDate, Placements placementRecord, F_serviceplanutilization serviceUtilization,
          Persons personRecord, ref string _rateOccurrence,  ref double _rate, ref double _totalAmount)
        {
            var serviceCatalogService = serviceUtilization.Servicecatalogtype();

            // Get the Standard SRA
            var standardSRARecord = GetStandardSRA(eventHelper, startDate, endDate);
            if (standardSRARecord == null)
            {
                eventHelper.AddErrorLog(missingStandardSRA);
                return;
            }

            // get person's level of care
            (var levelOfCare, var locScore) = GetLevelOfCareAndScoreFromHistory(eventHelper, startDate, placementRecord, serviceUtilization);

            // get service rate records
            var rateRecords = GetRateRecords(standardSRARecord, startDate, endDate, levelOfCare, serviceCatalogService.RecordInstanceID);
            if (!rateRecords.Any())
            {
                eventHelper.AddErrorLog("No matching rate records found.");
                return;
            }

            // get person's DOB
            DateTime personDOB = GetPersonDOB(personRecord);

            (double totalAmount, int totalDays, string rateOccurenceToUse) = HandleRates(eventHelper, personDOB, rateRecords, startDate, endDate, locScore);

            // Finalize total and average rate
            _totalAmount = totalAmount;
            _rate = totalDays > 0 ? totalAmount / totalDays : 0;
            _rateOccurrence = rateOccurenceToUse;
        }


        private void HandleAdoptionAssistanceAgreement(AEventHelper eventHelper, Placements placementRecord, DateTime startDate, DateTime endDate, Persons personRecord,
            ref string _rateOccurrence, ref double _rate, ref double _totalAmount)
        {
            _rateOccurrence = F_servicerateStatic.DefaultValues.Monthly;
            Cases caseRecord = GetParentRecord(placementRecord);
            // get case participant Records 
            var caseParticipants = caseRecord.GetChildrenCaseparticipants()
                .Where(cp => cp.Caseparticipantname().RecordInstanceID == personRecord.RecordInstanceID)
                .FirstOrDefault();

            var adoptionAssistanceAgreement = caseRecord.GetChildrenAdoptionassistanceagreement()
                .Where(r => r.Child().RecordInstanceID == caseParticipants.RecordInstanceID)
                .Where(r => r.Assistancepaymentdate <= endDate)
                .ToList();

            if (adoptionAssistanceAgreement.Any())
            {
                _rate = CalculateRateForAdoptionAndGuardianship(startDate, endDate, adoptionAssistanceAgreement, null);
            }

            else
            {
                eventHelper.AddWarningLog("No Active Adoption Assistance Agreement found for the Case.");
            }
            _totalAmount = _rate;

        }

        private void HandleGuardianshipAssistanceAgreement(AEventHelper eventHelper, Placements placementRecord, DateTime startDate, DateTime endDate, Persons personRecord,
            ref string _rateOccurrence, ref double _rate, ref double _totalAmount)
        {
            _rateOccurrence = F_servicerateStatic.DefaultValues.Monthly;
            Cases caseRecord = GetParentRecord(placementRecord);

            // get case participant Records 
            var caseParticipants = caseRecord.GetChildrenCaseparticipants()
                .Where(cp => cp.Caseparticipantname().RecordInstanceID == personRecord.RecordInstanceID)
                .FirstOrDefault();

            var guardianshipAgreementAssistanceRecords = caseRecord.GetChildrenGuardianshipassistanceagreement()
                .Where(r => r.Gaachildddd().RecordInstanceID == caseParticipants.RecordInstanceID)
                .Where(r => r.Gaaeffectivedate <= endDate)
                .ToList();

            if (guardianshipAgreementAssistanceRecords.Any())
            {
                _rate = CalculateRateForAdoptionAndGuardianship(startDate, endDate, null, guardianshipAgreementAssistanceRecords);
            }
            else
            {
                eventHelper.AddWarningLog("No Active Adoption Assistance Agreement found for the Case.");
            }

            _totalAmount = _rate;
        }

    }
}