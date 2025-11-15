using CalculatorLib;

namespace Lab2Calculadora
{
    /// <summary>
    /// Windows Forms Calculator application with basic arithmetic operations.
    /// Uses CalculatorLib.dll for operation implementations.
    /// </summary>
    public partial class Form1 : Form
    {
        private readonly Operaciones operaciones;

        public Form1()
        {
            InitializeComponent();
            operaciones = new Operaciones();
        }

        /// <summary>
        /// Handles the addition operation.
        /// </summary>
        private void BtnAdd_Click(object? sender, EventArgs e)
        {
            PerformOperation('+');
        }

        /// <summary>
        /// Handles the subtraction operation.
        /// </summary>
        private void BtnSubtract_Click(object? sender, EventArgs e)
        {
            PerformOperation('-');
        }

        /// <summary>
        /// Handles the multiplication operation.
        /// </summary>
        private void BtnMultiply_Click(object? sender, EventArgs e)
        {
            PerformOperation('*');
        }

        /// <summary>
        /// Handles the division operation.
        /// </summary>
        private void BtnDivide_Click(object? sender, EventArgs e)
        {
            PerformOperation('/');
        }

        /// <summary>
        /// Clears the results list and input fields.
        /// </summary>
        private void BtnClear_Click(object? sender, EventArgs e)
        {
            lstResults.Items.Clear();
            txtOperand1.Clear();
            txtOperand2.Clear();
            txtOperand1.Focus();
        }

        /// <summary>
        /// Performs the specified arithmetic operation using CalculatorLib.
        /// </summary>
        /// <param name="operation">The operation symbol (+, -, *, /).</param>
        private void PerformOperation(char operation)
        {
            if (!TryParseOperands(out double operand1, out double operand2))
            {
                return;
            }

            double result;
            string operationSymbol = operation.ToString();

            try
            {
                switch (operation)
                {
                    case '+':
                        result = operaciones.Sumar(operand1, operand2);
                        break;
                    case '-':
                        result = operaciones.Restar(operand1, operand2);
                        break;
                    case '*':
                        result = operaciones.Multiplicar(operand1, operand2);
                        operationSymbol = "×";
                        break;
                    case '/':
                        result = operaciones.Dividir(operand1, operand2);
                        operationSymbol = "÷";
                        break;
                    default:
                        ShowError("[AGENT] Operación no válida.");
                        return;
                }

                string resultText = $"{operand1} {operationSymbol} {operand2} = {result:F2}";
                lstResults.Items.Add(resultText);
                lstResults.TopIndex = lstResults.Items.Count - 1;
            }
            catch (DivideByZeroException ex)
            {
                ShowError(ex.Message);
            }
        }

        /// <summary>
        /// Attempts to parse the operands from the text boxes.
        /// </summary>
        /// <param name="operand1">The first operand.</param>
        /// <param name="operand2">The second operand.</param>
        /// <returns>True if parsing succeeds; otherwise, false.</returns>
        private bool TryParseOperands(out double operand1, out double operand2)
        {
            operand1 = 0;
            operand2 = 0;

            if (string.IsNullOrWhiteSpace(txtOperand1.Text))
            {
                ShowError("[AGENT] Por favor, ingrese el primer valor.");
                txtOperand1.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtOperand2.Text))
            {
                ShowError("[AGENT] Por favor, ingrese el segundo valor.");
                txtOperand2.Focus();
                return false;
            }

            if (!double.TryParse(txtOperand1.Text, out operand1))
            {
                ShowError("[AGENT] El primer valor no es un número válido.");
                txtOperand1.Focus();
                txtOperand1.SelectAll();
                return false;
            }

            if (!double.TryParse(txtOperand2.Text, out operand2))
            {
                ShowError("[AGENT] El segundo valor no es un número válido.");
                txtOperand2.Focus();
                txtOperand2.SelectAll();
                return false;
            }

            return true;
        }

        /// <summary>
        /// Displays an error message to the user.
        /// </summary>
        /// <param name="message">The error message to display.</param>
        private void ShowError(string message)
        {
            MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
