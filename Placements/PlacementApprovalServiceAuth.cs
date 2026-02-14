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
using static MCase.Event.NMImpact.NMFinancialUtils;

namespace MCase.Event.NMImpact
{
    /// <summary>
    /// Post Update Event on Placements when status is changed to Active
    /// Create Service Auth. 
    /// </summary>
    public class PlacementApprovalServiceAuth : AMCaseValidateCustomEvent
    {
        public override string PrefixName => "[NMImpact] Financials";
        public override string ExactName => "Placement Approval Create Service Auth";

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
            if (!recordInsData.TryParseRecord(eventHelper, out Placements placementRecord))
            {
                eventHelper.AddErrorLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                return new EventReturnObject(EventStatusCode.Failure, new List<string> { GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity });
            }

            var status = placementRecord.O_status;
            if (!status.Equals(PlacementsStatic.DefaultValues.Active))
            {
                return new EventReturnObject(EventStatusCode.Success);
            }

            var validationMessages = new List<string>();
            var parentRecordCase = placementRecord.GetParentCases();
            var parentRecordInv = placementRecord.GetParentInvestigations();

            var currentDateTime = DateTime.Now;
            Persons childPerson = null;
            if (parentRecordCase != null)
            {
                var serviceAuthRecord = new F_servicelineInfo(eventHelper);
                var newServiceAuthRecord = serviceAuthRecord.NewF_serviceline(parentRecordCase);

                // check if Service Auth already exist for Placement, if not create it otherwise check if we should end date it
                var existingServiceAuthRecord = GetServiceAutorizationForPlacement(eventHelper, placementRecord);
                if (existingServiceAuthRecord == null)
                {
                    bool createSericeAuth = SetAuthorizationFields(eventHelper, newServiceAuthRecord, placementRecord, parentRecordCase.RecordInstanceID, validationMessages);
                    if (createSericeAuth)
                    {
                        var caseParticipantRemoved = placementRecord.Childyouth(); //get a case participant from placement
                        childPerson = caseParticipantRemoved.Caseparticipantname(); // get a Person Record

                        // Get removal county, if not populated, use county from Case
                        var county = GetLatestRemovalCounty(eventHelper, childPerson, placementRecord);
                        if (county != null)
                            newServiceAuthRecord.County(county);
                        else
                            newServiceAuthRecord.County(parentRecordCase.Casecounty()); // get county from parent Record

                        newServiceAuthRecord.Participant(new List<Persons> { childPerson });
                        newServiceAuthRecord.Parentdl = F_servicelineStatic.DefaultValues.Case;

                        bool createAnother = false;
                        #region Extended Foster Care Setting
                        DateTime? dateReported = default;
                        F_servicecatalog serviceCatalogRecord = null;
                        F_providerservice providerServiceRecord = null;
                        if (placementRecord.Placementsetting.Equals(PlacementsStatic.DefaultValues.Extendedfostercaresetting)
                            && placementRecord.Placementtype == PlacementsStatic.DefaultValues.Supervisedindependentliving)
                        {
                            // check for preganant and parenting youth
                            var placementStartDate = placementRecord.GetFieldValue<DateTime>(PlacementsStatic.SystemNames.Placementdate);
                            (_, dateReported) = GetDeliveryDate(eventHelper, placementRecord, childPerson);
                            if (dateReported != null && dateReported > placementStartDate)
                            {
                                // check if Provider provides service for new service type
                                (serviceCatalogRecord, providerServiceRecord) = GetServiceCatalogAndService(eventHelper, placementRecord);
                                // if not we do not want to end date Service Auth and create a new record
                                if (providerServiceRecord != null)
                                {
                                    newServiceAuthRecord.SetValue(F_servicelineStatic.SystemNames.Enddate, dateReported.Value.AddDays(-1).ToString(MCaseEventConstants.DateStorageFormat));
                                    createAnother = true;
                                }

                            }
                        }
                        #endregion

                        newServiceAuthRecord.SaveRecord();

                        // If start date is in the previous month, call gateway on approval.
                        if (newServiceAuthRecord.Startdate < new DateTime(currentDateTime.Year, currentDateTime.Month, 1))
                        {
                            HandleGatewayCall(eventHelper, triggeringUser, (DateTime)newServiceAuthRecord.Startdate, newServiceAuthRecord, NMFinancialConstants.ActionTypes.StartPayment);
                        }

                        #region Extended Foster Care Setting continue
                        if (createAnother)
                        {
                            CreateServiceAuthForExtendedFostercare(eventHelper, newServiceAuthRecord, parentRecordCase, placementRecord, (DateTime)dateReported,
                                 serviceCatalogRecord, providerServiceRecord);
                        }
                        #endregion
                    }
                }
                else
                {
                    if (placementRecord.Placementenddate != existingServiceAuthRecord.Enddate)
                    {
                        existingServiceAuthRecord.Enddate = placementRecord.Placementenddate;
                        existingServiceAuthRecord.SaveRecord();
                    }
                }
            }

            else if (parentRecordInv != null)
            {
                var serviceAuthRecord = new F_servicelineInfo(eventHelper);
                var newServiceAuthRecord = serviceAuthRecord.NewF_serviceline(parentRecordInv);

                var existingServiceAuthRecord = GetServiceAutorizationForPlacement(eventHelper, placementRecord);
                if (existingServiceAuthRecord == null)
                {

                    bool createSericeAuth = SetAuthorizationFields(eventHelper, newServiceAuthRecord, placementRecord, parentRecordInv.RecordInstanceID, validationMessages);
                    if (createSericeAuth)
                    {
                        var investigationParticipant = placementRecord.Childyouthinv(); //get a inv participant from placement
                        childPerson = investigationParticipant.Invparticipantname(); // get Person record

                        // Get removal county, if not populated, use county from Investigation
                        var county = GetLatestRemovalCounty(eventHelper, childPerson, placementRecord);
                        if (county != null)
                            newServiceAuthRecord.County(county);
                        else
                            newServiceAuthRecord.County(parentRecordInv.Invcounty()); // get county from parent Record

                        newServiceAuthRecord.Participant(new List<Persons> { childPerson }); // get Child from removals
                        newServiceAuthRecord.Parentdl = F_servicelineStatic.DefaultValues.Investigation;
                        newServiceAuthRecord.SaveRecord();

                        // If start date is in the previous month, call gateway on approval.
                        if (newServiceAuthRecord.Startdate < new DateTime(currentDateTime.Year, currentDateTime.Month, 1))
                        {
                            HandleGatewayCall(eventHelper, triggeringUser, (DateTime)newServiceAuthRecord.Startdate, newServiceAuthRecord, NMFinancialConstants.ActionTypes.StartPayment);
                        }
                    }
                }
                else
                {
                    if (placementRecord.Placementenddate != existingServiceAuthRecord.Enddate)
                    {
                        existingServiceAuthRecord.Enddate = placementRecord.Placementenddate;
                        existingServiceAuthRecord.SaveRecord();
                    }
                }
            }

            if (childPerson != null)
            {
                placementRecord.Childplacedpersonddd(childPerson);
                placementRecord.SaveRecord();
            }


            return new EventReturnObject(EventStatusCode.Success);
        }


        private static (F_servicecatalog, F_providerservice)GetServiceCatalogAndService(AEventHelper eventHelper, Placements placementRecord)
        {
            // get Service Catalog
            var requiredService = NMFinancialConstants.ServiceCatalogServices.ExtendedFosterCarePregnantAndParentingYouth;
            var serviceCatalogRecord = GetServiceCatalogByUniqueCode(eventHelper, requiredService);

            if (serviceCatalogRecord == null)
            {
                // this should not happen
                eventHelper.AddErrorLog(string.Format(NMFinancialConstants.ErrorMessages.serviceCatalogNotFound, requiredService));
                return (null, null);
            }

            // Get Provider Service
            var providerID = placementRecord.GetFieldValue<long>(PlacementsStatic.SystemNames.Provider);
            var providerRecord = eventHelper.GetActiveRecordById(providerID);
            var providerServiceRecord = GetProviderService(eventHelper, serviceCatalogRecord, providerRecord, placementRecord);
            if (providerServiceRecord == null)
            {
                // we will not create Service Auth if Provider does not provide the service
                eventHelper.AddErrorLog(string.Format(NMFinancialConstants.ErrorMessages.serviceNotOffered, providerRecord.Label, serviceCatalogRecord.Nameofservice));
                return (serviceCatalogRecord, null);
            }

            return (serviceCatalogRecord, providerServiceRecord);
        }
        private static void CreateServiceAuthForExtendedFostercare (AEventHelper eventHelper, F_serviceline newServiceAuthRecord, Cases parentRecordCase, Placements placementRecord, DateTime dateReported,
             F_servicecatalog serviceCatalogRecord, F_providerservice providerServiceRecord)
        {
            var anotherServiceAuth = newServiceAuthRecord.DeepCopyRecord(eventHelper, parentRecordCase.RecordInstanceID);
            if (!anotherServiceAuth.TryParseRecord(eventHelper, out F_serviceline anotherServiceAuthRecord))
            {
                eventHelper.AddErrorLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                return;
            }
            anotherServiceAuthRecord.Startdate = dateReported;
            anotherServiceAuthRecord.Enddate = null;

            // Create new Service Auth
            anotherServiceAuthRecord.Servicecategory = serviceCatalogRecord.Type; // Category from Service Catalog
            anotherServiceAuthRecord.Servicetype(providerServiceRecord); // DDD to Provider Service  
            anotherServiceAuthRecord.Servicecatalogtype(serviceCatalogRecord); // DDD to Service Catalog

            eventHelper.SaveRecord(anotherServiceAuthRecord);
        }

        private static bool SetAuthorizationFields(AEventHelper eventHelper, F_serviceline newServiceAuthRecord, Placements placementRecord, long parentRecordID, List<string> validationMessages)
        {
            newServiceAuthRecord.CreatedOn = DateTime.UtcNow;
            newServiceAuthRecord.Placement(placementRecord);
            var providerRecord = placementRecord.Provider();

            // check for TFC resource home and set provider to tfc Agency since the agency will be paid
            var tfcAgencyProvider = GetTFCAgencyProvider(eventHelper, providerRecord);
            if (tfcAgencyProvider != null)
            {
                newServiceAuthRecord.Tfchomeprovider(providerRecord);
                newServiceAuthRecord.Provider(tfcAgencyProvider);
            } else
            {
                newServiceAuthRecord.Provider(providerRecord);
            }

            newServiceAuthRecord.Placementslevoffcdd = placementRecord.Placementslevoffcdd;
            newServiceAuthRecord.Locassessscore = placementRecord.Locassessscore;
            // if LOC is level 2 or 3, check if training is complete
            if (string.IsNullOrEmpty(placementRecord.Placementslevoffcdd))
            {
                // default to level 1
                newServiceAuthRecord.Placementslevoffcdd = PlacementsStatic.DefaultValues.Level1;
            }
            else if (placementRecord.Placementslevoffcdd.Equals(PlacementsStatic.DefaultValues.Level2)
                || placementRecord.Placementslevoffcdd.Equals(PlacementsStatic.DefaultValues.Level3))
            {
                var dateTrainingCompleted = GetDateTrainingCompleted(eventHelper, placementRecord);
                // if not, set level of care = level 1.  They will be paid at this level until training is completed
                if (string.IsNullOrEmpty(dateTrainingCompleted))
                    newServiceAuthRecord.Placementslevoffcdd = PlacementsStatic.DefaultValues.Level1;
            }

            (F_servicecatalog serviceCatalogRecord, F_providerservice providerServiceRecord) =
                ValidateProviderOffersRequiredService(eventHelper, placementRecord, parentRecordID, validationMessages, true, newServiceAuthRecord.Placementslevoffcdd);

            if (providerServiceRecord == null || serviceCatalogRecord == null)
            {
                // (This should never happen since we check this in postinsert/preupdate)
                eventHelper.AddErrorLog("Provider Service / Service Catalog not found for the Provider.");
                return false; // we do not want to create Service Auth if provider does not provider service
            }

            newServiceAuthRecord.Servicetype(providerServiceRecord);
            newServiceAuthRecord.Servicecatalogtype(serviceCatalogRecord);
            newServiceAuthRecord.Sourcerequesttype = F_servicelineStatic.DefaultValues.Placementservices;
            newServiceAuthRecord.Servicecategory = F_servicelineStatic.DefaultValues.Placementbasedpayments;
            newServiceAuthRecord.Servicelinestatus = F_servicelineStatic.DefaultValues.Active;
            newServiceAuthRecord.Recurrence = F_servicelineStatic.DefaultValues.Monthly;
            newServiceAuthRecord.Startdate = placementRecord.Placementdate;
            newServiceAuthRecord.Enddate = placementRecord.Placementenddate;
            var unitsAuth = GetUnitsAuthorized(eventHelper, placementRecord);
            if (unitsAuth > 0)
            {
                newServiceAuthRecord.Unitsauthorized = unitsAuth.ToString();
            }
            newServiceAuthRecord.Dltype = F_servicelineStatic.DefaultValues.Serviceauthorization;

            return true;
        }

        private static int GetUnitsAuthorized(AEventHelper eventHelper, Placements placementRecord)
        {
            var unitsAuth = 0;
            var placementStart = placementRecord.Placementdate;
            var placementEnd = placementRecord.Placementenddate;
            if (placementEnd.HasValue)
            {
                unitsAuth = (placementEnd.Value - placementStart.Value).Days;
            }
            return unitsAuth;
        }
    }
}