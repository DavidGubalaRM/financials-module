# Service Request Module

The Service Request module handles multi-level approval workflows for financial service requests (e.g., MFD -- Miscellaneous Financial Disbursement).

## Events

### ServiceRequestApproval

**Trigger**: PostCreate, PostUpdate on Service Request

**Purpose**: Manage the approval flow and create approval history records.

#### Post-create Logic

1. Get Service Catalog record for the requested service type
2. Read approval levels (Level 1, 2, 3, Final) from the Service Catalog
3. For **OTA QA Manager** services:
   - If amount is $1,000 or less: single-level approval (OTA QA Manager is final)
   - If amount exceeds $1,000: two-level approval (OTA QA Manager is Level 1, OTA Deputy Director is final)
   - OTA approvers are resolved via **Event Value Mapping** (EVM) keys: `OTAQAMANAGERJOBTITLE`, `OTADEPUTYDIRECTORJOBTITLE`
4. For standard services: resolve approver users from the supervisor chain (submitter → supervisor → manager → associate deputy director → deputy director)
5. Set `Level1approver`, `Level2approver`, `Level3approver`, `Finallevelapprover` fields and their required/user fields
6. Set `Nextapprovallevel` and `Multilevelapproval` flag

#### Post-update Logic

1. Create approval history record based on `Approvalhistorytype` (submission, approval, rejection for each level)
2. Advance `Nextapprovallevel` to the next required level based on which levels have determinations
3. Set record to **ReadOnly** when status is Approved, Rejected, or Permanently Rejected

#### Approval Chain

| Role | Config Key | Source |
|------|-----------|--------|
| Supervisor | From Service Catalog Level 1-3/Final | User's SupervisorUserID |
| Manager | From Service Catalog | Supervisor's SupervisorUserID |
| Associate Deputy Director | From Service Catalog | Manager's SupervisorUserID |
| Deputy Director | From Service Catalog | Assoc. Deputy Dir's SupervisorUserID |
| OTA QA Manager | `OTAQAMANAGERJOBTITLE` | Staff Member lookup via EVM |
| OTA Deputy Director | `OTADEPUTYDIRECTORJOBTITLE` | Staff Member lookup via EVM |

### ServiceRequestApprovalServiceAuthUtil

**Trigger**: PostUpdate on Service Request

**Purpose**: When status becomes **Approved**, create a Service Authorization and Service Utilization records.

**Logic**:
1. Create Service Authorization under parent Case or Investigation
2. Set authorization fields: source type = Service Request, provider, service catalog, dates
3. Calculate units authorized based on recurrence:
   - **One-time**: 1 unit, 1 Service Utilization
   - **Weekly**: Units = weeks between start and end date
   - **Monthly**: Units = months between start and end date
4. Create Service Utilization records for each unit period with appropriate dates and amounts

### ServiceRequestValidations

**Trigger**: PostCreate, PreUpdate on Service Request

**Purpose**: Validate Service Request data.

**Validations**:
1. **Participant role restrictions**:
   - Parent/Guardian/Custodian participants: only "Foster Care Parent Incidentals" and "Involuntary Family Services" categories allowed
   - Child/Youth participants: those same categories are NOT allowed
2. **Multiple participants**: Cannot select multiple Child/Youth participants if the funding model uses Title IV-E or child-specific funds
3. **Ceiling validation**: Checks amount against frequency-based ceilings:
   - Per incident
   - Per child per calendar year
   - Per child per month
   - Per foster parent (lifetime)
   - Calculates existing utilization amounts to enforce ceiling

### ServiceRequestMirrorFields

**Trigger**: PostCreate on Service Request

**Purpose**: Collect medications and future medical appointments for all selected participants and populate HTML-formatted fields on the Service Request. Includes child name information.

### ServiceRequestReminderNotifications

**Trigger**: PostCreate on Service Request (via batch notification triggers)

**Purpose**: Send 1st and 2nd reminder notifications if Service Request is not approved within 48 hours. Notifications go to approvers and submitter with appropriate subject and message.

## Approval History

`CreateServiceRequestApprovalHistory` creates approval history records for multi-level approval Service Requests. Handles submission records for Level 2, Level 3, and Final Level when the request automatically advances through levels.

Standard approval history types handled by `ServiceRequestApproval`:
- Level 1/2/3: Submission, Approval, Rejection
- Final Level: Submission, Approval, Rejection, Temporary Rejection
