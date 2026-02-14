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
    /// Creates an approval history record for each stage of the Deposits approval process
    /// Trigger Type: Post Update
    /// </summary>
    public class CreateDepositsApprovalHistory : AMCaseValidateCustomEvent
    {
        public override string PrefixName => "[NMImpact] Deposits";

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

            if (!recordInsData.TryParseRecord(eventHelper, out F_deposits depositsRecord))
            {
                eventHelper.AddDebugLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                eventHelper.AddErrorLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                return new EventReturnObject(EventStatusCode.Failure, new List<string> { GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity });
            }

            switch (depositsRecord.Approvalhistorytype)
            {
                case F_depositsStatic.DefaultValues.Submission:
                    CreateApprovalHistoryRecord(eventHelper, ApprovalhistoryStatic.DefaultValues.Deposits, ApprovalhistoryStatic.DefaultValues.Submission,
                        depositsRecord.Submittedby(), depositsRecord.Submittedon, depositsRecord.Submitterscomments, depositsRecord);
                    break;
                case F_depositsStatic.DefaultValues.Approval:
                    CreateApprovalHistoryRecord(eventHelper, ApprovalhistoryStatic.DefaultValues.Deposits, ApprovalhistoryStatic.DefaultValues.Approval,
                        depositsRecord.Approvedrejectedby(), depositsRecord.Approvedon, depositsRecord.Approverscomments, depositsRecord);
                    break;
                case F_depositsStatic.DefaultValues.Temporary:
                    CreateApprovalHistoryRecord(eventHelper, ApprovalhistoryStatic.DefaultValues.Deposits, ApprovalhistoryStatic.DefaultValues.Temporaryrejection,
                        depositsRecord.Approvedrejectedby(), depositsRecord.Rejectedon, depositsRecord.Approverscomments, depositsRecord);
                    break;
                case F_depositsStatic.DefaultValues.Rejection:
                    CreateApprovalHistoryRecord(eventHelper, ApprovalhistoryStatic.DefaultValues.Deposits, ApprovalhistoryStatic.DefaultValues.Rejection,
                        depositsRecord.Approvedrejectedby(), depositsRecord.Rejectedon, depositsRecord.Approverscomments, depositsRecord);
                    break;
                default:
                    eventHelper.AddErrorLog($"No approval action type found for {depositsRecord.RecordInstanceID}");
                    break;
            }

            depositsRecord.Approvalhistorytype = "";
            depositsRecord.SaveRecord();
            eventHelper.AddInfoLog($"{TechName} - ProcessEvent completed successfully");

            return new EventReturnObject(EventStatusCode.Success);
        }
    }
}