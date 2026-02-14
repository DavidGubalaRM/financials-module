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
    /// Post Insert CE on Pregnancy and Parenting Information DL to change Service Type on Service Auth if needed
    /// </summary>
    public class DeterminePregnancyParentingServiceType : AMCaseValidateCustomEvent
    {
        public override string PrefixName => $"[NMImpact] Financials";
        public override string ExactName => "Determine Pregnancy & Parenting Service Type";
        protected override Dictionary<string, List<string>> SpecificFieldSystemNamesByListSystemName => new Dictionary<string, List<string>>
        {

        };
        protected override List<EventTrigger> ValidEventTriggers => new List<EventTrigger>
        {
            EventTrigger.PostCreate,
        };
        protected override Dictionary<string, List<string>> NeededRelationships => new Dictionary<string, List<string>>
        {

        };
        protected override List<string> RecordDatalistType => new List<string>
        {

        };

        /// <summary>
        /// Post Insert Field Validations
        /// </summary>
        /// <param name="eventHelper"></param>
        /// <param name="triggeringUser"></param>
        /// <param name="workflow"></param>
        /// <param name="recordInsData"></param>
        /// <param name="preSaveRecordData"></param>
        /// <param name="datalistsBySystemName"></param>
        /// <param name="fieldsBySystemNameByListName"></param>
        /// <param name="triggerType"></param>
        /// <returns></returns>
        protected override EventReturnObject ProcessEventSpecificLogic(AEventHelper eventHelper, UserData triggeringUser, WorkFlowData workflow,
            RecordInstanceData recordInsData, RecordInstanceData preSaveRecordData, Dictionary<string, DataListData> datalistsBySystemName,
            Dictionary<string, Dictionary<string, FieldData>> fieldsBySystemNameByListName, string triggerType)
        {
            // Begin
            eventHelper.AddInfoLog($"{TechName} - Begin");
            if (!recordInsData.TryParseRecord(eventHelper, out Pregnancyparentinginformation pregnancyParentingRecord))
            {
                eventHelper.AddErrorLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                return new EventReturnObject(EventStatusCode.Failure, new List<string> { GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity });
            }

            var childPersonRecord = pregnancyParentingRecord.GetParentPersons();

            // get placement record
            // TODO If RELATEDPLACEMENTID is populated on Persons, we can use that.
            var placementInfo = new PlacementsInfo(eventHelper);
            var placementFilter = new List<DirectSQLFieldFilterData>
            {
                placementInfo.CreateFilter(PlacementsStatic.SystemNames.Childplacedpersonddd, new List<string> { childPersonRecord.RecordInstanceID.ToString() }),
                placementInfo.CreateFilter(PlacementsStatic.SystemNames.Placementenddate, string.Empty, string.Empty)
            };
            var placementRecord = eventHelper.SearchSingleDataListSQLProcess(placementInfo.GetDataListId(), placementFilter)
                .FirstOrDefault();

            if (placementRecord == null)
            {
                return new EventReturnObject(EventStatusCode.Success);
            }

            // if placement is not (active and extended fostercare) do nothing
            if (!(placementRecord.GetFieldValue(PlacementsStatic.SystemNames.O_status).Equals(PlacementsStatic.DefaultValues.Active)
            && placementRecord.GetFieldValue(PlacementsStatic.SystemNames.Placementsetting).Equals(PlacementsStatic.DefaultValues.Extendedfostercaresetting)))
            {
                return new EventReturnObject(EventStatusCode.Success);
            }

            var newService = NMFinancialConstants.ServiceCatalogServices.ExtendedFosterCarePregnantAndParentingYouth;
            var existingAuth = FindRelatedServiceAuth(eventHelper, childPersonRecord, (long)placementRecord.ParentRecordID);
            if (existingAuth == null || existingAuth.Servicecatalogtype().Uniquecode.Equals(newService))
            {
                //youth already on higher rate so no changes needed
                return new EventReturnObject(EventStatusCode.Success);
            }

            // get Service Catalog
            var serviceCatalogRecord = GetServiceCatalogByUniqueCode(eventHelper, newService);
            if (serviceCatalogRecord == null)
            {
                // this should not happen, but if it does, do nothing
                eventHelper.AddErrorLog(string.Format(NMFinancialConstants.ErrorMessages.serviceCatalogNotFound, newService));
                return new EventReturnObject(EventStatusCode.Success);
            }

            // Get Provider Service
            var providerID = placementRecord.GetFieldValue<long>(PlacementsStatic.SystemNames.Provider);
            var providerRecord = eventHelper.GetActiveRecordById(providerID);
            var providerServiceRecord = GetProviderService(eventHelper, serviceCatalogRecord, providerRecord, placementRecord);
            if (providerServiceRecord == null)
            {
                // if the Provider does not provider the service, do nothing
                eventHelper.AddErrorLog(string.Format(NMFinancialConstants.ErrorMessages.serviceNotOffered, providerRecord.Label, serviceCatalogRecord.Nameofservice));
                return new EventReturnObject(EventStatusCode.Success);
            }

            DateTime dateReported = (DateTime)pregnancyParentingRecord.Datepregnancyreported;
            // End date Service Auth
            HandlePausePayment(eventHelper, triggeringUser, existingAuth, dateReported.AddDays(-1));

            // Create new Service Auth
            var newServiceAuth = HandleReinstatePayment(eventHelper, existingAuth, childPersonRecord, dateReported);
            newServiceAuth.Servicecategory = serviceCatalogRecord.Type; // Category from Service Catalog
            newServiceAuth.Servicetype(providerServiceRecord); // DDD to Provider Service  
            newServiceAuth.Servicecatalogtype(serviceCatalogRecord); // DDD to Service Catalog
            newServiceAuth.Description = $"Create new Service Authorization because Pregant & Parenting Information added.";
            newServiceAuth.SaveRecord();

            HandleGatewayCallForReinstatement(eventHelper, triggeringUser, newServiceAuth, dateReported);


            // show warning message that Rate Override is needed for Pregnant & Parenting service type
            var validationMessages = new List<string>
            {
                $"Please add Rate Override for youth on placement {placementRecord.Label}."
            };
            // End
            eventHelper.AddInfoLog($"{TechName} - End");
            return new EventReturnObject(EventStatusCode.Success, null, validationMessages);
        }

    }
}