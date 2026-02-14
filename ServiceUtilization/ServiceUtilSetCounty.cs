using MCase.Core.Event;
using MCase.Event.NMImpact.Constants;
using MCase.Event.NMImpact.Utils.DatalistUtils;
using MCaseCustomEvents.NMImpact.Generated.Entities;
using MCaseEventsSDK;
using MCaseEventsSDK.Util.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MCase.Event.NMImpact
{
    /// <summary>
    /// Post Insert Event on Service Utilization to set County (if blank)
    /// </summary>
    public class ServiceUtilSetCounty : AMCaseValidateCustomEvent
    {
        public override string PrefixName => "[NMImpact] Financials";
        public override string ExactName => "Service Utilization Set County";

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
            if (!recordInsData.TryParseRecord(eventHelper, out F_serviceplanutilization serviceUtilRecord))
            {
                eventHelper.AddErrorLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                return new EventReturnObject(EventStatusCode.Failure, new List<string> { GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity });
            }

            // Set county (if not populated).  This is needed when Service Utils are created from Function App for Special Payments
            if (serviceUtilRecord.County() == null)
            {
                Countylist county = null;
                var persons = serviceUtilRecord.Participant();

                if (persons.Count > 0)
                {
                    county = NMFinancialUtils.GetLatestRemovalCounty(eventHelper, persons.FirstOrDefault());
                }
                if (county != null)
                    serviceUtilRecord.County(county);
                else
                {
                    var caseRecord = serviceUtilRecord.Case();
                    var invRecord = serviceUtilRecord.Investigation();

                    // get county from parent Record
                    if (caseRecord != null)
                        serviceUtilRecord.County(caseRecord.Casecounty()); 

                    if (invRecord != null)
                        serviceUtilRecord.County(invRecord.Invcounty());
                }

                recordInsData.UserID = triggeringUser.UserID;
                eventHelper.SaveRecord(recordInsData);
            }

            return new EventReturnObject(EventStatusCode.Success);
        }

    }
}