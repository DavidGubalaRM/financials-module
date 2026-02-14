using MCase.Core.Event;
using MCase.Event.NMImpact.Constants;
using MCase.Event.NMImpact.Financials;
using MCaseCustomEvents.NMImpact.Generated.Entities;
using MCaseEventsSDK;
using MCaseEventsSDK.Util.Data;
using System.Collections.Generic;

namespace MCase.Event.NMImpact
{
    /// <summary>
    /// Post Update Event on Invoice to send 'Actual Payment' to Finance Gateway
    /// </summary>
    public class SendActualFundsToGateway : AMCaseValidateCustomEvent
    {
        public override string PrefixName => "[NMImpact] Financials";
        public override string ExactName => "Send Actual Payment to Gateway";

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
            if (!recordInsData.TryParseRecord(eventHelper, out F_invoice invoiceRecord))
            {
                eventHelper.AddDebugLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                eventHelper.AddErrorLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                return new EventReturnObject(EventStatusCode.Failure, new List<string> { GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity });
            }

            var invoiceStatus = invoiceRecord.Paymentstatus;

            // only call Finance Gateway of Invoice is marked as Paid
            if (invoiceStatus == F_invoiceStatic.DefaultValues.Paid)
            {
                #region Call Finance Gateway
                // set up message
                ManageFundsMessage fundMessage = new ManageFundsMessage()
                {
                    RecordId = invoiceRecord.RecordInstanceID.ToString(),
                    ModifiedBy = triggeringUser.UserName
                };

                AMessage aMessage = new AMessage()
                {
                    Action = NMFinancialConstants.ActionTypes.ActualFunds,
                    Data = fundMessage
                };

                _ = FinanceServices.MakePostRestCall(aMessage, eventHelper);
                #endregion
            }

            return new EventReturnObject(EventStatusCode.Success);
        }
    }
}