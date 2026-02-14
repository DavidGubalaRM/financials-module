using MCase.Core.Event;
using MCase.Event.NMImpact.Utils.DatalistUtils;
using MCaseCustomEvents.NMImpact.Generated.Entities;
using MCaseEventsSDK;
using MCaseEventsSDK.Util;
using MCaseEventsSDK.Util.Data;
using System;
using System.Collections.Generic;
using static MCase.Event.NMImpact.Utils.DatalistUtils.ApprovalHistoryUtils;

namespace MCase.Event.NMImpact
{
    /// <summary>
    /// Trigger: Post Update 
    /// 
    /// Handles creation of Submission type Approval History on Service Request.
    /// This is needed because records are automatically submitted to next level.
    /// </summary>
    public class CreateServiceRequestApprovalHistory : AMCaseValidateCustomEvent
    {
        public override string PrefixName => "[NMImpact] Service Request";

        public override string ExactName => "Create Approval History";

        protected override Dictionary<string, List<string>> SpecificFieldSystemNamesByListSystemName => new Dictionary<string, List<string>>();

        protected override Dictionary<string, List<string>> NeededRelationships => new Dictionary<string, List<string>>();

        protected override List<string> RecordDatalistType => new List<string>();

        protected override List<EventTrigger> ValidEventTriggers => new List<EventTrigger>() { EventTrigger.PostUpdate };

        private UserData _supervisorUser = null;
        private UserData _managerUser = null;
        private UserData _associateDepDirUser = null;
        private UserData _depDirUser = null;
        private Servicerequest _serviceRequestRecord;

        /// <summary>
        /// Handles Approval Flow:
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
            // check if the record is a Placements record
            if (!recordInsData.TryParseRecord(eventHelper, out _serviceRequestRecord))
            {
                return new EventReturnObject(EventStatusCode.Failure);
            }

            var isMultiLevelApproval = _serviceRequestRecord.GetFieldValue<bool>(ServicerequestStatic.SystemNames.Multilevelapproval);
            var phase = _serviceRequestRecord.Approvalphase;

            if (isMultiLevelApproval && phase.Equals(ServicerequestStatic.DefaultValues.Submission) 
                && (_serviceRequestRecord.Approvalhistorytype.Equals(ServicerequestStatic.DefaultValues.Level2submission)
                    || _serviceRequestRecord.Approvalhistorytype.Equals(ServicerequestStatic.DefaultValues.Level3submission)
                    || _serviceRequestRecord.Approvalhistorytype.Equals(ServicerequestStatic.DefaultValues.Finallevelsubmission)))
            {

                switch (_serviceRequestRecord.Approvalhistorytype)
                {
                    case ServicerequestStatic.DefaultValues.Level2submission:

                        CreateApprovalHistoryRecord(eventHelper, ApprovalhistoryStatic.DefaultValues.Servicerequest, ApprovalhistoryStatic.DefaultValues.Level2submission,
                            triggeringUser, _serviceRequestRecord.Level2approvalsubmittedon, string.Empty, _serviceRequestRecord);
                        break;

                    case ServicerequestStatic.DefaultValues.Level3submission:
                        CreateApprovalHistoryRecord(eventHelper, ApprovalhistoryStatic.DefaultValues.Servicerequest, ApprovalhistoryStatic.DefaultValues.Level3submission,
                            triggeringUser, _serviceRequestRecord.Level3approvalsubmittedon, string.Empty, _serviceRequestRecord);
                        break;

                    case ServicerequestStatic.DefaultValues.Finallevelsubmission:
                        CreateApprovalHistoryRecord(eventHelper, ApprovalhistoryStatic.DefaultValues.Servicerequest, ApprovalhistoryStatic.DefaultValues.Finallevelsubmission,
                            triggeringUser, _serviceRequestRecord.Finallevelapprovalsubmittedon, string.Empty, _serviceRequestRecord);
                        break;

                    default:
                        break;
                }


                _serviceRequestRecord.Approvalhistorytype = "";
            }

            _serviceRequestRecord.SaveRecord();

            return new EventReturnObject(EventStatusCode.Success);
        }

    }
}
