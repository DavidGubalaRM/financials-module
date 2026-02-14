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
    /// Pre-Update Event on Service Rate Datalist.
    /// Prevent users from changing any values except the end date.
    /// </summary>
    public class ServiceRateValidation : AMCaseValidateCustomEvent
    {
        public override string PrefixName => "[NMImpact] Financials";
        public override string ExactName => "Service Rate Validations";

        protected override Dictionary<string, List<string>> SpecificFieldSystemNamesByListSystemName => new Dictionary<string, List<string>>()
        {

        };

        protected override List<EventTrigger> ValidEventTriggers => new List<EventTrigger>
        {
            EventTrigger.PreUpdate
        };

        protected override Dictionary<string, List<string>> NeededRelationships => new Dictionary<string, List<string>>()
        {

        };

        protected override List<string> RecordDatalistType => new List<string>()
        {

        };

        private List<string> _validationMsgs;

        protected string EndDateValidation = "Users cannot update any values other than End Date.";

        protected override EventReturnObject ProcessEventSpecificLogic(AEventHelper eventHelper, UserData triggeringUser, WorkFlowData workflow, RecordInstanceData recordInsData,
            RecordInstanceData preSaveRecordData, Dictionary<string, DataListData> datalistsBySystemName, Dictionary<string, Dictionary<string, FieldData>> fieldsBySystemNameByListName, string triggerType)
        {
            _validationMsgs = new List<string>();

            // Begin
            eventHelper.AddInfoLog($"{TechName} - Begin");
            if (!recordInsData.TryParseRecord(eventHelper, out F_servicerate serviceRateRecord) && !recordInsData.TryParseRecord(eventHelper, out Cttribalfmaprates ctTribalFMAPRateRecord))
            {
                eventHelper.AddDebugLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                eventHelper.AddErrorLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                return new EventReturnObject(EventStatusCode.Failure, new List<string> { GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity });
            }

            ValidateFieldValueChanges(eventHelper, recordInsData, preSaveRecordData, ref _validationMsgs);
            if (_validationMsgs.Any())
            {
                return new EventReturnObject(EventStatusCode.Failure, _validationMsgs);
            }

            return new EventReturnObject(EventStatusCode.Success);
        }

        private void ValidateFieldValueChanges(AEventHelper eventHelper, RecordInstanceData recordInsData, RecordInstanceData preSaveRecordData, ref List<string> _validationMsgs)
        {
            var currentDate = DateTime.Now.Date;
            var createdOnDate = recordInsData.CreatedOn.Date;
            if (createdOnDate == currentDate)
            {
                // If the record is being edited the same day it was created, skip validation
                return;
            }

            var fieldsToValidate = new List<(string SystemName, string DisplayName)>
            {
                (F_servicerateStatic.SystemNames.Forservice, "Service Field"),
                (F_servicerateStatic.SystemNames.Isthisapurchaseorder, "Is This a Purchase Order Field"),
                (F_servicerateStatic.SystemNames.Levelofcare, "Level of Care Field"),
                (F_servicerateStatic.SystemNames.Rateoccurrence, "Rate Occurrence Field"),
                (F_servicerateStatic.SystemNames.Purchaseorderamount, "Purchase Order Amount Field"),
                (F_servicerateStatic.SystemNames.Startdate, "Start Date Field")
            };

            foreach (var (systemName, displayName) in fieldsToValidate)
            {
                var currentValue = recordInsData.GetFieldValue(systemName);
                var previousValue = preSaveRecordData.GetFieldValue(systemName);

                if (!Equals(currentValue, previousValue))
                {
                    _validationMsgs.Add($"cannot update {displayName}");
                }
            }

            var currentEndDate = preSaveRecordData.GetFieldValue(F_servicerateStatic.SystemNames.Enddate);
            var previousEndDate = recordInsData.GetFieldValue(F_servicerateStatic.SystemNames.Enddate);
            if (currentEndDate.Equals(previousEndDate))
            {
                _validationMsgs.Add(EndDateValidation);
            }
        }
    }
}