using MCase.Core.Event;
using MCase.Event.NMImpact.Constants;
using MCase.Event.NMImpact.Utils.DatalistUtils;
using MCaseCustomEvents.NMImpact.Generated.Entities;
using MCaseEventsSDK;
using MCaseEventsSDK.Util.Data;
using System;
using System.Collections.Generic;
using static MCase.Event.NMImpact.NMFinancialUtils;

namespace MCase.Event.NMImpact
{
    /// <summary>
    /// Button Event on Placements when Placement is end dated
    /// End date Service Auth. 
    /// </summary>
    public class PlacementEndDated : AMCaseValidateCustomEvent
    {
        public override string PrefixName => "[NMImpact] Financials";
        public override string ExactName => "Placement End Dated";

        protected override Dictionary<string, List<string>> SpecificFieldSystemNamesByListSystemName => new Dictionary<string, List<string>>()
        {

        };
        protected override List<EventTrigger> ValidEventTriggers => new List<EventTrigger>
        {
            EventTrigger.Button
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
            if (!recordInsData.TryParseRecord(eventHelper, out Placements placementRecord))
            {
                eventHelper.AddErrorLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                return new EventReturnObject(EventStatusCode.Failure, new List<string> { GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity });
            }

            var status = placementRecord.O_status;
            if (!status.Equals(PlacementsStatic.DefaultValues.Active))
            {
                return new EventReturnObject(EventStatusCode.Success);
            }

            F_serviceline existingServiceAuthRecord = null; 

            // check if Service Auth exists for Placement (Investigation and Cases) end date Service Auth
            existingServiceAuthRecord = GetServiceAutorizationForPlacement(eventHelper, placementRecord);
            if (existingServiceAuthRecord != null)
            {
                HandlePausePayment(eventHelper, triggeringUser, existingServiceAuthRecord, (DateTime)placementRecord.Placementenddate);
            }

           
            return new EventReturnObject(EventStatusCode.Success);
        }

    }
}