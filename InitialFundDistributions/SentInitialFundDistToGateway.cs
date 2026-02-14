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
    /// Post Insert Event on Initial Fund Distribution Records
    /// </summary>
    public class SentInitialFundDistToGateway : AMCaseValidateCustomEvent
    {
        public override string PrefixName => "[NMImpact] Financials";
        public override string ExactName => "Send Initial Funds Distribution to Gateway";

        protected override Dictionary<string, List<string>> SpecificFieldSystemNamesByListSystemName => new Dictionary<string, List<string>>()
        {

        };

        protected override List<EventTrigger> ValidEventTriggers => new List<EventTrigger>
        {
            EventTrigger.PostCreate
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
            if (!recordInsData.TryParseRecord(eventHelper, out F_initialfunddistributions initialFundDistRecord))
            {
                eventHelper.AddDebugLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                eventHelper.AddErrorLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                return new EventReturnObject(EventStatusCode.Failure, new List<string> { GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity });
            }

            #region Call Finance Gateway
            // set up message
            ManageFundsMessage fundMessage = new ManageFundsMessage()
            {
                RecordId = initialFundDistRecord.RecordInstanceID.ToString(),
                ModifiedBy = triggeringUser.UserName
            };

            AMessage aMessage = new AMessage()
            {
                Action = NMFinancialConstants.ActionTypes.CommitFunds,
                Data = fundMessage
            };

            _ = FinanceServices.MakePostRestCall(aMessage, eventHelper);
            #endregion

            return new EventReturnObject(EventStatusCode.Success);
        }
    }
}
