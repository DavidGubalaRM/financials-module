using MCase.Core.Event;
using MCase.Event.NMImpact.Constants;
using MCaseCustomEvents.NMImpact.Generated.Entities;
using MCaseEventsSDK;
using MCaseEventsSDK.Util.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using static MCase.Event.NMImpact.NMFinancialUtils;

namespace MCase.Event.NMImpact
{
    /// <summary>
    /// Post Insert Event on Service Utilization Records
    /// Find Fund Balance, and Create Initial Fund Distribution Record
    /// </summary>
    public class PopulateInitialFundDistribution : AMCaseValidateCustomEvent
    {
        public override string PrefixName => "[NMImpact] Financials";
        public override string ExactName => "Populate Initial Fund Distribution";

        protected override Dictionary<string, List<string>> SpecificFieldSystemNamesByListSystemName => new Dictionary<string, List<string>>()
        {

        };

        protected override List<EventTrigger> ValidEventTriggers => new List<EventTrigger>
        {
            EventTrigger.PostCreate, EventTrigger.PostUpdate // for testing
        };

        protected override Dictionary<string, List<string>> NeededRelationships => new Dictionary<string, List<string>>()
        {

        };

        protected override List<string> RecordDatalistType => new List<string>()
        {

        };

        private double _initialAmountOwed;
        private double _remainingServiceAmountOwed;
        private int _fundsUsedCounter;
        private bool _childIsIVEEligible;
        private bool _childIsIVEReimbursible;

        protected override EventReturnObject ProcessEventSpecificLogic(AEventHelper eventHelper, UserData triggeringUser, WorkFlowData workflow, RecordInstanceData recordInsData,
            RecordInstanceData preSaveRecordData, Dictionary<string, DataListData> datalistsBySystemName, Dictionary<string, Dictionary<string, FieldData>> fieldsBySystemNameByListName, string triggerType)
        {
            // Begin
            eventHelper.AddInfoLog($"{TechName} - Begin");
            if (!recordInsData.TryParseRecord(eventHelper, out F_serviceplanutilization serviceUtilization))
            {
                eventHelper.AddDebugLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                eventHelper.AddErrorLog(GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity);
                return new EventReturnObject(EventStatusCode.Failure, new List<string> { GeneralConstants.ErrorMessages.FailedToParseRecordAsORMEntity });
            }

            #region Get Amount Owed
            // Get the amount owed, if nothing is owed exit event with success
            var totalAmountDue = serviceUtilization.Totalbillableamount;

            if (!double.TryParse(totalAmountDue, out double amount) || amount == 0)
            {
                return new EventReturnObject(EventStatusCode.Success);
            }
            #endregion

            _initialAmountOwed = amount;
            _remainingServiceAmountOwed = amount;
            _fundsUsedCounter = 0;
            _childIsIVEEligible = false;
            _childIsIVEReimbursible = false;

            var errorMsg = string.Empty;

            DateTime today = DateTime.Today;
            List<Persons> persons = serviceUtilization.Participant();
            var provider = serviceUtilization.GetParentProviders();
            var serviceCatalogRecord = serviceUtilization.Servicecatalogtype();
            // maximumTitleIVEReimbursibleAmount applies to ANY IVE Reimbursible fund. 
            double maximumTitleIVEReimbursibleAmount = double.TryParse(serviceCatalogRecord?.Maximumtitleivereimbursableamount, out var result) ? result : 0;
            var county = provider.County();

            // get funding model on utilization start date
            var fundAllocationRecords = GetFundAllocations(serviceUtilization, serviceCatalogRecord);

            if (fundAllocationRecords == null || fundAllocationRecords.Count() == 0)
            {
                errorMsg = "No Funding Model set up for this Service Catalog Item. Please Resolve";
                eventHelper.AddWarningLog($"{TechName} - {errorMsg}");
                CreateBlankInitialFundDisRecord(eventHelper, serviceUtilization, errorMsg, ref _fundsUsedCounter);
                return new EventReturnObject(EventStatusCode.Success);
            }

            var placement = GetPlacement(serviceUtilization);
            Ctfmaprates ctFMAPRecord = null;
            Cttribalfmaprates ctTribalFMAPRecord = null;

            Iveeligibility iVEEligibility = null;

            #region Determine Eligibility
            if (!_childIsIVEEligible &&
                (iVEEligibility = IsChildIVEEligible(eventHelper, serviceUtilization)) != null)
            {
                _childIsIVEEligible = true;
                _childIsIVEReimbursible = iVEEligibility.Reimbursableverification == IveeligibilityStatic.DefaultValues.Yes;
            }
            #endregion

            // If there is more than 1 Fund Allocation, use based on priority (percentage). 
            foreach (var fundAllocation in fundAllocationRecords)
            {
                if (_remainingServiceAmountOwed <= 0)
                {
                    return new EventReturnObject(EventStatusCode.Success);
                }

                var iVE = fundAllocation.Iveeligible;
                var percentageType = fundAllocation.Percentagetype;

                if (iVE == F_fundallocationStatic.DefaultValues.Eligible)
                {
                    if (!_childIsIVEEligible)
                    {
                        eventHelper.AddWarningLog("Child is not IVE eligible, skipping fund allocation: {fundAllocation}");
                        continue;
                    }

                    HandleIVEEligibleFund(eventHelper, triggeringUser, serviceUtilization, fundAllocation, today, persons, county, placement, ref _fundsUsedCounter, ref _initialAmountOwed, ref _remainingServiceAmountOwed);
                }
                else if (iVE == F_fundallocationStatic.DefaultValues.Reimbursable && string.IsNullOrWhiteSpace(percentageType))
                {
                    if (!_childIsIVEReimbursible)
                    {
                        eventHelper.AddWarningLog("Child is not IVE reimbursible, skipping fund allocation: {fundAllocation}");
                        continue;
                    }

                    HandleIVEReimbursibleFund(eventHelper, triggeringUser, serviceUtilization, fundAllocation, persons, county, iVEEligibility, maximumTitleIVEReimbursibleAmount, ref _fundsUsedCounter, ref _initialAmountOwed, ref _remainingServiceAmountOwed);
                }
                else if (iVE == F_fundallocationStatic.DefaultValues.Reimbursable && (percentageType == F_fundallocationStatic.DefaultValues.Fmap || percentageType == F_fundallocationStatic.DefaultValues.Fmapremainder))
                {
                    if (!_childIsIVEReimbursible)
                    {
                        eventHelper.AddWarningLog("Child is not IVE eligible, skipping fund allocation: {fundAllocation}");
                        continue;
                    }
                    if (ctFMAPRecord == null)
                    {
                        ctFMAPRecord = GetCTFMAPRateRecord(eventHelper, serviceUtilization, fundAllocation);
                    }

                    HandleIVEReimbursibleFMAPFund(eventHelper, triggeringUser, serviceUtilization, fundAllocation, persons, county, ctFMAPRecord, maximumTitleIVEReimbursibleAmount, ref _fundsUsedCounter, ref _initialAmountOwed, ref _remainingServiceAmountOwed);
                }
                else if (iVE == F_fundallocationStatic.DefaultValues.Reimbursable && (percentageType == F_fundallocationStatic.DefaultValues.Tribalfmap || percentageType == F_fundallocationStatic.DefaultValues.Tribalfmapremainder))
                {
                    if (!_childIsIVEReimbursible)
                    {
                        eventHelper.AddWarningLog("Child is not IVE eligible, skipping fund allocation: {fundAllocation}");
                        continue;
                    }
                    if (ctTribalFMAPRecord == null)
                    {
                        ctTribalFMAPRecord = GetCTTribalFMAPRateRecord(eventHelper, serviceUtilization, fundAllocation);
                    }
                    HandleIVEReimbursibleTribalFMAPFund(eventHelper, triggeringUser, serviceUtilization, fundAllocation, persons, county, ctTribalFMAPRecord, maximumTitleIVEReimbursibleAmount, ref _fundsUsedCounter, ref _initialAmountOwed, ref _remainingServiceAmountOwed);
                }
                else if (iVE == F_fundallocationStatic.DefaultValues.Reimbursablewithsignedvssa)
                {
                    if (!_childIsIVEReimbursible)
                    {
                        eventHelper.AddWarningLog("Child is not IVE reimbursible, skipping fund allocation: {fundAllocation}");
                        continue;
                    }
                    HandleIVEReimbursibleSignedVSSAFund(eventHelper, triggeringUser, serviceUtilization, fundAllocation, persons, county, placement, iVEEligibility, maximumTitleIVEReimbursibleAmount, ref _fundsUsedCounter, ref _initialAmountOwed, ref _remainingServiceAmountOwed);
                }
                else
                {
                    var percentToPay = fundAllocation.Percentage;
                    if (string.IsNullOrWhiteSpace(percentToPay)) percentToPay = 100.ToString();
                    HandleFundAllocations(eventHelper, triggeringUser, fundAllocation.Fund(), persons, serviceUtilization, county, fundAllocation, percentToPay, ref _initialAmountOwed, ref _remainingServiceAmountOwed, ref _fundsUsedCounter);
                }

            }
            if (_remainingServiceAmountOwed > 0)
            {
                eventHelper.AddWarningLog($"{TechName} - There is a remaining Balance - Issue must be resolved.");
                errorMsg = "There is a remaining Balance - Issue must be resolved.";
                CreateBlankInitialFundDisRecord(eventHelper, serviceUtilization, errorMsg, ref _fundsUsedCounter);
            }

            var approvalStatus = serviceUtilization.O_status;
            if (approvalStatus == F_serviceplanutilizationStatic.DefaultValues.Pendingpayment_approved_)
            {
                recordInsData.ReadOnly = true;
                serviceUtilization.ReadOnly = true;
                serviceUtilization.SaveRecord();
            }

            return new EventReturnObject(EventStatusCode.Success);
        }
    }
}
