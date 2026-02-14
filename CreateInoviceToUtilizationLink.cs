using MCase.Core.Event;
using MCase.Event.NMImpact.Constants;
using MCaseCustomEvents.NMImpact.Constants;
using MCaseEventsSDK;
using MCaseEventsSDK.Util;
using MCaseEventsSDK.Util.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using static MCase.Event.NMImpact.Utils.DatalistUtils.GeneralUtils;

namespace MCase.Event.NMImpact
{
    /// <summary>
    /// Custom Event to link a invoice request to a utilization record.
    /// - for demo, utilization ID is exposed  for user to key in.
    /// </summary>
    public class CreateInoviceToUtilizationLink : AMCaseValidateCustomEvent
    {
        public override string PrefixName => "[NMImpact] Provider Service Invoice";

        public override string ExactName => "Create Invoice to Utilization Link";

        protected override Dictionary<string, List<string>> SpecificFieldSystemNamesByListSystemName =>
             new Dictionary<string, List<string>>()
            {
                {
                    ProviderServiceInvoiceDataList.SysName,
                    new List<string>
                    {
                        ProviderServiceInvoiceDataList.Fields.ProviderUtilizationDD,
                        ProviderServiceInvoiceDataList.Fields.InvoiceDate
                    }
                },
                {
                    ServiceUtilizationDataList.SysName,
                    new List<string>
                    {
                        ServiceUtilizationDataList.Fields.ServiceUtilizationID,
                        ServiceUtilizationDataList.Fields.ProviderInvoiceDate,
                        ServiceUtilizationDataList.Fields.ProviderInvoice,
                        ServiceUtilizationDataList.Fields.ProviderInvoiceDate                        
                    }
                }
            };

        protected override List<EventTrigger> ValidEventTriggers => new List<EventTrigger> { EventTrigger.PostCreate };

        protected override Dictionary<string, List<string>> NeededRelationships =>
            new Dictionary<string, List<string>>() 
            { 
                {
                    ProviderServiceInvoiceDataList.SysName, new List<string>() 
                    { 
                    } 
                } 
            };

        protected override List<string> RecordDatalistType => new List<string>() { ProviderServiceInvoiceDataList.SysName };

        /// <summary>
        ///  Method link a Provider service invoice record with the correct service utilization record.
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
        protected override EventReturnObject ProcessEventSpecificLogic(AEventHelper eventHelper, UserData triggeringUser, WorkFlowData workflow, RecordInstanceData recordInsData, RecordInstanceData preSaveRecordData, Dictionary<string, DataListData> datalistsBySystemName, Dictionary<string, Dictionary<string, FieldData>> fieldsBySystemNameByListName, string triggerType)
        {
            var invoiceDate = recordInsData.GetFieldValue(ProviderServiceInvoiceDataList.Fields.InvoiceDate);
            var utilizationDD = recordInsData.GetFieldValue(ProviderServiceInvoiceDataList.Fields.ProviderUtilizationDD);

            var serviceUtilizationDLID = GetDatalistIdByListName(ServiceUtilizationDataList.SysName, datalistsBySystemName);
            var utilizationFieldID = GetFieldIdByListNameAndFieldName(ServiceUtilizationDataList.SysName, ServiceUtilizationDataList.Fields.ServiceUtilizationID, fieldsBySystemNameByListName);

            //set up filters
            var utilizationIDFieldFilter = new DirectSQLFieldFilterData(utilizationFieldID, new List<string>() { utilizationDD });

            //get the Utilication record instance by utilizationDD
            RecordInstanceData serviceUtilicationRecord = eventHelper.SearchSingleDataListSQLProcess(serviceUtilizationDLID, new List<DirectSQLFieldFilterData> { utilizationIDFieldFilter }).FirstOrDefault();
                    
          
            serviceUtilicationRecord.SetValue(ServiceUtilizationDataList.Fields.ProviderInvoice, recordInsData.RecordInstanceID.ToString());
            serviceUtilicationRecord.SetValue(ServiceUtilizationDataList.Fields.ProviderInvoiceDate, invoiceDate);

            eventHelper.SaveRecord(serviceUtilicationRecord);
            return new EventReturnObject(EventStatusCode.Success);

        }
    }
}
