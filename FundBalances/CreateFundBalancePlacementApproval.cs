using MCase.Core.Event;
using MCase.Event.NMImpact.Constants;
using MCase.Event.NMImpact.Utils.DatalistUtils;
using MCaseCustomEvents.NMImpact.Generated.Entities;
using MCaseEventsSDK;
using MCaseEventsSDK.Util.Data;
using System.Collections.Generic;
using System.Linq;
using static MCase.Event.NMImpact.NMFinancialUtils;

namespace MCase.Event.NMImpact
{
    /// <summary>
    /// Trigger: Post Update
    /// When Placement is Approved, create 3 fund balance records for the following funds if they don't already exist:
    /// Child’s Personal Account – Dedicated SSI Account, Child’s Personal Account – RSDI, Child’s Personal Account – SSI 
    /// </summary>
    public class CreateFundBalancePlacementApproval : AMCaseValidateCustomEvent
    {
        public override string PrefixName => "[NMImpact] Financials";

        public override string ExactName => "Create Fund Balances on Placement Approval";

        protected override Dictionary<string, List<string>> SpecificFieldSystemNamesByListSystemName => new Dictionary<string, List<string>>()
        {
        };

        protected override List<EventTrigger> ValidEventTriggers => new List<EventTrigger>()
        {
            EventTrigger.PostUpdate, EventTrigger.Button // for Testing
        };

        protected override Dictionary<string, List<string>> NeededRelationships => new Dictionary<string, List<string>>
        {
        };
        protected override List<string> RecordDatalistType => new List<string>()
        {
        };

        private readonly List<string> accountTypes = new List<string>
        {
            F_fundStatic.DefaultValues.Child_spersonalaccount_dedicatedssiaccount,
            F_fundStatic.DefaultValues.Child_spersonalaccount_rsdi,
            F_fundStatic.DefaultValues.Child_spersonalaccount_ssi
        };

        protected override EventReturnObject ProcessEventSpecificLogic(AEventHelper eventHelper, UserData triggeringUser, WorkFlowData workflow, RecordInstanceData recordInsData, RecordInstanceData preSaveRecordData, Dictionary<string, DataListData> datalistsBySystemName, Dictionary<string, Dictionary<string, FieldData>> fieldsBySystemNameByListName, string triggerType)
        {
            eventHelper.AddInfoLog($"{TechName} - Beginning ProcessEvent");

            if (!recordInsData.TryParseRecord(eventHelper, out Placements placementRecord))
            {
                eventHelper.AddDebugLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                eventHelper.AddErrorLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                return new EventReturnObject(EventStatusCode.Failure, new List<string> { GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity });
            }

            var childName = GetChildFromPlacement(placementRecord);
            if (childName == null)
            {
                eventHelper.AddWarningLog("Child Not Found");
                return new EventReturnObject(EventStatusCode.Success);
            }

            var fundBalanceInfo = new F_fundbalancesInfo(eventHelper);
            var fundInfo = new F_fundInfo(eventHelper);

            foreach (var accountType in accountTypes)
            {
                var fundRecord = GetFundRecord(eventHelper, fundInfo, accountType);
                if (fundRecord == null)
                {
                    eventHelper.AddWarningLog($"Fund record for '{accountType}' not found.");
                    continue;
                }

                var childFundBalance = CheckForFundBalanceRecord(eventHelper, childName, fundRecord);
                if (childFundBalance == null)
                {
                    CreateNewFundBalanceRecord(eventHelper, fundBalanceInfo, childName, fundRecord);
                }
            }

            return new EventReturnObject(EventStatusCode.Success);
        }

        private static F_fund GetFundRecord(AEventHelper eventHelper, F_fundInfo fundInfo, string fundName)
        {
            var fundFilter = fundInfo.CreateFilter(F_fundStatic.SystemNames.Fundname, new List<string> { fundName });
            var fundRecord = fundInfo.CreateQuery(new List<DirectSQLFieldFilterData> { fundFilter }).FirstOrDefault();

            if (fundRecord == null) { return null; }

            return fundRecord;
        }

        private static F_fundbalances CheckForFundBalanceRecord(AEventHelper eventHelper, Persons childName, F_fund fundRecord)
        {
            var fundBalanceRecord = GetFundBalanceRecords(eventHelper, childName, fundRecord, true).FirstOrDefault();

            if (fundBalanceRecord == null) { return null; }

            return fundBalanceRecord;
        }

        private void CreateNewFundBalanceRecord(AEventHelper eventHelper, F_fundbalancesInfo fundBalanceInfo, Persons childName, F_fund fundRecord)
        {
            var newFundBalanceRecord = fundBalanceInfo.NewF_fundbalances();
            newFundBalanceRecord.Fund(fundRecord);
            newFundBalanceRecord.Childspecificfund = F_fundbalancesStatic.DefaultValues.True;
            newFundBalanceRecord.Child(childName);
            newFundBalanceRecord.Balance = 0.ToString();
            newFundBalanceRecord.SaveRecord();
        }
    }
}