using MCase.Core.Event;
using MCase.Event.NMImpact.Constants;
using MCase.Event.NMImpact.Utils.DatalistUtils;
using MCaseCustomEvents.NMImpact.Generated.Entities;
using MCaseEventsSDK;
using MCaseEventsSDK.Util;
using MCaseEventsSDK.Util.Data;
using System.Collections.Generic;
using System.Linq;
using static MCase.Event.NMImpact.NMFinancialUtils;

namespace MCase.Event.NMImpact
{
    /// <summary>
    /// Post Insert/Pre Update Event on Placements that validates selected
    /// Provider offers the required service.
    /// </summary>
    public class ValidateProviderServiceOnPlacement : AMCaseValidateCustomEvent
    {
        public override string PrefixName => "[NMImpact] Financials";
        public override string ExactName => "Placements Validate Provider Service";

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

        protected override EventReturnObject ProcessEventSpecificLogic(AEventHelper eventHelper, UserData triggeringUser, WorkFlowData workflow, RecordInstanceData recordInsData,
            RecordInstanceData preSaveRecordData, Dictionary<string, DataListData> datalistsBySystemName, Dictionary<string, Dictionary<string, FieldData>> fieldsBySystemNameByListName, string triggerType)
        {
            // Begin
            eventHelper.AddInfoLog($"{TechName} - Begin");
            if (!recordInsData.TryParseRecord(eventHelper, out Placements placementRecord))
            {
                eventHelper.AddErrorLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                return new EventReturnObject(EventStatusCode.Failure, new List<string> { GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity });
            }
            var validationMessages = new List<string>();

            var record = workflow.TriggerType.Equals(EventTrigger.PreUpdate.GetEnumDescription())
                ? preSaveRecordData : recordInsData;

            var provider = record.GetFieldValue(PlacementsStatic.SystemNames.Provider);
            if (string.IsNullOrWhiteSpace(provider))
            {
                eventHelper.AddInfoLog("No Provider selected, skipping validation.");
                return new EventReturnObject(EventStatusCode.Success);
            }

            var parentRecordID = record.ParentRecordID.Value;
            // See if Provider, Setting or Type have changed - otherwise skip validation
            var previousProvider = recordInsData.GetFieldValue(PlacementsStatic.SystemNames.Provider);
            var currentProvider = preSaveRecordData.GetFieldValue(PlacementsStatic.SystemNames.Provider);

            var previousSetting = recordInsData.GetFieldValue(PlacementsStatic.SystemNames.Placementsetting);
            var currentSetting = preSaveRecordData.GetFieldValue(PlacementsStatic.SystemNames.Placementsetting);

            var previousType = recordInsData.GetFieldValue(PlacementsStatic.SystemNames.Placementtype);
            var currentType = preSaveRecordData.GetFieldValue(PlacementsStatic.SystemNames.Placementtype);
            if (previousProvider != currentProvider || previousSetting != currentSetting || previousType != currentType)
            {
                (F_servicecatalog serviceCatalogRecord, _) =
                   ValidateProviderOffersRequiredService(eventHelper, record, parentRecordID, validationMessages);

                if (serviceCatalogRecord != null && workflow.TriggerType.Equals(EventTrigger.PostCreate.GetEnumDescription()))
                {
                    PlacementUtils.CheckIfRateOverrideIsNeeded(serviceCatalogRecord, placementRecord, eventHelper, validationMessages);
                    placementRecord.SaveRecord();
                }
            }

            if (validationMessages.Any())
            {
                eventHelper.AddWarningLog(validationMessages.ToString());
                // TODO: uncomment below when ready to enforce, for now show warning
                //return new EventReturnObject(EventStatusCode.Failure, validationMessages);
                return new EventReturnObject(EventStatusCode.Success, null, validationMessages);
            }

            return new EventReturnObject(EventStatusCode.Success);
        }

    }
}