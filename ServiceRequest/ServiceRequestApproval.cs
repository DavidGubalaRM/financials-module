using MCase.Core.Event;
using MCase.Event.NMImpact.Utils.DatalistUtils;
using MCaseCustomEvents.NMImpact.Generated.Entities;
using MCaseEventsSDK;
using MCaseEventsSDK.Util;
using MCaseEventsSDK.Util.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using static MCase.Event.NMImpact.Utils.DatalistUtils.ApprovalHistoryUtils;

namespace MCase.Event.NMImpact
{
    /// <summary>
    /// Trigger: Post Create / Update
    /// 
    /// Handles Approval Flow of Service Request
    /// </summary>
    public class ServiceRequestApproval : AMCaseValidateCustomEvent
    {
        public override string PrefixName => "[NMImpact] Financials";

        public override string ExactName => "Service Request Approval";

        protected override Dictionary<string, List<string>> SpecificFieldSystemNamesByListSystemName => new Dictionary<string, List<string>>();

        protected override Dictionary<string, List<string>> NeededRelationships => new Dictionary<string, List<string>>();

        protected override List<string> RecordDatalistType => new List<string>();

        protected override List<EventTrigger> ValidEventTriggers => new List<EventTrigger>() { EventTrigger.PostCreate, EventTrigger.PostUpdate };

        private UserData _supervisorUser = null;
        private UserData _managerUser = null;
        private UserData _associateDepDirUser = null;
        private UserData _depDirUser = null;
        private UserData _otaQAManagerUser = null;
        private UserData _otaDepDirUser = null;
        private Servicerequest _serviceRequestRecord;

        private const string otaQAManagerKey = "OTAQAMANAGERJOBTITLE";
        private const string otaDeputyDirectorKey = "OTADEPUTYDIRECTORJOBTITLE";

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
            // check if the record is a Placements record
            if (!recordInsData.TryParseRecord(eventHelper, out _serviceRequestRecord))
            {
                return new EventReturnObject(EventStatusCode.Failure);
            }

            #region Set All approval fields based on the Service Type
            //do this only on insert 
            // otherwise it will update for the supervisor and manager when they approve

            if (workflow.TriggerType.Equals(EventTrigger.PostCreate.GetEnumDescription()))
            {
                var serviceCatalogRecord = _serviceRequestRecord.Servicetypememo();

                // based on levels, set appropriate fields
                var level1Approval = serviceCatalogRecord.Level1;
                var level2Approval = serviceCatalogRecord.Level2;
                var level3Approval = serviceCatalogRecord.Level3;
                var finalApproval = serviceCatalogRecord.Finallevel;

                var nextLevel = ServicerequestStatic.DefaultValues.Level1;

                if (!string.IsNullOrEmpty(level1Approval) && level1Approval.Equals(F_servicecatalogStatic.DefaultValues.Otaqamanager))
                {
                    _otaQAManagerUser = GetUserFromStaffMember(eventHelper, otaQAManagerKey);

                    var amount = Double.Parse(_serviceRequestRecord.Mfdamount);
                    // If amount > $1000, then 2nd approval needed by OTA Deputy Director
                    if (amount > 1000)
                    {
                        level1Approval = F_servicecatalogStatic.DefaultValues.Otaqamanager;
                        finalApproval = F_servicecatalogStatic.DefaultValues.Otadeputydirector;
                        _otaDepDirUser = GetUserFromStaffMember(eventHelper, otaDeputyDirectorKey);
                        SetUserForLevel(level1Approval, ServicerequestStatic.SystemNames.Level1approveruser, ServicerequestStatic.SystemNames.Level1approvalrequired, ServicerequestStatic.SystemNames.Level1approver);
                        _serviceRequestRecord.Multilevelapproval = "Yes";

                    }
                    else
                    {
                        finalApproval = F_servicecatalogStatic.DefaultValues.Otaqamanager;
                        nextLevel = ServicerequestStatic.DefaultValues.Finallevel;
                        _serviceRequestRecord.Multilevelapproval = "No";
                    }

                }
                else
                {
                    _supervisorUser = GetUserRecord(triggeringUser, eventHelper);

                    // if no approval levels, then one and only level is final and we default to Supervisor
                    if (string.IsNullOrEmpty(level1Approval) && string.IsNullOrEmpty(level2Approval)
                        && string.IsNullOrEmpty(level3Approval) && string.IsNullOrEmpty(finalApproval))
                    {
                        finalApproval = F_servicecatalogStatic.DefaultValues.Supervisor;
                        nextLevel = ServicerequestStatic.DefaultValues.Finallevel;
                        _serviceRequestRecord.Multilevelapproval = "No";
                    }
                    else
                    {

                        if (_supervisorUser != null)
                            _managerUser = GetUserRecord(_supervisorUser, eventHelper);

                        if (_managerUser != null)
                            _associateDepDirUser = GetUserRecord(_managerUser, eventHelper);

                        if (_associateDepDirUser != null)
                            _depDirUser = GetUserRecord(_associateDepDirUser, eventHelper);

                        // set user and flag for levels
                        SetUserForLevel(level1Approval, ServicerequestStatic.SystemNames.Level1approveruser, ServicerequestStatic.SystemNames.Level1approvalrequired, ServicerequestStatic.SystemNames.Level1approver);
                        SetUserForLevel(level2Approval, ServicerequestStatic.SystemNames.Level2approveruser, ServicerequestStatic.SystemNames.Level2approvalrequired, ServicerequestStatic.SystemNames.Level2approver);
                        SetUserForLevel(level3Approval, ServicerequestStatic.SystemNames.Level3approveruser, ServicerequestStatic.SystemNames.Level3approvalrequired, ServicerequestStatic.SystemNames.Level3approver);
                        _serviceRequestRecord.Multilevelapproval = "Yes";

                    }

                }

                SetUserForLevel(finalApproval, ServicerequestStatic.SystemNames.Finallevelapproveruser, ServicerequestStatic.SystemNames.Finallevelapprovalrequired, ServicerequestStatic.SystemNames.Finallevelapprover);
                _serviceRequestRecord.Nextapprovallevel = nextLevel;

            }
            #endregion

            if (workflow.TriggerType.Equals(EventTrigger.PostUpdate.GetEnumDescription()))
            {

                var isMultiLevelApproval = _serviceRequestRecord.GetFieldValue<bool>(ServicerequestStatic.SystemNames.Multilevelapproval);

                #region Create Submission Approval History
                if (!string.IsNullOrEmpty(_serviceRequestRecord.Approvalhistorytype))
                {
                    CreateApprovalHistory(eventHelper, triggeringUser, isMultiLevelApproval);
                    _serviceRequestRecord.Approvalhistorytype = "";
                }

                #endregion

                #region Determine Next Approval

                var status = _serviceRequestRecord.Servicerequeststatus;
                var phase = _serviceRequestRecord.Approvalphase;
                var rejectType = _serviceRequestRecord.Rejecttype; ;

                if (status.Equals(ServicerequestStatic.DefaultValues.Approved) || status.Equals(ServicerequestStatic.DefaultValues.Rejected)
                    || rejectType.Equals(ServicerequestStatic.DefaultValues.Permanentlyrejected))
                {
                    recordInsData.ReadOnly = true;
                    _serviceRequestRecord.ReadOnly = true;
                } else
                {
                    if (isMultiLevelApproval && phase.Equals(ServicerequestStatic.DefaultValues.Approval))
                    {
                        var level1Req = _serviceRequestRecord.Level1approvalrequired;
                        var level1Deter = _serviceRequestRecord.Level1determination;
                        var level2Req = _serviceRequestRecord.Level2approvalrequired;
                        var level2Deter = _serviceRequestRecord.Level2determination;
                        var level3Req = _serviceRequestRecord.Level3approvalrequired;
                        var level3Deter = _serviceRequestRecord.Level3determination;
                        var finalLevelReq = _serviceRequestRecord.Finallevelapprovalrequired;
                        var finalLevelDeter = _serviceRequestRecord.Finalleveldetermination;

                        var nextAppr = _serviceRequestRecord.Nextapprovallevel;
                        // Final level is always requried
                        if (nextAppr.Equals(ServicerequestStatic.DefaultValues.Finallevel)
                           && !string.IsNullOrEmpty(finalLevelDeter))
                        {
                            nextAppr = string.Empty;
                        }
                        else if (nextAppr.Equals(ServicerequestStatic.DefaultValues.Level3)
                                && !string.IsNullOrEmpty(level3Deter))
                        {
                            nextAppr = ServicerequestStatic.DefaultValues.Finallevel;
                        }
                        else if (nextAppr.Equals(ServicerequestStatic.DefaultValues.Level2)
                                && !string.IsNullOrEmpty(level2Deter))
                        {
                            if (level3Req.Equals(ServicerequestStatic.DefaultValues.True))
                                nextAppr = ServicerequestStatic.DefaultValues.Level3;
                            else
                                nextAppr = ServicerequestStatic.DefaultValues.Finallevel;
                        }
                        else if (nextAppr.Equals(ServicerequestStatic.DefaultValues.Level1)
                                 && !string.IsNullOrEmpty(level1Deter))
                        {
                            if (level2Req.Equals(ServicerequestStatic.DefaultValues.True))
                            {
                                nextAppr = ServicerequestStatic.DefaultValues.Level2;
                            }
                            else if (level3Req.Equals(ServicerequestStatic.DefaultValues.True))
                            {
                                nextAppr = ServicerequestStatic.DefaultValues.Level3;
                            }
                            else
                            {
                                nextAppr = ServicerequestStatic.DefaultValues.Finallevel;
                            }
                        }
                        _serviceRequestRecord.Nextapprovallevel = nextAppr;
                    }
                }

                #endregion

            }

            _serviceRequestRecord.SaveRecord();

            return new EventReturnObject(EventStatusCode.Success);
        }

        private UserData GetUserFromStaffMember (AEventHelper eventHelper, string evmKey)
        {
            EventvaluemappingInfo evmInfo = new EventvaluemappingInfo(eventHelper);
            var evmFilters = evmInfo.CreateFilter(EventvaluemappingStatic.SystemNames.Key, new List<string> { evmKey });
            var evmRecords = evmInfo.CreateQuery(new List<DirectSQLFieldFilterData> { evmFilters });

            if (evmRecords.Count() == 0)
            {
                return null;
            }

            var evmRecord = evmRecords.First();

            // Get Staff Member with current position = "Manager, Quality Assurance OTA"
            StaffdirectoryInfo staffdirectoryInfo = new StaffdirectoryInfo(eventHelper);
            var smFilters = staffdirectoryInfo.CreateFilter(StaffdirectoryStatic.SystemNames.Position, new List<string> { evmRecord.Value });

            var smRecords = staffdirectoryInfo.CreateQuery(new List<DirectSQLFieldFilterData> { smFilters });

            if (smRecords.Count() == 0)
            {
                return null;
            }

            return smRecords.First().Userprofile();


        }
        private UserData GetUserRecord (UserData userData, AEventHelper eventHelper)
        {
            UserData returnUserData = null;
            var userID = userData.SupervisorUserID;
            if (userID.HasValue)
                returnUserData = eventHelper.GetUser(userID.Value);

            return returnUserData;
        }

        private void SetUserForLevel (string approvalLevelType, string levelUserSystemName, string levelApprovalSystemName, string levelApproverSystemName)
        {
            UserData returnUser = null;
            bool levelReq;
            if (!string.IsNullOrEmpty(approvalLevelType))
            {
                levelReq = true;
                switch (approvalLevelType)
                {
                    case F_servicecatalogStatic.DefaultValues.Supervisor:
                        levelReq = true;
                        returnUser = _supervisorUser;
                        break;
                    case F_servicecatalogStatic.DefaultValues.Manager:
                        returnUser = _managerUser;
                        break;
                    case F_servicecatalogStatic.DefaultValues.Associatedeputydirector:
                        returnUser = _associateDepDirUser;
                        break;
                    case F_servicecatalogStatic.DefaultValues.Deputydirector:
                        returnUser = _depDirUser;
                        break;
                    case F_servicecatalogStatic.DefaultValues.Otaqamanager:
                        returnUser = _otaQAManagerUser;
                        break;
                    case F_servicecatalogStatic.DefaultValues.Otadeputydirector:
                        returnUser = _otaDepDirUser;

                        break;

                    default:
                        break;
                }

                _serviceRequestRecord.SetValue(levelApproverSystemName, approvalLevelType);
                _serviceRequestRecord.SetValue(levelApprovalSystemName, levelReq.ToString());
                if (returnUser != null)
                {
                    _serviceRequestRecord.SetValue(levelUserSystemName, returnUser.UserName);
                }

            }
        }

        private void CreateApprovalHistory (AEventHelper eventHelper, UserData triggeringUser, bool isMultiLevelApproval)
        {
            switch (_serviceRequestRecord.Approvalhistorytype)
            {
                #region level 1
                case ServicerequestStatic.DefaultValues.Level1submission:
                    CreateApprovalHistoryRecord(eventHelper, ApprovalhistoryStatic.DefaultValues.Servicerequest, ApprovalhistoryStatic.DefaultValues.Level1submission,
                        _serviceRequestRecord.Mfdsubby(), _serviceRequestRecord.Mfdsubon, _serviceRequestRecord.Mfdsubcomments, _serviceRequestRecord);
                    break;
                case ServicerequestStatic.DefaultValues.Level1approval:
                    CreateApprovalHistoryRecord(eventHelper, ApprovalhistoryStatic.DefaultValues.Servicerequest, ApprovalhistoryStatic.DefaultValues.Level1approval,
                        _serviceRequestRecord.Level1apprdenby(), _serviceRequestRecord.Level1approvedon, _serviceRequestRecord.Level1comments, _serviceRequestRecord);
                    break;
                case ServicerequestStatic.DefaultValues.Level1rejection:
                    CreateApprovalHistoryRecord(eventHelper, ApprovalhistoryStatic.DefaultValues.Servicerequest, ApprovalhistoryStatic.DefaultValues.Level1rejection,
                        _serviceRequestRecord.Level1apprdenby(), _serviceRequestRecord.Level1deniedon, _serviceRequestRecord.Level1comments, _serviceRequestRecord);
                    break;
                #endregion

                #region level 2

                case ServicerequestStatic.DefaultValues.Level2approval:
                    CreateApprovalHistoryRecord(eventHelper, ApprovalhistoryStatic.DefaultValues.Servicerequest, ApprovalhistoryStatic.DefaultValues.Level2approval,
                        _serviceRequestRecord.Level2apprdenby(), _serviceRequestRecord.Level2approvedon, _serviceRequestRecord.Level2comments, _serviceRequestRecord);
                    break;
                case ServicerequestStatic.DefaultValues.Level2rejection:
                    CreateApprovalHistoryRecord(eventHelper, ApprovalhistoryStatic.DefaultValues.Servicerequest, ApprovalhistoryStatic.DefaultValues.Level2rejection,
                        _serviceRequestRecord.Level2apprdenby(), _serviceRequestRecord.Level2deniedon, _serviceRequestRecord.Level2comments, _serviceRequestRecord);
                    break;
                #endregion

                #region level 3

                case ServicerequestStatic.DefaultValues.Level3approval:
                    CreateApprovalHistoryRecord(eventHelper, ApprovalhistoryStatic.DefaultValues.Servicerequest, ApprovalhistoryStatic.DefaultValues.Level3approval,
                        _serviceRequestRecord.Level3apprdenby(), _serviceRequestRecord.Level3approvedon, _serviceRequestRecord.Level3comments, _serviceRequestRecord);
                    break;
                case ServicerequestStatic.DefaultValues.Level3rejection:
                    CreateApprovalHistoryRecord(eventHelper, ApprovalhistoryStatic.DefaultValues.Servicerequest, ApprovalhistoryStatic.DefaultValues.Level3rejection,
                        _serviceRequestRecord.Level3apprdenby(), _serviceRequestRecord.Level3deniedon, _serviceRequestRecord.Level3comments, _serviceRequestRecord);
                    break;
                #endregion

                #region final level 
                case ServicerequestStatic.DefaultValues.Finallevelsubmission:
                    if (!isMultiLevelApproval)
                    {
                        CreateApprovalHistoryRecord(eventHelper, ApprovalhistoryStatic.DefaultValues.Servicerequest, ApprovalhistoryStatic.DefaultValues.Finallevelsubmission,
                            _serviceRequestRecord.Mfdsubby(), _serviceRequestRecord.Mfdsubon, _serviceRequestRecord.Mfdsubcomments, _serviceRequestRecord);
                    }
                    break;
                case ServicerequestStatic.DefaultValues.Finallevelapproval:
                   CreateApprovalHistoryRecord(eventHelper, ApprovalhistoryStatic.DefaultValues.Servicerequest, ApprovalhistoryStatic.DefaultValues.Finallevelapproval,
                         _serviceRequestRecord.Finallevelapprdenby(), _serviceRequestRecord.Finallevelapprovedon, _serviceRequestRecord.Finallevelcomments, _serviceRequestRecord);
                    break;
                case ServicerequestStatic.DefaultValues.Finallevelrejection:
                    CreateApprovalHistoryRecord(eventHelper, ApprovalhistoryStatic.DefaultValues.Servicerequest, ApprovalhistoryStatic.DefaultValues.Finallevelrejection,
                        _serviceRequestRecord.Finallevelapprdenby(), _serviceRequestRecord.Finalleveldeniedon, _serviceRequestRecord.Finallevelcomments, _serviceRequestRecord);
                    break;
                case ServicerequestStatic.DefaultValues.Temporaryrejection:
                     CreateApprovalHistoryRecord(eventHelper, ApprovalhistoryStatic.DefaultValues.Servicerequest, ApprovalhistoryStatic.DefaultValues.Temporaryrejection,
                        _serviceRequestRecord.Finallevelapprdenby(), _serviceRequestRecord.Finalleveldeniedon, _serviceRequestRecord.Finallevelcomments, _serviceRequestRecord);
                    break;
                #endregion

                default:
                    eventHelper.AddErrorLog($"No approval action type found for {_serviceRequestRecord.RecordInstanceID}");
                    break;
            }

        }

    }
}
