using MCase.Core.Event;
using MCase.Event.NMImpact.Constants;
using MCase.Event.NMImpact.Financials;
using MCase.Event.NMImpact.Utils.DatalistUtils;
using MCaseCustomEvents.NMImpact.Generated.Entities;
using MCaseEventsSDK;
using MCaseEventsSDK.Util.Data;
using System.Collections.Generic;

namespace MCase.Event.NMImpact
{
    /// <summary>
    /// Trigger: Post Insert
    /// When new Capped Fund is added, create Fund Balace Record (0 balance), and call Finance Gateway to create account
    /// </summary>
    public class CreateFundBalanceNewFundAdded : AMCaseValidateCustomEvent
    {
        public override string PrefixName => "[NMImpact] Financials";

        public override string ExactName => "Create Fund Balances for Added Capped Fund";

        protected override Dictionary<string, List<string>> SpecificFieldSystemNamesByListSystemName => new Dictionary<string, List<string>>()
        {
        };

        protected override List<EventTrigger> ValidEventTriggers => new List<EventTrigger>()
        {
            EventTrigger.PostCreate
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

            if (!recordInsData.TryParseRecord(eventHelper, out F_fund fundRecord))
            {
                eventHelper.AddDebugLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                eventHelper.AddErrorLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                return new EventReturnObject(EventStatusCode.Failure, new List<string> { GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity });
            }

            var isCappedFund = fundRecord.Cappedfund;
            if (isCappedFund == F_fundStatic.DefaultValues.Yes)
            {
                var fundBalanceInfo = new F_fundbalancesInfo(eventHelper);
                CreateNewCappedFundBalanceRecord(fundBalanceInfo, fundRecord);
            }


            #region Call Finance Gateway
            // set up message
            AccountMessage fundMessage = new AccountMessage()
            {
                FundName = fundRecord.Fundname,
                FundCode = fundRecord.Fundcode,
                RecordId = fundRecord.RecordInstanceID.ToString(),
                ModifiedBy = triggeringUser.UserName
            };

            AMessage aMessage = new AMessage()
            {
                Action = NMFinancialConstants.ActionTypes.CreateAccount,
                Data = fundMessage
            };

            _ = FinanceServices.MakePostRestCall(aMessage, eventHelper);
            #endregion

            return new EventReturnObject(EventStatusCode.Success);
        }

        private void CreateNewCappedFundBalanceRecord(F_fundbalancesInfo fundBalanceInfo, F_fund fundRecord)
        {
            var newFundBalanceRecord = fundBalanceInfo.NewF_fundbalances();
            newFundBalanceRecord.Fund(fundRecord);
            newFundBalanceRecord.Childspecificfund = F_fundbalancesStatic.DefaultValues.No;
            newFundBalanceRecord.Cappedfund = F_fundbalancesStatic.DefaultValues.Yes;
            newFundBalanceRecord.Balance = 0.ToString();
            newFundBalanceRecord.SaveRecord();
        }
    }
}