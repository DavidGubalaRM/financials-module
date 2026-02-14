using MCase.Core.Event;
using MCase.Event.NMImpact.Constants;
using MCase.Event.NMImpact.Utils.DatalistUtils;
using MCaseCustomEvents.NMImpact.Generated.Entities;
using MCaseEventsSDK;
using MCaseEventsSDK.Util.Data;
using System.Collections.Generic;
using static MCase.Event.NMImpact.NMFinancialUtils;

namespace MCase.Event.NMImpact
{
    /// <summary>
    /// Post Update Event on Staffings Record. 
    /// When approved with staffing type = voluntary services support agreement pause payment staffing, end date service auth
    /// When approved with staffing type = voluntary services support agreement reinstate payment staffing, add new service auth with start date = approval date
    /// </summary>
    public class PauseReInstatePaymentFromStaffingApprovals : AMCaseValidateCustomEvent
    {
        public override string PrefixName => "[NMImpact] Financials";
        public override string ExactName => "Staffing Approval Pause or ReInstate Service Auth";

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
            if (!recordInsData.TryParseRecord(eventHelper, out Staffings staffingRecord))
            {
                eventHelper.AddDebugLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                eventHelper.AddErrorLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                return new EventReturnObject(EventStatusCode.Failure, new List<string> { GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity });
            }

            var approvalStatus = staffingRecord.Approvalstatus;
            if (approvalStatus != StaffingsStatic.DefaultValues.Approved)
                return new EventReturnObject(EventStatusCode.Success);

            var approvedDate = staffingRecord.Approvedon;
            // there are a lot of options for this field. 
            var staffingType = GetRecordStaffingTypeValue(staffingRecord);

            if (string.IsNullOrWhiteSpace(staffingType))
                return new EventReturnObject(EventStatusCode.Success);

            var persons = GetChildren(staffingRecord);
            var parentRecordID = staffingRecord.ParentRecordID.Value;

            foreach (var personRecord in persons)
            {
                var existingServiceAuth = FindRelatedServiceAuth(eventHelper, personRecord, parentRecordID);
                if (existingServiceAuth == null)
                    continue;

                if (staffingType == StaffingsStatic.DefaultValues.Voluntaryservicessupportagreementpausepaymentstaffing)
                {
                    HandlePausePayment(eventHelper, triggeringUser, existingServiceAuth, approvedDate.Value);
                }
                else if (staffingType == StaffingsStatic.DefaultValues.Voluntaryservicessupportagreementre_instatepaymentstaffing)
                {
                    var newServiceAuth = HandleReinstatePayment(eventHelper, existingServiceAuth, personRecord, approvedDate.Value);
                    newServiceAuth.SaveRecord();

                    HandleGatewayCallForReinstatement(eventHelper, triggeringUser, newServiceAuth, approvedDate.Value);

                }
            }

            return new EventReturnObject(EventStatusCode.Success);
        }

        private List<Persons> GetChildren(Staffings staffingRecord)
        {
            List<Persons> children = new List<Persons>();
            var invParticipants = staffingRecord.Childrenyouthinvestigationsddd();
            var caseParticipants = staffingRecord.Childrenyouthcasesddd();

            if (invParticipants.Count > 0)
            {
                foreach (var participant in invParticipants)
                {
                    var person = participant.Invparticipantname();
                    children.Add(person);
                }
            }
            else if (caseParticipants.Count > 0)
            {
                foreach (var participant in caseParticipants)
                {
                    var person = participant.Caseparticipantname();
                    children.Add(person);
                }
            }

            return children;
        }

        private string GetRecordStaffingTypeValue(Staffings staffingRecord)
        {
            var staffingType = string.Empty;
            var staffingTypeChildFreed = staffingRecord.Staffingtypechildfreed;
            var staffingTypeFosteringConnections = staffingRecord.Staffingtypeextendedfostercare;
            var staffingTypeFosterCare = staffingRecord.Staffingtypefostercare;

            string vssaPausePayment = StaffingsStatic.DefaultValues.Voluntaryservicessupportagreementpausepaymentstaffing;
            string vssaReInstatePayment = StaffingsStatic.DefaultValues.Voluntaryservicessupportagreementre_instatepaymentstaffing;

            // Check each list
            if (staffingTypeChildFreed.Contains(vssaPausePayment))
            {
                return vssaPausePayment;
            }
            else if (staffingTypeChildFreed.Contains(vssaReInstatePayment))
            {
                return vssaReInstatePayment;
            }
            else if (staffingTypeFosteringConnections.Contains(vssaPausePayment))
            {
                return vssaPausePayment;
            }
            else if (staffingTypeFosteringConnections.Contains(vssaReInstatePayment))
            {
                return vssaReInstatePayment;
            }
            else if (staffingTypeFosterCare.Contains(vssaPausePayment))
            {
                return vssaPausePayment;
            }
            else if (staffingTypeFosterCare.Contains(vssaReInstatePayment))
            {
                return vssaReInstatePayment;
            }

            return staffingType;
        }
    }
}