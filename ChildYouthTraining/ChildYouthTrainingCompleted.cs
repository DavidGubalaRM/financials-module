using MCase.Core.Event;
using MCase.Event.NMImpact.Constants;
using MCase.Event.NMImpact.Utils.DatalistUtils;
using MCaseCustomEvents.NMImpact.Generated.Entities;
using MCaseEventsSDK;
using MCaseEventsSDK.Util;
using MCaseEventsSDK.Util.Data;
using System;
using System.Collections.Generic;
using static MCase.Event.NMImpact.NMFinancialUtils;

namespace MCase.Event.NMImpact
{
    /// <summary>
    /// Post Insert CE on Child/Youth Spesific Training Dl to create Service Auth (if needed)
    /// </summary>
    public class ChildYouthTrainingCompleted : AMCaseValidateCustomEvent
    {
        public override string PrefixName => $"[NMImpact] Financials";
        public override string ExactName => "Child/Youth Spesific Training Completed";
        protected override Dictionary<string, List<string>> SpecificFieldSystemNamesByListSystemName => new Dictionary<string, List<string>>
        {

        };
        protected override List<EventTrigger> ValidEventTriggers => new List<EventTrigger>
        {
            EventTrigger.PostCreate
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

            if (!recordInsData.TryParseRecord(eventHelper, out Childyouthspecifictraining childYouthSpesificTrainingRecord))
            {
                eventHelper.AddErrorLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                return new EventReturnObject(EventStatusCode.Failure, new List<string> { GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity });
            }

            var allTrainingComplete = (bool)childYouthSpesificTrainingRecord.Alltrainingcomplete.ToBoolean();

            if (allTrainingComplete)
            {

                var placementRecord = childYouthSpesificTrainingRecord.GetParentPlacements();
                var placementLocScore = placementRecord.GetFieldValue(PlacementsStatic.SystemNames.Locassessscore);
                var placementLoc = placementRecord.GetFieldValue(PlacementsStatic.SystemNames.Placementslevoffcdd);
                if (placementRecord.GetFieldValue(PlacementsStatic.SystemNames.O_status).Equals(PlacementsStatic.DefaultValues.Active)
                    && (placementLoc.Equals(PlacementsStatic.DefaultValues.Level2)
                         || placementLoc.Equals(PlacementsStatic.DefaultValues.Level3)))
                {
                    var dateTrainingCompleted = (DateTime)childYouthSpesificTrainingRecord.Datetrainingcomplete;

                    // get new service type
                    List<string> validationMessages = new List<string>();

                    (F_servicecatalog serviceCatalogRecord, F_providerservice providerServiceRecord) =
                        ValidateProviderOffersRequiredService(eventHelper, placementRecord, (long)placementRecord.ParentRecordID, validationMessages, true, placementLoc);

                    // get existing Service Auth
                    var childPersonRecord = placementRecord.Childplacedpersonddd();

                    var existingAuth = FindRelatedServiceAuth(eventHelper, childPersonRecord, (long)placementRecord.ParentRecordID);
                    if (existingAuth != null && !placementLoc.Equals(existingAuth.Placementslevoffcdd))
                    {
                        // End date Service Auth
                        HandlePausePayment(eventHelper, triggeringUser, existingAuth, dateTrainingCompleted.AddDays(-1));

                        if (providerServiceRecord == null || serviceCatalogRecord == null)
                        {
                            // TODO what should we do here?
                            eventHelper.AddErrorLog("Provider Service / Service Catalog not found for the Provider.");
                            return new EventReturnObject(EventStatusCode.Success);
                        }

                        // Create new Service Auth
                        var newServiceAuth = HandleReinstatePayment(eventHelper, existingAuth, childPersonRecord, dateTrainingCompleted);
                        newServiceAuth.Servicecategory = serviceCatalogRecord.Type; // Category from Service Catalog
                        newServiceAuth.Servicetype(providerServiceRecord); // DDD to Provider Service  
                        newServiceAuth.Servicecatalogtype(serviceCatalogRecord); // DDD to Service Catalog
                        newServiceAuth.Placementslevoffcdd = placementLoc;
                        newServiceAuth.Locassessscore = placementLocScore;
                        newServiceAuth.Description = $"Create new Service Authorization because LOC {existingAuth.Placementslevoffcdd} -> {placementLoc} / Score {existingAuth.Locassessscore} -> {placementLocScore} changed.";
                        newServiceAuth.SaveRecord();

                        HandleGatewayCallForReinstatement(eventHelper, triggeringUser, newServiceAuth, dateTrainingCompleted);

                    }
                }
                recordInsData.ReadOnly = true;
                eventHelper.SaveRecord(recordInsData);
            }
          

            // End
            eventHelper.AddInfoLog($"{TechName} - End");
            return new EventReturnObject(EventStatusCode.Success);
        }

    }
}