namespace MCase.Event.NMImpact.Financials
{
    /// <summary>
    /// Classes to support accounting activities in the middle-tier removing the need
    /// to pass in mCase data objects and just the minimum required information
    /// The middle-tier follows mCase structures and any external (accounting) activity
    /// is handled within the abstraction layer.
    /// </summary>

    // Base class for all messages
    public class BaseMessage
    {
        public string RecordId { get; set; } = string.Empty;
        public string ModifiedBy { get; set; }
    }

    // Message send to middle-tier. Data can be any of the other messages
    public class AMessage
    {
        public string Action { get; set; }
        public object Data { get; set; }
    }

    #region Messages send in the Data field

    #region Create an account
    public class AccountMessage : BaseMessage
    {
        public string FundName { get; set; } = string.Empty;
        public string FundCode { get; set; } = string.Empty;
    }
    #endregion

    #region Deposit / Transfer / Commit / Actual Funds
    public class ManageFundsMessage : BaseMessage
    {
        public string TransactionType { get; set; }
        // Record ID of Fund Balance weare transferring from
        public string FromRecordId { get; set; } = string.Empty;
    }
    #endregion

    #region Over/Uder payments
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
    #endregion

    #endregion

}