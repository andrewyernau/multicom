namespace Lab1
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Button btnAbrir;
        private System.Windows.Forms.Button btnSalvarComo;
        private System.Windows.Forms.ComboBox cmbFormatoEntrada;
        private System.Windows.Forms.ComboBox cmbFormatoSalida;
        private System.Windows.Forms.PictureBox pictureBoxImagen;

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
            btnAbrir = new Button();
            btnSalvarComo = new Button();
            cmbFormatoEntrada = new ComboBox();
            cmbFormatoSalida = new ComboBox();
            pictureBoxImagen = new PictureBox();
            ((System.ComponentModel.ISupportInitialize)pictureBoxImagen).BeginInit();
            SuspendLayout();
            // 
            // btnAbrir
            // 
            btnAbrir.Location = new Point(23, 56);
            btnAbrir.Margin = new Padding(3, 4, 3, 4);
            btnAbrir.Name = "btnAbrir";
            btnAbrir.Size = new Size(114, 40);
            btnAbrir.TabIndex = 0;
            btnAbrir.Text = "Abrir";
            btnAbrir.UseVisualStyleBackColor = true;
            btnAbrir.Click += btnAbrir_Click;
            // 
            // btnSalvarComo
            // 
            btnSalvarComo.Location = new Point(23, 406);
            btnSalvarComo.Margin = new Padding(3, 4, 3, 4);
            btnSalvarComo.Name = "btnSalvarComo";
            btnSalvarComo.Size = new Size(114, 40);
            btnSalvarComo.TabIndex = 1;
            btnSalvarComo.Text = "Salvar como";
            btnSalvarComo.UseVisualStyleBackColor = true;
            btnSalvarComo.Click += btnSalvarComo_Click;
            // 
            // cmbFormatoEntrada
            // 
            cmbFormatoEntrada.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbFormatoEntrada.FormattingEnabled = true;
            cmbFormatoEntrada.Items.AddRange(new object[] { "*.png", "*.jpg", "*.gif", "*.tiff" });
            cmbFormatoEntrada.Location = new Point(23, 20);
            cmbFormatoEntrada.Margin = new Padding(3, 4, 3, 4);
            cmbFormatoEntrada.Name = "cmbFormatoEntrada";
            cmbFormatoEntrada.Size = new Size(114, 28);
            cmbFormatoEntrada.TabIndex = 2;
            cmbFormatoEntrada.SelectedIndexChanged += cmbFormatoEntrada_SelectedIndexChanged;
            // 
            // cmbFormatoSalida
            // 
            cmbFormatoSalida.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbFormatoSalida.FormattingEnabled = true;
            cmbFormatoSalida.Items.AddRange(new object[] { "*.png", "*.jpg", "*.gif", "*.tiff" });
            cmbFormatoSalida.Location = new Point(23, 370);
            cmbFormatoSalida.Margin = new Padding(3, 4, 3, 4);
            cmbFormatoSalida.Name = "cmbFormatoSalida";
            cmbFormatoSalida.Size = new Size(114, 28);
            cmbFormatoSalida.TabIndex = 3;
            cmbFormatoSalida.SelectedIndexChanged += cmbFormatoSalida_SelectedIndexChanged;
            // 
            // pictureBoxImagen
            // 
            pictureBoxImagen.BorderStyle = BorderStyle.FixedSingle;
            pictureBoxImagen.Location = new Point(174, 20);
            pictureBoxImagen.Margin = new Padding(3, 4, 3, 4);
            pictureBoxImagen.Name = "pictureBoxImagen";
            pictureBoxImagen.Size = new Size(374, 426);
            pictureBoxImagen.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBoxImagen.TabIndex = 4;
            pictureBoxImagen.TabStop = false;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(593, 468);
            Controls.Add(btnAbrir);
            Controls.Add(btnSalvarComo);
            Controls.Add(cmbFormatoEntrada);
            Controls.Add(cmbFormatoSalida);
            Controls.Add(pictureBoxImagen);
            Margin = new Padding(3, 4, 3, 4);
            Name = "Form1";
            Text = "Conversor de Imágenes";
            ((System.ComponentModel.ISupportInitialize)pictureBoxImagen).EndInit();
            ResumeLayout(false);
        }

        #endregion
    }
}
