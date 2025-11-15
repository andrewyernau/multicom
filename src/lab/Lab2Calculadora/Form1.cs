using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Http;
using CalculatorLib;

namespace Lab2Calculadora
{
    /// <summary>
    /// Windows Forms Calculator application with basic arithmetic operations.
    /// Uses remote object via .NET Remoting over HTTP.
    /// </summary>
    public partial class Form1 : Form
    {
        private Operaciones? operaciones;
        private const string SERVER_URL = "http://localhost:8090/CalculatorService/Operaciones";

        public Form1()
        {
            InitializeComponent();
            InitializeRemoteConnection();
        }

        /// <summary>
        /// Initializes the connection to the remote calculator service.
        /// </summary>
        private void InitializeRemoteConnection()
        {
            try
            {
                // Register HTTP channel for client
                HttpChannel channel = new HttpChannel();
                ChannelServices.RegisterChannel(channel, false);

                // Get reference to remote object using Activator.GetObject
                operaciones = (Operaciones)Activator.GetObject(
                    typeof(Operaciones),
                    SERVER_URL
                );

                // Test connection
                lstResults.Items.Add("[INFO] Connected to remote server");
                lstResults.Items.Add($"[INFO] Server URL: {SERVER_URL}");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"[AGENT] Could not connect to remote server:\n{ex.Message}\n\nMake sure CalculatorServer is running.",
                    "Connection Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
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
        /// Performs the specified arithmetic operation using the remote calculator service.
        /// </summary>
        /// <param name="operation">The operation symbol (+, -, *, /).</param>
        private void PerformOperation(char operation)
        {
            if (!TryParseOperands(out double operand1, out double operand2))
            {
                return;
            }

            // Check if remote object is available
            if (operaciones == null)
            {
                ShowError("[AGENT] Not connected to remote server.\nPlease start CalculatorServer first.");
                return;
            }

            double result;
            string operationSymbol = operation.ToString();

            try
            {
                switch (operation)
                {
                    case '+':
                        // Call remote method Sumar
                        result = operaciones.Sumar(operand1, operand2);
                        break;
                    case '-':
                        // Call remote method Restar
                        result = operaciones.Restar(operand1, operand2);
                        break;
                    case '*':
                        // Call remote method Multiplicar
                        result = operaciones.Multiplicar(operand1, operand2);
                        operationSymbol = "×";
                        break;
                    case '/':
                        // Call remote method Dividir (handles division by zero)
                        result = operaciones.Dividir(operand1, operand2);
                        operationSymbol = "÷";
                        break;
                    default:
                        ShowError("[AGENT] Operación no válida.");
                        return;
                }

                string resultText = $"{operand1} {operationSymbol} {operand2} = {result:F2} [REMOTE]";
                lstResults.Items.Add(resultText);
                lstResults.TopIndex = lstResults.Items.Count - 1;
            }
            catch (System.Runtime.Remoting.RemotingException ex)
            {
                ShowError($"[AGENT] Server connection error:\n{ex.Message}\n\nIs the server running?");
            }
            catch (DivideByZeroException ex)
            {
                ShowError(ex.Message);
            }
            catch (Exception ex)
            {
                ShowError($"[AGENT] Error executing operation:\n{ex.Message}");
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
                ShowError("Por favor, ingrese el primer valor.");
                txtOperand1.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtOperand2.Text))
            {
                ShowError("Por favor, ingrese el segundo valor.");
                txtOperand2.Focus();
                return false;
            }

            if (!double.TryParse(txtOperand1.Text, out operand1))
            {
                ShowError("El primer valor no es un número válido.");
                txtOperand1.Focus();
                txtOperand1.SelectAll();
                return false;
            }

            if (!double.TryParse(txtOperand2.Text, out operand2))
            {
                ShowError("El segundo valor no es un número válido.");
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

        private void txtOperand1_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
