namespace Lab2
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Button btnSuma;
        private System.Windows.Forms.Button btnResta;
        private System.Windows.Forms.Button btnMulti;
        private System.Windows.Forms.Button btnDiv;

        private System.Windows.Forms.TextBox txtNum1;
        private System.Windows.Forms.TextBox txtNum2;

        private System.Windows.Forms.Label lblResultado;
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

            //
            // Initialize UI Components
            //
            btnSuma = new Button();
            btnResta = new Button();
            btnMulti = new Button();
            btnDiv = new Button();
            txtNum1 = new TextBox();
            txtNum2 = new TextBox();
            lblResultado = new Label();

            // 
            // btnSuma
            //
            btnSuma.Location = new Point(50, 150);
            btnSuma.Margin = new Padding(3, 4, 3, 4);
            btnSuma.Name = "btnSuma";
            btnSuma.Size = new Size(115, 40);
            btnSuma.TabIndex = 0;
            btnSuma.Text = "Suma";
            btnSuma.UseVisualStyleBackColor = true;
            btnSuma.Click += btnSuma_Click;

            // 
            // btnResta
            //
            btnResta.Location = new Point(50, 150);
            btnResta.Margin = new Padding(3, 4, 3, 4);
            btnResta.Name = "btnResta";
            btnResta.Size = new Size(115, 40);
            btnResta.TabIndex = 0;
            btnResta.Text = "Resta";
            btnResta.UseVisualStyleBackColor = true;
            btnResta.Click += btnResta_Click;

            // 
            // btnMulti
            //
            btnMulti.Location = new Point(50, 150);
            btnMulti.Margin = new Padding(3, 4, 3, 4);
            btnMulti.Name = "btnMulti";
            btnMulti.Size = new Size(115, 40);
            btnMulti.TabIndex = 0;
            btnMulti.Text = "Multiplicación";
            btnMulti.UseVisualStyleBackColor = true;
            btnMulti.Click += btnMulti_Click;

            // 
            // btnDiv
            //
            btnDiv.Location = new Point(50, 150);
            btnDiv.Margin = new Padding(3, 4, 3, 4);
            btnDiv.Name = "btnDiv";
            btnDiv.Size = new Size(115, 40);
            btnDiv.TabIndex = 0;
            btnDiv.Text = "División";
            btnDiv.UseVisualStyleBackColor = true;
            btnDiv.Click += btnDiv_Click;

            // 
            // txtNum1
            //
            txtNum1.Location = new Point(50, 150);
            txtNum1.Margin = new Padding(3, 4, 3, 4);
            txtNum1.Name = "txtNum1";
            txtNum1.Size = new Size(115, 40);
            txtNum1.TabIndex = 0;
            txtNum1.Text = "Numero 1";

            // 
            // txtNum2
            //
            txtNum2.Location = new Point(50, 150);
            txtNum2.Margin = new Padding(3, 4, 3, 4);
            txtNum2.Name = "txtNum2";
            txtNum2.Size = new Size(115, 40);
            txtNum2.TabIndex = 0;
            txtNum2.Text = "Numero 2";





            this.components = new System.ComponentModel.Container();
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Text = "Form1";
            this.Controls.Add(btnSuma);
            this.Controls.Add(btnResta);
            this.Controls.Add(btnMulti);
            this.Controls.Add(btnDiv);
            this.Controls.Add(txtNum1);
            this.Controls.Add(txtNum2);
            this.Controls.Add(lblResultado);

        }

        #endregion
    }
}
