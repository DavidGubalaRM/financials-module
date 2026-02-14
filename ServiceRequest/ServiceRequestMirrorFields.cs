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
    /// Post Insert on Service Request to get all medications and medical appointments
    /// for all the selected participants. Including name of the child.
    /// </summary>
    public class ServiceRequestMirrorFields : AMCaseValidateCustomEvent
    {
        public override string PrefixName => "[NMImpact] Financials";
        public override string ExactName => "Service Request Post Insert Mirroring";

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

        private string _medications;
        private string _medicalAppointments;

        protected override EventReturnObject ProcessEventSpecificLogic(AEventHelper eventHelper, UserData triggeringUser, WorkFlowData workflow, RecordInstanceData recordInsData,
            RecordInstanceData preSaveRecordData, Dictionary<string, DataListData> datalistsBySystemName, Dictionary<string, Dictionary<string, FieldData>> fieldsBySystemNameByListName, string triggerType)
        {
            // Begin
            eventHelper.AddInfoLog($"{TechName} - Begin");
            if (!recordInsData.TryParseRecord(eventHelper, out Servicerequest serviceRequestRecord))
            {
                eventHelper.AddDebugLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                eventHelper.AddErrorLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                return new EventReturnObject(EventStatusCode.Failure, new List<string> { GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity });
            }

            _medications = string.Empty;
            _medicalAppointments = string.Empty;

            var parentRecordCase = serviceRequestRecord.GetParentCases();
            var parentRecordInv = serviceRequestRecord.GetParentInvestigations();

            if (parentRecordCase != null)
            {
                var caseParticipants = serviceRequestRecord.Mfdpersidname();
                foreach (var participant in caseParticipants)
                {
                    // get person record
                    var personRecord = participant.Caseparticipantname();
                    GetFieldsToMirror(personRecord, serviceRequestRecord, ref _medications, ref _medicalAppointments);
                }
            }

            else if (parentRecordInv != null)
            {
                var investigationParticipants = serviceRequestRecord.Investigationsparticipants();
                foreach (var participant in investigationParticipants)
                {
                    // get person record
                    var personRecord = participant.Invparticipantname();
                    GetFieldsToMirror(personRecord, serviceRequestRecord, ref _medications, ref _medicalAppointments);
                }
            }

            return new EventReturnObject(EventStatusCode.Success);
        }

        private void GetFieldsToMirror(Persons personRecord, Servicerequest serviceRequestRecord, ref string medications, ref string medicalAppointments)
        {
            var newLine = "<br />";
            bool hasMedications = !string.IsNullOrEmpty(medications);
            bool hasAppointments = !string.IsNullOrEmpty(medicalAppointments);

            if (hasMedications)
                medications += newLine;

            medications += $"<strong>{personRecord.Fnamelname}:</strong>{newLine}";
            var medicationSnapshotRecords = personRecord.GetChildrenMedicationssnapshot();
            foreach (var medRecord in medicationSnapshotRecords)
            {
                medications += $"{medRecord.Nameofprescribedmedication}, {medRecord.Dosage}, {medRecord.Reasonformedication}{newLine}";
            }

            if (hasAppointments)
                medicalAppointments += newLine;

            medicalAppointments += $"<strong>{personRecord.Fnamelname}:</strong>{newLine}";
            var medicalAppointmentRecords = personRecord.GetChildrenMedicalappointments()
                .Where(r => r.Datetimeofvisit > DateTime.Today);

            foreach (var appRecord in medicalAppointmentRecords)
            {
                medicalAppointments += $"{appRecord.Datetimeofvisit}, {appRecord.Appointmenttype}, {appRecord.Describevisittype}{newLine}";
            }

            serviceRequestRecord.Mfdmedications = medications;
            serviceRequestRecord.Mfdmedappts = medicalAppointments;

            serviceRequestRecord.SaveRecord();
        }

    }
}