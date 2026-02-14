using MCase.Core.Event;
using MCase.Event.NMImpact.Constants;
using MCase.Event.NMImpact.Utils.DatalistUtils;
using MCaseCustomEvents.NMImpact.Generated.Entities;
using MCaseEventsSDK;
using MCaseEventsSDK.Util.Data;
using System.Collections.Generic;
using System.Linq;
using static MCase.Event.NMImpact.NMFinancialUtils;

namespace MCase.Event.NMImpact
{
    /// <summary>
    /// Post Insert/ Post Update Event on Service Rates Datalist
    /// This event checks for existing PCOSC records related to the Service and Provider 
    /// and sets Has Price and Is Age Based data
    /// </summary>
    public class ServiceRatesUpdatePCOSC : AMCaseValidateCustomEvent
    {
        public override string PrefixName => "[NMImpact] Financials";
        public override string ExactName => "Service Rates Update PCOSC";

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
            if (!recordInsData.TryParseRecord(eventHelper, out F_servicerate serviceRateRecord))
            {
                eventHelper.AddDebugLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                eventHelper.AddErrorLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                return new EventReturnObject(EventStatusCode.Failure, new List<string> { GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity });
            }

            // get all Service Records, and update each one
            var serviceRecords = serviceRateRecord.Forservice();
            foreach (var serviceRecord in serviceRecords)
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

                var contractRecord = serviceRateRecord.GetParentF_contract();
                if (contractRecord != null)
                {
                    // providers have a ddd to service rate agreement. 
                    // get all providers with this contract as a ddd. 
                    var provider = new ProvidersInfo(eventHelper);
                    var providerFilter = provider.CreateFilter(ProvidersStatic.SystemNames.Servicerateagreeid, new List<string> { contractRecord.RecordInstanceID.ToString() });
                    var providersWithThisContract = provider.CreateQuery(new List<DirectSQLFieldFilterData> { providerFilter });

                    foreach (var providerRecord in providersWithThisContract)
                    {
                        var existingPCOSCRecord = CheckForExistingPCOSC(eventHelper, serviceRecord, providerRecord);
                        if (existingPCOSCRecord != null)
                        {
                            existingPCOSCRecord.Hasprice = hasPrice;
                            existingPCOSCRecord.Agebased = isAgeBased;
                            existingPCOSCRecord.SaveRecord();
                        }
                    }
                }
            }

            return new EventReturnObject(EventStatusCode.Success);
        }
    }
}