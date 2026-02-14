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
    /// Post Update Event on Approval of Guardianship Assistance Agreement Suspension Datalist
    /// On Suspension, end date the service auth - if suspenstion date is prior to the previous month, call gateway  on approval
    /// On ReInstatement, create a new service auth - if reinstatement date is prior to the previous month, call gateway on approval
    /// </summary>
    public class GuardianshipSuspensionReInstatement : AMCaseValidateCustomEvent
    {
        public override string PrefixName => "[NMImpact] Financials";
        public override string ExactName => "Guardianship Suspension and ReInstatement";

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
            if (!recordInsData.TryParseRecord(eventHelper, out Guardianshipassistanceagreementsuspension guardianshipAssistanceSuspension))
            {
                eventHelper.AddDebugLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                eventHelper.AddErrorLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                return new EventReturnObject(EventStatusCode.Failure, new List<string> { GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity });
            }

            var approvalStatus = guardianshipAssistanceSuspension.Approvalstatus;

            var parentGuardianshipAssistance = guardianshipAssistanceSuspension.GetParentGuardianshipassistanceagreement();
            var parentCase = parentGuardianshipAssistance?.GetParentCases();

            var isSuspended = guardianshipAssistanceSuspension.Issuspended;
            var isReInstated = guardianshipAssistanceSuspension.Isreinstated;
            var reInstatementDate = guardianshipAssistanceSuspension.Aasaaareinstatedate;

            var childParticipant = parentGuardianshipAssistance.Gaachildddd();
            if (childParticipant == null)
            {
                eventHelper.AddErrorLog("No Child associated with Guardianship Assistance Agreement");
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
            if (isSuspended == GuardianshipassistanceagreementsuspensionStatic.DefaultValues.False
                && approvalStatus == GuardianshipassistanceagreementsuspensionStatic.DefaultValues.Suspensionapproved)
            {
                var guardianshipSuspensionDate = guardianshipAssistanceSuspension.Gaassuspenddate.Value;
                HandlePausePayment(eventHelper, triggeringUser, serviceAuth, guardianshipSuspensionDate);
                guardianshipAssistanceSuspension.Issuspended = GuardianshipassistanceagreementsuspensionStatic.DefaultValues.True;
                guardianshipAssistanceSuspension.Isreinstated = GuardianshipassistanceagreementsuspensionStatic.DefaultValues.False;
                guardianshipAssistanceSuspension.SaveRecord();

            }

            // On ReInstatement, create a new service auth
            else if (isReInstated == GuardianshipassistanceagreementsuspensionStatic.DefaultValues.False && reInstatementDate != null
                && approvalStatus == GuardianshipassistanceagreementsuspensionStatic.DefaultValues.Reinstatementapproved)
            {
                var guardianshipReInstatementDate = reInstatementDate.Value;
                var newServiceAuth = HandleReinstatePayment(eventHelper, serviceAuth, child, guardianshipReInstatementDate);
                newServiceAuth.SaveRecord();

                guardianshipAssistanceSuspension.Isreinstated = GuardianshipassistanceagreementsuspensionStatic.DefaultValues.True;
                guardianshipAssistanceSuspension.Issuspended = GuardianshipassistanceagreementsuspensionStatic.DefaultValues.False;
                guardianshipAssistanceSuspension.SaveRecord();

                // If reinstatement date is in the previous month, call gateway on approval.
                HandleGatewayCallForReinstatement(eventHelper, triggeringUser, newServiceAuth, guardianshipReInstatementDate);
            }

            return new EventReturnObject(EventStatusCode.Success);
        }
    }
}