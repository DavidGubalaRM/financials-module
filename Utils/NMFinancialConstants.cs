using MCaseCustomEvents.NMImpact.Generated.Entities;
using System.Collections.Generic;

namespace MCase.Event.NMImpact.Constants
{
    public static class NMFinancialConstants
    {
        public static class ErrorMessages
        {
            public const string inValidCombination = "Invalid combination of placement setting {0} and placement type {1}.";
            public const string noProviderServicesFound = "The selected Provider {0} does not offer any Services.";
            public const string serviceNotOffered = "Provider {0} does not offer the required service ({1}) based on the selections.";
            public const string placementNotMapped = "The placement setting/type {0}/{1} could not be mapped to a Service Type.";
            public const string inActiveServiceFound = "Required Service ({0}) was found, but it is currently Inactive.";
            public const string serviceCatalogNotFound = "Service Catalog not found for code {0}.";
            public const string rateOverrideNeeded = "Placement requires an Approved Rate Override Record for Submit for Approval button to show";
        }
        public static class ServiceCatalogServices
        {
            public const string CongregateCareCommunityHome = "SC8";
            public const string CongregateCareMultiServicHome = "SC11";
            public const string CongregateCarePregnantAndParentingHome = "SC12";
            public const string CongregateCareShelter = "SC15";
            public const string ExtendedFosterCarePregnantAndParentingYouth = "SC17";
            public const string ExtendedFosterCareBasicYouthAsPayee = "SC18";
            public const string AdoptionPreDecreeLevel1 = "SC1";
            public const string AdoptionPreDecreeLevel2 = "SC2";
            public const string AdoptionPreDecreeLevel3 = "SC3";
            public const string AdoptionPreDecreeOutOfState = "SC4";
            public const string ResourceFamilyFosterCareLevel1 = "SC28";
            public const string ResourceFamilyFosterCareLevel2 = "SC29";
            public const string ResourceFamilyFosterCareLevel3 = "SC30";
            public const string ResourceFamilyFosterCareOutOfState = "SC31";
            public const string ARCAHome = "SC5";
            public const string RelativeTreatmentFosterCareAgency = "SC27";
            public const string TreatmentFosterCareAgency = "SC34";
            public const string IVESubsidizedAdoptionPostDecree = "SC25";
            public const string IVETribalSubsidizedAdoptionPostDecree = "SC24";
            public const string StateSubsidizedAdoptionPostDecree = "SC32";
            public const string StateTribalIGAAdoptionPostDecree = "SC33";
            public const string GuardianshipSubsidyGapIVE = "SC19";
            public const string GuardianshipSubsidyGapIVETribal = "SC20";
            public const string GuardianshipSubsidyState = "SC21";
            public const string GuardianshipSubsidyStateTribal = "SC22";
            public const string IndependentLivingPlacementUnder18 = "SC23";
            public const string OutOfStateCongregateCare = "SC26";
            public const string CongregateCareAcuteHospitalBehavioral = "SC7";
            public const string CongregateCareAcuteHospitalMedical = "SC6";
            public const string CongregateCareGroupHomeCare = "SC9";
            public const string CongregateCareLongTermCareFacilities = "SC10";
            public const string CongregateCareQualifiedResidentialTreatmentProgram = "SC13";
            public const string CongregateCareRTCJHACOAccredited = "SC14";
            public const string TYLA1820 = "SC210";
            public const string TYLA2123 = "SC211";
            public const string Respite = "SC35";
        }

        // Non traditional and out of state placements not included below and are not paid services
        public static Dictionary<string, List<(List<string> PlacementTypes, string RequiredService)>> requiredServiceMap = new Dictionary<string, List<(List<string>, string)>>()
        {
            {
                // these all map to individual services
                PlacementsStatic.DefaultValues.Congregatecaresetting,
                new List<(List<string>, string)>
                {
                    (new List<string> { PlacementsStatic.DefaultValues.Communityhomes }, NMFinancialConstants.ServiceCatalogServices.CongregateCareCommunityHome),
                    (new List<string> { PlacementsStatic.DefaultValues.Multiservicehome }, NMFinancialConstants.ServiceCatalogServices.CongregateCareMultiServicHome),
                    (new List<string> { PlacementsStatic.DefaultValues.Pregnantandparentinghome }, NMFinancialConstants.ServiceCatalogServices.CongregateCarePregnantAndParentingHome),
                    (new List<string> { PlacementsStatic.DefaultValues.Shelter }, NMFinancialConstants.ServiceCatalogServices.CongregateCareShelter)
                }
            },
            {
                PlacementsStatic.DefaultValues.Extendedfostercaresetting,
                new List<(List<string>, string)>
                {
                    // look at pregnancy and parenting DL
                    (new List<string> { PlacementsStatic.DefaultValues.Supervisedindependentliving }, NMFinancialConstants.ServiceCatalogServices.ExtendedFosterCarePregnantAndParentingYouth),
                    (new List<string> { PlacementsStatic.DefaultValues.Supervisedindependentliving }, NMFinancialConstants.ServiceCatalogServices.ExtendedFosterCareBasicYouthAsPayee),
                    // these each map to their own service - good here.
                    (new List<string> { PlacementsStatic.DefaultValues.Transitionallivingforyoungadults_tlya_18_20 }, NMFinancialConstants.ServiceCatalogServices.TYLA1820),
                }
            },
            {
                PlacementsStatic.DefaultValues.Subsidyandmedicaidplacements,
                new List<(List<string>, string)>
                {
                    (new List<string> { PlacementsStatic.DefaultValues.Adoptionsubsidy }, NMFinancialConstants.ServiceCatalogServices.IVESubsidizedAdoptionPostDecree),
                    (new List<string> { PlacementsStatic.DefaultValues.Adoptionsubsidy }, NMFinancialConstants.ServiceCatalogServices.IVETribalSubsidizedAdoptionPostDecree),
                    (new List<string> { PlacementsStatic.DefaultValues.Adoptionsubsidy }, NMFinancialConstants.ServiceCatalogServices.StateSubsidizedAdoptionPostDecree),
                    (new List<string> { PlacementsStatic.DefaultValues.Adoptionsubsidy }, NMFinancialConstants.ServiceCatalogServices.StateTribalIGAAdoptionPostDecree),
                    (new List<string> { PlacementsStatic.DefaultValues.Guardianshipsubsidy }, NMFinancialConstants.ServiceCatalogServices.GuardianshipSubsidyGapIVE),
                    (new List<string> { PlacementsStatic.DefaultValues.Guardianshipsubsidy }, NMFinancialConstants.ServiceCatalogServices.GuardianshipSubsidyGapIVETribal),
                    (new List<string> { PlacementsStatic.DefaultValues.Guardianshipsubsidy }, NMFinancialConstants.ServiceCatalogServices.GuardianshipSubsidyState),
                    (new List<string> { PlacementsStatic.DefaultValues.Guardianshipsubsidy }, NMFinancialConstants.ServiceCatalogServices.GuardianshipSubsidyStateTribal),
                }
            },
            {
                PlacementsStatic.DefaultValues.Outofhomeplacement_familyhomesetting,
                new List<(List<string>, string)>
                {
                    // level 1, 2, 3 from placement.
                    (new List<string>{ PlacementsStatic.DefaultValues.Non_relativefosterhome, PlacementsStatic.DefaultValues.Relativefosterhome, PlacementsStatic.DefaultValues.Fictivekinfosterhome, PlacementsStatic.DefaultValues.Nativeamericanfosterhome }, NMFinancialConstants.ServiceCatalogServices.AdoptionPreDecreeLevel1),
                    (new List<string>{ PlacementsStatic.DefaultValues.Non_relativefosterhome, PlacementsStatic.DefaultValues.Relativefosterhome, PlacementsStatic.DefaultValues.Fictivekinfosterhome, PlacementsStatic.DefaultValues.Nativeamericanfosterhome }, NMFinancialConstants.ServiceCatalogServices.AdoptionPreDecreeLevel2),
                    (new List<string>{ PlacementsStatic.DefaultValues.Non_relativefosterhome, PlacementsStatic.DefaultValues.Relativefosterhome, PlacementsStatic.DefaultValues.Fictivekinfosterhome, PlacementsStatic.DefaultValues.Nativeamericanfosterhome }, NMFinancialConstants.ServiceCatalogServices.AdoptionPreDecreeLevel3),
                    // check if child has ICPC Outgoing Record 
                    (new List<string>{ PlacementsStatic.DefaultValues.Non_relativefosterhome, PlacementsStatic.DefaultValues.Relativefosterhome, PlacementsStatic.DefaultValues.Fictivekinfosterhome, PlacementsStatic.DefaultValues.Nativeamericanfosterhome }, NMFinancialConstants.ServiceCatalogServices.AdoptionPreDecreeOutOfState),
                    (new List<string>{ PlacementsStatic.DefaultValues.Non_relativefosterhome, PlacementsStatic.DefaultValues.Relativefosterhome, PlacementsStatic.DefaultValues.Fictivekinfosterhome, PlacementsStatic.DefaultValues.Nativeamericanfosterhome }, NMFinancialConstants.ServiceCatalogServices.ResourceFamilyFosterCareLevel1),
                    (new List<string>{ PlacementsStatic.DefaultValues.Non_relativefosterhome, PlacementsStatic.DefaultValues.Relativefosterhome, PlacementsStatic.DefaultValues.Fictivekinfosterhome, PlacementsStatic.DefaultValues.Nativeamericanfosterhome }, NMFinancialConstants.ServiceCatalogServices.ResourceFamilyFosterCareLevel2),
                    (new List<string>{ PlacementsStatic.DefaultValues.Non_relativefosterhome, PlacementsStatic.DefaultValues.Relativefosterhome, PlacementsStatic.DefaultValues.Fictivekinfosterhome, PlacementsStatic.DefaultValues.Nativeamericanfosterhome }, NMFinancialConstants.ServiceCatalogServices.ResourceFamilyFosterCareLevel3),
                    // check if child has ICPC Outgoing Record
                    (new List<string>{ PlacementsStatic.DefaultValues.Non_relativefosterhome, PlacementsStatic.DefaultValues.Relativefosterhome, PlacementsStatic.DefaultValues.Fictivekinfosterhome, PlacementsStatic.DefaultValues.Nativeamericanfosterhome }, NMFinancialConstants.ServiceCatalogServices.ResourceFamilyFosterCareOutOfState)
                }
            },
            {
                PlacementsStatic.DefaultValues.Outofhomeplacement_privatefamilyhomesetting,
                new List<(List<string>, string)>
                {
                    (new List<string>{ PlacementsStatic.DefaultValues.Childplacementagencyhome }, NMFinancialConstants.ServiceCatalogServices.ARCAHome),
                    (new List<string>{ PlacementsStatic.DefaultValues.Familybasedshelter, PlacementsStatic.DefaultValues.Treatmentfostercarehome }, NMFinancialConstants.ServiceCatalogServices.TreatmentFosterCareAgency),
                }
            },
            {
                PlacementsStatic.DefaultValues.Therapeuticsetting,
                new List<(List<string>, string)>
                {
                    (new List<string>{ PlacementsStatic.DefaultValues.Residentialtreatmentcare }, NMFinancialConstants.ServiceCatalogServices.CongregateCareQualifiedResidentialTreatmentProgram),
                    (new List<string>{ PlacementsStatic.DefaultValues.Residentialtreatmentcare }, NMFinancialConstants.ServiceCatalogServices.CongregateCareRTCJHACOAccredited),
                    // check if child has ICPC Outgoing Record 
                    (new List<string>{ PlacementsStatic.DefaultValues.Residentialtreatmentcare }, NMFinancialConstants.ServiceCatalogServices.OutOfStateCongregateCare),
                    // following all have their own service
                    (new List<string>{ PlacementsStatic.DefaultValues.Acutehospitalstays_behavioral_psychiatric }, NMFinancialConstants.ServiceCatalogServices.CongregateCareAcuteHospitalBehavioral),
                    (new List<string>{ PlacementsStatic.DefaultValues.Acutehospitalstays_medical }, NMFinancialConstants.ServiceCatalogServices.CongregateCareAcuteHospitalMedical),
                    (new List<string>{ PlacementsStatic.DefaultValues.Grouphomecare }, NMFinancialConstants.ServiceCatalogServices.CongregateCareGroupHomeCare),
                    (new List<string>{ PlacementsStatic.DefaultValues.Longtermcarefacilities }, NMFinancialConstants.ServiceCatalogServices.CongregateCareLongTermCareFacilities),
                }
            },
            {
                PlacementsStatic.DefaultValues.Outofstateplacementsetting,
                new List<(List<string>, string)>
                {
                   (new List<string>{ PlacementsStatic.DefaultValues.Outofstatefosterfamilynonrelative, PlacementsStatic.DefaultValues.Outofstatefosterfamilyrelative }, NMFinancialConstants.ServiceCatalogServices.AdoptionPreDecreeOutOfState),
                   (new List<string>{ PlacementsStatic.DefaultValues.Outofstatefosterfamilynonrelative, PlacementsStatic.DefaultValues.Outofstatefosterfamilyrelative }, NMFinancialConstants.ServiceCatalogServices.ResourceFamilyFosterCareOutOfState),
               }
            },

        };
        public static class AccountingAPI
        {
            // the end point token cannot contain an ampersand
            public const string EndPoint = "FINANCEGATEWAY_URL";
            public const string EndPointToken = "FINANCEGATEWAY_KEY";
        }

        public static class ActionTypes
        {
            public const string CreateAccount = "CreateAccount";
            public const string DepositFunds = "DepositFunds";
            public const string CommitFunds = "CommitFunds";
            public const string ActualFunds = "ActualFunds";
            public const string OverUnderPayments = "OverUnderPayments";
            public const string StopPayment = "StopPayment";
            public const string StartPayment = "StartPayment";
        }

        public static class TransactionTypes
        {
            public const string DepositFunds = "D";
            public const string TransferFunds = "T";
        }
    }
}