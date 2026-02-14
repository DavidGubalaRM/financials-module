using MCase.Core.Event;
using MCase.Event.NMImpact.Constants;
using MCase.Event.NMImpact.Utils.DatalistUtils;
using MCaseCustomEvents.NMImpact.Generated.Entities;
using MCaseEventsSDK;
using MCaseEventsSDK.Util;
using MCaseEventsSDK.Util.Data;
using System.Collections.Generic;
using System.Linq;

namespace MCase.Event.NMImpact
{
    /// <summary>
    /// Post Insert / Pre Update event on Deposits to validate transfer amount
    /// </summary>
    public class DepositsValidations : AMCaseValidateCustomEvent
    {
        public override string PrefixName => "[NMImpact] Financials";
        public override string ExactName => "Deposits Validations";

        protected override Dictionary<string, List<string>> SpecificFieldSystemNamesByListSystemName => new Dictionary<string, List<string>>()
        {
        };

        protected override List<EventTrigger> ValidEventTriggers => new List<EventTrigger>
        {
            EventTrigger.PostCreate, EventTrigger.PreUpdate
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

            var record = workflow.TriggerType.Equals(EventTrigger.PreUpdate.GetEnumDescription())
               ? preSaveRecordData
               : recordInsData;

            if (!record.TryParseRecord(eventHelper, out F_deposits depositsRecord))
            {
                eventHelper.AddDebugLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                eventHelper.AddErrorLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                return new EventReturnObject(EventStatusCode.Failure, new List<string> { GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity });
            }

            List<string> errorMessages = new List<string>();

            // get parent Fund Balance and Fund
            var parentFundBalance = depositsRecord.GetParentF_fundbalances();
            var parentFund = parentFundBalance.Fund();

            // If  Fund is not capped and not child spesific, cannot add deposit
            if (!(parentFund.Childspecificfund.Equals(F_fundbalancesStatic.DefaultValues.Yes)
                || parentFund.Cappedfund.Equals(F_fundbalancesStatic.DefaultValues.Yes)))
            {
                errorMessages.Add("Deposits can only be made for Child Spesific Funds and Uncapped Funds");
                eventHelper.AddErrorLog(string.Join(", ", errorMessages));
            }

            // If Transfer from Account is equal to Parent Fund Balance Record, cannot transfer money from same account
            var transferFromAccount = record.GetFieldValue<long>(F_depositsStatic.SystemNames.Transferfromaccount);
            var transferFromAccountRecord = eventHelper.GetActiveRecordById(transferFromAccount);
            if (transferFromAccountRecord.RecordInstanceID == parentFundBalance.RecordInstanceID)
            {
                errorMessages.Add("Deposits cannot be made from the same Account");
                eventHelper.AddErrorLog(string.Join(", ", errorMessages));

            }

            // validate if we are transfering funds
            var newTransferFromID = record.GetFieldValue(F_depositsStatic.SystemNames.Transferfromaccount);
            if (!string.IsNullOrEmpty(newTransferFromID))
            {
                var depositAmount = double.Parse(depositsRecord.Amount);

                // get Fund balance for account amount is transfered from
                var newTransferFromRecord = eventHelper.GetActiveRecordById(long.Parse(newTransferFromID));
                var balance = newTransferFromRecord.GetFieldValue(F_fundbalancesStatic.SystemNames.Balance);

                if (string.IsNullOrEmpty(balance) || depositAmount > double.Parse(balance))
                {
                    errorMessages.Add($"Deposit amount is greater than the Transfer From Fund Balance of {balance}");
                    eventHelper.AddErrorLog(string.Join(", ", errorMessages));
                }

            }

            if (errorMessages.Any())
            {
                return new EventReturnObject(EventStatusCode.Failure, errorMessages);
            }

            return new EventReturnObject(EventStatusCode.Success);
        }
    }
}
