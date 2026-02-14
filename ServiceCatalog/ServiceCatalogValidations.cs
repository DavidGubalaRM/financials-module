using MCase.Core.Event;
using MCase.Event.NMImpact.Constants;
using MCase.Event.NMImpact.Utils.DatalistUtils;
using MCaseCustomEvents.NMImpact.Generated.Entities;
using MCaseEventsSDK;
using MCaseEventsSDK.Util;
using MCaseEventsSDK.Util.Data;
using System.Collections.Generic;
using System.Linq;

namespace MCase.Event.NMImpact
{
    /// <summary>
    /// Post Insert / Pre Update event on Service Catalog to perform validations
    ///  - Ceiling Frequency
    /// </summary>
    public class ServiceCatalogValidations : AMCaseValidateCustomEvent
    {
        public override string PrefixName => "[NMImpact] Financials";
        public override string ExactName => "Service Catalog Validations";

        protected override Dictionary<string, List<string>> SpecificFieldSystemNamesByListSystemName => new Dictionary<string, List<string>>()
        {
        };

        protected override List<EventTrigger> ValidEventTriggers => new List<EventTrigger>
        {
            EventTrigger.PostCreate, EventTrigger.PreUpdate
        };

        protected override Dictionary<string, List<string>> NeededRelationships => new Dictionary<string, List<string>>()
        {
        };

        protected override List<string> RecordDatalistType => new List<string>()
        {
        };


        private readonly List<string> PerChildCeilingFrequencies = new List<string>
        {
            F_servicecatalogStatic.DefaultValues.Perchildpercalendaryear,
            F_servicecatalogStatic.DefaultValues.Perchildpermonth,
            F_servicecatalogStatic.DefaultValues.Perchild_lifetime_
        };

        protected override EventReturnObject ProcessEventSpecificLogic(AEventHelper eventHelper, UserData triggeringUser, WorkFlowData workflow, RecordInstanceData recordInsData,
            RecordInstanceData preSaveRecordData, Dictionary<string, DataListData> datalistsBySystemName, Dictionary<string, Dictionary<string, FieldData>> fieldsBySystemNameByListName, string triggerType)
        {
            // Begin
            eventHelper.AddInfoLog($"{TechName} - Begin");

            var record = workflow.TriggerType.Equals(EventTrigger.PreUpdate.GetEnumDescription())
               ? preSaveRecordData
               : recordInsData;

            if (!record.TryParseRecord(eventHelper, out F_servicecatalog serviceCatalogRecord))
            {
                eventHelper.AddDebugLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                eventHelper.AddErrorLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                return new EventReturnObject(EventStatusCode.Failure, new List<string> { GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity });
            }

            List<string> errorMessages = new List<string>();

            // Ceiling Frequency = Per Foster Parent (lifetime) can only be used for Foster Care Parent Incidentals
            if (serviceCatalogRecord.Ceilingfrequency.Equals(F_servicecatalogStatic.DefaultValues.Perfosterparent_lifetime_)
                && !serviceCatalogRecord.Type.Equals(F_servicecatalogStatic.DefaultValues.Fostercareparentincidentals))
            {
                errorMessages.Add($"Ceiling Frequency {serviceCatalogRecord.Ceilingfrequency} can only be used for {F_servicecatalogStatic.DefaultValues.Fostercareparentincidentals}");
                eventHelper.AddErrorLog(string.Join(", ", errorMessages));
            }

            if (PerChildCeilingFrequencies.Contains(serviceCatalogRecord.Ceilingfrequency)
                && !serviceCatalogRecord.Type.Equals(F_servicecatalogStatic.DefaultValues.Fostercarechildincidentals))
            {
                errorMessages.Add($"Ceiling Frequency {serviceCatalogRecord.Ceilingfrequency} can only be used for {F_servicecatalogStatic.DefaultValues.Fostercarechildincidentals}");
                eventHelper.AddErrorLog(string.Join(", ", errorMessages));
            }

            if (errorMessages.Any())
            {
                return new EventReturnObject(EventStatusCode.Failure, errorMessages);
            }

            return new EventReturnObject(EventStatusCode.Success);
        }
    }
}
