using MCase.Core.Event;
using MCase.Event.NMImpact.Constants;
using MCase.Event.NMImpact.Utils.Batch;
using MCase.Event.NMImpact.Utils.DatalistUtils;
using MCaseCustomEvents.NMImpact.Generated.Entities;
using MCaseEventsSDK;
using MCaseEventsSDK.Util;
using MCaseEventsSDK.Util.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using static MCase.Event.NMImpact.NMFinancialUtils;

namespace MCase.Event.NMImpact
{
    /// <summary>
    /// Post Insert/Post Update Event on Service Rate Agreement that will create a Batch Notification Trigger
    /// for 30 days prior to end date of the Service Rate Agreement.
    /// </summary>
    public class ServiceRateAgreementEndingNotification : AMCaseValidateCustomEvent
    {
        public override string PrefixName => "[NMImpact] Financials";
        public override string ExactName => "Service Rate Agreement Ends in 30 days Notification";

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

        protected override EventReturnObject ProcessEventSpecificLogic(AEventHelper eventHelper, UserData triggeringUser, WorkFlowData workflow, RecordInstanceData recordInsData,
            RecordInstanceData preSaveRecordData, Dictionary<string, DataListData> datalistsBySystemName, Dictionary<string, Dictionary<string, FieldData>> fieldsBySystemNameByListName, string triggerType)
        {
            // Begin
            eventHelper.AddInfoLog($"{TechName} - Begin");
            if (!recordInsData.TryParseRecord(eventHelper, out F_contract serviceRateAgreement))
            {
                eventHelper.AddDebugLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                eventHelper.AddErrorLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                return new EventReturnObject(EventStatusCode.Failure, new List<string> { GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity });
            }
            var status = serviceRateAgreement.O_status;
            if (status != F_contractStatic.DefaultValues.Approved)
            {
                return new EventReturnObject(EventStatusCode.Success);
            }

            var today = DateTime.Today;
            var serviceRateAgreementEndDate = serviceRateAgreement.Enddate;
            // End date is not mandatory, if it is blank, do nothing
            if (!serviceRateAgreementEndDate.HasValue)
            {
                return new EventReturnObject(EventStatusCode.Success);
            }

            var nextTriggerDate = serviceRateAgreementEndDate.Value.AddDays(-30);

            // Get notification trigger
            var notificationTriggerRecord = BatchUtils.GetNotificationTriggerRecordsOfTypeBySource(eventHelper, recordInsData,
                NotificationTriggerDatalist.FieldValues.NotificationType.ThirtyDaysServiceRateAgreementExipires,
                datalistsBySystemName, fieldsBySystemNameByListName).FirstOrDefault();

            /* 1. Send notification (now)
             * 2. Setup notification trigger (future)
             * 3. Delete notification trigger (past)
             */
            var shouldDeleteTrigger = false;
            if (today >= nextTriggerDate && today < serviceRateAgreementEndDate)
            {
                // Send notification
                SendServiceRateAgreementEndingNotification(eventHelper, triggeringUser, workflow, serviceRateAgreement);

                // Delete trigger
                shouldDeleteTrigger = true;
            }
            else if (today < nextTriggerDate)
            {
                // Setup notification trigger
                if (notificationTriggerRecord != null)
                {
                    // Update
                    notificationTriggerRecord.SetValue(NotificationTriggerDatalist.Fields.NextTriggerDate, nextTriggerDate.ToString(MCaseEventConstants.DateStorageFormat));
                    eventHelper.SaveRecord(notificationTriggerRecord);
                }
                else
                {
                    // Create
                    BatchUtils.CreateNotificationTrigger(eventHelper, nextTriggerDate.ToString(MCaseEventConstants.DateStorageFormat), today.ToString(MCaseEventConstants.DateStorageFormat),
                        recordInsData, F_contractStatic.SystemName, recordInsData, F_contractStatic.SystemName,
                        NotificationTriggerDatalist.FieldValues.NotificationType.ThirtyDaysServiceRateAgreementExipires);
                }
            }
            else
            {
                // Delete trigger
                shouldDeleteTrigger = true;
            }

            // Cleanup
            if (notificationTriggerRecord != null && shouldDeleteTrigger)
            {
                notificationTriggerRecord.Status = MCaseEventConstants.RecordStatusDeleted;
                eventHelper.SaveRecord(notificationTriggerRecord);
            }

            // End
            eventHelper.AddInfoLog($"{TechName} - End");
            return new EventReturnObject(EventStatusCode.Success);

        }
    }
}