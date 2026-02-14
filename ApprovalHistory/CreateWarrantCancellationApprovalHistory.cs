using MCase.Core.Event;
using MCase.Event.NMImpact.Constants;
using MCase.Event.NMImpact.Utils.DatalistUtils;
using MCaseCustomEvents.NMImpact.Generated.Entities;
using MCaseEventsSDK;
using MCaseEventsSDK.Util.Data;
using System.Collections.Generic;
using static MCase.Event.NMImpact.Utils.DatalistUtils.ApprovalHistoryUtils;

namespace MCase.Event.NMImpact
{
    /// <summary>
    /// Creates an approval history record for each stage of the Warrant Cancellation approval process
    /// Trigger Type: Post Update
    /// </summary>
    public class CreateWarrantCancellationApprovalHistory : AMCaseValidateCustomEvent
    {
        public override string PrefixName => "[NMImpact] Warrant Cancellation";

        public override string ExactName => "Create Approval History";

        protected override Dictionary<string, List<string>> SpecificFieldSystemNamesByListSystemName => new Dictionary<string, List<string>>()
        {
        };

        protected override List<EventTrigger> ValidEventTriggers => new List<EventTrigger>()
        {
            EventTrigger.PostUpdate
        };

        protected override Dictionary<string, List<string>> NeededRelationships => new Dictionary<string, List<string>>
        {
        };
        protected override List<string> RecordDatalistType => new List<string>()
        {
        };

        protected override EventReturnObject ProcessEventSpecificLogic(AEventHelper eventHelper, UserData triggeringUser, WorkFlowData workflow, RecordInstanceData recordInsData, RecordInstanceData preSaveRecordData, Dictionary<string, DataListData> datalistsBySystemName, Dictionary<string, Dictionary<string, FieldData>> fieldsBySystemNameByListName, string triggerType)
        {
            eventHelper.AddInfoLog($"{TechName} - Beginning ProcessEvent");

            if (!recordInsData.TryParseRecord(eventHelper, out Warrantcancellationrequest warrantCancellationRecord))
            {
                eventHelper.AddDebugLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                eventHelper.AddErrorLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                return new EventReturnObject(EventStatusCode.Failure, new List<string> { GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity });
            }

            switch (warrantCancellationRecord.Approvalhistorytype)
            {
                case WarrantcancellationrequestStatic.DefaultValues.Submission:
                    CreateApprovalHistoryRecord(eventHelper, ApprovalhistoryStatic.DefaultValues.Warrantcancellationrequest, ApprovalhistoryStatic.DefaultValues.Submission,
                        warrantCancellationRecord.Submittedby(), warrantCancellationRecord.Submittedon, warrantCancellationRecord.Submitterscomments, warrantCancellationRecord);
                    break;
                case WarrantcancellationrequestStatic.DefaultValues.Approval:
                    CreateApprovalHistoryRecord(eventHelper, ApprovalhistoryStatic.DefaultValues.Warrantcancellationrequest, ApprovalhistoryStatic.DefaultValues.Approval,
                        warrantCancellationRecord.Approvedrejectedby(), warrantCancellationRecord.Approvedon, warrantCancellationRecord.Approverscomments, warrantCancellationRecord);
                    break;
                case WarrantcancellationrequestStatic.DefaultValues.Temporary:
                    CreateApprovalHistoryRecord(eventHelper, ApprovalhistoryStatic.DefaultValues.Warrantcancellationrequest, ApprovalhistoryStatic.DefaultValues.Temporaryrejection,
                        warrantCancellationRecord.Approvedrejectedby(), warrantCancellationRecord.Rejectedon, warrantCancellationRecord.Approverscomments, warrantCancellationRecord);
                    break;
                case WarrantcancellationrequestStatic.DefaultValues.Rejection:
                    CreateApprovalHistoryRecord(eventHelper, ApprovalhistoryStatic.DefaultValues.Warrantcancellationrequest, ApprovalhistoryStatic.DefaultValues.Rejection,
                        warrantCancellationRecord.Approvedrejectedby(), warrantCancellationRecord.Rejectedon, warrantCancellationRecord.Approverscomments, warrantCancellationRecord);
                    break;
                default:
                    eventHelper.AddErrorLog($"No approval action type found for {warrantCancellationRecord.RecordInstanceID}");
                    break;
            }

            warrantCancellationRecord.Approvalhistorytype = "";
            warrantCancellationRecord.SaveRecord();
            eventHelper.AddInfoLog($"{TechName} - ProcessEvent completed successfully");

            return new EventReturnObject(EventStatusCode.Success);
        }
    }
}