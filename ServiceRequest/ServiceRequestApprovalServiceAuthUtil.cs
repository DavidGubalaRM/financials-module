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
    /// Post Update Event on Service Request when status is changed to Active
    /// Create Service Auth and Service Utilization. 
    /// update to ensure only ONE auth is created
    /// </summary>
    public class ServiceRequestApprovalServiceAuthUtil : AMCaseValidateCustomEvent
    {
        public override string PrefixName => "[NMImpact] Financials";
        public override string ExactName => "Service Request Approval Create Service Auth";

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
            if (!recordInsData.TryParseRecord(eventHelper, out Servicerequest serviceRequestRecord))
            {
                eventHelper.AddDebugLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                eventHelper.AddErrorLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                return new EventReturnObject(EventStatusCode.Failure, new List<string> { GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity });
            }

            var parentRecordCase = serviceRequestRecord.GetParentCases();
            var parentRecordInv = serviceRequestRecord.GetParentInvestigations();

            var status = serviceRequestRecord.Servicerequeststatus;
            if (status != ServicerequestStatic.DefaultValues.Approved)
            {
                return new EventReturnObject(EventStatusCode.Success);
            }

            // get Provider record and not Provider selected, exit
            Providers providerRecord = null;
            if (serviceRequestRecord.Typeofprovidersearch.Equals(ServicerequestStatic.DefaultValues.Resourcefamilyprovider))
            {
                providerRecord = serviceRequestRecord.Resourceprovidname()?.Provider();
            }
            else if (serviceRequestRecord.Typeofprovidersearch.Equals(ServicerequestStatic.DefaultValues.Goodsandservicesprovider))
            {
                providerRecord = serviceRequestRecord.Mfdprovidname()?.Provider();
            }

            if (providerRecord == null)
            {
                return new EventReturnObject(EventStatusCode.Success);
            }

            // create Service Auth and Service Utils
            var recurrence = DetermineRecurrence(eventHelper, serviceRequestRecord);

            if (parentRecordCase != null)
            {
                CreateAuthorizationsForCase(eventHelper, serviceRequestRecord, parentRecordCase, recurrence, providerRecord);
            }

            else if (parentRecordInv != null)
            {
                CreateAuthorizationForInvestigation(eventHelper, serviceRequestRecord, parentRecordInv, recurrence, providerRecord);
            }

            return new EventReturnObject(EventStatusCode.Success);
        }

        private static void CreateAuthorizationsForCase(AEventHelper eventHelper, Servicerequest serviceRequestRecord, Cases parentRecordCase, string recurrence, Providers providerRecord)
        {

            // for one or other reason the UniqueID MFDID is not generated as part of ORM
            var serviceRequestId = serviceRequestRecord.GetFieldValue(ServicerequestStatic.SystemNames.Mfdid);


            var serviceAuth = CreateServiceAuthorization(eventHelper, serviceRequestRecord, parentRecordCase, recurrence, F_servicelineStatic.DefaultValues.Case, providerRecord);
            // create one auth for all persons 
            var participants = serviceRequestRecord.Mfdpersidname();
            var persons = new List<Persons>();
            foreach (var participant in participants)
            {
                var person = participant.Caseparticipantname();
                persons.Add(person);
            }
            serviceAuth.Participant(persons);

            Countylist county = null;
            if (persons.Count > 0)
            {
                county = NMFinancialUtils.GetLatestRemovalCounty(eventHelper, persons.FirstOrDefault());
            }
            if (county != null)
                serviceAuth.County(county);
            else
                serviceAuth.County(parentRecordCase.Casecounty()); // get county from parent Record

            serviceAuth.SaveRecord();

            var startDate = serviceAuth.Startdate ?? DateTime.UtcNow;
            var endDate = serviceAuth.Enddate ?? DateTime.UtcNow;
            int units = int.TryParse(serviceRequestRecord.Unitsreccuring, out var parsedUnits) ? parsedUnits : 1;
            for (int i = 0; i < units; i++)
            {
                var unitsUtilized = i + 1;

                SetEndDate(startDate, recurrence, ref endDate);

                var serviceUtilizaion = CreateServiceUtilization(eventHelper, serviceAuth, startDate, endDate, serviceRequestId, unitsUtilized);
                serviceUtilizaion.Case(parentRecordCase);
                serviceUtilizaion.Serviceauthorizationcase(serviceAuth);
                serviceUtilizaion.SaveRecord();

                SetStartDate(recurrence, ref startDate);
            }

            serviceAuth.Enddate = endDate;
            serviceAuth.SaveRecord();

        }

        private static void CreateAuthorizationForInvestigation(AEventHelper eventHelper, Servicerequest serviceRequestRecord, Investigations parentRecordInv, string recurrence, Providers providerRecord)
        {

            // for one or other reason the UniqueID MFDID is not generated as part of ORM
            var serviceRequestId = serviceRequestRecord.GetFieldValue(ServicerequestStatic.SystemNames.Mfdid);

            var serviceAuth = CreateServiceAuthorization(eventHelper, serviceRequestRecord, parentRecordInv, recurrence, F_servicelineStatic.DefaultValues.Investigation, providerRecord);

            // currently creating an auth for each participant listed. 
            var participants = serviceRequestRecord.Investigationsparticipants();
            var persons = new List<Persons>();
            foreach (var participant in participants)
            {
                var person = participant.Invparticipantname();
                persons.Add(person);
            }
            serviceAuth.Participant(persons);

            Countylist county = null;
            if (persons.Count > 0)
            {
                county = NMFinancialUtils.GetLatestRemovalCounty(eventHelper, persons.FirstOrDefault());
            }
            if (county != null)
                serviceAuth.County(county);
            else
                serviceAuth.County(parentRecordInv.Invcounty()); // get county from parent Record

            serviceAuth.SaveRecord();

            var startDate = serviceAuth.Startdate ?? DateTime.UtcNow;
            var endDate = serviceAuth.Enddate ?? DateTime.UtcNow;
            int units = int.TryParse(serviceRequestRecord.Unitsreccuring, out var parsedUnits) ? parsedUnits : 1;
            for (int i = 0; i < units; i++)
            {
                var unitsUtilized = i + 1;

                SetEndDate(startDate, recurrence, ref endDate);

                var serviceUtilizaion = CreateServiceUtilization(eventHelper, serviceAuth, startDate, endDate, serviceRequestId, unitsUtilized);
                serviceUtilizaion.Investigation(parentRecordInv);
                serviceUtilizaion.Serviceauthorizationinv(serviceAuth);
                serviceUtilizaion.SaveRecord();

                SetStartDate(recurrence, ref startDate);
            }

            serviceAuth.Enddate = endDate;
            serviceAuth.SaveRecord();
        }

        private static void SetEndDate(DateTime startDate, string recurrence, ref DateTime endDate)
        {
            if (recurrence == F_servicelineStatic.DefaultValues.Weekly)
            {
                // update start date
                endDate = startDate.AddDays(6);
            }
            else if (recurrence == F_servicelineStatic.DefaultValues.Monthly)
            {
                // update start date
                endDate = startDate.AddMonths(1).AddDays(-1);
            }

        }

        private static void SetStartDate(string recurrence, ref DateTime startDate)
        {
            if (recurrence == F_servicelineStatic.DefaultValues.Weekly)
            {
                // update start date
                startDate = startDate.AddDays(7);
            }
            else if (recurrence == F_servicelineStatic.DefaultValues.Monthly)
            {
                // update start date
                startDate = startDate.AddMonths(1);
            }

        }

        private static F_serviceline CreateServiceAuthorization(AEventHelper eventHelper, dynamic requestRecord, dynamic parentRecord, string recurrence, string parentType, Providers providerRecord)
        {
            var serviceAuthInfo = new F_servicelineInfo(eventHelper);
            var newAuth = serviceAuthInfo.NewF_serviceline(parentRecord);

            SetAuthorizationFields(eventHelper, newAuth, requestRecord, recurrence, providerRecord);
            newAuth.Parentdl = parentType;

            return newAuth;
        }

        private static void SetAuthorizationFields(AEventHelper eventHelper, F_serviceline newServiceAuthRecord, Servicerequest serviceRequestRecord, string recurrence, Providers providerRecord)
        {
            newServiceAuthRecord.CreatedOn = DateTime.UtcNow;
            newServiceAuthRecord.Sourcerequesttype = F_servicelineStatic.DefaultValues.Servicerequest;
            newServiceAuthRecord.Servicerequest(serviceRequestRecord);

            newServiceAuthRecord.Provider(providerRecord);
            newServiceAuthRecord.Totalamount = serviceRequestRecord.Mfdamount;

            newServiceAuthRecord.Servicecategory = serviceRequestRecord.Mfdapprovaltype;
            newServiceAuthRecord.Servicecatalogtype(serviceRequestRecord.Servicetypememo());
            if (serviceRequestRecord.Typeofprovidersearch.Equals(ServicerequestStatic.DefaultValues.Goodsandservicesprovider))
            {
                var providerServiceRecord = GetProviderService(eventHelper, providerRecord, serviceRequestRecord);

                if (providerServiceRecord != null)
                {
                    newServiceAuthRecord.Servicetype(providerServiceRecord);
                }
            }

            newServiceAuthRecord.Servicelinestatus = F_servicelineStatic.DefaultValues.Active;

            newServiceAuthRecord.Recurrence = recurrence;
            newServiceAuthRecord.Startdate = serviceRequestRecord.Requestdate;

            var unitsAuth = GetUnitsAuthorized(eventHelper, serviceRequestRecord);
            newServiceAuthRecord.Unitsauthorized = unitsAuth.ToString();

            if (recurrence == F_servicelineStatic.DefaultValues.Onetime)
            {
                newServiceAuthRecord.Enddate = serviceRequestRecord.Requestdate;
            }

            newServiceAuthRecord.Dltype = F_servicelineStatic.DefaultValues.Servicerequest;
        }

        private static string DetermineRecurrence(AEventHelper eventHelper, Servicerequest serviceRequestRecord)
        {
            var recurringService = serviceRequestRecord.Recurringservice;
            if (recurringService == ServicerequestStatic.DefaultValues.No)
            {
                return F_servicelineStatic.DefaultValues.Onetime;
            }
            return serviceRequestRecord.Freqreoccur;
        }

        private static int GetUnitsAuthorized(AEventHelper eventHelper, Servicerequest serviceRequestRecord)
        {
            var recurringService = serviceRequestRecord.Recurringservice;
            if (recurringService == ServicerequestStatic.DefaultValues.No)
            {
                return 1;
            }
            return int.Parse(serviceRequestRecord.Unitsreccuring);

        }

        private static F_providerservice GetProviderService(AEventHelper eventHelper, Providers providerRecord, Servicerequest serviceRequestRecord)
        {
            var serviceCatalogRecord = serviceRequestRecord.Servicetypememo();

            var providerServiceInfo = new F_providerserviceInfo(eventHelper);
            var filter = providerServiceInfo.CreateFilter(F_providerserviceStatic.SystemNames.Service, new List<string> { serviceCatalogRecord.RecordInstanceID.ToString() });
            var providerServiceRecords = providerServiceInfo.CreateQuery(new List<DirectSQLFieldFilterData> { filter });

            if (providerServiceRecords.Count() > 0)
            {
                return providerServiceRecords.First();
            }

            return null;
        }

        private static F_serviceplanutilization CreateServiceUtilization(AEventHelper eventHelper, F_serviceline serviceAuthorization, DateTime? startDate, DateTime? endDate,
            string serviceRequestId, int unitsUtilized)
        {
            var provider = serviceAuthorization.Provider();
            var serviceUtil = new F_serviceplanutilizationInfo(eventHelper);
            var serviceUtilization = serviceUtil.NewF_serviceplanutilization(provider);

            serviceUtilization.O_status = F_serviceplanutilizationStatic.DefaultValues.Draft;
            serviceUtilization.Sourcerequesttype = F_serviceplanutilizationStatic.DefaultValues.Servicerequest;
            serviceUtilization.Participant(serviceAuthorization.Participant());
            serviceUtilization.Provider(provider);
            serviceUtilization.Servicetype(serviceAuthorization.Servicetype());
            serviceUtilization.Servicecatalogtype(serviceAuthorization.Servicecatalogtype());
            serviceUtilization.Unitsauthorized = serviceAuthorization.Unitsauthorized;
            serviceUtilization.Unitsutilized = unitsUtilized.ToString();
            serviceUtilization.Rateoccurrence = serviceAuthorization.Recurrence;
            serviceUtilization.Totalbillableamount = serviceAuthorization.Totalamount;
            serviceUtilization.Dateofservice = startDate; // TODO: required field and not really sure what this date should be.
            serviceUtilization.Startdate = startDate;
            serviceUtilization.Enddate = endDate;
            serviceUtilization.Servicecategory = serviceAuthorization.Servicecategory;
            serviceUtilization.County(serviceAuthorization.County());

            serviceUtilization.Servicerequestid = serviceRequestId;
            return serviceUtilization;
        }
    }
}