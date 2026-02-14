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
    /// Creates an approval history record for each stage of the Invoice approval process
    /// Trigger Type: Post Update
    /// </summary>
    public class CreateInvoiceApprovalHistory : AMCaseValidateCustomEvent
    {
        public override string PrefixName => "[NMImpact] Invoice";

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

            if (!recordInsData.TryParseRecord(eventHelper, out F_invoice invoiceRecord))
            {
                eventHelper.AddDebugLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                eventHelper.AddErrorLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                return new EventReturnObject(EventStatusCode.Failure, new List<string> { GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity });
            }

            switch (invoiceRecord.Approvalhistorytype)
            {
                case F_invoiceStatic.DefaultValues.Submission:
                    CreateApprovalHistoryRecord(eventHelper, ApprovalhistoryStatic.DefaultValues.Invoice, ApprovalhistoryStatic.DefaultValues.Submission,
                        invoiceRecord.Submittedby(), invoiceRecord.Submittedon, invoiceRecord.Submitterscomments, invoiceRecord);
                    break;
                case F_invoiceStatic.DefaultValues.Approval:
                    CreateApprovalHistoryRecord(eventHelper, ApprovalhistoryStatic.DefaultValues.Invoice, ApprovalhistoryStatic.DefaultValues.Approval,
                        invoiceRecord.Approvedrejectedby(), invoiceRecord.Approvedon, invoiceRecord.Approverscomments, invoiceRecord);
                    break;
                case F_invoiceStatic.DefaultValues.Temporaryrejection:
                    CreateApprovalHistoryRecord(eventHelper, ApprovalhistoryStatic.DefaultValues.Invoice, ApprovalhistoryStatic.DefaultValues.Temporaryrejection,
                        invoiceRecord.Approvedrejectedby(), invoiceRecord.Rejectedon, invoiceRecord.Approverscomments, invoiceRecord);
                    break;
                case F_invoiceStatic.DefaultValues.Rejection:
                    CreateApprovalHistoryRecord(eventHelper, ApprovalhistoryStatic.DefaultValues.Invoice, ApprovalhistoryStatic.DefaultValues.Rejection,
                        invoiceRecord.Approvedrejectedby(), invoiceRecord.Rejectedon, invoiceRecord.Approverscomments, invoiceRecord);
                    break;
                default:
                    eventHelper.AddErrorLog($"No approval action type found for {invoiceRecord.RecordInstanceID}");
                    break;
            }

            invoiceRecord.Approvalhistorytype = "";
            invoiceRecord.SaveRecord();
            eventHelper.AddInfoLog($"{TechName} - ProcessEvent completed successfully");

            return new EventReturnObject(EventStatusCode.Success);
        }
    }
}