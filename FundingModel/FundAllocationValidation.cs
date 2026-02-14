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
    /// Post-Insert and Pre-Update Event on Fund Allocation Datalist.
    /// Ensure that Rates do not overlap, check that startdate is the 1st of the month and end date is the last day of the month.
    /// </summary>
    public class FundAllocationValidation : AMCaseValidateCustomEvent
    {
        public override string PrefixName => "[NMImpact] Financials";
        public override string ExactName => "Fund Allocation Validations";

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
        protected string EndDateValidation = "End date must be the last day of month.";
        protected string OverlappingDatesValidation = "This Fund Allocation overlaps with an existing record. Records must not overlap.";

        protected override EventReturnObject ProcessEventSpecificLogic(AEventHelper eventHelper, UserData triggeringUser, WorkFlowData workflow, RecordInstanceData recordInsData,
            RecordInstanceData preSaveRecordData, Dictionary<string, DataListData> datalistsBySystemName, Dictionary<string, Dictionary<string, FieldData>> fieldsBySystemNameByListName, string triggerType)
        {
            List<string> validationMessages = new List<string>();

            var record = workflow.TriggerType.Equals(EventTrigger.PreUpdate.GetEnumDescription())
               ? preSaveRecordData
               : recordInsData;

            // Begin
            eventHelper.AddInfoLog($"{TechName} - Begin");
            if (!record.TryParseRecord(eventHelper, out F_fundallocation fundAllocationRecord))
            {
                eventHelper.AddErrorLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                return new EventReturnObject(EventStatusCode.Failure, new List<string> { GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity });
            }

            var fundAllocationInfo = new F_fundallocationInfo(eventHelper);

            #region Validate Start and End Date
            var startDate = fundAllocationRecord.Startdate;
            var endDate = fundAllocationRecord.Enddate;

            if (startDate.HasValue && startDate.Value.Day != 1)
            {
                validationMessages.Add(StartDateValidation);
                return new EventReturnObject(EventStatusCode.Failure, validationMessages);
            }

            if (endDate.HasValue)
            {
                var lastDayOfMonth = DateTime.DaysInMonth(endDate.Value.Year, endDate.Value.Month);
                if (endDate.Value.Day != lastDayOfMonth)
                {
                    validationMessages.Add(EndDateValidation);
                    return new EventReturnObject(EventStatusCode.Failure, validationMessages);
                }
            }
            #endregion

            #region Validate No Overlapping Dates
            IEnumerable<F_fundallocation> overlappingRecords = new List<F_fundallocation>();
            var endDateFilter = fundAllocationInfo.CreateFilter(F_fundallocationStatic.SystemNames.Enddate, startDate.Value.ToString("yyyy-MM-dd"), null);
            if (endDate.HasValue)
            {
                var startDateFilter = fundAllocationInfo.CreateFilter(F_fundallocationStatic.SystemNames.Startdate, null, endDate.Value.ToString("yyyy-MM-dd"));
                overlappingRecords = fundAllocationInfo.CreateQuery(new List<DirectSQLFieldFilterData> { startDateFilter, endDateFilter });
            } else
            {
                overlappingRecords = fundAllocationInfo.CreateQuery(new List<DirectSQLFieldFilterData> { endDateFilter });
            }

            bool isOverlapping = false;
            foreach (var overlappingRecord in overlappingRecords)
            {
                if (overlappingRecord.RecordInstanceID != record.RecordInstanceID)
                {
                    isOverlapping = true;
                    break;
                }
            }

            if (isOverlapping)
            {
                validationMessages.Add(OverlappingDatesValidation);
                return new EventReturnObject(EventStatusCode.Failure, validationMessages);
            }
            #endregion

            return new EventReturnObject(EventStatusCode.Success);
        }

    }
}