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
    /// Creates an approval history record for each stage of the Service Utilization approval process
    /// Trigger Type: Post Update
    /// </summary>
    public class CreateServiceUtilApprovalHistory : AMCaseValidateCustomEvent
    {
        public override string PrefixName => "[NMImpact] Finance";

        public override string ExactName => "Service Utilization Create Approval History";

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

            if (!recordInsData.TryParseRecord(eventHelper, out F_serviceplanutilization serviceUtilizationRecord))
            {
                eventHelper.AddDebugLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                eventHelper.AddErrorLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                return new EventReturnObject(EventStatusCode.Failure, new List<string> { GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity });
            }

            switch (serviceUtilizationRecord.Approvalhistorytype)
            {
                case F_serviceplanutilizationStatic.DefaultValues.Submission:
                    CreateApprovalHistoryRecord(eventHelper, ApprovalhistoryStatic.DefaultValues.Serviceutilization, ApprovalhistoryStatic.DefaultValues.Submission,
                        serviceUtilizationRecord.Submittedby(), serviceUtilizationRecord.Submittedon, serviceUtilizationRecord.Submitterscomments, serviceUtilizationRecord);
                    break;
                case F_serviceplanutilizationStatic.DefaultValues.Approval:
                    CreateApprovalHistoryRecord(eventHelper, ApprovalhistoryStatic.DefaultValues.Serviceutilization, ApprovalhistoryStatic.DefaultValues.Approval,
                        serviceUtilizationRecord.Approvedrejectedby(), serviceUtilizationRecord.Approvedon, serviceUtilizationRecord.Approverscomments, serviceUtilizationRecord);
                    break;
                case F_serviceplanutilizationStatic.DefaultValues.Rejection:
                    CreateApprovalHistoryRecord(eventHelper, ApprovalhistoryStatic.DefaultValues.Serviceutilization, ApprovalhistoryStatic.DefaultValues.Rejection,
                        serviceUtilizationRecord.Approvedrejectedby(), serviceUtilizationRecord.Rejectedon, serviceUtilizationRecord.Approverscomments, serviceUtilizationRecord);
                    break;
                default:
                    eventHelper.AddErrorLog($"No approval action type found for {serviceUtilizationRecord.RecordInstanceID}");
                    break;
            }

            serviceUtilizationRecord.Approvalhistorytype = "";
            serviceUtilizationRecord.SaveRecord();
            eventHelper.AddInfoLog($"{TechName} - ProcessEvent completed successfully");

            return new EventReturnObject(EventStatusCode.Success);
        }
    }
}