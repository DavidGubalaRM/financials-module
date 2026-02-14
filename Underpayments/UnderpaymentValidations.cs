using MCase.Core.Event;
using MCase.Event.NMImpact.Constants;
using MCase.Event.NMImpact.Utils.DatalistUtils;
using MCaseCustomEvents.NMImpact.Generated.Entities;
using MCaseEventsSDK;
using MCaseEventsSDK.Util;
using MCaseEventsSDK.Util.Data;
using System.Collections.Generic;

namespace MCase.Event.NMImpact
{
    /// <summary>
    /// Post Insert/PreUpdate Event to validate that corrected amount is less than the original amount.
    /// </summary>
    public class UnderpaymentValidations : AMCaseValidateCustomEvent
    {
        public override string PrefixName => "[NMImpact] Financials";

        public override string ExactName => "Underpayment Validations";

        protected override Dictionary<string, List<string>> SpecificFieldSystemNamesByListSystemName => new Dictionary<string, List<string>>()
        {
        };

        protected override List<EventTrigger> ValidEventTriggers => new List<EventTrigger>()
        {
            EventTrigger.PostCreate, EventTrigger.PreUpdate
        };

        protected override Dictionary<string, List<string>> NeededRelationships => new Dictionary<string, List<string>>
        {
        };
        protected override List<string> RecordDatalistType => new List<string>()
        {
        };

        private string CorrectedAmountValidation = "Corrected Amount must be greater than the Original Amount.";

        protected override EventReturnObject ProcessEventSpecificLogic(AEventHelper eventHelper, UserData triggeringUser, WorkFlowData workflow, RecordInstanceData recordInsData, RecordInstanceData preSaveRecordData, Dictionary<string, DataListData> datalistsBySystemName, Dictionary<string, Dictionary<string, FieldData>> fieldsBySystemNameByListName, string triggerType)
        {
            eventHelper.AddInfoLog($"{TechName} - Beginning ProcessEvent");

            if (!recordInsData.TryParseRecord(eventHelper, out Underpayments underpaymentRecord))
            {
                eventHelper.AddDebugLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                eventHelper.AddErrorLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                return new EventReturnObject(EventStatusCode.Failure, new List<string> { GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity });
            }

            var underpayment = workflow.TriggerType.Equals(EventTrigger.PreUpdate.GetEnumDescription())
               ? preSaveRecordData
               : recordInsData;

            double.TryParse(underpayment.GetFieldValue(UnderpaymentsStatic.SystemNames.Originalamount), out double originalAmount);
            double.TryParse(underpayment.GetFieldValue(UnderpaymentsStatic.SystemNames.Correctedamount), out double correctedAmount);

            if (correctedAmount < originalAmount)
            {
                return new EventReturnObject(EventStatusCode.Failure, new List<string> { CorrectedAmountValidation });
            }

            eventHelper.AddInfoLog($"{TechName} - ProcessEvent completed successfully");

            return new EventReturnObject(EventStatusCode.Success);
        }
    }
}