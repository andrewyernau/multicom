using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Net.Mail;

namespace SMTP
{
    public partial class Default : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {

        }
        protected void Send_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(T1.Text) ||
                    string.IsNullOrWhiteSpace(T2.Text) ||
                    string.IsNullOrWhiteSpace(T3.Text) ||
                    string.IsNullOrWhiteSpace(T4.Text))
                {
                    T5.Text = "Alguno de los campos obligatorios están vacios";
                    return;
                }

                MailAddress fromAddress = new MailAddress(T2.Text);  // Remitente
                MailAddress toAddress = new MailAddress(T1.Text);    // Destinatario

                MailMessage mailMessage = new MailMessage(fromAddress, toAddress);

                mailMessage.Subject = T3.Text;
                mailMessage.Body = T4.Text;
                mailMessage.IsBodyHtml = false;  // Texto plano

                if (fileAttach.HasFile)
                {
                    string fileName = fileAttach.FileName;
                    string filePath = Server.MapPath("~/Uploads/" + fileName);

                    if (!System.IO.Directory.Exists(Server.MapPath("~/Uploads/")))
                    {
                        System.IO.Directory.CreateDirectory(Server.MapPath("~/Uploads/"));
                    }

                    fileAttach.SaveAs(filePath);

                    Attachment attachment = new Attachment(filePath);
                    mailMessage.Attachments.Add(attachment);
                }

                // Configuracion SMTP
                SmtpClient smtpClient = new SmtpClient();

                smtpClient.Credentials = new System.Net.NetworkCredential(
                    "prueba@qartia.com",
                    "mafsys-2dynba-nebtYx"
                );
                
                smtpClient.Host = "qartia-com.correoseguro.dinaserver.com";
                smtpClient.Port = 587;
                smtpClient.EnableSsl = true;

                System.Net.ServicePointManager.ServerCertificateValidationCallback =
                    new System.Net.Security.RemoteCertificateValidationCallback(
                        RemoteServerCertificateValidation
                    );

                smtpClient.Send(mailMessage);

                T5.Text = "Correo enviado exitosamente a " + T1.Text +
                          " desde " + T2.Text +
                          "\nFecha: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

                T1.Text = "";
                T2.Text = "";
                T3.Text = "";
                T4.Text = "";
            }
            catch (Exception ex)
            {
                T5.Text = "Error al enviar correo:\n" + ex.Message;

                System.Diagnostics.Debug.WriteLine("Error: " + ex.ToString());
            }
        }

        private static bool RemoteServerCertificateValidation(
            object sender,
            System.Security.Cryptography.X509Certificates.X509Certificate certificate,
            System.Security.Cryptography.X509Certificates.X509Chain chain,
            System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
    }
}