# Finance Gateway Messages

Message classes used in the `Data` field of `AMessage`. Defined in `Utils/FinanceModels.cs`.

## BaseMessage

All data messages extend `BaseMessage`:

```csharp
public class BaseMessage
{
    public string RecordId { get; set; }   // mCase record ID
    public string ModifiedBy { get; set; } // Username of modifying user
}
```

## AccountMessage

Used for the **CreateAccount** action.

```csharp
public class AccountMessage : BaseMessage
{
    public string FundName { get; set; }
    public string FundCode { get; set; }
}
```

**When used**: `CreateFundBalanceNewFundAdded` sends this when a new fund is created, passing the fund name and code to the accounting system.

## ManageFundsMessage

Used for **DepositFunds**, **CommitFunds**, **ActualFunds**, **StartPayment**, and **StopPayment**.

```csharp
public class ManageFundsMessage : BaseMessage
{
    public string TransactionType { get; set; }  // "D" (Deposit) or "T" (Transfer)
    public string FromRecordId { get; set; }     // Record ID of source Fund Balance (for transfers)
}
```

### TransactionTypes

| Value | Meaning | When Used |
|-------|---------|-----------|
| `"D"` | Deposit | Adding funds to an account |
| `"T"` | Transfer | Moving funds between accounts |

**Note**: `TransactionType` and `FromRecordId` are primarily used with the **DepositFunds** action. For other actions (CommitFunds, ActualFunds, etc.), typically only `RecordId` and `ModifiedBy` are populated.

## OverUnderMessage

Used for the **OverUnderPayments** action.

```csharp
public class OverUnderMessage : BaseMessage
{
    public string StartDate { get; set; }
    public OverUnderDetails Previous { get; set; }
    public OverUnderDetails New { get; set; }
}

public class OverUnderDetails
{
    public long ServiceAuthId { get; set; }
    public long ProviderId { get; set; }
}
```

This message carries both the previous and new service authorization/provider details to allow the accounting system to adjust records.

## Examples

### Actual Funds (Invoice Paid)

```csharp
ManageFundsMessage fundMessage = new ManageFundsMessage()
{
    RecordId = invoiceRecord.RecordInstanceID.ToString(),
    ModifiedBy = triggeringUser.UserName
};

AMessage aMessage = new AMessage()
{
    Action = NMFinancialConstants.ActionTypes.ActualFunds,
    Data = fundMessage
};
```

### Deposit with Transfer

```csharp
ManageFundsMessage fundMessage = new ManageFundsMessage()
{
    TransactionType = NMFinancialConstants.TransactionTypes.TransferFunds,  // "T"
    RecordId = depositsRecord.RecordInstanceID.ToString(),
    FromRecordId = transferFromId.ToString(),
    ModifiedBy = triggeringUser.UserName
};

AMessage aMessage = new AMessage()
{
    Action = NMFinancialConstants.ActionTypes.DepositFunds,
    Data = fundMessage
};
```

### Create Account (New Fund)

```csharp
AccountMessage fundMessage = new AccountMessage()
{
    FundName = fundRecord.Fundname,
    FundCode = fundRecord.Fundcode,
    RecordId = fundRecord.RecordInstanceID.ToString(),
    ModifiedBy = triggeringUser.UserName
};

AMessage aMessage = new AMessage()
{
    Action = NMFinancialConstants.ActionTypes.CreateAccount,
    Data = fundMessage
};
```
