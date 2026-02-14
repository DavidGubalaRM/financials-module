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
    /// Daily Batch Event on Provider Service that maintains an up to date record of
    /// Providers that offer any Service in the Service Catalog. This allows us to configure a 
    /// CDDD from Service Catalog -> Provider in Service Requests. 
    /// </summary>

    public class CheckEndDateOnServiceRateAgreementBatch : AMCaseValidateCustomEvent
    {
        public override string PrefixName => "[NMImpact] Financials";
        public override string ExactName => "Check End Date on Service Rate Agreement Batch";

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
            if (!recordInsData.TryParseRecord(eventHelper, out F_contract serviceRateAgreement))
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

            // Get all Service Rate Agreement Reocrds where status is approved and end date is between 1/1/2000 and today
            var serviceRateAgreementInfo = new F_contractInfo(eventHelper);
            var serviceRateAgreementStatusFilter = serviceRateAgreementInfo.CreateFilter(F_contractStatic.SystemNames.O_status, new List<string> { F_contractStatic.DefaultValues.Approved });
            var serviceRateAgreementEndDateFilter = serviceRateAgreementInfo.CreateFilter(F_contractStatic.SystemNames.Enddate, minDateString, today);

            var sraRecords = serviceRateAgreementInfo.CreateQuery(new List<DirectSQLFieldFilterData> { serviceRateAgreementStatusFilter, serviceRateAgreementEndDateFilter });

            try
            {
                foreach (var sraRecord in sraRecords)
                {
                    // Get all Service Rate Records that are not end dated and are children of current Service Rate Agreement record
                    var serviceRateRecords = sraRecord.GetChildrenF_servicerate()
                        .Where(x => x.Enddate == null || x.Enddate > DateTime.Today);

                    //var providerRecord = sraRecord.();
                    foreach (var serviceRateRecord in serviceRateRecords)
                    {
                        // get service catalog record from the Service Rate Record
                        var serviceCatalogRecords = serviceRateRecord.Forservice();
                        foreach (var serviceCatalogRecord in serviceCatalogRecords)
                        {
                            // Check if there is an existing PCOSC record for the Provider on linked Service Catalog
                            var providersInfo = new ProvidersInfo(eventHelper);
                            var providerFilter = providersInfo.CreateFilter(ProvidersStatic.SystemNames.Servicerateagreeid, new List<string> { sraRecord.RecordInstanceID.ToString() });
                            var providersWithContract = providersInfo.CreateQuery(new List<DirectSQLFieldFilterData> { providerFilter });
                            foreach (var providerRecord in providersWithContract)
                            {
                                var existingPCOSCRecord = CheckForExistingPCOSC(eventHelper, serviceCatalogRecord, providerRecord);

                                if (existingPCOSCRecord != null)
                                {
                                    var hasPrice = ProviderschildofservicecatalogStatic.DefaultValues.False;
                                    var isAgeBased = ProviderschildofservicecatalogStatic.DefaultValues.False;

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
                FailRunRecord(eventHelper, currentRunRecord, sraRecords.Count(), recordsSucceeded, recordsFailed, String.Format("Error while running batch: {0}", ex.Message));

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
            eventHelper.AddInfoLog($"Query took {stopwatch.ElapsedMilliseconds} milliseconds to process {sraRecords.Count()} records for {Name}.");

            return new EventReturnObject(EventStatusCode.Success);
        }
    }
}