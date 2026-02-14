using MCase.Core.Event;
using MCase.Event.NMImpact.Constants;
using MCase.Event.NMImpact.Utils.DatalistUtils;
using MCaseCustomEvents.NMImpact.Generated.Entities;
using MCaseEventsSDK;
using MCaseEventsSDK.Util;
using MCaseEventsSDK.Util.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MCase.Event.NMImpact
{
    /// <summary>
    /// Post-Insert and Pre-Update Event on CT FMAP and CT Tribal FMAP Datalists.
    /// Ensure that Rates do not overlap, check that startdate is the 1st of the month and end date is the last day of the month.
    /// </summary>
    public class RatesValidation : AMCaseValidateCustomEvent
    {
        public override string PrefixName => "[NMImpact] Financials";
        public override string ExactName => "Code Table Rate Validations";

        protected override Dictionary<string, List<string>> SpecificFieldSystemNamesByListSystemName => new Dictionary<string, List<string>>()
        {

        };

        protected override List<EventTrigger> ValidEventTriggers => new List<EventTrigger>
        {
            EventTrigger.PostCreate, EventTrigger.PreUpdate
        };

        protected override Dictionary<string, List<string>> NeededRelationships => new Dictionary<string, List<string>>()
        {

        };

        protected override List<string> RecordDatalistType => new List<string>()
        {

        };

        protected string StartDateValidation = "Start Date must be the first day of the month.";
        protected string EndDateValidation = "End date must be the last day of the month.";
        protected string OverlappingDatesValidation = "This FMAP rate overlaps with an existing rate. Rates must not overlap.";

        protected override EventReturnObject ProcessEventSpecificLogic(AEventHelper eventHelper, UserData triggeringUser, WorkFlowData workflow, RecordInstanceData recordInsData,
            RecordInstanceData preSaveRecordData, Dictionary<string, DataListData> datalistsBySystemName, Dictionary<string, Dictionary<string, FieldData>> fieldsBySystemNameByListName, string triggerType)
        {
            List<string> validationMessages = new List<string>();

            // Begin
            eventHelper.AddInfoLog($"{TechName} - Begin");

           var datalistName = eventHelper.DatalistSystemName(recordInsData.DataListID);
           var record = workflow.TriggerType.Equals(EventTrigger.PreUpdate.GetEnumDescription())
               ? preSaveRecordData
               : recordInsData;

            var (codeTableRecord, infoRecord) = GetCodeTableAndInfoRecord(eventHelper, record, datalistName);

            if (codeTableRecord == null)
            {
                eventHelper.AddErrorLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                return new EventReturnObject(EventStatusCode.Failure, new List<string> { GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity });
            }

            #region Validate Start and End Date
            var startDate = codeTableRecord.Startdate;
            var endDate = codeTableRecord.Enddate;

            if (startDate.Day != 1)
            {
                validationMessages.Add(StartDateValidation);
                return new EventReturnObject(EventStatusCode.Failure, validationMessages);
            }
            var lastDayOfMonth = DateTime.DaysInMonth(endDate.Year, endDate.Month);
            if (endDate.Day != lastDayOfMonth)
            {
                validationMessages.Add(EndDateValidation);
                return new EventReturnObject(EventStatusCode.Failure, validationMessages);
            }
            #endregion

            #region Validate No Overlapping Dates

            var startDateFilter = infoRecord.CreateFilter(CtfmapratesStatic.SystemNames.Startdate, null, endDate.ToString("yyyy-MM-dd"));
            var endDateFilter = infoRecord.CreateFilter(CtfmapratesStatic.SystemNames.Enddate, startDate.ToString("yyyy-MM-dd"), null);
            var overlappingRecords = infoRecord.CreateQuery(new List<DirectSQLFieldFilterData> { startDateFilter, endDateFilter });

            var overlappingRecordsFinal = new List<dynamic>();
            foreach (var overlappingRecord in overlappingRecords)
            {
                if (overlappingRecord.RecordInstanceID != record.RecordInstanceID)
                {
                    overlappingRecordsFinal.Add(record);
                }
            }

            if (overlappingRecordsFinal.Any())
            {
                validationMessages.Add(OverlappingDatesValidation);
                return new EventReturnObject(EventStatusCode.Failure, validationMessages);
            }
            #endregion

            return new EventReturnObject(EventStatusCode.Success);
        }

        private (dynamic codeTableRecord, dynamic infoRecord) GetCodeTableAndInfoRecord(AEventHelper eventHelper, RecordInstanceData recordInsData, string datalistName)
        {
            if (datalistName.Equals(CtfmapratesStatic.SystemName) && recordInsData.TryParseRecord(eventHelper, out Ctfmaprates ctFMAPRate))
            {
                return (ctFMAPRate, new CtfmapratesInfo(eventHelper));
            }

            if (datalistName.Equals(CttribalfmapratesStatic.SystemName) && recordInsData.TryParseRecord(eventHelper, out Cttribalfmaprates ctTribalFMAPRate))
            {
                return (ctTribalFMAPRate, new CttribalfmapratesInfo(eventHelper));
            }

            return (null, null);
        }

    }
}