namespace Lab1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private System.Drawing.Bitmap currentBitmap;

        private void btnAbrir_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = this.cmbFormatoEntrada.Text + "|" + this.cmbFormatoSalida.Text + "|All files (*.*)|*.*";
                DialogResult dr = ofd.ShowDialog();
                if (dr == DialogResult.OK)
                {
                    // liberar recursos
                    if (currentBitmap != null) { currentBitmap.Dispose(); currentBitmap = null; }
                    // cargar imagen
                    currentBitmap = new System.Drawing.Bitmap(ofd.FileName);
                    pictureBoxImagen.Image = currentBitmap;

                    this.Width = Math.Max(this.Width, currentBitmap.Width + 40);
                    this.Height = Math.Max(this.Height, currentBitmap.Height + 120);
                }
            }
        }

        private void btnSalvarComo_Click(object sender, EventArgs e)
        {
            if (currentBitmap == null) { MessageBox.Show("No hay imagen cargada."); return; }

            using (SaveFileDialog sfd = new SaveFileDialog())
            {   
                // Formato de salida
                string pattern = this.cmbFormatoSalida.Text;
                string desc = pattern switch {
                    "*.png" => "PNG files (*.png)",
                    "*.jpg" => "JPEG files (*.jpg)",
                    "*.gif" => "GIF files (*.gif)",
                    "*.tiff" => "TIFF files (*.tiff)",
                    _ => "Image files"
                };
                sfd.Filter = $"{desc}|{pattern}|All files (*.*)|*.*";
                DialogResult dr = sfd.ShowDialog();
                if (dr == DialogResult.OK)
                {
                    var targetPath = sfd.FileName;
                    // hacer la conversión
                    var fmt = GetImageFormatFromComboBox(this.cmbFormatoSalida.Text);
                    currentBitmap.Save(targetPath, fmt);
                    MessageBox.Show("Imagen salvada exitosamente en "+targetPath);
                }
            }
        }
        private System.Drawing.Imaging.ImageFormat GetImageFormatFromComboBox(string text)
        {
            switch (text.ToLower())
            {
                case "*.bmp": return System.Drawing.Imaging.ImageFormat.Bmp;
                case "*.jpg": return System.Drawing.Imaging.ImageFormat.Jpeg;
                case "*.gif": return System.Drawing.Imaging.ImageFormat.Gif;
                case "*.tiff": return System.Drawing.Imaging.ImageFormat.Tiff;
                default: return System.Drawing.Imaging.ImageFormat.Png;
            }
        }

        private void cmbFormatoSalida_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void cmbFormatoEntrada_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
