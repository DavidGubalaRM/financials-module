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
    /// Creates an approval history record for each stage of the Overpayment approval process
    /// Trigger Type: Post Update
    /// </summary>
    public class CreateOverpaymentApprovalHistory : AMCaseValidateCustomEvent
    {
        public override string PrefixName => "[NMImpact] Overpayments";

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

            if (!recordInsData.TryParseRecord(eventHelper, out F_overpayments overpaymentRecord))
            {
                eventHelper.AddDebugLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                eventHelper.AddErrorLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                return new EventReturnObject(EventStatusCode.Failure, new List<string> { GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity });
            }

            switch (overpaymentRecord.Approvalhistorytype)
            {
                case F_overpaymentsStatic.DefaultValues.Submission:
                    CreateApprovalHistoryRecord(eventHelper, ApprovalhistoryStatic.DefaultValues.Overpayments, ApprovalhistoryStatic.DefaultValues.Submission,
                        overpaymentRecord.Submittedby(), overpaymentRecord.Submittedon, overpaymentRecord.Submitterscomments, overpaymentRecord);
                    break;
                case F_overpaymentsStatic.DefaultValues.Approval:
                    CreateApprovalHistoryRecord(eventHelper, ApprovalhistoryStatic.DefaultValues.Overpayments, ApprovalhistoryStatic.DefaultValues.Approval,
                        overpaymentRecord.Approvedrejectedby(), overpaymentRecord.Approvedon, overpaymentRecord.Approverscomments, overpaymentRecord);
                    break;
                case F_overpaymentsStatic.DefaultValues.Temporary:
                    CreateApprovalHistoryRecord(eventHelper, ApprovalhistoryStatic.DefaultValues.Overpayments, ApprovalhistoryStatic.DefaultValues.Temporaryrejection,
                        overpaymentRecord.Approvedrejectedby(), overpaymentRecord.Rejectedon, overpaymentRecord.Approverscomments, overpaymentRecord);
                    break;
                case F_overpaymentsStatic.DefaultValues.Rejection:
                    CreateApprovalHistoryRecord(eventHelper, ApprovalhistoryStatic.DefaultValues.Overpayments, ApprovalhistoryStatic.DefaultValues.Rejection,
                        overpaymentRecord.Approvedrejectedby(), overpaymentRecord.Rejectedon, overpaymentRecord.Approverscomments, overpaymentRecord);
                    break;
                default:
                    eventHelper.AddErrorLog($"No approval action type found for {overpaymentRecord.RecordInstanceID}");
                    break;
            }

            overpaymentRecord.Approvalhistorytype = "";
            overpaymentRecord.SaveRecord();
            eventHelper.AddInfoLog($"{TechName} - ProcessEvent completed successfully");

            return new EventReturnObject(EventStatusCode.Success);
        }
    }
}