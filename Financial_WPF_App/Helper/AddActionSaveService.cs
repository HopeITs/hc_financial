using Financial_WPF_App.Models;
using System;
using System.Data;
using System.Windows;
using ActionModel = Financial_WPF_App.Models.Action;

namespace Financial_WPF_App.Helper
{
    public static class AddActionSaveService
    {
        public static AddActionSaveResult Execute(AddActionSaveContext context)
        {
            var result = new AddActionSaveResult();
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (context.ActionModel == null)
                context.ActionModel = new ActionModel();
            if (context.RateModel == null)
                context.RateModel = new Rate();

            var document = new Document();
            var op = context.Operation ?? new AddActionOperationFields();

            string insertedValue = string.Empty;
            double amount;
            string actionDate = string.Empty;
            string observeTillType = string.Empty;
            int destinationTillId = -1;
            int exchangeAccId = -1;
            int donorId = -1;
            int microProjectId = -1;
            int budgetLineId = -1;
            int cashAccountId = -1;
            int observeCashAccountId = -1;
            int exchangeCashId = -1;
            int isReceipt = -1;
            int originalRateId = -1;
            string receiptNum = string.Empty;
            int employeeSalaryId = -1;
            bool withExAcc = false;
            bool withExchangeRate = false;
            double rate = 0;

            #region Check and get data

            if (string.IsNullOrWhiteSpace(context.OperationTypeText))
            {
                MessageBox.Show("Please select the operation type.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                result.ValidationFailed = true;
                return result;
            }

            insertedValue = AddActionSaveHelpers.ResolveInsertedValue(context.OperationTypeText);
            if (string.IsNullOrEmpty(insertedValue))
            {
                MessageBox.Show("Please select the operation type.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                result.ValidationFailed = true;
                return result;
            }

            if (insertedValue == "E")
            {
                // Personal accounts derive currency from the Till (currency ComboBox is disabled).
                // Add Action 2 can explicitly require a selection for project SYR expenses.
                bool personalUsesTillCurrency = string.Equals(context.AccountType, "Personal", StringComparison.OrdinalIgnoreCase);
                bool currencyRequired = context.RequireExpenseCurrencySelection ||
                    (!personalUsesTillCurrency &&
                     !AddActionSaveHelpers.IsExpenseFromSyrTill(insertedValue, context.TillType));
                if (currencyRequired && op.SelectedCurrencyId <= 0)
                {
                    MessageBox.Show("Please make sure to select the currency.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    if (context.FocusCurrencySelection != null)
                        context.FocusCurrencySelection();
                    result.ValidationFailed = true;
                    return result;
                }
            }
            // Project Exchange AvgRate availability is checked after Amount/Date validation below.

            if (string.IsNullOrEmpty(context.AmountText))
            {
                MessageBox.Show("Please make sure to enter the amount.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                result.ValidationFailed = true;
                return result;
            }

            if (!InputValidationHelper.TryParseFormattedNumber(context.AmountText, out amount))
            {
                MessageBox.Show("Please make sure to enter a valid amount.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                result.ValidationFailed = true;
                return result;
            }

            if (context.ActionDate == null)
            {
                MessageBox.Show("Please make sure to select the operation date.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                result.ValidationFailed = true;
                return result;
            }

            actionDate = context.ActionDate.Value.ToString("yyyy/MM/dd");

            if (insertedValue == "Ex")
            {
                bool projectOriginAvgRateRequired = context.AccountType == "Project" &&
                    AddActionSaveHelpers.IsProjectExchangeOriginAvgRateRequired(
                        context.TillType, op.CorrespondingTillType);

                if (projectOriginAvgRateRequired)
                {
                    string rateFromTill;
                    string rateToTill;
                    AddActionSaveHelpers.GetProjectExchangeOriginAvgRatePair(
                        context.TillType, op.CorrespondingTillType, out rateFromTill, out rateToTill);

                    if (!AddActionSaveHelpers.CheckAvgRate(context.RateModel, context.AccountId.ToString(),
                            context.ActionDate.Value, rateFromTill, rateToTill))
                    {
                        MessageBox.Show("Please ensure that there is at least one AvgRate that matches the conversion currency and extends for a period that includes the transaction date.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                        result.ValidationFailed = true;
                        return result;
                    }
                }
            }

            if (op.CashAccountRequired && op.CashAccountSelectedIndex == -1)
            {
                MessageBox.Show("Please make sure to select the cash till.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                result.ValidationFailed = true;
                return result;
            }

            if (op.CashAccountSelectedIndex != -1)
                cashAccountId = op.CashAccountId;

            if (op.MicroProjectRequired)
            {
                if (!string.IsNullOrEmpty(op.MicroProjectText) && !string.IsNullOrEmpty(op.BeneficiaryNameText))
                    int.TryParse(op.MicroProjectText, out microProjectId);
                else
                    microProjectId = -1;

                if (microProjectId == -1)
                {
                    MessageBox.Show("Please make sure to select the micro project.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    result.ValidationFailed = true;
                    return result;
                }
            }

            if (op.BudgetLineRequired)
            {
                if (op.BudgetLineSelectedIndex == -1)
                {
                    MessageBox.Show("Please make sure to select the budget, category and budget line.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    result.ValidationFailed = true;
                    return result;
                }

                budgetLineId = op.BudgetLineId;
            }

            if (op.DocumentTypeRequired)
            {
                if (op.DocumentTypeSelectedIndex == -1 && context.AccountType != "Personal")
                {
                    MessageBox.Show("Please make sure to select the documet type and to specify its date", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    result.ValidationFailed = true;
                    return result;
                }

                if (op.DocumentTypeSelectedIndex != -1)
                {
                    string itemText = op.DocumentTypeItemText ?? string.Empty;
                    if (itemText.Contains("invoice"))
                        isReceipt = 1;
                    else if (itemText.Contains("receipt"))
                        isReceipt = 0;
                    else if (itemText.Contains("payslip"))
                        isReceipt = 2;
                }
            }

            if (op.DocumentNumberRequired)
            {
                if (string.IsNullOrEmpty(op.DocumentNumber) && context.AccountType != "Personal")
                {
                    MessageBox.Show("Please make sure to enter the document number", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    result.ValidationFailed = true;
                    return result;
                }

                receiptNum = op.DocumentNumber ?? string.Empty;
            }

            if (op.EmployeeRequired)
            {
                if (op.EmployeeSelectedIndex == -1)
                {
                    MessageBox.Show("Please make sure to select the Volunteer.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    result.ValidationFailed = true;
                    return result;
                }

                employeeSalaryId = op.EmployeeSalaryId;
            }

            if (op.OriginalAvgRateRequired && op.OriginalAvgRateSelectedIndex == -1)
            {
                // Project SYR expenses retain the original requirement. Project exchanges use
                // the pair-based required rule (USD→SYR is optional but may still be offered).
                bool projectSyrExpense = insertedValue == "E" && context.AccountType == "Project" &&
                    !string.IsNullOrEmpty(context.TillType) &&
                    context.TillType.Equals("syr", StringComparison.OrdinalIgnoreCase);
                bool projectExchangeOriginAvgRate = insertedValue == "Ex" &&
                    context.AccountType == "Project" &&
                    AddActionSaveHelpers.IsProjectExchangeOriginAvgRateRequired(
                        context.TillType, op.CorrespondingTillType);

                if (projectSyrExpense || projectExchangeOriginAvgRate)
                {
                    MessageBox.Show("Please make sure to select the origin Avg rate", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    result.ValidationFailed = true;
                    return result;
                }
            }

            // Persist a selected Origin Avg Rate even when the field is optional (e.g. USD→SYR).
            if (op.OriginalAvgRateSelectedIndex != -1)
                originalRateId = op.OriginalRateId;

            if (op.DonorRequired)
            {
                if (op.DonorSelectedIndex == -1)
                {
                    MessageBox.Show("Please make sure to select the donor", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    result.ValidationFailed = true;
                    return result;
                }

                donorId = op.DonorId;
            }

            if (op.RateRequired)
            {
                if (string.IsNullOrEmpty(op.RateText))
                {
                    MessageBox.Show("Please make sure to enter the rate", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                    result.ValidationFailed = true;
                    return result;
                }

                InputValidationHelper.TryParseFormattedNumber(op.RateText, out rate);
            }

            if (op.DestinationTillRequired || (context.ActionId > 0 && op.DestinationTillSelectedIndex != -1))
            {
                if (context.OperationTypeText.Contains("Exchange"))
                {
                    if (op.DestinationTillSelectedIndex == -1)
                    {
                        MessageBox.Show("Please make sure to select the Corresponding till.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                        result.ValidationFailed = true;
                        return result;
                    }

                    destinationTillId = op.DestinationTillId;
                    observeTillType = AddActionSaveHelpers.GetObserveTillType(context.CorrespondingTills, destinationTillId);
                    if (string.IsNullOrWhiteSpace(observeTillType))
                    {
                        MessageBox.Show("Please make sure to select a valid Corresponding till.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                        result.ValidationFailed = true;
                        return result;
                    }

                    if (context.AccountType == "Project" && op.ExchangeAccountSelectedIndex != -1)
                        withExAcc = true;

                    if (context.AccountType == "Exchange")
                        withExchangeRate = true;
                }
                else if (context.OperationTypeText.Contains("Move"))
                {
                    if (op.DestinationTillSelectedIndex == -1)
                    {
                        MessageBox.Show("Please make sure to select the Corresponding account.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                        result.ValidationFailed = true;
                        return result;
                    }

                    if (op.DestinationTillSelectedIndex != -1)
                        destinationTillId = op.DestinationTillId;

                    observeTillType = context.TillType;
                }
            }

            if (op.ObserveCashAccountRequired && op.ObserveCashAccountSelectedIndex == -1)
            {
                MessageBox.Show("Please make sure to select the corresponding cash till.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                result.ValidationFailed = true;
                return result;
            }

            if (op.ObserveCashAccountSelectedIndex != -1)
                observeCashAccountId = op.ObserveCashAccountId;

            if (op.ExchangeAccountEnabled || (context.ActionId > 0 && op.ExchangeAccountSelectedIndex != -1))
            {
                if (op.ExchangeAccountSelectedIndex != -1)
                {
                    exchangeAccId = op.ExchangeAccountId;

                    if (op.ExchangeCashRequired && op.ExchangeCashSelectedIndex == -1)
                    {
                        MessageBox.Show("Please make sure to select the Exchange cash till.", "Error", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
                        result.ValidationFailed = true;
                        return result;
                    }

                    if (op.ExchangeCashSelectedIndex != -1)
                        exchangeCashId = op.ExchangeCashId;
                }
            }

            string notes = context.Notes ?? string.Empty;
            string eNotes = context.ENotes ?? string.Empty;

            #endregion

            result.IsEditSave = context.ActionId != -1;

            if (context.ActionId != -1)
            {
                SaveExistingAction(context, document, insertedValue, amount, actionDate, observeTillType, destinationTillId,
                    exchangeAccId, donorId, microProjectId, budgetLineId, cashAccountId, observeCashAccountId,
                    exchangeCashId, isReceipt, originalRateId, receiptNum, employeeSalaryId, withExAcc, withExchangeRate,
                    rate, op.SelectedCurrencyId);
                result.Success = true;
                result.SavedActionId = context.ActionId;
                return result;
            }

            int selectedCurrencyId = op.SelectedCurrencyId;
            bool isPersonalAccount = string.Equals(context.AccountType, "Personal", StringComparison.OrdinalIgnoreCase);
            int currencyTilltypeId = isPersonalAccount
                ? -1
                : AddActionSaveHelpers.GetCurrencyTilltypeIdForAddActionSave(insertedValue, context.TillType, selectedCurrencyId);
            bool isSpecialCase = insertedValue == "E" &&
                                 !string.IsNullOrEmpty(context.TillType) &&
                                 context.TillType.ToLower() == "syr" &&
                                 selectedCurrencyId == AddActionSaveContext.UsdCurrencyTilltypeId;

            int savedActionId;
            if (isSpecialCase)
            {
                int usdTillId = AddActionSaveHelpers.GetUsdTillIdForCurrentAccount(context.Tills, context.AccountId);
                if (usdTillId == -1)
                {
                    MessageBox.Show("There is no USD till associated with this account");
                    result.ValidationFailed = true;
                    return result;
                }

                savedActionId = context.ActionModel.Insert(context.TillId, "syr", usdTillId, "usd", donorId, -1, true, false, false, "E",
                    amount, rate, actionDate, notes, eNotes, isReceipt, receiptNum, employeeSalaryId, originalRateId,
                    microProjectId, budgetLineId, -1, cashAccountId, observeCashAccountId, exchangeCashId, -1,
                    context.SourceFormName, currencyTilltypeId);
            }
            else if (insertedValue == "Ex" || insertedValue == "M")
            {
                savedActionId = context.ActionModel.Insert(context.TillId, context.TillType, destinationTillId, observeTillType,
                    donorId, exchangeAccId, true, withExAcc, withExchangeRate, insertedValue, amount, rate, actionDate,
                    notes, eNotes, isReceipt, receiptNum, employeeSalaryId, originalRateId, microProjectId, budgetLineId,
                    -1, cashAccountId, observeCashAccountId, exchangeCashId, context.AccountId, context.SourceFormName, -1);
            }
            else
            {
                savedActionId = context.ActionModel.Insert(context.TillId, context.TillType, destinationTillId, observeTillType,
                    donorId, -1, false, false, false, insertedValue, amount, rate, actionDate, notes, eNotes, isReceipt,
                    receiptNum, employeeSalaryId, originalRateId, microProjectId, budgetLineId, -1, cashAccountId,
                    observeCashAccountId, exchangeCashId, -1, context.SourceFormName, currencyTilltypeId);
            }

            int attachmentSuccess = SaveAttachments(context, savedActionId, document);
            ShowSuccessMessage(context.AttachmentPaths, attachmentSuccess);

            result.Success = true;
            result.SavedActionId = savedActionId;
            return result;
        }

        private static void SaveExistingAction(AddActionSaveContext context, Document document, string insertedValue, double amount, string actionDate,
            string observeTillType, int destinationTillId, int exchangeAccId, int donorId, int microProjectId, int budgetLineId,
            int cashAccountId, int observeCashAccountId, int exchangeCashId, int isReceipt, int originalRateId, string receiptNum,
            int employeeSalaryId, bool withExAcc, bool withExchangeRate, double rate, int selectedCurrencyId)
        {
            bool isUsdExpenseEdit = insertedValue == "E" &&
                                    !string.IsNullOrEmpty(context.TillType) &&
                                    context.TillType.ToLower() == "syr" &&
                                    selectedCurrencyId == AddActionSaveContext.UsdCurrencyTilltypeId;

            if (context.ApprovedAction)
            {
                if (insertedValue == "Ex" || insertedValue == "M")
                {
                    context.ActionModel.Update(context.ActionId, insertedValue, context.TillId, context.TillType, destinationTillId,
                        observeTillType, donorId, exchangeAccId, true, withExAcc, withExchangeRate, amount, rate, actionDate,
                        context.Notes, context.ENotes, isReceipt, receiptNum, employeeSalaryId, originalRateId, microProjectId,
                        budgetLineId, -1, cashAccountId, observeCashAccountId, exchangeCashId, context.AccountId,
                        context.SourceFormName, selectedCurrencyId);
                }
                else
                {
                    context.ActionModel.Update(context.ActionId, insertedValue, context.TillId, context.TillType, destinationTillId,
                        observeTillType, donorId, exchangeAccId, isUsdExpenseEdit, false, false, amount, rate, actionDate,
                        context.Notes, context.ENotes, isReceipt, receiptNum, employeeSalaryId, originalRateId, microProjectId,
                        budgetLineId, -1, cashAccountId, observeCashAccountId, exchangeCashId, -1, context.SourceFormName,
                        selectedCurrencyId);
                }
            }
            else
            {
                if (insertedValue == "Ex" || insertedValue == "M")
                {
                    context.ActionModel.Update_Not_Approved(context.ActionId, insertedValue, context.TillId, context.TillType,
                        destinationTillId, observeTillType, donorId, exchangeAccId, true, withExAcc, withExchangeRate, amount, rate,
                        actionDate, context.Notes, context.ENotes, isReceipt, receiptNum, employeeSalaryId, originalRateId,
                        microProjectId, budgetLineId, -1, cashAccountId, observeCashAccountId, exchangeCashId, context.AccountId,
                        context.SourceFormName, selectedCurrencyId);
                }
                else
                {
                    context.ActionModel.Update_Not_Approved(context.ActionId, insertedValue, context.TillId, context.TillType,
                        destinationTillId, observeTillType, donorId, exchangeAccId, isUsdExpenseEdit, false, false, amount, rate,
                        actionDate, context.Notes, context.ENotes, isReceipt, receiptNum, employeeSalaryId, originalRateId,
                        microProjectId, budgetLineId, -1, cashAccountId, observeCashAccountId, exchangeCashId, -1,
                        context.SourceFormName, selectedCurrencyId);
                }
            }

            int attachmentSuccess = SaveAttachments(context, context.ActionId, document);
            ShowSuccessMessage(context.AttachmentPaths, attachmentSuccess);
        }

        private static int SaveAttachments(AddActionSaveContext context, int actionId, Document document)
        {
            int success = 0;
            if (context.AttachmentPaths == null || context.AttachmentPaths.Length == 0)
                return success;

            foreach (string fullAttachName in context.AttachmentPaths)
            {
                if (SaveAttachment(document, fullAttachName, actionId, context.ActionId, context.SourceFormName))
                    success++;
            }

            return success;
        }

        private static bool SaveAttachment(Document document, string fullAttachName, int actionId, int editActionId, string sourceFormName)
        {
            string extension;
            document.GetNameFromPath(fullAttachName, out extension, out _);

            while (document.file_Exist("Action_id_" + editActionId.ToString() + "_" + extension))
                extension = "(1)" + extension;

            if (!document.SaveDocument(fullAttachName, "Action_id_" + actionId.ToString() + "_" + extension))
                return false;

            document.AddAttachmentToDatabase(actionId, -1, extension, false, sourceFormName);
            return true;
        }

        private static void ShowSuccessMessage(string[] attachmentPaths, int attachmentSuccess)
        {
            if (attachmentPaths != null && attachmentPaths.Length > 0)
            {
                MessageBox.Show("Saved successfully." + Environment.NewLine + "Attachments: Succeeded" +
                                attachmentPaths.Length.ToString() + "/" + attachmentSuccess.ToString(),
                    "Confirmation", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
            }
            else
            {
                MessageBox.Show("Saved successfully.", "Confirmation", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
            }
        }
    }
}
