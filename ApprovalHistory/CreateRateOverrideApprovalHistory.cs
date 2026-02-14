using MCase.Core.Event;
using MCaseCustomEvents.NMImpact.Generated.Entities;
using MCaseEventsSDK;
using MCase.Event.NMImpact.Constants;
using MCase.Event.NMImpact.Utils.DatalistUtils;
using static MCase.Event.NMImpact.Utils.DatalistUtils.ApprovalHistoryUtils;
using MCaseEventsSDK.Util;
using MCaseEventsSDK.Util.Data;
using System;
using System.Collections.Generic;

namespace MCase.Event.NMImpact
{
    /// <summary>
    /// Creates an approval history record for each stage of the Rate Override approval process
    /// Trigger Type: Post Update
    /// </summary>
    public class CreateRateOverrideApprovalHistory : AMCaseValidateCustomEvent
    {
        public override string PrefixName => "[NMImpact] Rate Override";

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

            if (!recordInsData.TryParseRecord(eventHelper, out Rateoverride rateOverrideRecord))
            {
                eventHelper.AddDebugLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                eventHelper.AddErrorLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                return new EventReturnObject(EventStatusCode.Failure, new List<string> { GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity });
            }

            switch (rateOverrideRecord.Approvalhistorytype)
            {
                case RateoverrideStatic.DefaultValues.Submission:
                    CreateApprovalHistoryRecord(eventHelper, ApprovalhistoryStatic.DefaultValues.Rateoverride, ApprovalhistoryStatic.DefaultValues.Submission,
                        rateOverrideRecord.Submittedby(), rateOverrideRecord.Submittedon, rateOverrideRecord.Submitterscomments, rateOverrideRecord);
                    break;
                case RateoverrideStatic.DefaultValues.Approval:
                    CreateApprovalHistoryRecord(eventHelper, ApprovalhistoryStatic.DefaultValues.Rateoverride, ApprovalhistoryStatic.DefaultValues.Approval,
                        rateOverrideRecord.Approvedrejectedby(), rateOverrideRecord.Approvedon, rateOverrideRecord.Approverscomments, rateOverrideRecord);
                    break;
                case RateoverrideStatic.DefaultValues.Temporaryrejection:
                    CreateApprovalHistoryRecord(eventHelper, ApprovalhistoryStatic.DefaultValues.Rateoverride, ApprovalhistoryStatic.DefaultValues.Temporaryrejection, 
                        rateOverrideRecord.Approvedrejectedby(), rateOverrideRecord.Rejectedon, rateOverrideRecord.Approverscomments, rateOverrideRecord);
                    break;
                case RateoverrideStatic.DefaultValues.Rejection:
                    CreateApprovalHistoryRecord(eventHelper, ApprovalhistoryStatic.DefaultValues.Rateoverride, ApprovalhistoryStatic.DefaultValues.Rejection,
                        rateOverrideRecord.Approvedrejectedby(), rateOverrideRecord.Rejectedon, rateOverrideRecord.Approverscomments, rateOverrideRecord);
                    break;
                default:
                    eventHelper.AddErrorLog($"No approval action type found for {rateOverrideRecord.RecordInstanceID}");
                    break;
            }

            rateOverrideRecord.Approvalhistorytype = "";
            rateOverrideRecord.SaveRecord();
            eventHelper.AddInfoLog($"{TechName} - ProcessEvent completed successfully");

            return new EventReturnObject(EventStatusCode.Success);
        }
    }
}