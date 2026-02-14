using MCase.Core.Event;
using MCase.Event.NMImpact.Constants;
using MCase.Event.NMImpact.Utils.DatalistUtils;
using MCaseCustomEvents.NMImpact.Generated.Entities;
using MCaseEventsSDK;
using MCaseEventsSDK.Util.Data;
using System.Collections.Generic;

namespace MCase.Event.NMImpact
{
    /// <summary>
    /// Post Update Event on Rate Override Approval that checks if Placement needs an Approved
    /// Rate Override Record. If so, on approval of rate override record, sets show approval button to true.
    /// </summary>
    public class RateOverridePostUpdate : AMCaseValidateCustomEvent
    {
        public override string PrefixName => "[NMImpact] Financials";
        public override string ExactName => "Rate Override Approval";

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
            if (!recordInsData.TryParseRecord(eventHelper, out Rateoverride rateOverride))
            {
                eventHelper.AddDebugLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                eventHelper.AddErrorLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                return new EventReturnObject(EventStatusCode.Failure, new List<string> { GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity });
            }

            var approvalStatus = rateOverride.Rateoverridestatus;
            if (approvalStatus != RateoverrideStatic.DefaultValues.Approved)
            {
                return new EventReturnObject(EventStatusCode.Failure);
            }

            var placementRecord = rateOverride.GetParentPlacements();
            if (placementRecord == null)
            {
                return new EventReturnObject(EventStatusCode.Failure);
            }

            var rateOverrideNeeded = placementRecord.Rateoverrideneeded.ToBoolean();
            var showApprovalButton = placementRecord.Showsubmitforapprovalbutton.ToBoolean();
            if (rateOverrideNeeded == true && showApprovalButton == false)
            {
                placementRecord.Showsubmitforapprovalbutton = PlacementsStatic.DefaultValues.True;
                placementRecord.SaveRecord();
            }


            return new EventReturnObject(EventStatusCode.Success);
        }

    }
}