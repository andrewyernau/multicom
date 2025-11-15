namespace Lab2Calculadora
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            txtOperand1 = new TextBox();
            txtOperand2 = new TextBox();
            btnAdd = new Button();
            btnSubtract = new Button();
            btnMultiply = new Button();
            btnDivide = new Button();
            lstResults = new ListBox();
            lblOperand1 = new Label();
            lblOperand2 = new Label();
            lblResults = new Label();
            btnClear = new Button();
            SuspendLayout();
            // 
            // txtOperand1
            // 
            txtOperand1.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point);
            txtOperand1.Location = new Point(171, 40);
            txtOperand1.Margin = new Padding(3, 4, 3, 4);
            txtOperand1.Name = "txtOperand1";
            txtOperand1.Size = new Size(228, 34);
            txtOperand1.TabIndex = 0;
            txtOperand1.TextChanged += txtOperand1_TextChanged;
            // 
            // txtOperand2
            // 
            txtOperand2.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point);
            txtOperand2.Location = new Point(171, 107);
            txtOperand2.Margin = new Padding(3, 4, 3, 4);
            txtOperand2.Name = "txtOperand2";
            txtOperand2.Size = new Size(228, 34);
            txtOperand2.TabIndex = 1;
            // 
            // btnAdd
            // 
            btnAdd.BackColor = SystemColors.Window;
            btnAdd.Font = new Font("Segoe UI", 14F, FontStyle.Bold, GraphicsUnit.Point);
            btnAdd.Location = new Point(34, 187);
            btnAdd.Margin = new Padding(3, 4, 3, 4);
            btnAdd.Name = "btnAdd";
            btnAdd.Size = new Size(80, 67);
            btnAdd.TabIndex = 2;
            btnAdd.Text = "+";
            btnAdd.UseVisualStyleBackColor = false;
            btnAdd.Click += BtnAdd_Click;
            // 
            // btnSubtract
            // 
            btnSubtract.BackColor = SystemColors.Window;
            btnSubtract.Font = new Font("Segoe UI", 14F, FontStyle.Bold, GraphicsUnit.Point);
            btnSubtract.Location = new Point(137, 187);
            btnSubtract.Margin = new Padding(3, 4, 3, 4);
            btnSubtract.Name = "btnSubtract";
            btnSubtract.Size = new Size(80, 67);
            btnSubtract.TabIndex = 3;
            btnSubtract.Text = "-";
            btnSubtract.UseVisualStyleBackColor = false;
            btnSubtract.Click += BtnSubtract_Click;
            // 
            // btnMultiply
            // 
            btnMultiply.BackColor = SystemColors.Window;
            btnMultiply.Font = new Font("Segoe UI", 14F, FontStyle.Bold, GraphicsUnit.Point);
            btnMultiply.Location = new Point(240, 187);
            btnMultiply.Margin = new Padding(3, 4, 3, 4);
            btnMultiply.Name = "btnMultiply";
            btnMultiply.Size = new Size(80, 67);
            btnMultiply.TabIndex = 4;
            btnMultiply.Text = "×";
            btnMultiply.UseVisualStyleBackColor = false;
            btnMultiply.Click += BtnMultiply_Click;
            // 
            // btnDivide
            // 
            btnDivide.BackColor = SystemColors.Window;
            btnDivide.Font = new Font("Segoe UI", 14F, FontStyle.Bold, GraphicsUnit.Point);
            btnDivide.Location = new Point(343, 187);
            btnDivide.Margin = new Padding(3, 4, 3, 4);
            btnDivide.Name = "btnDivide";
            btnDivide.Size = new Size(80, 67);
            btnDivide.TabIndex = 5;
            btnDivide.Text = "÷";
            btnDivide.UseVisualStyleBackColor = false;
            btnDivide.Click += BtnDivide_Click;
            // 
            // lstResults
            // 
            lstResults.Font = new Font("Consolas", 10F, FontStyle.Regular, GraphicsUnit.Point);
            lstResults.FormattingEnabled = true;
            lstResults.ItemHeight = 20;
            lstResults.Location = new Point(34, 333);
            lstResults.Margin = new Padding(3, 4, 3, 4);
            lstResults.Name = "lstResults";
            lstResults.Size = new Size(388, 224);
            lstResults.TabIndex = 6;
            // 
            // lblOperand1
            // 
            lblOperand1.AutoSize = true;
            lblOperand1.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            lblOperand1.Location = new Point(34, 47);
            lblOperand1.Name = "lblOperand1";
            lblOperand1.Size = new Size(108, 23);
            lblOperand1.TabIndex = 7;
            lblOperand1.Text = "Primer Valor:";
            // 
            // lblOperand2
            // 
            lblOperand2.AutoSize = true;
            lblOperand2.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            lblOperand2.Location = new Point(34, 113);
            lblOperand2.Name = "lblOperand2";
            lblOperand2.Size = new Size(126, 23);
            lblOperand2.TabIndex = 8;
            lblOperand2.Text = "Segundo Valor:";
            // 
            // lblResults
            // 
            lblResults.AutoSize = true;
            lblResults.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
            lblResults.Location = new Point(34, 293);
            lblResults.Name = "lblResults";
            lblResults.Size = new Size(101, 23);
            lblResults.TabIndex = 9;
            lblResults.Text = "Resultados:";
            // 
            // btnClear
            // 
            btnClear.BackColor = Color.LightGray;
            btnClear.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            btnClear.Location = new Point(309, 573);
            btnClear.Margin = new Padding(3, 4, 3, 4);
            btnClear.Name = "btnClear";
            btnClear.Size = new Size(114, 40);
            btnClear.TabIndex = 10;
            btnClear.Text = "Limpiar";
            btnClear.UseVisualStyleBackColor = false;
            btnClear.Click += BtnClear_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(457, 640);
            Controls.Add(btnClear);
            Controls.Add(lblResults);
            Controls.Add(lblOperand2);
            Controls.Add(lblOperand1);
            Controls.Add(lstResults);
            Controls.Add(btnDivide);
            Controls.Add(btnMultiply);
            Controls.Add(btnSubtract);
            Controls.Add(btnAdd);
            Controls.Add(txtOperand2);
            Controls.Add(txtOperand1);
            Margin = new Padding(3, 4, 3, 4);
            Name = "Form1";
            Text = "Calculadora - Lab2";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox txtOperand1;
        private TextBox txtOperand2;
        private Button btnAdd;
        private Button btnSubtract;
        private Button btnMultiply;
        private Button btnDivide;
        private ListBox lstResults;
        private Label lblOperand1;
        private Label lblOperand2;
        private Label lblResults;
        private Button btnClear;
    }
}
