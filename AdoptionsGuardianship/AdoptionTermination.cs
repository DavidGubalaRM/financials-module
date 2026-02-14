using MCase.Core.Event;
using MCase.Event.NMImpact.Constants;
using MCase.Event.NMImpact.Financials;
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
    /// Post Update Event on Approval of Adoption Assistance Agreement Termination Datalist
    /// if termination date is in the previous month, call gateway on approval.
    /// </summary>
    public class AdoptionTermination : AMCaseValidateCustomEvent
    {
        public override string PrefixName => "[NMImpact] Financials";
        public override string ExactName => "Adoption Assistance Agreement Termination";

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
            if (!recordInsData.TryParseRecord(eventHelper, out Adoptionassistancetermination adoptionAssistanceTermination))
            {
                eventHelper.AddDebugLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                eventHelper.AddErrorLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                return new EventReturnObject(EventStatusCode.Failure, new List<string> { GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity });
            }

            var currentDateTime = DateTime.Now;
            var approvalStatus = adoptionAssistanceTermination.Approvalstatus;
            if (approvalStatus != StaffingsStatic.DefaultValues.Approved)
                return new EventReturnObject(EventStatusCode.Success);

            var isTerminated = adoptionAssistanceTermination.Isterminated;
            if (isTerminated == AdoptionassistanceterminationStatic.DefaultValues.False)
            {
                var parentAdoptionAssistance = adoptionAssistanceTermination.GetParentAdoptionassistanceagreement();
                var parentCase = parentAdoptionAssistance?.GetParentCases();

                var childParticipant = parentAdoptionAssistance.Child();
                if (childParticipant == null)
                {
                    eventHelper.AddErrorLog("No Child associated with Adoption Assistance Agreement");
                    return new EventReturnObject(EventStatusCode.Success);
                }

                // get person
                var child = childParticipant?.Caseparticipantname();

                var serviceAuth = FindRelatedServiceAuth(eventHelper, child, parentCase.RecordInstanceID);
                if (serviceAuth == null)
                {
                    eventHelper.AddErrorLog("No related Service Auth found for Child");
                    return new EventReturnObject(EventStatusCode.Success);
                }
                // Pause Payment
                HandlePausePayment(eventHelper, triggeringUser, serviceAuth, adoptionAssistanceTermination.Aattermdate.Value);
                adoptionAssistanceTermination.Isterminated = AdoptionassistanceterminationStatic.DefaultValues.True;
                adoptionAssistanceTermination.SaveRecord();

                parentAdoptionAssistance.Isterminated = AdoptionassistanceterminationStatic.DefaultValues.True;
                parentAdoptionAssistance.SaveRecord();

            }

            return new EventReturnObject(EventStatusCode.Success);
        }
    }
}