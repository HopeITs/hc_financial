using Financial_WPF_App.Models;
using System;
using System.Data;

namespace Financial_WPF_App.Helper
{
    public class AddActionSaveContext
    {
        // The currency ids that used to sit here as constants are gone. They assumed the
        // tilltypes rows carry the same ids in every database, which nothing enforces,
        // and they made a newly added local currency impossible to express. Ask
        // CurrencyService for a currency by name instead.

        public int ActionId { get; set; } = -1;
        public bool ApprovedAction { get; set; }
        public string OperationTypeText { get; set; }
        public string AccountType { get; set; }
        public string TillType { get; set; }
        public int AccountId { get; set; }
        public int TillId { get; set; }
        public int SubprojectId { get; set; }
        public string AmountText { get; set; }
        public DateTime? ActionDate { get; set; }
        public string Notes { get; set; }
        public string ENotes { get; set; }
        public AddActionOperationFields Operation { get; set; } = new AddActionOperationFields();
        public DataTable CorrespondingTills { get; set; }
        public DataTable Tills { get; set; }
        public string[] AttachmentPaths { get; set; }
        public string SourceFormName { get; set; } = "AddAction_Window2";
        public Models.Action ActionModel { get; set; }
        public Rate RateModel { get; set; }
        public bool RequireExpenseCurrencySelection { get; set; }

        /// <summary>Invoked when currency validation fails (original focuses currency_comboBox).</summary>
        public System.Action FocusCurrencySelection { get; set; }
    }

    public class AddActionOperationFields
    {
        public bool CashAccountRequired { get; set; }
        public int CashAccountSelectedIndex { get; set; } = -1;
        public int CashAccountId { get; set; } = -1;

        public bool MicroProjectRequired { get; set; }
        public string MicroProjectText { get; set; }
        public string BeneficiaryNameText { get; set; }

        public bool BudgetLineRequired { get; set; }
        public int BudgetLineSelectedIndex { get; set; } = -1;
        public int BudgetLineId { get; set; } = -1;

        public bool DocumentTypeRequired { get; set; }
        public int DocumentTypeSelectedIndex { get; set; } = -1;
        public string DocumentTypeItemText { get; set; }

        public bool DocumentNumberRequired { get; set; }
        public string DocumentNumber { get; set; }

        public bool OriginalAvgRateRequired { get; set; }
        public int OriginalAvgRateSelectedIndex { get; set; } = -1;
        public int OriginalRateId { get; set; } = -1;

        public bool DonorRequired { get; set; }
        public int DonorSelectedIndex { get; set; } = -1;
        public int DonorId { get; set; } = -1;

        public bool RateRequired { get; set; }
        public string RateText { get; set; }

        public bool DestinationTillRequired { get; set; }
        public int DestinationTillSelectedIndex { get; set; } = -1;
        public int DestinationTillId { get; set; } = -1;
        public string DestinationTillDisplayText { get; set; }
        public string CorrespondingTillType { get; set; }

        public bool ObserveCashAccountRequired { get; set; }
        public int ObserveCashAccountSelectedIndex { get; set; } = -1;
        public int ObserveCashAccountId { get; set; } = -1;

        public bool ExchangeAccountEnabled { get; set; }
        public int ExchangeAccountSelectedIndex { get; set; } = -1;
        public int ExchangeAccountId { get; set; } = -1;

        public bool ExchangeCashRequired { get; set; }
        public int ExchangeCashSelectedIndex { get; set; } = -1;
        public int ExchangeCashId { get; set; } = -1;

        public bool EmployeeRequired { get; set; }
        public int EmployeeSelectedIndex { get; set; } = -1;
        public int EmployeeSalaryId { get; set; } = -1;

        public int SelectedCurrencyId { get; set; } = -1;
    }

    public class AddActionSaveResult
    {
        public bool Success { get; set; }
        public bool ValidationFailed { get; set; }
        public bool IsEditSave { get; set; }
        public int SavedActionId { get; set; } = -1;
    }
}
