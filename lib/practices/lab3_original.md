Universidad Politécnica de Cartagena
Escuela Técnica Superior de Ingeniería de
Telecomunicación
LABORATORIO DE CONTENIDOS DIGITALES
Práctica 3: Aplicación Web ASP.NET. Cliente
SMTP de Correo Electrónico.
 Profesores:
Antonio Javier García Sánchez.
Rubén Martínez Sandoval
En esta práctica, el alumno implementará una aplicación Web haciendo uso de las
facilidades ASP.NET. Para ello es necesario abrir un nuevo proyecto del tipo “Web 
Aplicación web vacía de ASP.NET” tal y como se observa en la siguiente imagen:
1.- Código HTML. Fichero Default.aspx
Una vez generado el proyecto Web, tendremos que crear el fichero .aspx que será
cargado cuando un usuario acceda a la web. Para ello, haciendo click derecho sobre la
solución (en el explorador de soluciones), iremos a Agregar  Nuevo Elemento (tal y
como se observa en la imagen de abajo):
(Imagen, pero no la voy a poner)

Una vez generado el proyecto Web, lo primero que se nos presenta es la
implementación de la página Web vía HTML. No se pretende en esta práctica que el
alumno conozca este lenguaje, por ello se le suministra parte de este código para que el
alumno lo estudie, comprenda y con la “Ayuda” de Visual Studio complete para la
realización de esta práctica. El código suministrado es el siguiente:
<%@ Page Language="C#" AutoEventWireup="true"
CodeFile="Default.aspx.cs" Inherits="_Default" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN"
"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml" >
<head id="Head1" runat="server">
 <title>Email sending</title>
</head>
<body>
 <form id="form1" runat="server">
 <div>
 <table cellpadding="10" cellspacing="0" border="2" width="50%"
>
 <tr>
 <td valign="top" style="padding-top:20px; background-color:
#C0C0C0;">
 <asp:Label ID="Label1" runat="server" Text="To "
ForeColor="Black"> </asp:Label> &nbsp; &nbsp; &nbsp;&nbsp; &nbsp;
 <asp:TextBox ID="T1" runat="server" BackColor="#C0FFFF"
></asp:TextBox>
 <br /><br />
 <asp:Label ID="Label2" runat="server" Text="From"
ForeColor="Black"> </asp:Label>&nbsp; &nbsp;
 <asp:TextBox ID="T2" runat="server"
BackColor="#C0FFFF"></asp:TextBox><br /><br />
 .
 .
 .

 <asp:Label ID="Label6" runat="server" Text="Attach"
ForeColor="Black"></asp:Label>


 <asp:FileUpload ID="fileAttach" runat="server" Width="578px"
/>
 <asp:Button ID="Send" runat="server" Text="Send"
OnClick="Send_Click" BackColor="#C0C000" ForeColor="Navy" />
 </td>
 </tr>
 </table></div>
 </form>
</body>
</html>
Los campos suministrados son los siguientes:
- “To”, es decir el correo electrónico a donde se envía el mensaje.
- “From”, el correo electrónico desde donde se envía.
- “attach”, por si se desea añadir un fichero.
- Botón “Send”. Botón de envío de mensajes.
Quedaría por desarrollar por parte del alumno las siguientes Labels y TextBoxes:
- “Subject”. Asunto del mensaje.
- “Body”. Cuerpo del mensaje.
- “List Status”. Estado del mensaje enviado. Si se produce alguna excepción en el
código principal, ésta será capturada y mostrada en este Textbox.
Pulsando en el explorador de soluciones en Default.aspx el botón derecho del ratón y
pinchando en Ver diseñador, el alumno obtendrá una muestra gráfica de cómo queda su
código HTML.


A continuación, buscaremos en la lista de la derecha el apartado “Web Forms” y
crearemos un nuevo ítem del tipo “Formularios Web Form” con el nombre
Default.aspx (importante que tenga ese nombre).
Una vez creado el elemento, haremos click derecho sobre él en el explorador de
soluciones y clicaremos en “Ver Diseñador” para acceder al diseño de la interfaz del
formulario.
El alumno deberá colocar tantos TextBox con sus respectivas Label para permitir
introducir:
• Dirección origen
• Dirección destino
• Asunto del correo
• Mensaje del correo
• Estado del envío (para comprobar que todo haya ido bien cuando se envíe)
Además, se insertará un elemento de tipo FileUpload (y su respectiva Label) para
permitir adjuntar archivos. Finalmente se añadirá un Button que permita enviar el correo
(recordar que la pestaña “Cuadro de Herramientas” suele estar escondida en la parte
izquierda del editor).
Una vez que se hayan añadido todos los elementos gráficos, el alumno puede acceder a
ver el código correspondiente generado (en ASP.NET) haciendo click en el botón
“Código” tal y como se muestra en la siguiente imagen:
2.- Implementación de la aplicación SMTP. Fichero Default.aspx.cs
En el explorador de soluciones en Default.aspx, pulsamos el botón derecho y pinchamos
en ver código. Se presenta al usuario el fichero Default.aspx.cs, la sección de nuestra
aplicación Web donde se va a desarrollar el cliente SMTP. Para ello es necesario añadir
la siguiente using:
using System.Net.Mail;
Las clases que se van a utilizar van a a ser cuatro: (i) SmtpClient y (ii) MailMessage,
(iii) MailAddress y (iv) Attachment.
- SmtpClient es la clase que va a realizar las funciones de indicar el host del
servidor, puerto de escucha de éste y envío del mensaje. El host será el tipo de
servidor al cual nosotros queramos acceder, por ejemplo si usamos el de la
universidad: imap.upct.es. El puerto smtp es el 25. Una vez formado el mensaje,
esta clase implementa la función para enviarlo.
- MailAddress es la clase donde indicaremos desde donde envíamos el correo
electrónico y a dónde lo envíamos. Para ello, el alumno deberá generar dos objetos
diferentes.
- MailMessage es la clase que formará el mensaje. A partir de las direcciones origen
y destino, el alumno usará las funciones necesarias de esta clase para añadir el
asunto y el cuerpo del mensaje. Además una vez seleccionado el fichero a enviar
con la clase Attachment, se adjuntará al cuerpo del mensaje.
- Attachment es la clase que se usa para seleccionar un fichero.


HOST A UTILIZAR:smtpclient.Credentials = new
System.Net.NetworkCredential("prueba@qartia.com", "nakmec-casra2-
fenfIx");
 smtpclient.Host = "qartia-com.correoseguro.dinaserver.com";
 smtpclient.Port = 587;
 //smtpclient.Host = "smtp.gmail.com";
 //smtpclient.Port = 587;
 smtpclient.EnableSsl = true;

System.Net.ServicePointManager.ServerCertificateValidationCallback = new
System.Net.Security.RemoteCertificateValidationCallback(RemoteServerCerti
ficateValidationCallback);

CONTRASEÑA A USAR: mafsys-2dynba-nebtYx