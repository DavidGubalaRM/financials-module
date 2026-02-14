using MCase.Core.Event;
using MCase.Event.NMImpact.Utils.DatalistUtils;
using MCaseCustomEvents.NMImpact.Generated.Entities;
using MCaseEventsSDK;
using MCaseEventsSDK.Util.Data;
using System;
using System.Collections.Generic;

namespace MCase.Event.NMImpact
{
    /// <summary>
    /// Trigger: Post Insert 
    /// 
    /// Sends 1st and 2nd Reminder notificaions if Service Request is not approved within 48 hours
    /// </summary>
    public class ServiceRequestReminderNotifications : AMCaseValidateCustomEvent
    {
        public override string PrefixName => "[NMImpact] Financials";

        public override string ExactName => "Service Request Reminder Notifications";

        protected override Dictionary<string, List<string>> SpecificFieldSystemNamesByListSystemName => new Dictionary<string, List<string>>();

        protected override Dictionary<string, List<string>> NeededRelationships => new Dictionary<string, List<string>>();

        protected override List<string> RecordDatalistType => new List<string>();

        protected override List<EventTrigger> ValidEventTriggers => new List<EventTrigger>() { EventTrigger.PostCreate };

        private Interfacenotifications _interfaceNotifRecord;
        private Servicerequest _serviceRequestRecord;

        /// <summary>
        /// Handles Approval Flow:
        /// </summary>
        /// <param name="eventHelper"></param>
        /// <param name="triggeringUser"></param>
        /// <param name="workflow"></param>
        /// <param name="recordInsData"></param>
        /// <param name="preSaveRecordData"></param>
        /// <param name="datalistsBySystemName"></param>
        /// <param name="fieldsBySystemNameByListName"></param>
        /// <param name="triggerType"></param>
        /// <returns></returns>
        protected override EventReturnObject ProcessEventSpecificLogic(AEventHelper eventHelper, UserData triggeringUser, WorkFlowData workflow,
            RecordInstanceData recordInsData, RecordInstanceData preSaveRecordData, Dictionary<string, DataListData> datalistsBySystemName,
            Dictionary<string, Dictionary<string, FieldData>> fieldsBySystemNameByListName, string triggerType)
        {
            // check if the record is a Interface Notification record
            if (!recordInsData.TryParseRecord(eventHelper, out _interfaceNotifRecord))
            {
                return new EventReturnObject(EventStatusCode.Failure);
            }

            var reminderType = _interfaceNotifRecord.Interfacenotificationtype;

            // return if reminder type is not valid value
            if (!(reminderType.Equals(InterfacenotificationsStatic.DefaultValues.First_reminder)
                || reminderType.Equals(InterfacenotificationsStatic.DefaultValues.Second_reminder)))
                return new EventReturnObject(EventStatusCode.Success);

            // get Service Request Record
            var recordId = _interfaceNotifRecord.Targetrecordid;
            if (string.IsNullOrEmpty(recordId))
                return new EventReturnObject(EventStatusCode.Success);

            var serviceRecord = eventHelper.GetActiveRecordById(long.Parse(recordId));

            if (serviceRecord == null)
                return new EventReturnObject(EventStatusCode.Success);

            if (serviceRecord.TryParseRecord(eventHelper, out _serviceRequestRecord))
            {
                // send reminders
                var serviceType = _serviceRequestRecord.Servicetypememo().Nameofservice;
                var caseInvName = _serviceRequestRecord.Caseinvestigationnamecoalesce;

                var level1User = _serviceRequestRecord.Level1approveruser();
                var submittedByUser = _serviceRequestRecord.Mfdsubby();

                WorkflowNotificationData userNotification = new WorkflowNotificationData
                {
                    WorkflowID = workflow.WorkflowID,
                    TargetType = WorkflowNotificationData.TargetTypeSpecificUser
                };

                if (reminderType.Equals(InterfacenotificationsStatic.DefaultValues.First_reminder))
                {
                    userNotification.Subject = $"Reminder – Approval Still Pending for {serviceType} {caseInvName}";
                    userNotification.Message = $"This is a reminder that the Service Request for {serviceType}, submitted yesterday, still requires approval. Please review and take action as soon as possible.";

                    SendNotification(submittedByUser, userNotification, eventHelper, triggeringUser, serviceRecord);
                    SendNotification(level1User, userNotification, eventHelper, triggeringUser, serviceRecord);
                    _interfaceNotifRecord.Notificationdate = DateTime.UtcNow;
                }

                if (reminderType.Equals(InterfacenotificationsStatic.DefaultValues.Second_reminder))
                {
                    userNotification.Subject = $"Final Notice – Approval Due Today for {serviceType} {caseInvName}";
                    userNotification.Message = $"The Service Request for {serviceType} must be approved today. Please ensure the record is reviewed and finalized by the end of the business day.";

                    var level2User = _serviceRequestRecord.Level2approveruser();
                    var level3User = _serviceRequestRecord.Level3approveruser();
                    var finalLevelUser = _serviceRequestRecord.Finallevelapproveruser();

                    SendNotification(submittedByUser, userNotification, eventHelper, triggeringUser, serviceRecord);
                    SendNotification(level1User, userNotification, eventHelper, triggeringUser, serviceRecord);
                    SendNotification(level2User, userNotification, eventHelper, triggeringUser, serviceRecord);
                    SendNotification(level3User, userNotification, eventHelper, triggeringUser, serviceRecord);
                    SendNotification(finalLevelUser, userNotification, eventHelper, triggeringUser, serviceRecord);
                    _interfaceNotifRecord.Notificationdate = DateTime.UtcNow;
                }
                
                _interfaceNotifRecord.SaveRecord();

            }

            return new EventReturnObject(EventStatusCode.Success);
        }

        private void SendNotification(UserData userData, WorkflowNotificationData userNotification, AEventHelper eventHelper,
            UserData triggeringUser, RecordInstanceData recordInsData)
        {
            if (userData != null)
            {
                userNotification.TargetID = userData.UserID;
                eventHelper.SendInboxNotification(triggeringUser, userNotification, recordInsData.RecordInstanceID, includeHyperlink: true);
            }
        }

    }
}
