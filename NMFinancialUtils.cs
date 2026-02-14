using MCase.Core.Event;
using MCase.Event.NMImpact.Constants;
using MCase.Event.NMImpact.Financials;
using MCaseCustomEvents.NMImpact.Generated.Entities;
using MCaseEventsSDK;
using MCaseEventsSDK.Util;
using MCaseEventsSDK.Util.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static MCase.Event.NMImpact.Constants.NMFinancialConstants;

namespace MCase.Event.NMImpact
{
    public class NMFinancialUtils
    {
        public static Providerschildofservicecatalog CheckForExistingPCOSC(AEventHelper eventHelper, F_servicecatalog serviceCatalogRecord, Providers providerRecord)
        {
            var pcosc = new ProviderschildofservicecatalogInfo(eventHelper);
            var pcoscFilter = pcosc.CreateFilter(ProviderschildofservicecatalogStatic.SystemNames.Provider, new List<string> { providerRecord.RecordInstanceID.ToString() });

            var PCOSCDatalistID = eventHelper.GetDataListID(ProviderschildofservicecatalogStatic.SystemName);
            var existingPCOSC = eventHelper.SearchSingleDataListSQLProcess(PCOSCDatalistID.Value, new List<DirectSQLFieldFilterData> { pcoscFilter }, serviceCatalogRecord.RecordInstanceID)
                .FirstOrDefault();

            if (existingPCOSC != null)
            {
                var existingPCOSCRecord = existingPCOSC.TryParseRecord(eventHelper, out Providerschildofservicecatalog pcoscRecord);

                return pcoscRecord;
            }

            return null;
        }

        public static Batchinterfacerun GetRunRecord(AEventHelper eventHelper, List<string> validationMsgs, DateTime? dateToStartFiltering = null, DateTime? dateToEndFiltering = null)
        {
            var batchProcessID = BatchinterfaceprocessStatic.DefaultValues.Checkenddateonservicerateagreement;
            var batchProcessInfo = new BatchinterfaceprocessInfo(eventHelper);
            var batchProcessFilter = batchProcessInfo.CreateFilter(BatchinterfaceprocessStatic.SystemNames.Batchprocessid, new List<string> { batchProcessID });
            var batchProcessRecord = batchProcessInfo.CreateQuery(new List<DirectSQLFieldFilterData>() { batchProcessFilter }).LastOrDefault();

            if (batchProcessRecord != null)
            {
                var latestRunRecord = batchProcessRecord.GetChildrenBatchinterfacerun()
                    .OrderByDescending(x => x.Runnumber).FirstOrDefault();

                var batchRunRecordInfo = new BatchinterfacerunInfo(eventHelper);
                var batchRunRecord = batchRunRecordInfo.NewBatchinterfacerun(latestRunRecord);
                if (dateToStartFiltering != null && dateToEndFiltering != null)
                {
                    batchRunRecord.Filteringstartdate = dateToStartFiltering;
                    batchRunRecord.Filteringenddate = dateToEndFiltering;
                }
                // if null, then creating first run record with run number of 1 and status of running
                if (latestRunRecord == null)
                {
                    batchRunRecord.Runstatus = BatchinterfacerunStatic.DefaultValues.Running;
                    batchRunRecord.Startdatetime = DateTime.UtcNow;
                    batchRunRecord.Creationdatetime = DateTime.UtcNow;
                    batchRunRecord.Batchinterfaceprocess(batchProcessRecord);
                    batchRunRecord.Processtype = batchProcessRecord.Processtype;
                    batchRunRecord.Runnumber = 1.ToString();
                }
                else
                {
                    // if not null, then check whether status us running or completed
                    var latestRunRecordStatus = latestRunRecord.Runstatus;
                    if (latestRunRecordStatus.Equals(BatchinterfacerunStatic.DefaultValues.Running))
                    {
                        // if running, the previous failure, use that record
                        return latestRunRecord;
                    }
                    else if (latestRunRecordStatus.Equals(BatchinterfacerunStatic.DefaultValues.Completed)
                        || latestRunRecordStatus.Equals(BatchinterfacerunStatic.DefaultValues.Failed))
                    {
                        // if completed, or previous run failed, create new run record with run number + 1 and status = running
                        batchRunRecord.Runnumber = latestRunRecord.Runnumber + 1;

                        return batchRunRecord;
                    }
                }

            }

            else
            {
                string MissingProcessRecord = "The Batch Interface Process for the Process ID: {0} does not exist.";
                var missingProcessRecord = string.Format(MissingProcessRecord, batchProcessID);
                eventHelper.AddErrorLog(missingProcessRecord);
                validationMsgs.Add(missingProcessRecord);
                return null;
            }

            return null;
        }

        public static void FailRunRecord(AEventHelper eventHelper, Batchinterfacerun currentRunRecord, int totalRecordsProcessed, int recordsSucceeded, int recordsFailed, string runError)
        {
            currentRunRecord.Enddatetime = DateTime.UtcNow;
            currentRunRecord.Totalrecordsprocessed = totalRecordsProcessed.ToString();
            currentRunRecord.Totalrecordssucceeded = recordsSucceeded.ToString();
            currentRunRecord.Totalrecordsfailed = recordsFailed.ToString();
            currentRunRecord.Runstatus = BatchinterfacerunStatic.DefaultValues.Failed;
            currentRunRecord.Runerrors = runError.ToString();

            eventHelper.SaveRecord(currentRunRecord);
        }

        public static dynamic GetServiceAuthorization(F_serviceplanutilization serviceUtil)
        {
            dynamic serviceAuth = null;
            var caseAuth = serviceUtil.Serviceauthorizationcase();
            var invAuth = serviceUtil.Serviceauthorizationinv();
            if (caseAuth != null)
            {
                serviceAuth = caseAuth;
            }
            else if (invAuth != null)
            {
                serviceAuth = invAuth;
            }
            return serviceAuth;
        }

        public static dynamic GetParentRecord(dynamic recordInsData)
        {
            dynamic parentRecord = null;
            var parentInvestigation = recordInsData.GetParentInvestigations();
            var parentCase = recordInsData.GetParentCases();
            if (parentInvestigation != null)
            {
                parentRecord = parentInvestigation;
            }
            else if (parentCase != null)
            {
                parentRecord = parentCase;
            }

            return parentRecord;
        }

        public static DateTime GetPersonDOB(Persons personRecord)
        {
            DateTime dob = DateTime.MinValue;
            DateTime? persondob = personRecord.Dateofbirth;
            DateTime? approxDOB = personRecord.Approximatedateofbirth;

            if (persondob != null && persondob != DateTime.MinValue)
            {
                dob = persondob.Value;
            }
            else if (approxDOB != null && approxDOB != DateTime.MinValue)
            {
                dob = approxDOB.Value;
            }

            return dob;
        }

        public static double CalculateRateForAge(List<F_serviceratesagebased> ageBasedRates, int personAge = 0)
        {
            double amountPerUnit = 0;
            var today = DateTime.Today;

            foreach (var ageBasedRate in ageBasedRates)
            {
                var rateBasedOn = ageBasedRate.Ratebasedon;
                var rate = ageBasedRate.Rate;
                if (rateBasedOn == F_serviceratesagebasedStatic.DefaultValues.Age)
                {
                    var startingAgeString = ageBasedRate.Startingage;
                    var endingAgeString = ageBasedRate.Endingage;

                    int hasStartingAge = int.TryParse(startingAgeString, out int startingAge) ? startingAge : 0;
                    int hasEndingAge = int.TryParse(endingAgeString, out int endingAge) ? endingAge : 0;

                    if (personAge >= startingAge && personAge <= endingAge)
                    {
                        if (rate != null)
                        {
                            amountPerUnit = double.Parse(rate);
                            return amountPerUnit;
                        }
                    }
                }

                else
                {
                    if (rate != null)
                    {
                        amountPerUnit = double.Parse(rate);
                        return amountPerUnit;
                    }
                }
            }
            return amountPerUnit;
        }

        public static int CalculateAgeUpToGivenDate(DateTime dateOFBirth, DateTime givenDate)
        {
            int age = givenDate.Year - dateOFBirth.Year;
            if (dateOFBirth > givenDate.AddYears(-age))
            {
                age--;
            }

            return age;
        }

        public static double CalculateRateBasedOnAge(List<F_serviceratesagebased> ageBasedRates, DateTime personDOB, DateTime startDate, DateTime endDate, string rateOccurrence)
        {
            double rate = 0;
            bool hasBirthday = PersonHasBirthdayDuringPeriod(personDOB, startDate, endDate);

            if (personDOB == DateTime.MinValue)
            {
                double flatRate = CalculateRateForAge(ageBasedRates);
                rate = flatRate;
                return flatRate;
            }

            if (!hasBirthday)
            {
                int age = CalculateAgeUpToGivenDate(personDOB, startDate);
                rate = CalculateRateForAge(ageBasedRates, age);
                return rate;
            }

            // If birthday occurs during the period
            int ageStart = CalculateAgeUpToGivenDate(personDOB, startDate);
            int ageEnd = ageStart + 1;

            double rateStart = CalculateRateForAge(ageBasedRates, ageStart);
            double rateEnd = CalculateRateForAge(ageBasedRates, ageEnd);

            DateTime birthdayThisYear = new DateTime(startDate.Year, personDOB.Month, personDOB.Day);
            if (birthdayThisYear < startDate)
                birthdayThisYear = birthdayThisYear.AddYears(1);

            int daysBefore = (birthdayThisYear - startDate).Days;
            int daysAfter = (endDate - birthdayThisYear).Days + 1;
            int totalDays = daysBefore + daysAfter;

            if (rateOccurrence == F_servicerateStatic.DefaultValues.Monthly)
            {
                // We don't convert to daily ctFMAPRecord — just prorate monthly rates for total amount
                rate = (rateStart * daysBefore / totalDays) + (rateEnd * daysAfter / totalDays);
                return rate;
            }
            else if (rateOccurrence == F_servicerateStatic.DefaultValues.Daily)
            {
                rate = (daysBefore * rateStart) + (daysAfter * rateEnd);
                // TODO: this assumes an average ctFMAPRecord - check with BA to make sure this is okay
                return rate / totalDays;
            }

            return 0;
        }

        public static double CalculateRate(F_serviceratesagebased determineRateType,  DateTime startDate, ref string rateOccurrence, ref int segmentDays)
        {

            double currentRate = double.TryParse(determineRateType.Rate, out double rate) ? rate : 0;

            int daysInMonth = DateTime.DaysInMonth(startDate.Year, startDate.Month);
            if (rateOccurrence == F_servicerateStatic.DefaultValues.Monthly)
            {
                // if it is monthly and full month, else if monthly an not full month, prorate the rate and use daily rate
                if (daysInMonth == segmentDays)
                {
                    segmentDays = 1;
                }
                else
                {
                    rateOccurrence = F_servicerateStatic.DefaultValues.Daily;
                    currentRate = currentRate / daysInMonth;
                }
            }
            return currentRate;
/*

            if (rateOccurrence == F_servicerateStatic.DefaultValues.Daily)
            {
                return double.TryParse(locScoreRate.Rate, out double dailyRate) ? dailyRate : 0;
            }

            return double.TryParse(locScoreRate.Rate, out double rate) ? rate : 0;*/
        }

        public static double CalculateRateForAdoptionAndGuardianship(DateTime startDate, DateTime endDate,
            List<Adoptionassistanceagreement> adoptionAssistanceAgreement = null, List<Guardianshipassistanceagreement> guardianshipAgreementAssistance = null)
        {
            double totalAmount = 0;
            int totalDays = 0;

            // Process Adoption Agreements
            if (adoptionAssistanceAgreement != null && adoptionAssistanceAgreement.Count > 0)
            {
                var sortedAgreements = adoptionAssistanceAgreement
                    .Where(a => a.Assistancepaymentdate.HasValue)
                    .OrderBy(a => a.Assistancepaymentdate.Value)
                    .ToList();

                for (int i = 0; i < sortedAgreements.Count; i++)
                {
                    var current = sortedAgreements[i];
                    DateTime effectiveStart = current.Assistancepaymentdate.Value;

                    // Determine effective end: either the next agreement's start - 1 day, or the overall endDate
                    DateTime effectiveEnd = (i < sortedAgreements.Count - 1)
                        ? sortedAgreements[i + 1].Assistancepaymentdate.Value.AddDays(-1)
                        : endDate;

                    DateTime segmentStart = effectiveStart > startDate ? effectiveStart : startDate;
                    DateTime segmentEnd = effectiveEnd < endDate ? effectiveEnd : endDate;

                    if (segmentEnd < segmentStart)
                        continue;

                    int days = (segmentEnd - segmentStart).Days + 1;
                    double rate = Convert.ToDouble(current.Aaaamount);
                    totalAmount += rate * days;
                    totalDays += days;
                }
            }

            // Process Guardianship Agreements
            if (guardianshipAgreementAssistance != null && guardianshipAgreementAssistance.Count > 0)
            {
                var sortedAgreements = guardianshipAgreementAssistance
                    .Where(g => g.Gaaeffectivedate.HasValue)
                    .OrderBy(g => g.Gaaeffectivedate.Value)
                    .ToList();

                for (int i = 0; i < sortedAgreements.Count; i++)
                {
                    var current = sortedAgreements[i];
                    DateTime effectiveStart = current.Gaaeffectivedate.Value;

                    DateTime effectiveEnd = (i < sortedAgreements.Count - 1)
                        ? sortedAgreements[i + 1].Gaaeffectivedate.Value.AddDays(-1)
                        : endDate;

                    DateTime segmentStart = effectiveStart > startDate ? effectiveStart : startDate;
                    DateTime segmentEnd = effectiveEnd < endDate ? effectiveEnd : endDate;

                    if (segmentEnd < segmentStart)
                        continue;

                    int days = (segmentEnd - segmentStart).Days + 1;
                    double rate = Convert.ToDouble(current.Gaaamount);
                    totalAmount += rate * days;
                    totalDays += days;
                }
            }

            return totalDays > 0 ? totalAmount / totalDays : 0;
        }



        public static bool PersonHasBirthdayDuringPeriod(DateTime dob, DateTime startDate, DateTime endDate)
        {
            DateTime birthdayThisYear = new DateTime(startDate.Year, dob.Month, dob.Day);
            if (birthdayThisYear < dob)
                birthdayThisYear = birthdayThisYear.AddYears(1);

            return birthdayThisYear >= startDate && birthdayThisYear <= endDate;
        }

        public static int SendServiceRateAgreementEndingNotification(AEventHelper eventHelper, UserData triggeringUser, WorkFlowData workflow, F_contract serviceRateAgreement)
        {
            // Null check
            if (eventHelper == null || triggeringUser == null || workflow == null || serviceRateAgreement == null)
            {
                return default;
            }

            var providerInfo = new ProvidersInfo(eventHelper);
            var providerFilter = providerInfo.CreateFilter(ProvidersStatic.SystemNames.Servicerateagreeid, new List<string> { serviceRateAgreement.RecordInstanceID.ToString() });
            var providerRecords = providerInfo.CreateQuery(new List<DirectSQLFieldFilterData> { providerFilter });

            if (!providerRecords.Any())
            {
                // do not send notification if not in use by any providers
                return default;
            }

            StringBuilder providerNames = new StringBuilder();
            if (providerRecords.Count() == 1)
            {
                var providerName = providerRecords.First().Providernames;
                providerNames.AppendLine(providerName);
            }
            else
            {
                // Build comma-separated provider names
                var names = providerRecords.Select(p => p.Providernames);
                providerNames.Append(string.Join(", ", names));
            }

            StringBuilder services = new StringBuilder();
            var serviceRates = serviceRateAgreement.GetChildrenF_servicerate();
            var serviceNames = new List<string>();
            foreach (var serviceRate in serviceRates)
            {
                var serviceCatalogServices = serviceRate.Forservice();
                foreach (var service in serviceCatalogServices)
                {
                    serviceNames.Add(service.Nameofservice);
                }
            }

            if (serviceNames.Any())
            {
                services.Append(string.Join(", ", serviceNames));
            }

            // Get System Admin and Contracts Unit queues.
            var systemAdminQueue = eventHelper.GetWorkQueue(BatchNotificationConstants.WorkQueueNames.SystemAdminQueue);
            var contractsUnitQueue = eventHelper.GetWorkQueue(BatchNotificationConstants.WorkQueueNames.ContractsUnitQueue);

            // Send notification
            var notificationsCreated = 0;
            if (systemAdminQueue != null)
            {
                var notification = new WorkflowNotificationData
                {
                    WorkflowID = workflow.WorkflowID,
                    Subject = BatchNotificationConstants.ServiceRateAgreementExpiring.Subject,
                    Message = string.Format(BatchNotificationConstants.ServiceRateAgreementExpiring.Message, providerNames, services),
                    TargetType = WorkflowNotificationData.TargetTypeWorkQueue,
                    TargetID = systemAdminQueue.WorkQueueID
                };

                // Send notification
                eventHelper.SendInboxNotification(triggeringUser, notification, serviceRateAgreement, includeHyperlink: true);
                notificationsCreated++;
            }

            if (contractsUnitQueue != null)
            {
                var notification = new WorkflowNotificationData
                {
                    WorkflowID = workflow.WorkflowID,
                    Subject = BatchNotificationConstants.ServiceRateAgreementExpiring.Subject,
                    Message = string.Format(BatchNotificationConstants.ServiceRateAgreementExpiring.Message, providerNames, services),
                    TargetType = WorkflowNotificationData.TargetTypeWorkQueue,
                    TargetID = contractsUnitQueue.WorkQueueID
                };

                // Send notification
                eventHelper.SendInboxNotification(triggeringUser, notification, serviceRateAgreement, includeHyperlink: true);
                notificationsCreated++;
            }

            // Result
            return notificationsCreated;
        }

        public static int SendServiceRateEndingNotification(AEventHelper eventHelper, UserData triggeringUser, WorkFlowData workflow, F_servicerate serviceRate)
        {
            var service = serviceRate.Forservice();

            // Null check
            if (eventHelper == null || triggeringUser == null || workflow == null || serviceRate == null)
            {
                return default;
            }

            // Get System Admin and Contracts Unit queues.
            var systemAdminQueue = eventHelper.GetWorkQueue(BatchNotificationConstants.WorkQueueNames.SystemAdminQueue);

            // Send notification
            var notificationsCreated = 0;
            if (systemAdminQueue != null)
            {
                var notification = new WorkflowNotificationData
                {
                    WorkflowID = workflow.WorkflowID,
                    Subject = string.Format(BatchNotificationConstants.ServiceRateExpiring.Subject, service),
                    Message = string.Format(BatchNotificationConstants.ServiceRateExpiring.Message, service),
                    TargetType = WorkflowNotificationData.TargetTypeWorkQueue,
                    TargetID = systemAdminQueue.WorkQueueID
                };

                // Send notification
                eventHelper.SendInboxNotification(triggeringUser, notification, serviceRate, includeHyperlink: true);
                notificationsCreated++;
            }

            // Result
            return notificationsCreated;
        }

        public static int SendHolidayRunApproachingNotification(AEventHelper eventHelper, UserData triggeringUser, WorkFlowData workflow, F_timeperiod timePeriod)
        {
            // Null check
            if (eventHelper == null || triggeringUser == null || workflow == null || timePeriod == null)
            {
                return default;
            }

            // Get System Admin queue
            var systemAdminQueue = eventHelper.GetWorkQueue(BatchNotificationConstants.WorkQueueNames.SystemAdminQueue);

            // Send notification
            var notificationsCreated = 0;
            if (systemAdminQueue != null)
            {
                var notification = new WorkflowNotificationData
                {
                    WorkflowID = workflow.WorkflowID,
                    Subject = string.Format(BatchNotificationConstants.HolidayRunApproaching.Subject),
                    Message = string.Format(BatchNotificationConstants.HolidayRunApproaching.Message),
                    TargetType = WorkflowNotificationData.TargetTypeWorkQueue,
                    TargetID = systemAdminQueue.WorkQueueID
                };

                // Send notification
                eventHelper.SendInboxNotification(triggeringUser, notification, timePeriod, includeHyperlink: true);
                notificationsCreated++;
            }

            // Result
            return notificationsCreated;
        }

        public static int SendBackToSchoolRunApproachingNotification(AEventHelper eventHelper, UserData triggeringUser, WorkFlowData workflow, F_timeperiod timePeriod)
        {
            // Null check
            if (eventHelper == null || triggeringUser == null || workflow == null || timePeriod == null)
            {
                return default;
            }

            // Get System Admin queue
            var systemAdminQueue = eventHelper.GetWorkQueue(BatchNotificationConstants.WorkQueueNames.SystemAdminQueue);

            // Send notification
            var notificationsCreated = 0;
            if (systemAdminQueue != null)
            {
                var notification = new WorkflowNotificationData
                {
                    WorkflowID = workflow.WorkflowID,
                    Subject = string.Format(BatchNotificationConstants.BackToSchoolRunApproaching.Subject),
                    Message = string.Format(BatchNotificationConstants.BackToSchoolRunApproaching.Message),
                    TargetType = WorkflowNotificationData.TargetTypeWorkQueue,
                    TargetID = systemAdminQueue.WorkQueueID
                };

                // Send notification
                eventHelper.SendInboxNotification(triggeringUser, notification, timePeriod, includeHyperlink: true);
                notificationsCreated++;
            }

            // Result
            return notificationsCreated;
        }

        public static DateTime GetNextStartDate(DateTime today, string notifType)
        {
            var startDate = new DateTime();
            if (notifType == NotificationtriggerStatic.DefaultValues.Holidayrunapproaching)
            {
                var decemberFirstThisYear = new DateTime(today.Year, 12, 1);

                if (today >= decemberFirstThisYear)
                {
                    // Today is on or after December 1st, set start date to next year
                    startDate = new DateTime(today.Year + 1, 12, 1);
                }
                else
                {
                    // Today is before December 1st, set start date to this year
                    startDate = decemberFirstThisYear;
                }
            }

            else if (notifType == NotificationtriggerStatic.DefaultValues.Backtoschoolrunapproaching)
            {
                var julyFirstThisYear = new DateTime(today.Year, 7, 1);

                if (today >= julyFirstThisYear)
                {
                    // Today is on or after July 1st, set start date to next year
                    startDate = new DateTime(today.Year + 1, 7, 1);
                }
                else
                {
                    // Today is before July 1st, set start date to this year
                    startDate = julyFirstThisYear;
                }
            }

            return startDate;
        }

        public static F_timeperiod CheckForTimePeriodRecord(F_timeperiodInfo timePeriodInfo, DateTime today, string type)
        {
            var timePeriodFilter = timePeriodInfo.CreateFilter(F_timeperiodStatic.SystemNames.Name, new List<string> { type });
            var timePeriodStatusFilter = timePeriodInfo.CreateFilter(F_timeperiodStatic.SystemNames.O_status, new List<string> { F_timeperiodStatic.DefaultValues.Open });

            var timePeriodQuery = timePeriodInfo.CreateQuery(new List<DirectSQLFieldFilterData> { timePeriodFilter });

            var timePeriodRecord = timePeriodQuery
                .Where(ri => ri.Startdate.HasValue && ri.Startdate.Value.Year == today.Year)
                .OrderByDescending(ri => ri.Startdate)
                .FirstOrDefault();

            // need to check the end date, and if it is the end of the current month, change the status to "Closed"
            var endDate = timePeriodRecord?.Enddate;
            if (endDate.HasValue)
            {
                var lastDayofCurrentMonth = new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));
                if (endDate.Value.Date == lastDayofCurrentMonth)
                {
                    timePeriodRecord.O_status = F_timeperiodStatic.DefaultValues.Closed;
                    timePeriodRecord.SaveRecord();
                    // Reset to null to force creation of a new record
                    timePeriodRecord = null;
                }
            }

            if (timePeriodRecord != null)
            {
                return timePeriodRecord;
            }

            return null;
        }

        public static F_timeperiod CreateNewTimePeriodRecord(F_timeperiodInfo timePeriodInfo, string type, DateTime startDate)
        {
            var newTimePeriodRecord = timePeriodInfo.NewF_timeperiod();
            newTimePeriodRecord.Name = type;
            newTimePeriodRecord.Startdate = startDate;
            newTimePeriodRecord.Enddate = new DateTime(startDate.Year, startDate.Month, 1).AddMonths(1).AddDays(-1);
            newTimePeriodRecord.O_status = F_timeperiodStatic.DefaultValues.Open;
            newTimePeriodRecord.Paymentamount = 0.ToString();
            newTimePeriodRecord.Processed = false.ToString();

            newTimePeriodRecord.SaveRecord();
            return newTimePeriodRecord;
        }

        public static void CreateNextTriggerNotificationRecord(AEventHelper eventHelper, DateTime today, DateTime nextTriggerDate, string notificationType, string timePeriodType)
        {
            var timePeriodInfo = new F_timeperiodInfo(eventHelper);
            var timePeriodRecord = CheckForTimePeriodRecord(timePeriodInfo, today, timePeriodType);
            if (timePeriodRecord == null)
            {
                timePeriodRecord = CreateNewTimePeriodRecord(timePeriodInfo, timePeriodType, nextTriggerDate);
            }

            // Create next notification trigger record
            var notificationTriggerInfo = new NotificationtriggerInfo(eventHelper);

            var nextBackToSchoolRunNotification = notificationTriggerInfo.NewNotificationtrigger();
            nextBackToSchoolRunNotification.Notificationtype = notificationType;
            nextBackToSchoolRunNotification.Startdate = today;
            nextBackToSchoolRunNotification.Nexttriggerdate = nextTriggerDate;
            nextBackToSchoolRunNotification.Sourcedatalist = NotificationtriggerStatic.SystemName;
            nextBackToSchoolRunNotification.Sourcerecordinstanceid = timePeriodRecord.RecordInstanceID.ToString();
            nextBackToSchoolRunNotification.Targetdatalist = F_timeperiodStatic.SystemName;
            nextBackToSchoolRunNotification.Targetrecordinstanceid = timePeriodRecord.RecordInstanceID.ToString();
            nextBackToSchoolRunNotification.Active = NotificationtriggerStatic.DefaultValues.Yes;
            nextBackToSchoolRunNotification.SaveRecord();
        }

        public static IEnumerable<F_fundbalances> GetFundBalanceRecords(AEventHelper eventHelper, Persons person, F_fund fundRecord, bool isChildSpecific)
        {
            var fundBalanceInfo = new F_fundbalancesInfo(eventHelper);
            IEnumerable<F_fundbalances> fundBalancesRecords = null;

            if (isChildSpecific)
            {
                var personFundFilter = fundBalanceInfo.CreateFilter(F_fundbalancesStatic.SystemNames.Child, new List<string> { person.RecordInstanceID.ToString() });
                var fundBalanceFilter = fundBalanceInfo.CreateFilter(F_fundbalancesStatic.SystemNames.Fund, new List<string> { fundRecord.RecordInstanceID.ToString() });
                fundBalancesRecords = fundBalanceInfo.CreateQuery(new List<DirectSQLFieldFilterData> { personFundFilter, fundBalanceFilter });
            }

            else
            {
                var fundBalanceFilter = fundBalanceInfo.CreateFilter(F_fundbalancesStatic.SystemNames.Fund, new List<string> { fundRecord.RecordInstanceID.ToString() });
                fundBalancesRecords = fundBalanceInfo.CreateQuery(new List<DirectSQLFieldFilterData> { fundBalanceFilter });
            }

            return fundBalancesRecords;
        }

        public static void HandleIVEEligibleFund(AEventHelper eventHelper, UserData triggeringUser, F_serviceplanutilization serviceUtilization, F_fundallocation fundAllocation, DateTime today, List<Persons> persons, Countylist county,
            Placements placement, ref int fundsUsedCounter, ref double initialAmountOwed, ref double remainingServiceAmountOwed, Underpayments underPaymentRecord = null)
        {
            var percentToPay = fundAllocation.Percentage;
            var firstNumberOfDays = int.TryParse(fundAllocation.Firstnumberofdays, out var numberOFDays) ? numberOFDays : 0;
            if (firstNumberOfDays == 0)
            {
                // Get Fund
                var fundRecord = fundAllocation.Fund();
                if (fundRecord == null) { return; }

                var startDate = fundRecord.Startdate;
                var endDate = fundRecord.Enddate;
                if (!(today >= startDate && (endDate == null || today < endDate))) { return; }

                if (string.IsNullOrWhiteSpace(percentToPay)) percentToPay = 100.ToString();
                HandleFundAllocations(eventHelper, triggeringUser, fundRecord, persons, serviceUtilization, county, fundAllocation, percentToPay, ref initialAmountOwed, ref remainingServiceAmountOwed, ref fundsUsedCounter, 0, underPaymentRecord);
            }
            else
            {
                // determine number of days to be used for this fund
                int numberOfDays = DetermineNumberOfDays(eventHelper, placement, serviceUtilization, firstNumberOfDays);
                if (numberOfDays == 0)
                {
                    HandleFundAllocations(eventHelper, triggeringUser, fundAllocation.Fund(), persons, serviceUtilization, county, fundAllocation, percentToPay, ref initialAmountOwed, ref remainingServiceAmountOwed, ref fundsUsedCounter, 0, underPaymentRecord);
                }
                else
                {
                    // Use this specific fund for the number of days
                    var dailyRate = DetermineDailyRate(serviceUtilization);
                    var specificAmount = dailyRate * numberOfDays;
                    HandleFundAllocations(eventHelper, triggeringUser, fundAllocation.Fund(), persons, serviceUtilization, county, fundAllocation, percentToPay, ref initialAmountOwed, ref remainingServiceAmountOwed, ref fundsUsedCounter, specificAmount, underPaymentRecord);
                }
            }
        }

        public static void HandleIVEReimbursibleFund(AEventHelper eventHelper, UserData triggeringUser, F_serviceplanutilization serviceUtilization, F_fundallocation fundAllocation, List<Persons> persons, Countylist county,
            Iveeligibility iVEEligibility, double maximumTitleIVEReimursableAmount, ref int fundsUsedCounter, ref double initialAmountOwed, ref double remainingServiceAmountOwed, Underpayments underPaymentRecord = null)
        {
            var percentToPay = fundAllocation.Percentage;
            double percent = double.TryParse(percentToPay, out var result) ? result : 0;
            var specificAmount = 0.0;
            if (maximumTitleIVEReimursableAmount > 0)
            {
                // take percent of the max, check if it is less than the remaining amount owed
                double iVEPortion = maximumTitleIVEReimursableAmount * percent;
                if (iVEPortion < remainingServiceAmountOwed)
                {
                    // if less, use that amount
                    specificAmount = iVEPortion;
                }
            }
            // check how many days in the util are covered using start and end dates
            var daysValid = DetermineDaysValid(serviceUtilization.Startdate, serviceUtilization.Enddate, iVEEligibility.Reimbursementstartdate, iVEEligibility.Nonreimdate);
            if (daysValid <= 0)
            {
                eventHelper.AddWarningLog("No valid days for reimbursement found in Service Utilization: {serviceUtilization.RecordInstanceID}. Skipping fund allocation: {fundAllocation}");
                return;
            }

            var daysInMonth = DateTime.DaysInMonth(serviceUtilization.Startdate.Value.Year, serviceUtilization.Startdate.Value.Month);
            if (daysValid < daysInMonth)
            {
                // Use this specific fund for the number of days (only if the number of days is less than the number of days in the month)
                var dailyRate = DetermineDailyRate(serviceUtilization);
                specificAmount = dailyRate * daysValid;
            }

            HandleFundAllocations(eventHelper, triggeringUser, fundAllocation.Fund(), persons, serviceUtilization, county, fundAllocation, percentToPay, ref initialAmountOwed, ref remainingServiceAmountOwed, ref fundsUsedCounter, specificAmount, underPaymentRecord);

            return;
        }

        public static void HandleIVEReimbursibleFMAPFund(AEventHelper eventHelper, UserData triggeringUser, F_serviceplanutilization serviceUtilization, F_fundallocation fundAllocation, List<Persons> persons, Countylist county,
            Ctfmaprates ctFMAPRecord, double maximumTitleIVEReimursableAmount, ref int fundsUsedCounter, ref double initialAmountOwed, ref double remainingServiceAmountOwed, Underpayments underPaymentRecord = null)
        {
            var percentToPay = ctFMAPRecord.Fmappercentage;
            double percent = double.TryParse(percentToPay, out var result) ? result : 0;
            var specificAmount = 0.0;
            if (maximumTitleIVEReimursableAmount > 0)
            {
                // take percent of the max, check if it is less than the remaining amount owed
                double iVEPortion = (maximumTitleIVEReimursableAmount * percent) / 100;
                if (iVEPortion < remainingServiceAmountOwed)
                {
                    specificAmount = maximumTitleIVEReimursableAmount;
                }
            }

            HandleFundAllocations(eventHelper, triggeringUser, fundAllocation.Fund(), persons, serviceUtilization, county, fundAllocation, percentToPay, ref initialAmountOwed, ref remainingServiceAmountOwed, ref fundsUsedCounter, specificAmount);
        }

        public static void HandleIVEReimbursibleTribalFMAPFund(AEventHelper eventHelper, UserData triggeringUser, F_serviceplanutilization serviceUtilization, F_fundallocation fundAllocation, List<Persons> persons, Countylist county,
            Cttribalfmaprates ctTribalFMAPRecord, double maximumTitleIVEReimursableAmount, ref int fundsUsedCounter, ref double initialAmountOwed, ref double remainingServiceAmountOwed, Underpayments underPaymentRecord = null)
        {
            var percentToPay = ctTribalFMAPRecord.Tribalfmappercentage;
            double percent = double.TryParse(percentToPay, out var result) ? result : 0;
            var specificAmount = 0.0;
            if (maximumTitleIVEReimursableAmount > 0)
            {
                // take percent of the max, check if it is less than the remaining amount owed
                double iVEPortion = (maximumTitleIVEReimursableAmount * percent) / 100;
                if (iVEPortion < remainingServiceAmountOwed)
                {
                    // if less, use that amount
                    specificAmount = iVEPortion;
                }
            }
            HandleFundAllocations(eventHelper, triggeringUser, fundAllocation.Fund(), persons, serviceUtilization, county, fundAllocation, percentToPay, ref initialAmountOwed, ref remainingServiceAmountOwed, ref fundsUsedCounter, specificAmount);
        }

        public static void HandleIVEReimbursibleSignedVSSAFund(AEventHelper eventHelper, UserData triggeringUser, F_serviceplanutilization serviceUtilization, F_fundallocation fundAllocation, List<Persons> persons, Countylist county, Placements placement,
            Iveeligibility iVEEligibility, double maximumTitleIVEReimursableAmount, ref int fundsUsedCounter, ref double initialAmountOwed, ref double remainingServiceAmountOwed, Underpayments underPaymentRecord = null)
        {
            var percentToPay = fundAllocation.Percentage;
            double percent = double.TryParse(percentToPay, out var result) ? result : 0;
            var specificAmount = 0.0;
            if (maximumTitleIVEReimursableAmount > 0)
            {
                // take percent of the max, check if it is less than the remaining amount owed
                double iVEPortion = (maximumTitleIVEReimursableAmount * percent) / 100;
                if (iVEPortion < remainingServiceAmountOwed)
                {
                    // if less, use that amount
                    specificAmount = iVEPortion;
                }
            }
            // check how many days in the util are covered using start and end dates
            var daysValid = DetermineDaysValid(serviceUtilization.Startdate, serviceUtilization.Enddate, iVEEligibility.Reimbursementstartdate, iVEEligibility.Nonreimdate);
            if (daysValid <= 0)
            {
                eventHelper.AddWarningLog("No valid days for reimbursement found in Service Utilization: {serviceUtilization.RecordInstanceID}. Skipping fund allocation: {fundAllocation}");
                return;
            }

            // Check for signed VSSA 
            if (HasSignedVSSA(placement))
            {
                // Use this specific fund for the number of days
                var dailyRate = DetermineDailyRate(serviceUtilization);
                specificAmount = dailyRate * daysValid;
                HandleFundAllocations(eventHelper, triggeringUser, fundAllocation.Fund(), persons, serviceUtilization, county, fundAllocation, percentToPay, ref initialAmountOwed, ref remainingServiceAmountOwed, ref fundsUsedCounter, specificAmount, underPaymentRecord);
            }
            return;
        }

        public static IOrderedEnumerable<F_fundallocation> GetFundAllocations (F_serviceplanutilization serviceUtilization, F_servicecatalog serviceCatalogRecord)
        {
            // get funding model on utilization start date
            // Service Catalog Funding Model start date will alsways be 1st on month and end date (when populated) will be last day of the month
            // Service Utiliziation dates will fall withing a single month, so we should only get 1 record
            var serviceUtilStartDate = serviceUtilization.Startdate;
            var serviceUtilEndDate = serviceUtilization.Enddate;
            var serviceCatalogFundingModelRecord = serviceCatalogRecord.GetChildrenF_servicecatalogfundingmodel().Where(scfm => scfm.Startdate <= serviceUtilStartDate
                && (scfm.Enddate == null || scfm.Enddate >= serviceUtilStartDate)).FirstOrDefault();

            if (serviceCatalogFundingModelRecord == null)
            {
                return null;
            }

            var fundingModel = serviceCatalogFundingModelRecord.Fundsource();

            // Fund Allocation can be edn dated, however start date must be 1st of month and end date last day of month
            var fundAllocationRecords = fundingModel.GetChildrenF_fundallocation()
                .Where(scfm => scfm.Startdate <= serviceUtilStartDate && (scfm.Enddate == null || scfm.Enddate >= serviceUtilStartDate))
                .OrderBy(rec => rec.Priority);

            return fundAllocationRecords;
        }

        public static Iveeligibility IsChildIVEEligible(AEventHelper eventHelper, F_serviceplanutilization serviceUtilization)
        {
            var caseRecord = serviceUtilization.Case();
            if (caseRecord == null)
            {
                eventHelper.AddErrorLog("Unable to determine if child is IVE eligible, case record is null.");
                return null;
            }

            var persons = serviceUtilization.Participant();
            var personRecordIDs = new List<string>();
            foreach (var person in persons)
            {
                personRecordIDs.Add(person.RecordInstanceID.ToString());
            }

            var caseParticipantInfo = new CaseparticipantsInfo(eventHelper);
            var caseParticipantDataListID = eventHelper.GetDataListID(CaseparticipantsStatic.SystemName);
            var personFilter = new List<DirectSQLFieldFilterData>
            {
                caseParticipantInfo.CreateFilter(CaseparticipantsStatic.SystemNames.Caseparticipantname, personRecordIDs)
            };

            var caseParticipant = eventHelper.SearchSingleDataListSQLProcess(caseParticipantDataListID.Value, personFilter, caseRecord.RecordInstanceID)
                .FirstOrDefault();

            if (caseParticipant == null)
            {
                eventHelper.AddWarningLog("No Case Participant found for related case, Unable to determine if child is IVE Eligible.");
                return null;
            }

            var iveEligibilityInfo = new IveeligibilityInfo(eventHelper);
            var iveEligibilityDataListID = eventHelper.GetDataListID(IveeligibilityStatic.SystemName);

            var participantFilter = new List<DirectSQLFieldFilterData>
            {
                iveEligibilityInfo.CreateFilter(IveeligibilityStatic.SystemNames.Caseparticipant, new List<string> { caseParticipant.RecordInstanceID.ToString() })
            };

            var iveEligibilityRecord = eventHelper.SearchSingleDataListSQLProcess(iveEligibilityDataListID.Value, participantFilter, caseRecord.RecordInstanceID)
                .ToList()
                .OrderByDescending(r => r.GetFieldValue(IveeligibilityStatic.SystemNames.Dateofentry))
                .FirstOrDefault();

            var iveEligibility = iveEligibilityRecord as Iveeligibility;
            if (iveEligibility == null)
            {
                eventHelper.AddWarningLog("No IVE Eligibility found for related case participant, Unable to determine if child is IVE Eligible.");
                return null;
            }

            var status = iveEligibility.Eligibilitystatus;
            var determination = iveEligibility.Newdetermination;

            if (status == IveeligibilityStatic.DefaultValues.Iv_eineligible ||
                (string.IsNullOrWhiteSpace(status) &&
                 (string.IsNullOrWhiteSpace(determination) || determination == IveeligibilityStatic.DefaultValues.Iv_eineligible)))
            {
                eventHelper.AddWarningLog("IVE Eligibility Determination is not set or indicates ineligibility. Assuming ineligible.");
                return null;
            }

            return iveEligibility;
        }

        public static string HandleFundAllocations(AEventHelper eventHelper, UserData triggeringUser, F_fund fundRecord, List<Persons> persons, F_serviceplanutilization serviceUtilization, Countylist county, F_fundallocation fundAllocRecord,
            string percentToPay, ref double intialAmountOwed, ref double remainingServiceAmountOwed, ref int fundsUsedCounter, double specificAmount = 0, Underpayments underPaymentRecord = null)
        {
            var errorMsg = string.Empty;
            int runningPercentage = 0;
            bool isCapped = fundRecord.Cappedfund == F_fundStatic.DefaultValues.Yes;
            var isChildSpecific = fundRecord.Childspecificfund == F_fundStatic.DefaultValues.Yes;

            var fundBalancesRecords = GetFundBalanceRecords(eventHelper, persons.FirstOrDefault(), fundRecord, isChildSpecific);
            if (fundBalancesRecords.Count() == 0)
            {
                // No Fund Balance Records found, create a blank Initial Fund Distribution record with an error message
                errorMsg = "No Fund Balance Records found for the Fund Allocation - Issue must be resolved.";
                eventHelper.AddWarningLog("errorMsg");
                return errorMsg;
            }

            foreach (var fundBalance in fundBalancesRecords)
            {
                double requestedAmount = (double.Parse(percentToPay) / 100.0) * intialAmountOwed;
                if (specificAmount != 0)
                {
                    requestedAmount = (double.Parse(percentToPay) / 100.0) * specificAmount;
                }
                double amountToUse = Math.Min(requestedAmount, remainingServiceAmountOwed);
                double amountTaken;
                double actualPercentUsed;

                if (isCapped || isChildSpecific)
                {
                    if (!double.TryParse(fundBalance.Balance, out double balance) || balance <= 0)
                    {
                        continue;
                    }
                    // Fund can fully pay the requested percentage
                    if (balance >= amountToUse)
                    {
                        amountTaken = Math.Round(amountToUse, 2);
                    }
                    // Fund can't cover the full requested percentage, take what you can
                    else
                    {
                        amountTaken = Math.Round(balance, 2);
                    }

                    actualPercentUsed = Math.Round((amountTaken / intialAmountOwed) * 100.0, 2);
                    remainingServiceAmountOwed = Math.Round(remainingServiceAmountOwed - amountTaken, 2);
                    runningPercentage += (int)Math.Round((amountTaken / intialAmountOwed) * 100);

                    // Deduct from balance
                    double newFundBalance = balance - amountTaken;
                    // Update the Fund Balance record with the updated balance
                    fundBalance.Balance = newFundBalance.ToString();
                    fundBalance.SaveRecord();
                }
                else
                {
                    if (specificAmount != 0)
                    {
                        amountTaken = Math.Round(amountToUse, 2);
                        actualPercentUsed = Math.Round((amountTaken / intialAmountOwed) * 100.0, 2);
                        remainingServiceAmountOwed = Math.Round(remainingServiceAmountOwed - amountTaken, 2);
                    }
                    else
                    {
                        amountTaken = Math.Round(amountToUse, 2);
                        actualPercentUsed = Math.Round((amountTaken / intialAmountOwed) * 100.0, 2);
                        remainingServiceAmountOwed = Math.Round(remainingServiceAmountOwed - amountTaken, 2);
                    }
                }

                // Update number of funds used
                fundsUsedCounter++;

                // Create Initial Fund Distributions record for fund
                var intitialFundRecord = new F_initialfunddistributionsInfo(eventHelper);
                var newInitialFundDistRecord = intitialFundRecord.NewF_initialfunddistributions();

                if (underPaymentRecord != null)
                {
                    newInitialFundDistRecord.ParentRecordID = underPaymentRecord.RecordInstanceID;
                }
                else
                {
                    newInitialFundDistRecord.ParentRecordID = serviceUtilization.RecordInstanceID;
                }
                newInitialFundDistRecord.Fund(fundRecord);
                newInitialFundDistRecord.County(county);
                newInitialFundDistRecord.Person(persons);
                newInitialFundDistRecord.Linenumber = fundsUsedCounter.ToString();
                newInitialFundDistRecord.Amount = amountTaken.ToString("F2");
                newInitialFundDistRecord.Percentage = actualPercentUsed.ToString();
                newInitialFundDistRecord.Percentagetype = fundAllocRecord.Percentagetype;
                newInitialFundDistRecord.Iveeligible = fundAllocRecord.Iveeligible;
                newInitialFundDistRecord.SaveRecord();

                #region Call Finance Gateway to send Committed funds to Gateway
                // set up message
                ManageFundsMessage fundMessage = new ManageFundsMessage()
                {
                    RecordId = newInitialFundDistRecord.RecordInstanceID.ToString(),
                    ModifiedBy = triggeringUser.UserName
                };

                AMessage aMessage = new AMessage()
                {
                    Action = NMFinancialConstants.ActionTypes.CommitFunds,
                    Data = fundMessage
                };

                _ = FinanceServices.MakePostRestCall(aMessage, eventHelper);
                #endregion

            }
            return errorMsg;
        }

        public static int DetermineNumberOfDays(AEventHelper eventHelper, Placements placement, F_serviceplanutilization serviceUtil, int firstNumberOfDays)
        {
            // Validate required dates
            if (serviceUtil?.Startdate == null || serviceUtil.Enddate == null)
            {
                eventHelper.AddErrorLog("Service Utilization is missing a start or end date.");
                return 0;
            }

            var utilStartDate = serviceUtil.Startdate.Value;
            var utilEndDate = serviceUtil.Enddate.Value;

            int utilDays = (int)(utilEndDate - utilStartDate).TotalDays;
            if (placement == null)
            {
                eventHelper.AddErrorLog("Service Utilization has no placements. Please resolve.");
                return 0;
            }
            DateTime? placementStartDate = placement.Placementdate;
            if (placementStartDate != null)
            {
                var daysCovered = placementStartDate.Value.AddDays(firstNumberOfDays);
                if (daysCovered >= utilStartDate)
                {
                    var numberOfDays = (int)(daysCovered - utilStartDate).TotalDays;
                    return numberOfDays;
                }
            }

            return 0;
        }

        public static void CreateBlankInitialFundDisRecord(AEventHelper eventHelper, F_serviceplanutilization serviceUtilization, string errorMsg, ref int fundsUsedCounter, Underpayments underpaymentRecord = null)
        {
            // Create Blank Initial Fund Distributions record for fund with error message
            var intitialFundRecord = new F_initialfunddistributionsInfo(eventHelper);
            var blankInitFundRecord = intitialFundRecord.NewF_initialfunddistributions();
            if (underpaymentRecord != null)
            {
                blankInitFundRecord.ParentRecordID = underpaymentRecord.RecordInstanceID;
            }
            else
            {
                blankInitFundRecord.ParentRecordID = serviceUtilization.RecordInstanceID;
            }

            blankInitFundRecord.Linenumber = (++fundsUsedCounter).ToString();
            blankInitFundRecord.Errorbool = true.ToString();
            blankInitFundRecord.Errormessage = errorMsg;
            blankInitFundRecord.SaveRecord();
        }

        public static double DetermineDailyRate(F_serviceplanutilization serviceUtilization)
        {
            var occurrence = serviceUtilization.Rateoccurrence;
            if (occurrence == F_serviceplanutilizationStatic.DefaultValues.Daily)
            {
                if (double.TryParse(serviceUtilization.Rate, out double dailyRate))
                {
                    return dailyRate;
                }
            }
            var totalbillable = Convert.ToDouble(serviceUtilization.Totalbillableamount);
            var lastDay = DateTime.DaysInMonth(serviceUtilization.Enddate.Value.Year, serviceUtilization.Enddate.Value.Month);
            if (serviceUtilization.Startdate.Value.Day == 1 && serviceUtilization.Enddate.Value.Day == lastDay)
            {
                // if full month, return 0 so that spesific amount = 0 which means total billabel amount will be used
                return 0;
            }
            var daysInUtil = (serviceUtilization.Enddate.Value - serviceUtilization.Startdate.Value).Days + 1;
            return totalbillable / daysInUtil;
        }

        public static int DetermineDaysValid(DateTime? utilStartDate, DateTime? utilEndDate, DateTime? reimbursStartDate, DateTime? reimbursEndDate)
        {

            if (!reimbursEndDate.HasValue)
            {
                reimbursEndDate = DateTime.MaxValue;
            }
            if (!utilStartDate.HasValue || !utilEndDate.HasValue || !reimbursStartDate.HasValue || !reimbursEndDate.HasValue)
            {
                return 0;
            }

            var utilStart = utilStartDate.Value;
            var utilEnd = utilEndDate.Value;
            var reimburseStart = reimbursStartDate.Value;
            var reimburseEnd = reimbursEndDate.Value;

            // Calculate overlap
            var overlapStart = utilStart > reimburseStart ? utilStart : reimburseStart;
            var overlapEnd = utilEnd < reimburseEnd ? utilEnd : reimburseEnd;

            if (overlapStart > overlapEnd)
            {
                return 0;
            }

            // Return inclusive day count
            return (overlapEnd - overlapStart).Days + 1;
        }

        public static Placements GetPlacement(F_serviceplanutilization serviceUtilization)
        {
            var casePlacement = serviceUtilization.Placementcase();
            var invPlacement = serviceUtilization.Placementinv();
            if (casePlacement != null)
            {
                return casePlacement;
            }
            else if (invPlacement != null)
            {
                return invPlacement;
            }
            else
            {
                return null;
            }
        }
        public static bool HasSignedVSSA(Placements placement)
        {
            var vSSA = placement.GetChildrenVoluntary()
                .Where(r => r.O_status == VoluntaryStatic.DefaultValues.Approved);

            if (vSSA.Any())
            {
                return true;
            }
            return false;
        }

        public static Persons GetChildFromPlacement(Placements placement)
        {
            Persons child;
            var caseParticipant = placement.Childyouth();
            var investigationParticipant = placement.Childyouthinv();

            if (caseParticipant != null)
            {
                child = caseParticipant.Caseparticipantname();
                return child;
            }
            else if (investigationParticipant != null)
            {
                child = investigationParticipant.Invparticipantname();
                return child;
            }
            return null;
        }

        public static RecordInstanceData GetChildFromPlacementPreSave(AEventHelper eventHelper, RecordInstanceData placement)
        {
            RecordInstanceData child = null;
            var caseParticipant = placement.GetFieldValue(PlacementsStatic.SystemNames.Childyouth);
            var investigationParticipant = placement.GetFieldValue(PlacementsStatic.SystemNames.Childyouthinv);

            if (!string.IsNullOrWhiteSpace(caseParticipant))
            {
                var caseParticipantRecord = eventHelper.GetActiveRecordById(long.Parse(caseParticipant));
                var childID = caseParticipantRecord?.GetFieldValue(CaseparticipantsStatic.SystemNames.Caseparticipantname);
                if (!string.IsNullOrWhiteSpace(childID))
                {
                    child = eventHelper.GetActiveRecordById(long.Parse(childID));
                    return child;
                }
            }
            else if (!string.IsNullOrWhiteSpace(investigationParticipant))
            {
                var invParticipantRecord = eventHelper.GetActiveRecordById(long.Parse(investigationParticipant));
                var childID = invParticipantRecord?.GetFieldValue(InvestigationparticipantsStatic.SystemNames.Invparticipantname);
                if (!string.IsNullOrWhiteSpace(childID))
                {
                    child = eventHelper.GetActiveRecordById(long.Parse(childID));
                    return child;
                }
            }
            return child;
        }

        public static Ctfmaprates GetCTFMAPRateRecord(AEventHelper eventHelper, F_serviceplanutilization serviceUtilization, F_fundallocation fundAllocation)
        {
            var ctFMapInfo = new CtfmapratesInfo(eventHelper);

            var parsedStartDate = serviceUtilization.Startdate ?? DateTime.MinValue;
            var parsedEndDate = serviceUtilization.Enddate ?? DateTime.MaxValue;

            var startDateFilter = ctFMapInfo.CreateFilter(CtfmapratesStatic.SystemNames.Startdate, null, parsedEndDate.ToString("yyyy-MM-dd"));
            var endDateFilter = ctFMapInfo.CreateFilter(CtfmapratesStatic.SystemNames.Enddate, parsedStartDate.ToString("yyyy-MM-dd"), null);

            var ctFMAPRecords = ctFMapInfo.CreateQuery(new List<DirectSQLFieldFilterData> { startDateFilter, endDateFilter }).ToList();

            if (ctFMAPRecords.Count != 1)
            {
                eventHelper.AddWarningLog($"No valid CT FMAP rate found for Service Utilization: {serviceUtilization.RecordInstanceID}. Skipping fund allocation: {fundAllocation}");
                return null;
            }

            return ctFMAPRecords.FirstOrDefault();
        }

        public static Cttribalfmaprates GetCTTribalFMAPRateRecord(AEventHelper eventHelper, F_serviceplanutilization serviceUtilization, F_fundallocation fundAllocation)
        {
            var ctTribalFMapInfo = new CttribalfmapratesInfo(eventHelper);

            var parsedStartDate = serviceUtilization.Startdate ?? DateTime.MinValue;
            var parsedEndDate = serviceUtilization.Enddate ?? DateTime.MaxValue;

            var startDateFilter = ctTribalFMapInfo.CreateFilter(CttribalfmapratesStatic.SystemNames.Startdate, null, parsedEndDate.ToString("yyyy-MM-dd"));
            var endDateFilter = ctTribalFMapInfo.CreateFilter(CttribalfmapratesStatic.SystemNames.Enddate, parsedStartDate.ToString("yyyy-MM-dd"), null);

            var ctTribalFMAPRecords = ctTribalFMapInfo.CreateQuery(new List<DirectSQLFieldFilterData> { startDateFilter, endDateFilter }).ToList();

            if (ctTribalFMAPRecords.Count != 1)
            {
                eventHelper.AddWarningLog($"No valid CT Tribal FMAP rate found for Service Utilization: {serviceUtilization.RecordInstanceID}. Skipping fund allocation: {fundAllocation}");
                return null;
            }

            return ctTribalFMAPRecords.FirstOrDefault();
        }

        public static (F_servicecatalog, F_providerservice) ValidateProviderOffersRequiredService(AEventHelper eventHelper, RecordInstanceData placementRecord, long parentRecordID, List<string> validationMessages, bool checkProviderService = true, string levelOfCare = "")
        {
            var placementSetting = placementRecord.GetFieldValue(PlacementsStatic.SystemNames.Placementsetting);
            var placementType = placementRecord.GetFieldValue(PlacementsStatic.SystemNames.Placementtype);

            var providerID = placementRecord.GetFieldValue<long>(PlacementsStatic.SystemNames.Provider);
            var providerRecord = eventHelper.GetActiveRecordById(providerID);

            Providers provider = new Providers(providerRecord, eventHelper);
            // check for TFC resource home and set provider to tfc Agency since the agency will be paid
            var tfcAgencyProvider = GetTFCAgencyProvider(eventHelper, provider);
            if (tfcAgencyProvider != null)
            {
                providerRecord = tfcAgencyProvider;
            }

            var personRecord = GetChildFromPlacementPreSave(eventHelper, placementRecord);
            var requiredService = string.Empty;

            if (!requiredServiceMap.TryGetValue(placementSetting, out var entries))
            {
                validationMessages.Add(string.Format(NMFinancialConstants.ErrorMessages.inValidCombination, placementSetting, placementType));
                return (null, null);
            }

            var match = entries.FirstOrDefault(e => e.PlacementTypes.Contains(placementType));
            var matches = entries.Where(e => e.PlacementTypes.Contains(placementType)).ToList();

            if (matches.Count() == 0)
            {
                // if no mapping found it means we do not pay the Provider
               // validationMessages.Add(string.Format(NMFinancialConstants.ErrorMessages.inValidCombination, placementSetting, placementType));
                return (null, null);
            }
            else if (matches.Count == 1)
            {
                requiredService = match.RequiredService;
            }
            else if (matches.Count() > 1)
            {
                switch (placementSetting)  
                {
                    case PlacementsStatic.DefaultValues.Extendedfostercaresetting:
                        requiredService = DetermineRequiredServiceForExtendedFosterCareSetting(eventHelper, placementRecord, personRecord, placementType, requiredService);
                        break;

                    case PlacementsStatic.DefaultValues.Subsidyandmedicaidplacements:
                        requiredService = DetermineRequiredServiceForSubsidyAndMedicaidPlacementSetting(eventHelper, personRecord, placementType, requiredService, parentRecordID);
                        break;

                    case PlacementsStatic.DefaultValues.Outofhomeplacement_familyhomesetting:
                        requiredService = DetermineRequiredServiceForOutOfHomeFamilyHomeSetting(eventHelper, placementRecord, requiredService, parentRecordID, levelOfCare);
                        break;

                    case PlacementsStatic.DefaultValues.Therapeuticsetting:
                        requiredService = DetermineRequiredServiceForTheraputicSetting(eventHelper, placementRecord, requiredService);
                        break;

                    case PlacementsStatic.DefaultValues.Outofstateplacementsetting:
                        requiredService = DetermineRequiredServiceForOutOfStatePlacementSetting(eventHelper, placementRecord, requiredService, parentRecordID);
                        break;

                    default:
                        break;
                }

            }

            if (string.IsNullOrEmpty(requiredService))
            {
                validationMessages.Add(string.Format(NMFinancialConstants.ErrorMessages.placementNotMapped, placementSetting, placementType));
                return (null, null);
            }

            // get the service catalog based on the unique code
            var serviceCatalogRecord = GetServiceCatalogByUniqueCode(eventHelper, requiredService);

            if (serviceCatalogRecord == null)
            {
                validationMessages.Add(string.Format(NMFinancialConstants.ErrorMessages.serviceCatalogNotFound, requiredService));
                return (null, null);
            }

            if (!checkProviderService)
                return (serviceCatalogRecord, null);

            var providerServiceRecord = GetProviderService(eventHelper, serviceCatalogRecord, providerRecord, placementRecord);
            if (providerServiceRecord == null)
            {
                validationMessages.Add(string.Format(NMFinancialConstants.ErrorMessages.serviceNotOffered, providerRecord.Label, serviceCatalogRecord.Nameofservice));
            }

            return (serviceCatalogRecord, providerServiceRecord);
        }

        public static F_providerservice GetProviderService(AEventHelper eventHelper, F_servicecatalog serviceCatalogRecord, RecordInstanceData providerRecord, RecordInstanceData placementRecord)
        {
            var providerServiceInfo = new F_providerserviceInfo(eventHelper);

            var psFilters = new List<DirectSQLFieldFilterData>
            {
                providerServiceInfo.CreateFilter(F_providerserviceStatic.SystemNames.Service, new List<string> { serviceCatalogRecord.RecordInstanceID.ToString() })
            };
            var providerServiceRecord = eventHelper.SearchSingleDataListSQLProcess(providerServiceInfo.GetDataListId(), psFilters, providerRecord.RecordInstanceID)
                .Where(r => string.IsNullOrEmpty(r.GetFieldValue(F_providerserviceStatic.SystemNames.Enddate))
                    || r.GetFieldValue<DateTime>(F_providerserviceStatic.SystemNames.Enddate) > placementRecord.GetFieldValue<DateTime>(PlacementsStatic.SystemNames.Placementdate))
                .Select(x => new F_providerservice(x, eventHelper))
                .FirstOrDefault();
            return providerServiceRecord;
        }

        public static F_servicecatalog GetServiceCatalogByUniqueCode(AEventHelper eventHelper, string requiredService)
        {
            var serviceCatalogInfo = new F_servicecatalogInfo(eventHelper);
            var scFilters = new List<DirectSQLFieldFilterData>
            {
                serviceCatalogInfo.CreateFilter(F_servicecatalogStatic.SystemNames.Uniquecode, new List<string> { requiredService })
            };
            var serviceCatalogRecord = serviceCatalogInfo.CreateQuery(scFilters)
                .FirstOrDefault();

            return serviceCatalogRecord;
        }

        public static string DetermineRequiredServiceForExtendedFosterCareSetting(AEventHelper eventHelper, RecordInstanceData placementRecord, RecordInstanceData child, string placementType, string requiredService)
        {
            if (placementType == PlacementsStatic.DefaultValues.Supervisedindependentliving)
            {
                // get pregnancy record
                (_, DateTime? dateReported) = GetDeliveryDate(eventHelper, placementRecord, child);

                // if delivery date is prior to placement start date, select pregnancy service
                var placementStartDate = placementRecord.GetFieldValue<DateTime>(PlacementsStatic.SystemNames.Placementdate);
                if (dateReported != null && dateReported <= placementStartDate)
                {
                    requiredService = NMFinancialConstants.ServiceCatalogServices.ExtendedFosterCarePregnantAndParentingYouth;
                }
                else
                {
                    requiredService = NMFinancialConstants.ServiceCatalogServices.ExtendedFosterCareBasicYouthAsPayee;
                }
            }
            return requiredService;
        }

        public static (DateTime?, DateTime?) GetDeliveryDate (AEventHelper eventHelper, RecordInstanceData placementRecord, RecordInstanceData child)
        {
            // get pregnancy record
            var pregnancyInfo = new PregnancyparentinginformationInfo(eventHelper);
            var pregnancyDatalist = eventHelper.GetDataListID(PregnancyparentinginformationStatic.SystemName);
            var pregnancyRecord = eventHelper.SearchSingleDataListSQLProcess(pregnancyDatalist.Value, new List<DirectSQLFieldFilterData> { }, child.RecordInstanceID)
                .OrderByDescending(r => r.CreatedOn)
                .FirstOrDefault();

            DateTime? deliveryDate = pregnancyRecord?.GetFieldValue<DateTime>(PregnancyparentinginformationStatic.SystemNames.Deliverydate);
            DateTime? dateReported = pregnancyRecord?.GetFieldValue<DateTime>(PregnancyparentinginformationStatic.SystemNames.Datepregnancyreported);

            return (deliveryDate, dateReported);
        }

        public static string DetermineRequiredServiceForSubsidyAndMedicaidPlacementSetting(AEventHelper eventHelper, RecordInstanceData personRecord, string placementType, string requiredService, long parentRecordID)
        {
            string titleIVEorState;
            string memberOfTribe = personRecord.GetFieldValue(PersonsStatic.SystemNames.Memberoftribe);

            if (placementType == PlacementsStatic.DefaultValues.Adoptionsubsidy)
            {
                var adoptionAssistanceDatalistID = eventHelper.GetDataListID(AdoptionassistanceStatic.SystemName);
                var adoptionInfo = new AdoptionassistanceagreementInfo(eventHelper);
                var agreementFilters = new List<DirectSQLFieldFilterData>
                {
                    adoptionInfo.CreateFilter(AdoptionassistanceagreementStatic.SystemNames.Child, new List<string> { personRecord.RecordInstanceID.ToString() }),
                    adoptionInfo.CreateFilter(AdoptionassistanceagreementStatic.SystemNames.Aaastatus, new List<string> { AdoptionassistanceagreementStatic.DefaultValues.Approved })
                };

                var agreementRecord = eventHelper.SearchSingleDataListSQLProcess(adoptionAssistanceDatalistID.Value, agreementFilters, parentRecordID)
                    .FirstOrDefault();

                var agreement = agreementRecord as Adoptionassistanceagreement;

                titleIVEorState = agreement?.Adptassistivestate;

                requiredService = GetRequiredService(titleIVEorState, memberOfTribe, AdoptionassistanceagreementStatic.DefaultValues.Titleiv_e, AdoptionassistanceagreementStatic.DefaultValues.State,
                    NMFinancialConstants.ServiceCatalogServices.IVETribalSubsidizedAdoptionPostDecree, NMFinancialConstants.ServiceCatalogServices.IVESubsidizedAdoptionPostDecree,
                    NMFinancialConstants.ServiceCatalogServices.StateTribalIGAAdoptionPostDecree, NMFinancialConstants.ServiceCatalogServices.StateSubsidizedAdoptionPostDecree);
            }
            else if (placementType == PlacementsStatic.DefaultValues.Guardianshipsubsidy)
            {
                var guardianshipAssistanceDLID = eventHelper.GetDataListID(GuardianshipassistanceagreementStatic.SystemName);
                var guardianshipInfo = new GuardianshipassistanceagreementInfo(eventHelper);
                var guardianshipFilters = new List<DirectSQLFieldFilterData>
                {
                    guardianshipInfo.CreateFilter(GuardianshipassistanceagreementStatic.SystemNames.Gaachildddd, new List<string> { personRecord.RecordInstanceID.ToString() }),
                    guardianshipInfo.CreateFilter(GuardianshipassistanceagreementStatic.SystemNames.Gaastatus, new List<string> { GuardianshipassistanceagreementStatic.DefaultValues.Approved })
                };

                var agreementRecord = eventHelper.SearchSingleDataListSQLProcess(guardianshipAssistanceDLID.Value, guardianshipFilters, parentRecordID)
                    .FirstOrDefault();
                var agreement = agreementRecord as Guardianshipassistanceagreement;

                titleIVEorState = agreement?.Gaaiveorstatedd;

                requiredService = GetRequiredService(titleIVEorState, memberOfTribe, GuardianshipassistanceagreementStatic.DefaultValues.Titleiv_e, GuardianshipassistanceagreementStatic.DefaultValues.State,
                    NMFinancialConstants.ServiceCatalogServices.GuardianshipSubsidyGapIVETribal, NMFinancialConstants.ServiceCatalogServices.GuardianshipSubsidyGapIVE,
                    NMFinancialConstants.ServiceCatalogServices.GuardianshipSubsidyStateTribal, NMFinancialConstants.ServiceCatalogServices.GuardianshipSubsidyState);
            }
            return requiredService;
        }

        public static bool DetermineIfOutOfState(AEventHelper eventHelper, RecordInstanceData placementRecord)
        {
            bool isOutOfState = false;
            // check for person on ICPC Participants Record
            var icpcParticipantInfo = new IcpcparticipantsInfo(eventHelper);
            var icpcPersonFilter = icpcParticipantInfo.CreateFilter(IcpcparticipantsStatic.SystemNames.Childperson, new List<string> { placementRecord.GetFieldValue(PlacementsStatic.SystemNames.Childtobeplaced).ToString() });
            var icpcParticipantRecords = icpcParticipantInfo.CreateQuery(new List<DirectSQLFieldFilterData> { icpcPersonFilter }).ToList();
            var icpcParticipantRecordInstanceIds = icpcParticipantRecords
                .Select(record => record.RecordInstanceID.ToString())
                .ToList();

            // check if child has ICPC Outgoing Record 
            var icpcOutgoing100BInfo = new Icpc_100boutgoingInfo(eventHelper);
            var icpcParticipantFilter = icpcOutgoing100BInfo.CreateFilter(Icpc_100boutgoingStatic.SystemNames.Childparticipant, icpcParticipantRecordInstanceIds);
            var icpcOutgoing100BRecord = icpcOutgoing100BInfo.CreateQuery(new List<DirectSQLFieldFilterData> { icpcParticipantFilter })
                .OrderByDescending(r => r.Icpc100bapprovaldate)
                .FirstOrDefault();
            if (icpcOutgoing100BRecord != null)
            {
                var placementStatus = icpcOutgoing100BRecord.Placementstatusdd;
                if (placementStatus != Icpc_100boutgoingStatic.DefaultValues.Compactplacementtermination)
                {
                    isOutOfState = true;
                }
            }

            return isOutOfState;
        }

        public static Adoptionassistance GetAdoptionDetailsRecord (AEventHelper eventHelper, RecordInstanceData placementRecord, long parentRecordID)
        {

            string childParticipantId = "";
            var parentDl = placementRecord.GetFieldValue(PlacementsStatic.SystemNames.Parentdl);

            if (parentDl.Equals(PlacementsStatic.DefaultValues.Case))
            {
                // get a case participant from Case
                childParticipantId = placementRecord.GetFieldValue(PlacementsStatic.SystemNames.Childyouth);
            }
            else if (parentDl.Equals(PlacementsStatic.DefaultValues.Investigation))
            {
                // get a case participant from Investigation
                childParticipantId = placementRecord.GetFieldValue(PlacementsStatic.SystemNames.Childyouthinv);
            }

            var adoptionDetailsDLID = eventHelper.GetDataListID(AdoptionassistanceStatic.SystemName);
            var adoptionDetailsInfo = new AdoptionassistanceInfo(eventHelper);
            var adoptionChildFilter = new List<DirectSQLFieldFilterData>
            {
                adoptionDetailsInfo.CreateFilter(AdoptionassistanceStatic.SystemNames.Child, new List<string> { childParticipantId })
            };
            var adoptionDetailsRecord = eventHelper.SearchSingleDataListSQLProcess(adoptionDetailsDLID.Value, adoptionChildFilter, parentRecordID)
                .FirstOrDefault();

            if (adoptionDetailsRecord != null && adoptionDetailsRecord.TryParseRecord(eventHelper, out Adoptionassistance adoptionDetails))
                return adoptionDetails;
            else
                return null;
        }

        public static string DetermineRequiredServiceForOutOfHomeFamilyHomeSetting(AEventHelper eventHelper, RecordInstanceData placementRecord, string requiredService, long parentRecordID, string levelOfCare = "")
        {
            bool isOutOfState = DetermineIfOutOfState(eventHelper, placementRecord);

            var placementLevel = levelOfCare;
            if (string.IsNullOrEmpty(levelOfCare))
                 placementLevel = placementRecord.GetFieldValue(PlacementsStatic.SystemNames.Placementslevoffcdd);

            // default to Level 1 if no LOC 
            if (string.IsNullOrEmpty(placementLevel))
                placementLevel = PlacementsStatic.DefaultValues.Level1;

            var placementStartDate = placementRecord.GetFieldValue<DateTime>(PlacementsStatic.SystemNames.Placementdate);

            // check for Adoption Details Card Catalog
            var adoptionDetailsRecord = GetAdoptionDetailsRecord(eventHelper, placementRecord, parentRecordID);

            var adoptionDecreeStatus = adoptionDetailsRecord?.Adccdecreestatus;
            var decreeDate = adoptionDetailsRecord?.Adccdecreedate;

            if (isOutOfState && adoptionDetailsRecord != null)
            {
                // a. check for Adoption Decree and Records Information section
                // b.record must be found and status == finalized.placement start date should be >= Decree Date
                if (adoptionDecreeStatus == AdoptionassistanceStatic.DefaultValues.Finalized && placementStartDate >= decreeDate)
                {
                    requiredService = NMFinancialConstants.ServiceCatalogServices.AdoptionPreDecreeOutOfState;
                }
            }

            else if (isOutOfState && adoptionDetailsRecord == null)
            {
                requiredService = NMFinancialConstants.ServiceCatalogServices.ResourceFamilyFosterCareOutOfState;
            }
            else if (!isOutOfState && adoptionDetailsRecord != null)
            {
                // a. check for Adoption Decree and Records Information section
                // b.record must be found and status == finalized.placement start date should be >= Decree Date
                if (placementLevel == PlacementsStatic.DefaultValues.Level1 && adoptionDecreeStatus == AdoptionassistanceStatic.DefaultValues.Finalized && placementStartDate >= decreeDate)
                {
                    requiredService = NMFinancialConstants.ServiceCatalogServices.AdoptionPreDecreeLevel1;
                }
                else if (placementLevel == PlacementsStatic.DefaultValues.Level2 && adoptionDecreeStatus == AdoptionassistanceStatic.DefaultValues.Finalized && placementStartDate >= decreeDate)
                {
                    requiredService = NMFinancialConstants.ServiceCatalogServices.AdoptionPreDecreeLevel2;
                }
                else if (placementLevel == PlacementsStatic.DefaultValues.Level3 && adoptionDecreeStatus == AdoptionassistanceStatic.DefaultValues.Finalized && placementStartDate >= decreeDate)
                {
                    requiredService = NMFinancialConstants.ServiceCatalogServices.AdoptionPreDecreeLevel3;
                }
            }
            else if (!isOutOfState && adoptionDetailsRecord == null)
            {
                if (placementLevel == PlacementsStatic.DefaultValues.Level1)
                {
                    requiredService = NMFinancialConstants.ServiceCatalogServices.ResourceFamilyFosterCareLevel1;
                }
                else if (placementLevel == PlacementsStatic.DefaultValues.Level2)
                {
                    requiredService = NMFinancialConstants.ServiceCatalogServices.ResourceFamilyFosterCareLevel2;
                }
                else if (placementLevel == PlacementsStatic.DefaultValues.Level3)
                {
                    requiredService = NMFinancialConstants.ServiceCatalogServices.ResourceFamilyFosterCareLevel3;
                }
            }
            return requiredService;
        }

       public static string DetermineRequiredServiceForOutOfStatePlacementSetting(AEventHelper eventHelper, RecordInstanceData placementRecord, string requiredService, long parentRecordID)
        {
            var placementStartDate = placementRecord.GetFieldValue<DateTime>(PlacementsStatic.SystemNames.Placementdate);

            // check for Adoption Details Card Catalog
            var adoptionDetailsRecord = GetAdoptionDetailsRecord(eventHelper, placementRecord, parentRecordID);

            if (adoptionDetailsRecord != null)
            {
                var adoptionDecreeStatus = adoptionDetailsRecord?.Adccdecreestatus;
                var decreeDate = adoptionDetailsRecord?.Adccdecreedate;

                // a. check for Adoption Decree and Records Information section
                // b.record must be found and status == finalized.placement start date should be >= Decree Date
                if (adoptionDecreeStatus == AdoptionassistanceStatic.DefaultValues.Finalized && placementStartDate >= decreeDate)
                {
                    requiredService = NMFinancialConstants.ServiceCatalogServices.AdoptionPreDecreeOutOfState;
                } else
                {
                    requiredService = NMFinancialConstants.ServiceCatalogServices.ResourceFamilyFosterCareOutOfState;
                }
            } else
            {
                requiredService = NMFinancialConstants.ServiceCatalogServices.ResourceFamilyFosterCareOutOfState;
            }

            return requiredService;
        }
        public static string DetermineRequiredServiceForTheraputicSetting(AEventHelper eventHelper, RecordInstanceData placementRecord, string requiredService)
        {
            // look at provider licesnse type to see accredited or non accredited
            var providerRecordId = placementRecord.GetFieldValue(PlacementsStatic.SystemNames.Provider);
            if (string.IsNullOrWhiteSpace(providerRecordId))
            {
                return requiredService;
            }
            var providerRecord = eventHelper.GetActiveRecordById(long.Parse(providerRecordId));
            var liscenseInfo = new LicensesInfo(eventHelper);
            var liscenseDatalistID = eventHelper.GetDataListID(LicensesStatic.SystemName);
            var licenseFilter = new List<DirectSQLFieldFilterData>
            {
                liscenseInfo.CreateFilter(LicensesStatic.SystemNames.Licensestatus1, new List<string> { LicensesStatic.DefaultValues.Approved })
            };
            var licenseRecord = eventHelper.SearchSingleDataListSQLProcess(liscenseDatalistID.Value, licenseFilter, providerRecord.RecordInstanceID)
                .Where(ri => ri.GetFieldValue<DateTime>(LicensesStatic.SystemNames.Issuedate) < placementRecord.GetFieldValue<DateTime>(PlacementsStatic.SystemNames.Placementdate))
                .FirstOrDefault();

            if (licenseRecord != null)
            {
                var type = licenseRecord.GetFieldValue(LicensesStatic.SystemNames.Type);
                // check if accredited:
                if (type == LicensesStatic.DefaultValues.Accreditedresidentialtreatmentcenters)
                {
                    requiredService = NMFinancialConstants.ServiceCatalogServices.CongregateCareRTCJHACOAccredited;
                }
                else if (type == LicensesStatic.DefaultValues.Non_accreditedresidentialtreatmentcenters)
                {
                    var outofState = licenseRecord.GetFieldValue(LicensesStatic.SystemNames.Outofstate);
                    if (outofState == LicensesStatic.DefaultValues.Yes)
                    {
                        requiredService = NMFinancialConstants.ServiceCatalogServices.OutOfStateCongregateCare;
                    }
                    else
                    {
                        requiredService = NMFinancialConstants.ServiceCatalogServices.CongregateCareQualifiedResidentialTreatmentProgram;
                    }
                }
            }

            return requiredService;
        }

        public static string GetRequiredService(string titleIVEorState, string memberOfTribe, string titleIVEConst,
            string stateConst, string iveTribalService, string iveService, string stateTribalService, string stateService)
        {
            bool isTribal = memberOfTribe == PersonsStatic.DefaultValues.Yes;

            if (titleIVEorState == titleIVEConst)
            {
                return isTribal ? iveTribalService : iveService;
            }
            else if (titleIVEorState == stateConst)
            {
                return isTribal ? stateTribalService : stateService;
            }

            return null;
        }

        public static F_serviceline FindRelatedServiceAuth(AEventHelper eventHelper, Persons personRecord, long parentRecordID)
        {
            IEnumerable<RecordInstanceData> serviceAuthorizationRecords;
            var serviceAuthDatalistID = eventHelper.GetDataListID(F_servicelineStatic.SystemName);
            var serviceAuthInfo = new F_servicelineInfo(eventHelper);
            var personFilter = new List<DirectSQLFieldFilterData>
            {
                serviceAuthInfo.CreateFilter(F_servicelineStatic.SystemNames.Participant, new List<string> { personRecord.RecordInstanceID.ToString() })
            };
            serviceAuthorizationRecords = eventHelper.SearchSingleDataListSQLProcess(serviceAuthDatalistID.Value, personFilter, parentRecordID)
                .OrderByDescending(r => r.CreatedOn);

            var serviceAuthRecord = serviceAuthorizationRecords.FirstOrDefault();

            if (serviceAuthRecord != null && serviceAuthRecord.TryParseRecord(eventHelper, out F_serviceline serviceAuthorization))
                return serviceAuthorization;

            return null;
        }

        public static void HandlePausePayment(AEventHelper eventHelper, UserData triggeringUser, F_serviceline serviceAuth, DateTime approvedDate)
        {
            var authEndDate = serviceAuth.Enddate;
            if (authEndDate != null && authEndDate <= approvedDate)
            {
                return;
            }
            serviceAuth.Enddate = approvedDate;
            serviceAuth.SaveRecord();

            // If endDate is in the previous month, call gateway to handle overpayment
            var currentDateTime = DateTime.Now;
            if (approvedDate.Date < new DateTime(currentDateTime.Year, currentDateTime.Month, 1))
            {
                HandleGatewayCall(eventHelper, triggeringUser, approvedDate, serviceAuth, NMFinancialConstants.ActionTypes.StopPayment);
            }

        }

        public static F_serviceline HandleReinstatePayment(AEventHelper eventHelper, F_serviceline existingAuth, Persons personRecord, DateTime approvedDate, RecordInstanceData inpParentRecord = null)
        {
            RecordInstanceData parentRecord = new RecordInstanceData();
            if (inpParentRecord == null)
            {
                 parentRecord = eventHelper.GetActiveRecordById(existingAuth.ParentRecordID.Value);
            } else
            {
                parentRecord = inpParentRecord;
            }
            var serviceAuthInfo = new F_servicelineInfo(eventHelper);
            var newServiceAuth = serviceAuthInfo.NewF_serviceline(parentRecord);

            newServiceAuth.CreatedOn = approvedDate;
            newServiceAuth.County(existingAuth.County());
            newServiceAuth.Participant(new List<Persons> { personRecord });
            newServiceAuth.Sourcerequesttype = existingAuth.Sourcerequesttype;
            newServiceAuth.Placement(existingAuth.Placement());
            newServiceAuth.Servicecategory = existingAuth.Servicecategory;
            newServiceAuth.Provider(existingAuth.Provider());
            newServiceAuth.Servicetype(existingAuth.Servicetype());
            newServiceAuth.Servicecatalogtype(existingAuth.Servicecatalogtype());
            newServiceAuth.Description = existingAuth.Description;
            newServiceAuth.Totalamount = existingAuth.Totalamount;
            newServiceAuth.Servicelinestatus = F_servicelineStatic.DefaultValues.Active;
            newServiceAuth.Recurrence = existingAuth.Recurrence;
            newServiceAuth.Startdate = approvedDate;
            // leave end date blank
            newServiceAuth.Unitsauthorized = existingAuth.Unitsauthorized;
            newServiceAuth.Parentdl = existingAuth.Parentdl;
            newServiceAuth.Dltype = existingAuth.Dltype;
            newServiceAuth.Locassessscore = existingAuth.Locassessscore;
            newServiceAuth.Placementslevoffcdd = existingAuth.Placementslevoffcdd;
            newServiceAuth.Tfchomeprovider(existingAuth.Tfchomeprovider());

            return newServiceAuth;
        }

        public static void HandleGatewayCallForReinstatement(AEventHelper eventHelper, UserData triggeringUser, F_serviceline newServiceAuth, DateTime startDate)
        {
            // If startDate date is in the previous month, call gateway to handle underpayment
            var currentDateTime = DateTime.Now;
            if (startDate < new DateTime(currentDateTime.Year, currentDateTime.Month, 1))
            {
                HandleGatewayCall(eventHelper, triggeringUser, startDate, newServiceAuth, NMFinancialConstants.ActionTypes.StartPayment);
            }
        }

        public static void HandleGatewayCall(AEventHelper eventHelper, UserData triggeringUser, DateTime suspensionOrReinstatementDate, F_serviceline serviceAuth, string gatewayCallType)
        {
            var provider = serviceAuth.Provider();
            switch (gatewayCallType)
            {
                case NMFinancialConstants.ActionTypes.StopPayment:
                    #region Call Gateway
                    // set up message
                    OverUnderMessage overUnderMessageSuspenstionTermination = new OverUnderMessage()
                    {
                        StartDate = suspensionOrReinstatementDate.ToString("yyyy-MM-dd"),
                        Previous = new OverUnderDetails()
                        {
                            ServiceAuthId = serviceAuth.RecordInstanceID,
                            ProviderId = provider.RecordInstanceID
                        },
                        ModifiedBy = triggeringUser.UserName
                    };

                    AMessage aMessage = new AMessage()
                    {
                        Action = NMFinancialConstants.ActionTypes.StopPayment,
                        Data = overUnderMessageSuspenstionTermination
                    };

                    _ = FinanceServices.MakePostRestCall(aMessage, eventHelper);

                    #endregion
                    break;

                case NMFinancialConstants.ActionTypes.StartPayment:
                    #region Call Gateway
                    // set up message
                    OverUnderMessage overUnderMessageReinstatement = new OverUnderMessage()
                    {
                        StartDate = suspensionOrReinstatementDate.ToString("yyyy-MM-dd"),
                        New = new OverUnderDetails()
                        {
                            ServiceAuthId = serviceAuth.RecordInstanceID,
                            ProviderId = provider.RecordInstanceID
                        },
                        ModifiedBy = triggeringUser.UserName
                    };

                    AMessage message = new AMessage()
                    {
                        Action = NMFinancialConstants.ActionTypes.StartPayment,
                        Data = overUnderMessageReinstatement
                    };

                    _ = FinanceServices.MakePostRestCall(message, eventHelper);

                    #endregion
                    break;
            }

        }

        public static (string, string) GetLevelOfCareAndScoreFromHistory(AEventHelper eventHelper, DateTime startDate, Placements placementRecord, F_serviceplanutilization serviceUtil)
        {

            var levelOfCare = serviceUtil.Placementslevoffcdd;
            var locScore = serviceUtil.Locassessscore;

            var locHistoryInfo = new PlacementlevelofcarehistoryInfo(eventHelper);
            var locHistoryRecord = eventHelper.SearchSingleDataListSQLProcess(locHistoryInfo.GetDataListId(), new List<DirectSQLFieldFilterData> { }, placementRecord.RecordInstanceID)
                .Where(r => r.GetFieldValue<DateTime>(PlacementlevelofcarehistoryStatic.SystemNames.Locstartdate) <= startDate
                && (r.GetFieldValue<DateTime>(PlacementlevelofcarehistoryStatic.SystemNames.Locenddate) == default
                || r.GetFieldValue<DateTime>(PlacementlevelofcarehistoryStatic.SystemNames.Locenddate) >= startDate))
                .FirstOrDefault();

            if (locHistoryRecord != null)
            {
                levelOfCare = locHistoryRecord.GetFieldValue(PlacementlevelofcarehistoryStatic.SystemNames.Loc);
                locScore = locHistoryRecord.GetFieldValue(PlacementlevelofcarehistoryStatic.SystemNames.Locassessscore);
            }

            return (levelOfCare, locScore);
        }

	    protected static string inValidRateOccurrence = "Rate occurrence is not valid. Expected Daily or Monthly.";
        public static (double, int, string) HandleRates(AEventHelper eventHelper, DateTime personDOB, List<F_servicerate> rateRecords, DateTime startDate, DateTime endDate, string locScore = "")
        {

            double totalAmount = 0;
            int totalDays = 0;
            string rateOccurrenceToUse = null;

            foreach (var rateRecord in rateRecords)
            {
                DateTime rateStart = rateRecord.Startdate.HasValue && rateRecord.Startdate.Value > startDate
                    ? rateRecord.Startdate.Value
                    : startDate;

                DateTime rateEnd = rateRecord.Enddate.HasValue && rateRecord.Enddate.Value < endDate
                    ? rateRecord.Enddate.Value
                    : endDate;

                if (rateEnd < rateStart)
                    continue;

                var embeddedRateRecords = rateRecord.GetChildrenF_serviceratesagebased();
                var determineRateType = embeddedRateRecords.FirstOrDefault();
                // use first one to determine flat rate/score/age based
                var rateBasedOn = determineRateType.Ratebasedon;

                string rateOccurrence = rateRecord.Rateoccurrence;

                double currentRate = 0;
                int segmentDays = (rateEnd - rateStart).Days + 1;

                // look at rate based on - flat rate/score/age
                if (rateBasedOn == F_serviceratesagebasedStatic.DefaultValues.Age)
                {
                    if (rateOccurrence != F_servicerateStatic.DefaultValues.Daily &&
                        rateOccurrence != F_servicerateStatic.DefaultValues.Monthly)
                    {
                        eventHelper.AddErrorLog(inValidRateOccurrence);
                        continue;
                    }

                    currentRate = CalculateRateBasedOnAge(embeddedRateRecords, personDOB, rateStart, rateEnd, rateOccurrence);
                }
                else if (rateBasedOn == F_serviceratesagebasedStatic.DefaultValues.Score)
                {
                    var locScoreRate = embeddedRateRecords.FirstOrDefault(r => r.Score == locScore);

                    if (locScoreRate != null)
                    {
                        //currentRate = CalculateRateForLOCScore(locScoreRate, rateOccurrence);
                        currentRate = CalculateRate(locScoreRate, startDate, ref rateOccurrence, ref segmentDays);
                    }
                }
                else if (rateBasedOn == F_serviceratesagebasedStatic.DefaultValues.Flatrate)
                {
                    if (determineRateType != null)
                    {
                        currentRate = CalculateRate(determineRateType, startDate, ref rateOccurrence, ref segmentDays);
                    }
                }

                // Calculate amount for this segment
                double segmentAmount = currentRate * segmentDays;
                totalAmount += segmentAmount;
                totalDays += segmentDays;

                // Save the first valid rateOccurrence found
                if (rateOccurrenceToUse == null && !string.IsNullOrEmpty(rateOccurrence))
                {
                    rateOccurrenceToUse = rateOccurrence;
                }
            }

            return (totalAmount, totalDays, rateOccurrenceToUse);	
		}			
        public static Countylist GetLatestRemovalCounty(AEventHelper eventHelper, Persons childPerson, Placements placementRecord = null)
        {

            if (placementRecord != null)
            {
                // if removal is populated, get county from Removal
                var removal = placementRecord.Childtobeplaced();
                if (removal != null)
                {
                    return removal.Countyofjurisdiction(); // get county from Removal
                }
            }

            // get latest removal for child (get all removals for child regardless of Case/Investigation or active/enddated),
            var removalInfo = new RemovalInfo(eventHelper);
            var removalFilters = new List<DirectSQLFieldFilterData>
            {
                removalInfo.CreateFilter(RemovalStatic.SystemNames.Childremoved, new List<string> {childPerson.RecordInstanceID.ToString()})
            };
            var removalRecord = removalInfo.CreateQuery(removalFilters)
                .OrderByDescending(r => r.Startdateofcustody)
                .FirstOrDefault();
            if (removalRecord != null)
            {
                return removalRecord.Countyofjurisdiction(); // get county from Removal
            }

            return null;
        }

        public static string GetDateTrainingCompleted(AEventHelper eventHelper, Placements placementRecord)
        {
            ChildyouthspecifictrainingInfo cyTrainingInfo = new ChildyouthspecifictrainingInfo(eventHelper);

            var cyTrainingFilters = new List<DirectSQLFieldFilterData>
                {
                    cyTrainingInfo.CreateFilter(ChildyouthspecifictrainingStatic.SystemNames.Alltrainingcomplete, new List<string> { "1" })
                };
            var cyTrainingRecord = eventHelper.SearchSingleDataListSQLProcess(cyTrainingInfo.GetDataListId(), cyTrainingFilters, placementRecord.RecordInstanceID)
                .OrderByDescending(r => r.GetFieldValue<DateTime>(ChildyouthspecifictrainingStatic.SystemNames.Datetrainingcomplete))
                .FirstOrDefault();

            if (cyTrainingRecord != null)
                return cyTrainingRecord.GetFieldValue(ChildyouthspecifictrainingStatic.SystemNames.Datetrainingcomplete);

            return null;
        }

        public static Providers GetTFCAgencyProvider(AEventHelper eventHelper, Providers providerRecord)
        {

            // get provider license
            var providerLicense = GetProviderLicense(eventHelper, providerRecord, LicensesStatic.DefaultValues.Treatmentfostercare_tfc_resourcehome);

            if (providerLicense == null)
                return null;

            // If provider license is TFC, get parent agency
            F_providerrelationshipsInfo provRelInfo = new F_providerrelationshipsInfo(eventHelper);
            var linkTypeFilter = provRelInfo.CreateFilter(F_providerrelationshipsStatic.SystemNames.Type, new List<string> { F_providerrelationshipsStatic.DefaultValues.Thelinkedproviderisanumbrellatothisprovider });
            var relRecordInstanceData = eventHelper.SearchSingleDataListSQLProcess(provRelInfo.GetDataListId(), new List<DirectSQLFieldFilterData> { linkTypeFilter }, providerRecord.RecordInstanceID)
                .Where(r => string.IsNullOrEmpty(r.GetFieldValue(F_providerrelationshipsStatic.SystemNames.Enddate)))
                .FirstOrDefault();

            F_providerrelationships providerRelationshipRecord = null;
            if (relRecordInstanceData == null || !relRecordInstanceData.TryParseRecord(eventHelper, out providerRelationshipRecord))
                return null;

            return providerRelationshipRecord.Provider();
        }

        public static Licenses GetProviderLicense(AEventHelper eventHelper, Providers providerRecord, string type)
        {
            // check if placement provider is a TFC Resource Home
            LicensesInfo licensesInfo = new LicensesInfo(eventHelper);
            var statusFilter = licensesInfo.CreateFilter(LicensesStatic.SystemNames.Licensestatus1, new List<string> { LicensesStatic.DefaultValues.Approved });
            var typeFilter = licensesInfo.CreateFilter(LicensesStatic.SystemNames.Type, new List<string> { type });
            var licenseRecord = eventHelper.SearchSingleDataListSQLProcess(licensesInfo.GetDataListId(), new List<DirectSQLFieldFilterData> { statusFilter, typeFilter }, providerRecord.RecordInstanceID)
                .FirstOrDefault();
            if (licenseRecord != null && licenseRecord.TryParseRecord(eventHelper, out Licenses licenses))
                return licenses;

            return null;
        }

        public static F_serviceline GetServiceAutorizationForPlacement(AEventHelper eventHelper, Placements placementRecord)
        {
            var serviceAuthInfo = new F_servicelineInfo(eventHelper);
            var filter = serviceAuthInfo.CreateFilter(F_servicelineStatic.SystemNames.Placement, new List<string> { placementRecord.RecordInstanceID.ToString() });
            var serviceAuthRecord = eventHelper.SearchSingleDataListSQLProcess(serviceAuthInfo.GetDataListId(), new List<DirectSQLFieldFilterData> { filter }, placementRecord.ParentRecordID)
                .FirstOrDefault();
            if (serviceAuthRecord != null && serviceAuthRecord.TryParseRecord(eventHelper, out F_serviceline serviceAuth))
                return serviceAuth;

            return null;
        }

        /// <summary>
        /// End date Service Auth for old Placements and create new Service Auth for new placement
        /// </summary>
        /// <param name="eventHelper"></param>
        /// <param name="triggeringUser"></param>
        /// <param name="caseRec"></param>
        /// <param name="oldPlacementRec"></param>
        /// <param name="newPlacementRec"></param>
        public static void CopyServiceAuthorizationInfoToCase(AEventHelper eventHelper, UserData triggeringUser, Cases caseRec, Placements oldPlacementRec, Placements newPlacementRec)
        {

            if (oldPlacementRec.Placementenddate != null)
                return;

            var oldServAuth = GetServiceAutorizationForPlacement(eventHelper, oldPlacementRec);
            if (oldServAuth == null || oldServAuth.Enddate != null)
                return;

            // end date Service Auth
            HandlePausePayment(eventHelper, triggeringUser, oldServAuth, DateTime.Now.AddDays(-1));

            var personRecord = oldPlacementRec.Childplacedpersonddd();

            var newServiceAuth = HandleReinstatePayment(eventHelper, oldServAuth, personRecord, DateTime.Now, caseRec);

            newServiceAuth.Placement(newPlacementRec);
            newServiceAuth.Parentdl = F_servicelineStatic.DefaultValues.Case;
            newServiceAuth.SaveRecord();

        }

        /// <summary>
        /// End Date Service Auth if Investigation is linked to Open Case.
        /// </summary>
        /// <param name="eventHelper"></param>
		/// <param name="triggeringUser"></param>
        /// <param name="invRecord"></param>
        public static void EndDateServiceAuthForPlacements(AEventHelper eventHelper, UserData triggeringUser, Investigations invRecord)
        {
            var serviceAutgRecs = invRecord.GetChildrenF_serviceline()?.Where(r => null != r).ToList();
            serviceAutgRecs?.ForEach(sa =>
            {
                if (sa.Enddate == null)
                {
                    // end date Service Auth
                    HandlePausePayment(eventHelper, triggeringUser, sa, DateTime.Now.AddDays(-1));
                }
            });
        }

    }
}