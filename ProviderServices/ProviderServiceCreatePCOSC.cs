using MCase.Core.Event;
using MCase.Event.NMImpact.Constants;
using MCase.Event.NMImpact.Utils.DatalistUtils;
using MCaseCustomEvents.NMImpact.Generated.Entities;
using MCaseEventsSDK;
using MCaseEventsSDK.Util.Data;
using System.Collections.Generic;
using static MCase.Event.NMImpact.NMFinancialUtils;

namespace MCase.Event.NMImpact
{
    /// <summary>
    /// Post Insert/Post Update Event on Provider Service that maintains an up to date record of
    /// Providers that offer any Service in the Service Catalog. This allows us to configure a 
    /// CDDD from Service Catalog -> Provider in Service Requests. 
    /// </summary>
    public class ProviderServiceCreatePCOSC : AMCaseValidateCustomEvent
    {
        public override string PrefixName => "[NMImpact] Financials";
        public override string ExactName => "Create Provider copy under Service Catalog";

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
            if (!recordInsData.TryParseRecord(eventHelper, out F_providerservice providerServiceRecord))
            {
                eventHelper.AddDebugLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                eventHelper.AddErrorLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                return new EventReturnObject(EventStatusCode.Failure, new List<string> { GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity });
            }

            var hasPrice = ProviderschildofservicecatalogStatic.DefaultValues.False;
            var isAgeBased = ProviderschildofservicecatalogStatic.DefaultValues.False;

            var providerRecord = providerServiceRecord.GetParentProviders();
            var serviceCatalogRecord = providerServiceRecord.Service();

            // Check for Existing Provider Record as a Child of Service Catalog Service Item
            var existingPCOSCRecord = CheckForExistingPCOSC(eventHelper, serviceCatalogRecord, providerRecord);
            if (existingPCOSCRecord != null)
            {
                return new EventReturnObject(EventStatusCode.Success);
            }

            #region TODO: Determine how we are using Service Rate Agreements for Pricing in NM
            //var serviceRateAgreements = providerRecord.Servicerateagreeid();

            //foreach (var serviceRateAgreement in serviceRateAgreements)
            //{
            //    var agreementStatus = serviceRateAgreement.O_status;
            //    if (agreementStatus == F_contractStatic.DefaultValues.Approved)
            //    {
            //        var serviceRate = new F_servicerateInfo(eventHelper);
            //        var serviceRateRecords = serviceRateAgreement.GetChildrenF_servicerate()
            //            .Where(x => x.Enddate == DateTime.MinValue || x.Enddate > DateTime.Today);

            //        if (serviceRateRecords.Any())
            //        {
            //            var serviceRateRecord = serviceRateRecords.First();
            //            var ageBasedRecords = serviceRateRecord.GetChildrenF_serviceratesagebased();
            //            if (ageBasedRecords.Any())
            //            {
            //                var ageBased = ageBasedRecords.First().Ratebasedon;
            //                if (ageBased == F_serviceratesagebasedStatic.DefaultValues.Age)
            //                {
            //                    isAgeBased = ProviderschildofservicecatalogStatic.DefaultValues.True;
            //                }
            //                hasPrice = ProviderschildofservicecatalogStatic.DefaultValues.True;
            //            }
            //        }
            //    }
            //}
            #endregion

            // Add New Provider Record and Set Field Values
            var PCOSCRecord = new ProviderschildofservicecatalogInfo(eventHelper);
            var newPCOSCRecord = PCOSCRecord.NewProviderschildofservicecatalog(serviceCatalogRecord);
            newPCOSCRecord.Provider(providerRecord);
            newPCOSCRecord.Providerservice(providerServiceRecord);
            newPCOSCRecord.Agebased = isAgeBased;
            newPCOSCRecord.Hasprice = hasPrice;
            newPCOSCRecord.SaveRecord();

            return new EventReturnObject(EventStatusCode.Success);
        }
    }
}