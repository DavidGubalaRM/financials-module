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
    /// Post Update Event on Approval Adoption Assistance Agreement Suspension Datalist
    /// On Suspension, end date the service auth - if suspension date is in the previous month, call gateway on approval. 
    /// On ReInstatement, create a new service auth - if reinstatement date is in the previous month, call gateway on approval
    /// /// </summary>
    public class AdoptionSuspensionReInstatement : AMCaseValidateCustomEvent
    {
        public override string PrefixName => "[NMImpact] Financials";
        public override string ExactName => "Adoption Assistance Agreement Suspension and ReInstatement";

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
            if (!recordInsData.TryParseRecord(eventHelper, out Adoptionassistanceagreementsuspension adoptionAssistanceSuspension))
            {
                eventHelper.AddDebugLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                eventHelper.AddErrorLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                return new EventReturnObject(EventStatusCode.Failure, new List<string> { GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity });
            }

            var approvalStatus = adoptionAssistanceSuspension.Approvalstatus;

            var parentAdoptionAssistance = adoptionAssistanceSuspension.GetParentAdoptionassistanceagreement();
            var parentCase = parentAdoptionAssistance?.GetParentCases();

            var isSuspended = adoptionAssistanceSuspension.Issuspended;
            var isReInstated = adoptionAssistanceSuspension.Isreinstated;
            var reInstatementDate = adoptionAssistanceSuspension.Aasreinstatedate;

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

            // On Suspension, end date the service auth
            if (isSuspended == AdoptionassistanceagreementsuspensionStatic.DefaultValues.False
                && approvalStatus == AdoptionassistanceagreementsuspensionStatic.DefaultValues.Suspensionapproved)
            {
                var adoptionSuspensionDate = adoptionAssistanceSuspension.Aassuspenddate.Value;
                HandlePausePayment(eventHelper, triggeringUser, serviceAuth, adoptionSuspensionDate);
                adoptionAssistanceSuspension.Issuspended = AdoptionassistanceagreementsuspensionStatic.DefaultValues.True;
                adoptionAssistanceSuspension.Isreinstated = AdoptionassistanceagreementsuspensionStatic.DefaultValues.False;
                adoptionAssistanceSuspension.SaveRecord();

            }

            // On ReInstatement, create a new service auth
            if (isReInstated == AdoptionassistanceagreementsuspensionStatic.DefaultValues.False && reInstatementDate != null
                && approvalStatus == AdoptionassistanceagreementsuspensionStatic.DefaultValues.Reinstatementapproved)
            {
                var adoptionReInstatementDate = reInstatementDate.Value;
                var newServiceAuth = HandleReinstatePayment(eventHelper, serviceAuth, child, adoptionReInstatementDate);
                newServiceAuth.SaveRecord();

                adoptionAssistanceSuspension.Isreinstated = AdoptionassistanceagreementsuspensionStatic.DefaultValues.True;
                adoptionAssistanceSuspension.Issuspended = AdoptionassistanceagreementsuspensionStatic.DefaultValues.False;
                adoptionAssistanceSuspension.SaveRecord();

                // If reinstatement date is in the previous month, call gateway on approval.
                HandleGatewayCallForReinstatement(eventHelper, triggeringUser, newServiceAuth, adoptionReInstatementDate);
            }

            return new EventReturnObject(EventStatusCode.Success);
        }
    }
}