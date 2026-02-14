using MCase.Core.Event;
using MCase.Event.NMImpact.Constants;
using MCase.Event.NMImpact.Utils.DatalistUtils;
using MCaseCustomEvents.NMImpact.Generated.Entities;
using MCaseEventsSDK;
using MCaseEventsSDK.Util.Data;
using System.Collections.Generic;

namespace MCase.Event.NMImpact
{
    /// <summary>
    /// Post Insert Event to mirror values from service utilization record. 
    /// </summary>
    public class UnderpaymentPostInsert : AMCaseValidateCustomEvent
    {
        public override string PrefixName => "[NMImpact] Financials";

        public override string ExactName => "Underpayments Post Insert";

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

            if (!recordInsData.TryParseRecord(eventHelper, out Underpayments underpaymentRecord))
            {
                eventHelper.AddDebugLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                eventHelper.AddErrorLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                return new EventReturnObject(EventStatusCode.Failure, new List<string> { GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity });
            }

            var serviceUtilization = underpaymentRecord.GetParentF_serviceplanutilization();
            var serviceCatalogValue = serviceUtilization?.Servicecatalogtype();

            if (serviceCatalogValue != null)
            {
                underpaymentRecord.Originalfund(serviceCatalogValue);
                underpaymentRecord.SaveRecord();
            }

            eventHelper.AddInfoLog($"{TechName} - ProcessEvent completed successfully");

            return new EventReturnObject(EventStatusCode.Success);
        }
    }
}