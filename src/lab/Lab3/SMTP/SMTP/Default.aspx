<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="SMTP.Default" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head id="Head1" runat="server">
    <title>Email sending</title>
</head>
<body>
    <form id="form1" runat="server">
        <div>
            <table cellpadding="10" cellspacing="0" border="1" width="50%">
                <tr>
                    <td valign="top" style="padding-top:20px; background-color:dimgray">
                        
                        <!-- Destinatario -->
                        <asp:Label ID="Label1" runat="server" Text="To" ForeColor="White"></asp:Label>
                        &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
                        <asp:TextBox ID="T1" runat="server" BackColor="LightGray" style="margin-left: 10px"></asp:TextBox>
                        <br /><br />
                        
                        <!-- Remitente -->
                        <asp:Label ID="Label2" runat="server" Text="From" ForeColor="White"></asp:Label>
                        &nbsp;&nbsp;&nbsp;
                        <asp:TextBox ID="T2" runat="server" BackColor="LightGray" style="margin-left: 10px"></asp:TextBox>
                        <br /><br />
                        
                        <!-- Motivo -->
                        <asp:Label ID="Label3" runat="server" Text="Subject" ForeColor="White"></asp:Label>
                        &nbsp;&nbsp;&nbsp;
                        <asp:TextBox ID="T3" runat="server" BackColor="LightGray"></asp:TextBox>
                        <br /><br />
                        <!-- Cuerpo -->
                        <asp:Label ID="Label4" runat="server" Text="Body" ForeColor="White"></asp:Label>
                        &nbsp;&nbsp;&nbsp;
                        <asp:TextBox ID="T4" runat="server" BackColor="LightGray"></asp:TextBox>
                        <br /><br />
                        <!-- Archivo Adjunto -->
                        <asp:Label ID="Label6" runat="server" Text="Attach" ForeColor="White"></asp:Label>
                        <asp:FileUpload ID="fileAttach" runat="server" Width="578px" style="margin-left: 10px" BackColor="LightGray"/>
                        
                        <!-- Botón de Envío -->
                        <asp:Button ID="Send" runat="server" Text="Enviar" 
                                    OnClick="Send_Click" BackColor="White" ForeColor="Black" style="margin-left: 10px"/>
                        
                        <!-- Estado -->
                        <asp:TextBox ID="T5" runat="server" BackColor="Transparent" style="margin-left: 10px" ReadOnly="true" TextMode="SingleLine" Width="400px"></asp:TextBox>
                    </td>
                </tr>
            </table>
        </div>
    </form>
</body>
</html>
