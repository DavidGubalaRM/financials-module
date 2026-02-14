using MCase.Core.Event;
using MCase.Event.NMImpact.Constants;
using MCase.Event.NMImpact.Utils.DatalistUtils;
using MCaseCustomEvents.NMImpact.Generated.Entities;
using MCaseEventsSDK;
using MCaseEventsSDK.Util.Data;
using System.Collections.Generic;
using System.Linq;
using static MCase.Event.NMImpact.NMFinancialUtils;

namespace MCase.Event.NMImpact
{
    /// <summary>
    /// Post Update Event on Placements that checks if Rate Override is needed
    /// If so, checks for an approved rate override record, and sets show approval button to true.
    /// This event is only triggered when one of the following fields changes
    ///  - Placement Setting, Placement Type, Child/Youth, Provider  (client side event sets field to trigger this event)
    /// </summary>
    public class RateOverrideValidation : AMCaseValidateCustomEvent
    {
        public override string PrefixName => "[NMImpact] Financials";
        public override string ExactName => "Rate Override Validation";

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
            var validationMessages = new List<string>();
            // Begin
            eventHelper.AddInfoLog($"{TechName} - Begin");
            if (!recordInsData.TryParseRecord(eventHelper, out Placements placementRecord))
            {
                eventHelper.AddErrorLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                return new EventReturnObject(EventStatusCode.Failure, new List<string> { GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity });
            }
            var parentRecord = GetParentRecord(placementRecord);
            if (parentRecord == null)
            {
                return new EventReturnObject(EventStatusCode.Failure);
            }

            (F_servicecatalog serviceCatalogRecord, _) =
                  ValidateProviderOffersRequiredService(eventHelper, recordInsData, (long)recordInsData.ParentRecordID, validationMessages, false);

            if (serviceCatalogRecord != null)
            {
                PlacementUtils.CheckIfRateOverrideIsNeeded(serviceCatalogRecord, placementRecord, eventHelper, validationMessages);
                placementRecord.SaveRecord();
            }

            if (validationMessages.Any())
            {
                return new EventReturnObject(EventStatusCode.Success, null, validationMessages);
            }

            return new EventReturnObject(EventStatusCode.Success);
        }

    }
}