using MCase.Core.Event;
using MCase.Event.NMImpact.Constants;
using MCase.Event.NMImpact.Utils.DatalistUtils;
using MCaseCustomEvents.NMImpact.Generated.Entities;
using MCaseEventsSDK;
using MCaseEventsSDK.Util.Data;
using System.Collections.Generic;
using static MCase.Event.NMImpact.NMFinancialUtils;

namespace MCase.Event.NMImpact
{
    /// <summary>
    /// Post Update Event on Placement Approval that creates a new Resource Family Provider record
    /// under the Investigation or Case that the placement was created under. Will check to see if 
    /// this provider already exists 
    /// </summary>
    public class PlacementApprovalCreateResourceFamilyProvider : AMCaseValidateCustomEvent
    {
        public override string PrefixName => "[NMImpact] Financials";
        public override string ExactName => "Create Unique Resource Family Provider";

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
            if (!recordInsData.TryParseRecord(eventHelper, out Placements placementRecord))
            {
                eventHelper.AddDebugLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                eventHelper.AddErrorLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                return new EventReturnObject(EventStatusCode.Failure, new List<string> { GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity });
            }

            var approvalStatus = placementRecord.O_status;
            if (approvalStatus != PlacementsStatic.DefaultValues.Active)
            {
                return new EventReturnObject(EventStatusCode.Success);
            }

            var parentRecord = GetParentRecord(placementRecord);
            var placementProvider = placementRecord.Provider();

            // Check if there is an existing Resource Family Provider for this provider
            var resourceFamilyDataListID = eventHelper.GetDataListID(ResourcefamilyStatic.SystemName);
            var resourceFamilyProviderInfo = new ResourcefamilyInfo(eventHelper);
            var resourceFamilyFilter = new List<DirectSQLFieldFilterData>
            {
                resourceFamilyProviderInfo.CreateFilter(ResourcefamilyStatic.SystemNames.Provider, new List<string> { placementProvider.RecordInstanceID.ToString() })
            };
            List<RecordInstanceData> resourceFamilyProviderRecords = eventHelper.SearchSingleDataListSQLProcess(resourceFamilyDataListID.Value, resourceFamilyFilter, parentRecord.RecordInstanceID);

            if (resourceFamilyProviderRecords.Count > 0)
            {
                eventHelper.AddInfoLog($"Resource Family Provider already exists for Provider ID: {placementProvider.RecordInstanceID}");
                return new EventReturnObject(EventStatusCode.Success);
            }

            // If no existing Resource Family Provider, create a new one
            var newResourceFamilyProviderRecord = resourceFamilyProviderInfo.NewResourcefamily(parentRecord);
            newResourceFamilyProviderRecord.Provider(placementProvider);
            newResourceFamilyProviderRecord.Providername = placementProvider.Providernames;
            newResourceFamilyProviderRecord.Providerid = placementProvider.Provideridwithpr;
            newResourceFamilyProviderRecord.Providercounty(placementProvider.County());
            newResourceFamilyProviderRecord.Providerstate(placementProvider.State());
            newResourceFamilyProviderRecord.SaveRecord();

            return new EventReturnObject(EventStatusCode.Success);
        }
    }
}