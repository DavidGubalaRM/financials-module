using MCase.Core.Event;
using MCase.Event.NMImpact.Constants;
using MCase.Event.NMImpact.Utils.DatalistUtils;
using MCaseCustomEvents.NMImpact.Generated.Entities;
using MCaseEventsSDK;
using MCaseEventsSDK.Util.Data;
using System.Collections.Generic;
using System.Linq;

namespace MCase.Event.NMImpact
{
    /// <summary>
    /// Post-Insert and Post-Update event on the Service Rate DL.
    /// validate that starting age cannot be greater than ending age
    /// validate that there are no overlapping age ranges
    /// </summary>
    public class AgeBasedRatesValidations : AMCaseValidateCustomEvent
    {
        public override string PrefixName => "[NMImpact] Financials";
        public override string ExactName => "Service Rate Age-Based Rates Validations";

        protected override Dictionary<string, List<string>> SpecificFieldSystemNamesByListSystemName => new Dictionary<string, List<string>>()
        {

        };

        protected override List<EventTrigger> ValidEventTriggers => new List<EventTrigger>
        {
            EventTrigger.PostCreate, EventTrigger.PostUpdate
        };

        protected override Dictionary<string, List<string>> NeededRelationships => new Dictionary<string, List<string>>()
        {

        };

        protected override List<string> RecordDatalistType => new List<string>()
        {

        };

        protected string EndAgeCannotBeGreaterThanStartAge = "End age cannot be greater than start age.";
        protected string OverlappingAgeRange = "There is at least one overlapping age range. Please adjust accordingly";

        protected override EventReturnObject ProcessEventSpecificLogic(AEventHelper eventHelper, UserData triggeringUser, WorkFlowData workflow, RecordInstanceData recordInsData,
            RecordInstanceData preSaveRecordData, Dictionary<string, DataListData> datalistsBySystemName, Dictionary<string, Dictionary<string, FieldData>> fieldsBySystemNameByListName, string triggerType)
        {
            List<string> validationMessages = new List<string>();

            // Begin
            eventHelper.AddInfoLog($"{TechName} - Begin");
            if (!recordInsData.TryParseRecord(eventHelper, out F_servicerate serviceRate))
            {
                eventHelper.AddDebugLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                eventHelper.AddErrorLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                return new EventReturnObject(EventStatusCode.Failure, new List<string> { GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity });
            }

            // Get Age Based Rates Records
            var ageBasedRates = serviceRate.Rates()
                .Where(r => r.Ratebasedon == F_serviceratesagebasedStatic.DefaultValues.Age)
                .OrderBy(r => r.Startingage)
                .ToList();

            // Check for overlapping age ranges
            bool hasOverlaps = HasOverlappingAgeRanges(ageBasedRates);
            if (hasOverlaps)
            {
                validationMessages.Add(OverlappingAgeRange);
                return new EventReturnObject(EventStatusCode.Failure, validationMessages);
            }

            foreach (var ageBasedRate in ageBasedRates)
            {
                int startAge = int.TryParse(ageBasedRate.Startingage, out var startingAge) ? startingAge : 0;
                int endAge = int.TryParse(ageBasedRate.Endingage, out var endingAge) ? endingAge : 0;
                if (startAge >= endAge)
                {
                    validationMessages.Add(EndAgeCannotBeGreaterThanStartAge);
                    return new EventReturnObject(EventStatusCode.Failure, validationMessages);
                }
            }
            return new EventReturnObject(EventStatusCode.Success);
        }

        public static bool HasOverlappingAgeRanges(List<F_serviceratesagebased> ageRanges)
        {
            var sortedRanges = ageRanges
                .OrderBy(r => int.TryParse(r.Startingage, out var start) ? start : 0)
                .ToList();

            for (int i = 1; i < sortedRanges.Count; i++)
            {
                var previous = sortedRanges[i - 1];
                var current = sortedRanges[i];

                var previousEnd = int.TryParse(previous.Endingage, out var endAge) ? endAge : 0;
                var currentStart = int.TryParse(current.Startingage, out var startAge) ? startAge : 0;

                if (currentStart <= previousEnd)
                {
                    return true;
                }
            }

            return false;
        }

    }
}