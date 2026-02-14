using MCase.Core.Event;
using MCase.Event.NMImpact.Constants;
using MCase.Event.NMImpact.Utils.DatalistUtils;
using MCaseCustomEvents.NMImpact.Generated.Entities;
using MCaseEventsSDK;
using MCaseEventsSDK.Util;
using MCaseEventsSDK.Util.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using static MCase.Event.NMImpact.NMFinancialUtils;

namespace MCase.Event.NMImpact
{
    /// <summary>
    /// LNel (11/10/2025):  Disabling this event since it is updating fields we do not use.  I don't think we need this.
    /// Daily batch event that processes Service Rate Records. Checks if a Service Rate Record
    /// end date is today and sets prive and age flags on linked PCOSC records. 
    /// </summary>

    public class CheckEndDateOnServiceRateBatch : AMCaseValidateCustomEvent
    {
        public override string PrefixName => "[NMImpact] Financials";
        public override string ExactName => "Check End Date on Service Rate Batch";

        protected override Dictionary<string, List<string>> SpecificFieldSystemNamesByListSystemName => new Dictionary<string, List<string>>()
        {

        };

        protected override List<EventTrigger> ValidEventTriggers => new List<EventTrigger>
        {
            EventTrigger.OnSchedule, EventTrigger.Button
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
            if (!recordInsData.TryParseRecord(eventHelper, out F_servicerate serviceRate))
            {
                eventHelper.AddDebugLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                eventHelper.AddErrorLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                return new EventReturnObject(EventStatusCode.Failure, new List<string> { GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity });
            }

            var validationMessages = new List<string>();
            var minDate = new DateTime(2000, 1, 1);
            var minDateString = minDate.ToString(MCaseEventConstants.DateStorageFormat);
            var today = DateTime.Today.AddDays(-1).ToString(MCaseEventConstants.DateStorageFormat);
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            var currentRunRecord = GetRunRecord(eventHelper, validationMessages);
            if (validationMessages.Any())
            {
                return new EventReturnObject(EventStatusCode.Failure, validationMessages);
            }

            var recordsSucceeded = 0;
            var recordsFailed = 0;

            // Get all Service Rate Records where End Date is between 1/1/2000 and today
            var serviceRateInfo = new F_servicerateInfo(eventHelper);
            var serviceRateEndDateFilter = serviceRateInfo.CreateFilter(F_servicerateStatic.SystemNames.Enddate, minDateString, today);

            var serviceRateRecords = serviceRateInfo.CreateQuery(new List<DirectSQLFieldFilterData> { serviceRateEndDateFilter });

            try
            {
                RecordInstanceData serviceRateAgreement = null;
                foreach (var serviceRateRecord in serviceRateRecords)
                {
                    serviceRateAgreement = serviceRateRecord.GetParentF_contract();
                    if (serviceRateAgreement == null)
                    {
                        serviceRateAgreement = serviceRateRecord.GetParentF_standardservicerateagreement();
                    }

                    if (serviceRateAgreement != null)
                    {
                        // Get Service Catalog Record from the Service Rate record
                        var serviceCatalogRecords = serviceRateRecord.Forservice();
                        foreach (var serviceCatalogRecord in serviceCatalogRecords)
                        {
                            // Check if there is an existing PCOSC record for the Provider on linked Service Catalog
                            // Find all providers with this service rate agreement
                            var providersInfo = new ProvidersInfo(eventHelper);
                            var providerFilter = providersInfo.CreateFilter(ProvidersStatic.SystemNames.Servicerateagreeid, new List<string> { serviceRateAgreement.RecordInstanceID.ToString() });
                            var providersWithContract = providersInfo.CreateQuery(new List<DirectSQLFieldFilterData> { providerFilter });
                            foreach (var providerRecord in providersWithContract)
                            {
                                var existingPCOSCRecord = CheckForExistingPCOSC(eventHelper, serviceCatalogRecord, providerRecord);

                                if (existingPCOSCRecord != null)
                                {
                                    var hasPrice = ProviderschildofservicecatalogStatic.DefaultValues.False;
                                    var isAgeBased = ProviderschildofservicecatalogStatic.DefaultValues.False;
                                    // Get embedded Age Based Rate Records
                                    var ageBasedRateRecords = serviceRateRecord.GetChildrenF_serviceratesagebased();
                                    if (ageBasedRateRecords.Any())
                                    {
                                        var rateBasedOn = ageBasedRateRecords.First().Ratebasedon;
                                        if (rateBasedOn == F_serviceratesagebasedStatic.DefaultValues.Age)
                                        {
                                            isAgeBased = ProviderschildofservicecatalogStatic.DefaultValues.True;
                                        }
                                        hasPrice = ProviderschildofservicecatalogStatic.DefaultValues.True;
                                    }

                                    existingPCOSCRecord.Hasprice = hasPrice;
                                    existingPCOSCRecord.Agebased = isAgeBased;
                                    existingPCOSCRecord.SaveRecord();
                                }
                            }
                        }
                    }
                }
            }

            catch (Exception ex)
            {
                eventHelper.AddErrorLog(String.Format("Error while running batch: {0}, Error: {1}", Name, ex.Message));
                FailRunRecord(eventHelper, currentRunRecord, serviceRateRecords.Count(), recordsSucceeded, recordsFailed, String.Format("Error while running batch: {0}", ex.Message));

                return new EventReturnObject(EventStatusCode.Failure);
            }

            // update the run record
            if (currentRunRecord != null)
            {
                currentRunRecord.Enddatetime = DateTime.UtcNow;
                currentRunRecord.Runstatus = BatchinterfacerunStatic.DefaultValues.Completed;
                currentRunRecord.Totalrecordsprocessed = recordsSucceeded.ToString();
                currentRunRecord.SaveRecord();
            }

            stopwatch.Stop();
            eventHelper.AddInfoLog($"Query took {stopwatch.ElapsedMilliseconds} milliseconds to process {serviceRateRecords.Count()} records for {Name}.");

            return new EventReturnObject(EventStatusCode.Success);
        }
    }
}