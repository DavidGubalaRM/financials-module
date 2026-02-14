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
    /// Creates an approval history record for each stage of the Underpayment approval process
    /// Trigger Type: Post Update
    /// </summary>
    public class CreateUnderpaymentApprovalHistory : AMCaseValidateCustomEvent
    {
        public override string PrefixName => "[NMImpact] Underpayments";

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

            if (!recordInsData.TryParseRecord(eventHelper, out Underpayments underpaymentRecord))
            {
                eventHelper.AddDebugLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                eventHelper.AddErrorLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                return new EventReturnObject(EventStatusCode.Failure, new List<string> { GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity });
            }

            switch (underpaymentRecord.Approvalhistorytype)
            {
                case UnderpaymentsStatic.DefaultValues.Submission:
                    CreateApprovalHistoryRecord(eventHelper, ApprovalhistoryStatic.DefaultValues.Underpayments, ApprovalhistoryStatic.DefaultValues.Submission,
                        underpaymentRecord.Submittedby(), underpaymentRecord.Submittedon, underpaymentRecord.Submitterscomments, underpaymentRecord);
                    break;
                case UnderpaymentsStatic.DefaultValues.Approval:
                    CreateApprovalHistoryRecord(eventHelper, ApprovalhistoryStatic.DefaultValues.Underpayments, ApprovalhistoryStatic.DefaultValues.Approval,
                        underpaymentRecord.Approvedrejectedby(), underpaymentRecord.Approvedon, underpaymentRecord.Approverscomments, underpaymentRecord);
                    break;
                case UnderpaymentsStatic.DefaultValues.Temporary:
                    CreateApprovalHistoryRecord(eventHelper, ApprovalhistoryStatic.DefaultValues.Underpayments, ApprovalhistoryStatic.DefaultValues.Temporaryrejection,
                        underpaymentRecord.Approvedrejectedby(), underpaymentRecord.Rejectedon, underpaymentRecord.Approverscomments, underpaymentRecord);
                    break;
                case UnderpaymentsStatic.DefaultValues.Rejection:
                    CreateApprovalHistoryRecord(eventHelper, ApprovalhistoryStatic.DefaultValues.Underpayments, ApprovalhistoryStatic.DefaultValues.Rejection,
                        underpaymentRecord.Approvedrejectedby(), underpaymentRecord.Rejectedon, underpaymentRecord.Approverscomments, underpaymentRecord);
                    break;
                default:
                    eventHelper.AddErrorLog($"No approval action type found for {underpaymentRecord.RecordInstanceID}");
                    break;
            }

            underpaymentRecord.Approvalhistorytype = "";
            underpaymentRecord.SaveRecord();
            eventHelper.AddInfoLog($"{TechName} - ProcessEvent completed successfully");

            return new EventReturnObject(EventStatusCode.Success);
        }
    }
}