using MCase.Core.Event;
using MCase.Event.NMImpact.Constants;
using MCase.Event.NMImpact.Financials;
using MCase.Event.NMImpact.Utils.DatalistUtils;
using MCaseCustomEvents.NMImpact.Generated.Entities;
using MCaseEventsSDK;
using MCaseEventsSDK.Util;
using MCaseEventsSDK.Util.Data;
using System;
using System.Collections.Generic;
using NMFinancialConstants = MCase.Event.NMImpact.Constants.NMFinancialConstants;
using FinanceServices = MCase.Event.NMImpact.Financials.FinanceServices;

namespace MCase.Event.NMImpact
{
    /// <summary>
    /// Post Update Event on Deposits to make call to account to Deposit / Transfer Funds
    /// This is done when Deposit is Approved
    /// Also update Fund Balances with amount approved.
    /// </summary>
    public class DepositsPostUpdate : AMCaseValidateCustomEvent
    {
        public override string PrefixName => "[NMImpact] Financials";
        public override string ExactName => "Deposits Post Update";

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
            if (!recordInsData.TryParseRecord(eventHelper, out F_deposits depositsRecord))
            {
                eventHelper.AddDebugLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                eventHelper.AddErrorLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                return new EventReturnObject(EventStatusCode.Failure, new List<string> { GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity });
            }

            // only call Finance Gateway of Deposit is approved
            if (!depositsRecord.Approvalstatus.Equals(F_depositsStatic.DefaultValues.Approved))
                return new EventReturnObject(EventStatusCode.Success);

            // get parent Fund Balance and Fund
            var parentFundBalance = depositsRecord.GetParentF_fundbalances();
            var parentFund = parentFundBalance.Fund();

            // If  Fund is not capped and not child spesific, to not call Finance gateway 
            if (!(parentFund.Childspecificfund.Equals(F_fundbalancesStatic.DefaultValues.Yes)
                || parentFund.Cappedfund.Equals(F_fundbalancesStatic.DefaultValues.Yes)))
                return new EventReturnObject(EventStatusCode.Success);

            var transferFromId = recordInsData.GetFieldValue<long>(F_depositsStatic.SystemNames.Transferfromaccount);

            #region update Fund balances
            if (string.IsNullOrEmpty(parentFundBalance.Balance))
                parentFundBalance.Balance = double.Parse(depositsRecord.Amount).ToString("F2");
            else
                parentFundBalance.Balance = (double.Parse(parentFundBalance.Balance) + double.Parse(depositsRecord.Amount)).ToString("F2");
    
            parentFundBalance.SaveRecord();

            if (transferFromId != 0)
            {
                var fromFundBalance = depositsRecord.Transferfromaccount();
                if (fromFundBalance != null)
                {
                    fromFundBalance.Balance = (double.Parse(fromFundBalance.Balance) - double.Parse(depositsRecord.Amount)).ToString("F2");
                    fromFundBalance.SaveRecord();
                }
            }
            #endregion

            #region call finance gateway
            // set up message
            ManageFundsMessage fundMessage = new ManageFundsMessage()
            {
                TransactionType = NMFinancialConstants.TransactionTypes.DepositFunds,
                RecordId = depositsRecord.RecordInstanceID.ToString(),
                ModifiedBy = triggeringUser.UserName
            };

            // determine if transfer
           
            if (transferFromId != 0)
            {
                fundMessage.TransactionType = NMFinancialConstants.TransactionTypes.TransferFunds;
                fundMessage.FromRecordId = transferFromId.ToString();
            }

            AMessage aMessage = new AMessage()
            {
                Action = NMFinancialConstants.ActionTypes.DepositFunds,
                Data = fundMessage
            };

            _ = FinanceServices.MakePostRestCall(aMessage, eventHelper);
            #endregion

            return new EventReturnObject(EventStatusCode.Success);
        }
    }
}
