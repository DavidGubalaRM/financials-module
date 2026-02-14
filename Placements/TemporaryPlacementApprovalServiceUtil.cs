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
using static MCase.Event.NMImpact.Constants.NMFinancialConstants;

namespace MCase.Event.NMImpact
{
    /// <summary>
    /// Post Update Event on Temporary Placement when status is changed to Approved and Temporary Placement Type = 'Respite'
    /// Create Service Utilization. 
    /// </summary>
    public class TemporaryPlacementApprovalServiceUtil : AMCaseValidateCustomEvent
    {
        public override string PrefixName => "[NMImpact] Financials";
        public override string ExactName => "Temporary Placement Approval Create Service Util";

        protected override Dictionary<string, List<string>> SpecificFieldSystemNamesByListSystemName => new Dictionary<string, List<string>>()
        {

        };

        protected override List<EventTrigger> ValidEventTriggers => new List<EventTrigger>
        {
            EventTrigger.PostUpdate
        };

        protected override Dictionary<string, List<string>> NeededRelationships => new Dictionary<string, List<string>>()
        {

        };

        protected override List<string> RecordDatalistType => new List<string>()
        {

        };

        protected override EventReturnObject ProcessEventSpecificLogic(AEventHelper eventHelper, UserData triggeringUser, WorkFlowData workflow, RecordInstanceData recordInsData,
            RecordInstanceData preSaveRecordData, Dictionary<string, DataListData> datalistsBySystemName, Dictionary<string, Dictionary<string, FieldData>> fieldsBySystemNameByListName, string triggerType)
        {
            // Begin
            eventHelper.AddInfoLog($"{TechName} - Begin");
            if (!recordInsData.TryParseRecord(eventHelper, out Temporaryplacement tempPlacementRecord))
            {
                eventHelper.AddErrorLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                return new EventReturnObject(EventStatusCode.Failure, new List<string> { GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity });
            }

            var status = tempPlacementRecord.Temporaryplacementstatus;
            var reason = tempPlacementRecord.Temporaryabsencereason;
            if (status != TemporaryplacementStatic.DefaultValues.Approved || reason != TemporaryplacementStatic.DefaultValues.Respite)
            {
                return new EventReturnObject(EventStatusCode.Success);
            }

            Providers providerRecord = tempPlacementRecord.Provider();

            if (providerRecord == null)
            {
                return new EventReturnObject(EventStatusCode.Success);
            }

            var parentPlacement = tempPlacementRecord.GetParentPlacements();
            var parentRecordCase = parentPlacement.GetParentCases();
            var parentRecordInv = parentPlacement.GetParentInvestigations();

            // create Service Utils
            F_serviceplanutilization serviceUtilization = null;
            if (parentRecordCase != null)
            {
                serviceUtilization = CreateUtilizationForCase(eventHelper, tempPlacementRecord, parentRecordCase, providerRecord, parentPlacement);
            }
            else if (parentRecordInv != null)
            {
                serviceUtilization = CreateUtilizationForInvestigation(eventHelper, tempPlacementRecord, parentRecordInv, providerRecord, parentPlacement);
            }

            if (serviceUtilization != null)
            {
                var populateIfdResult = new PopulateInitialFundDistribution().ProcessEvent(eventHelper, triggeringUser, workflow, serviceUtilization, preSaveRecordData: preSaveRecordData);
                if (populateIfdResult.Status == EventStatusCode.Failure)
                {
                    // Error
                    eventHelper.AddErrorLog(string.Join(", ", populateIfdResult.Messages));
                    return new EventReturnObject(EventStatusCode.Failure, populateIfdResult.Messages);
                }
            }

            return new EventReturnObject(EventStatusCode.Success);
        }

        private static F_serviceplanutilization CreateUtilizationForCase(AEventHelper eventHelper, Temporaryplacement tempPlacementRecord, Cases parentRecordCase, Providers providerRecord, Placements placementRecord)
        {

            var personRecord = placementRecord.Childyouth().Caseparticipantname();
            var isTfcRespite = tempPlacementRecord.Templacetfcrespite;
         // if child is placed in TFC Home
           if (isTfcRespite.Equals(TemporaryplacementStatic.DefaultValues.Yes))
                {
                // if no siblings are in the same TFC Home, do not create Service util
                if (!CheckIfSiblingInSameTFCHome(eventHelper, tempPlacementRecord, providerRecord, personRecord))
                    return null;
            }

            // create service util
            var serviceUtilization = CreateServiceUtilization(eventHelper, tempPlacementRecord, personRecord, parentRecordCase.Casecounty());
           
            serviceUtilization.Case(parentRecordCase);
            serviceUtilization.Placementcase(placementRecord);
            serviceUtilization.SaveRecord();

            return serviceUtilization;
        }

        private static F_serviceplanutilization CreateUtilizationForInvestigation(AEventHelper eventHelper, Temporaryplacement tempPlacementRecord, Investigations parentRecordInv, Providers providerRecord, Placements placementRecord)
        {

            var personRecord = placementRecord.Childyouthinv().Invparticipantname();
            // it child is placed in TFC Home
            var isTfcRespite = tempPlacementRecord.Templacetfcrespite;
            if (isTfcRespite.Equals(TemporaryplacementStatic.DefaultValues.Yes))
            {
                // if no siblings are in the same TFC Home, do not create Service util
                if (!CheckIfSiblingInSameTFCHome(eventHelper, tempPlacementRecord, providerRecord, personRecord))
                    return null;
            }

            // create service util
            var serviceUtilization = CreateServiceUtilization(eventHelper, tempPlacementRecord, personRecord, parentRecordInv.Invcounty());
            
            serviceUtilization.Investigation(parentRecordInv);
            serviceUtilization.Placementinv(placementRecord);
            serviceUtilization.SaveRecord();

            return serviceUtilization;
        }

        private static readonly List<string> siblingRelationships = new List<string>
        { 
            RelationshipsStatic.DefaultValues.Sibling_adoptive_,
            RelationshipsStatic.DefaultValues.Sibling_biological_,
            RelationshipsStatic.DefaultValues.Sibling_foster_,
            RelationshipsStatic.DefaultValues.Sibling_half_,
            RelationshipsStatic.DefaultValues.Sibling_legal_,
            RelationshipsStatic.DefaultValues.Sibling_step_
        };

        private static bool CheckIfSiblingInSameTFCHome(AEventHelper eventHelper, Temporaryplacement tempPlacementRecord, Providers providerRecord, Persons personRecord)
        {
            // get siblings
            var startDate = tempPlacementRecord.Startdate ?? DateTime.UtcNow;
            var relationshipInfo = new RelationshipsInfo(eventHelper);
            var relationshipFilter = new List<DirectSQLFieldFilterData>
            {
                relationshipInfo.CreateFilter(RelationshipsStatic.SystemNames.Relationshiptype, siblingRelationships ),
            };

            var relationships = eventHelper.SearchSingleDataListSQLProcess(relationshipInfo.GetDataListId(), relationshipFilter, personRecord.RecordInstanceID)
                .Where(ri => ri.GetFieldValue<DateTime>(RelationshipsStatic.SystemNames.Startdate) <= startDate
                           && (ri.GetFieldValue<DateTime>(RelationshipsStatic.SystemNames.Enddate) == default
                              || ri.GetFieldValue<DateTime>(RelationshipsStatic.SystemNames.Enddate) > startDate))
                .ToList();

            // no siblings
            if (relationships.Count == 0)
                return false;

            // now check if any sibling is placed in same TFC Home
            foreach (RecordInstanceData relationshipRecInstance in relationships)
            {

                if (!relationshipRecInstance.TryParseRecord(eventHelper, out Relationships relationship))
                {
                    continue;
                }

                var relatedPerson = relationship.Relatedperson();

                var placementHistoryInfo = new PlacementhistoryInfo(eventHelper);
                var placementHistoryFilter = new List<DirectSQLFieldFilterData>
                {
                    placementHistoryInfo.CreateFilter(PlacementhistoryStatic.SystemNames.Clientname, new List<string> { relatedPerson.RecordInstanceID.ToString() }),
                };

                var placementHistoryRecords = eventHelper.SearchSingleDataListSQLProcess(placementHistoryInfo.GetDataListId(), placementHistoryFilter, providerRecord.RecordInstanceID)
                    .Where(ri => ri.GetFieldValue<DateTime>(PlacementhistoryStatic.SystemNames.Entrydate) <= startDate
                               && (ri.GetFieldValue<DateTime>(PlacementhistoryStatic.SystemNames.Exitdate) == default
                                  || ri.GetFieldValue<DateTime>(PlacementhistoryStatic.SystemNames.Exitdate) > startDate))
                    .ToList();

                if (placementHistoryRecords.Count > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static F_serviceplanutilization CreateServiceUtilization(AEventHelper eventHelper, Temporaryplacement tempPlacementRecord, Persons personRecord, Countylist countyFromparent)
        {
            var provider = tempPlacementRecord.Provider();
            var serviceUtil = new F_serviceplanutilizationInfo(eventHelper);
            var serviceUtilization = serviceUtil.NewF_serviceplanutilization(provider);

            var startDate = tempPlacementRecord.Startdate ?? DateTime.UtcNow;
            var endDate = tempPlacementRecord.Enddate ?? DateTime.UtcNow;
            int units = (endDate - startDate).Days + 1;

            F_servicecatalogInfo scInfo = new F_servicecatalogInfo(eventHelper);
            var scFilter = new List<DirectSQLFieldFilterData>
            {
                scInfo.CreateFilter(F_servicecatalogStatic.SystemNames.Uniquecode, new List<string> { ServiceCatalogServices.Respite }),
            };

            var serviceCatalogRecord = scInfo.CreateQuery(scFilter).FirstOrDefault();

            // determine rate
            (double totalAmount, double rate, string rateOccurence) = GetRate(eventHelper, startDate, endDate, personRecord, serviceCatalogRecord);

            serviceUtilization.O_status = F_serviceplanutilizationStatic.DefaultValues.Pendingpayment_approved_;
            serviceUtilization.Sourcerequesttype = F_serviceplanutilizationStatic.DefaultValues.Respite;
            serviceUtilization.Participant(new List<Persons> { personRecord });
            serviceUtilization.Provider(provider);
            serviceUtilization.Temporaryplacement(tempPlacementRecord);
            serviceUtilization.Servicecatalogtype(serviceCatalogRecord);
            serviceUtilization.Unitsauthorized = units.ToString();
            serviceUtilization.Unitsutilized = units.ToString();
            serviceUtilization.Rateoccurrence = rateOccurence; // F_serviceplanutilizationStatic.DefaultValues.Daily;
            serviceUtilization.Rate = rate.ToString();

            serviceUtilization.Totalbillableamount = string.Format("{0:N2}", totalAmount);
            serviceUtilization.Dateofservice = endDate;
            serviceUtilization.Startdate = startDate;
            serviceUtilization.Enddate = endDate;
            serviceUtilization.Servicecategory = F_servicecatalogStatic.DefaultValues.Placementbasedpayments;

            // County
            var placementRecord = tempPlacementRecord.GetParentPlacements();
            var county = NMFinancialUtils.GetLatestRemovalCounty(eventHelper, personRecord, placementRecord);
            if (county != null)
               serviceUtilization.County(county);
            else
                serviceUtilization.County(countyFromparent); // get county from parent Record

            return serviceUtilization;
        }

        private static (double, double, string) GetRate(AEventHelper eventHelper, DateTime startDate, DateTime endDate, Persons personRecord, F_servicecatalog serviceCatalogRecord)
        {
            var standardSRAInfo = new F_standardservicerateagreementInfo(eventHelper);

            var standardSRARecord = standardSRAInfo.CreateQuery(new List<DirectSQLFieldFilterData> { })
                .Where(r => r.Enddate == null || (r.Enddate >= startDate && r.Enddate <= endDate) && r.Startdate <= endDate)
                .OrderByDescending(ra => ra.Startdate)
                .FirstOrDefault();

            if (standardSRARecord == null)
            {
                eventHelper.AddErrorLog("No Standard Service Rate Agreement found for the given dates.");
                return (0, 0, "");
            }

            var rateRecords = standardSRARecord.GetChildrenF_servicerate()
                .Where(ra => ra.Startdate <= endDate && (ra.Enddate == null || ra.Enddate >= startDate))
                .Where(ra => ra.Forservice().Any(s => s.RecordInstanceID == serviceCatalogRecord.RecordInstanceID))
                .OrderBy(r => r.Startdate)
                .ToList();

            if (!rateRecords.Any())
            {
                eventHelper.AddErrorLog("No matching rate records found.");
                return (0, 0, "");
            }

            DateTime personDOB =NMFinancialUtils. GetPersonDOB(personRecord);
            (double totalAmount, int totalDays, string rateOccurenceToUse) = NMFinancialUtils.HandleRates(eventHelper, personDOB, rateRecords, startDate, endDate, "");

            // Finalize total and average rate
           return (totalAmount, totalDays > 0 ? totalAmount / totalDays : 0, rateOccurenceToUse);
        }
    }
}