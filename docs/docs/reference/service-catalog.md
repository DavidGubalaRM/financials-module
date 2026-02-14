# Service Catalog Reference

The Service Catalog defines service types used for placement-based payments. Each service has a unique code and maps to placement settings/types via `NMFinancialConstants.requiredServiceMap`.

## Service Codes (NMFinancialConstants.ServiceCatalogServices)

### Congregate Care
| Code | Service Name |
|------|--------------|
| SC6 | Congregate Care Acute Hospital Medical |
| SC7 | Congregate Care Acute Hospital Behavioral |
| SC8 | Congregate Care Community Home |
| SC9 | Congregate Care Group Home Care |
| SC10 | Congregate Care Long Term Care Facilities |
| SC11 | Congregate Care Multi-Service Home |
| SC12 | Congregate Care Pregnant and Parenting Home |
| SC13 | Congregate Care Qualified Residential Treatment Program (QRTP) |
| SC14 | Congregate Care RTC JHACO Accredited |
| SC15 | Congregate Care Shelter |
| SC26 | Out of State Congregate Care |

### Extended Foster Care
| Code | Service Name |
|------|--------------|
| SC17 | Extended Foster Care Pregnant and Parenting Youth |
| SC18 | Extended Foster Care Basic Youth As Payee |
| SC210 | TYLA 18-20 |
| SC211 | TYLA 21-23 |

### Family Home Placements
| Code | Service Name |
|------|--------------|
| SC1 | Adoption Pre-Decree Level 1 |
| SC2 | Adoption Pre-Decree Level 2 |
| SC3 | Adoption Pre-Decree Level 3 |
| SC4 | Adoption Pre-Decree Out of State |
| SC28 | Resource Family Foster Care Level 1 |
| SC29 | Resource Family Foster Care Level 2 |
| SC30 | Resource Family Foster Care Level 3 |
| SC31 | Resource Family Foster Care Out of State |

### Private Family Home
| Code | Service Name |
|------|--------------|
| SC5 | ARCA Home |
| SC27 | Relative Treatment Foster Care Agency |
| SC34 | Treatment Foster Care Agency |

### Subsidy & Medicaid
| Code | Service Name |
|------|--------------|
| SC24 | IVE Tribal Subsidized Adoption Post Decree |
| SC25 | IVE Subsidized Adoption Post Decree |
| SC32 | State Subsidized Adoption Post Decree |

| SC33 | State Tribal IGA Adoption Post Decree |
| SC19 | Guardianship Subsidy Gap IVE |
| SC20 | Guardianship Subsidy Gap IVE Tribal |
| SC21 | Guardianship Subsidy State |
| SC22 | Guardianship Subsidy State Tribal |

### Other
| Code | Service Name |
|------|--------------|
| SC23 | Independent Living Placement Under 18 |
| SC35 | Respite |

## Placement Mapping

The `requiredServiceMap` dictionary maps:
- **Key**: Placement setting (e.g., `PlacementsStatic.DefaultValues.Outofhomeplacement_familyhomesetting`)
- **Value**: List of (PlacementTypes, RequiredService) tuples

Example: For "Out of Home - Family Home" setting with "Non-relative foster home" type, possible services include SC28, SC29, SC30 (Level 1, 2, 3) based on Level of Care.

## Rate Determination

Each Service Catalog record has a `Ratedetermination` field:
- `Rateoverride` — Use approved Rate Override on placement
- `Servicerateagreement` — Use provider's Service Rate Agreement
- `Standardservicerateagreement` — Use Standard Service Rate Agreement
- `Adoptionassistanceagreement` — Use Adoption Assistance Agreement
- `Guardianshipassistanceagreement` — Use Guardianship Assistance Agreement
- `Norate` — No rate; no Service Authorization created
