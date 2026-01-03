# DIAGNÓSTICO: Clientes no reciben video/audio/chat

## PROBLEMA REPORTADO

✅ **Servidor:**
- Recibe audio correctamente
- Recibe chat de múltiples clientes
- Transmite (según logs)

❌ **Clientes:**
- NO reciben video
- NO reciben audio
- NO visualizan chat
- NO ven nada del servidor

---

## CAUSA RAÍZ IDENTIFICADA

### 1. Configuración incorrecta del socket multicast en cliente

**Problema en `ClientForm.cs` línea 145-148:**
```csharp
videoReceiver = new UdpClient();
IPEndPoint remoteEp = new IPEndPoint(IPAddress.Any, VIDEO_PORT);
videoReceiver.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
videoReceiver.Client.Bind(remoteEp);
videoReceiver.JoinMulticastGroup(IPAddress.Parse(MULTICAST_IP));
```

**ERROR:** El orden está mal. Debe hacer `Bind` ANTES de `SetSocketOption`.

**Solución correcta:**
```csharp
videoReceiver = new UdpClient();
videoReceiver.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
videoReceiver.Client.Bind(new IPEndPoint(IPAddress.Any, VIDEO_PORT));
videoReceiver.JoinMulticastGroup(IPAddress.Parse(MULTICAST_IP));
```

### 2. Falta configurar TTL en servidor

El servidor debe configurar el TTL (Time To Live) del multicast:

```csharp
videoSender.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 10);
```

### 3. Arquitectura incorrecta para videoconferencia

**Implementado actualmente:**
- Servidor: captura Y envía
- Cliente: solo recibe

**Debe ser (videoconferencia real):**
- Cada participante: captura SU cámara Y envía
- Cada participante: recibe TODAS las cámaras
- Muestra múltiples tiles (uno por participante)

---

## SOLUCIÓN PASO A PASO

### PASO 1: Arreglar recepción en cliente

Archivo: `src/project/MultiCom.Client/ClientForm.cs`

**Cambiar método `StartVideoReceiver`:**
```csharp
private void StartVideoReceiver()
{
    try
    {
        videoReceiver = new UdpClient();
        videoReceiver.ExclusiveAddressUse = false;
        videoReceiver.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        videoReceiver.Client.Bind(new IPEndPoint(IPAddress.Any, VIDEO_PORT));
        videoReceiver.JoinMulticastGroup(IPAddress.Parse(MULTICAST_IP));

        videoTask = Task.Run(() => ReceiveVideoLoop());
        Log("[INFO] Video receiver started on port " + VIDEO_PORT);
    }
    catch (Exception ex)
    {
        Log("[ERROR] Video receiver: " + ex.Message);
    }
}
```

**Cambiar método `StartAudioReceiver`:**
```csharp
private void StartAudioReceiver()
{
    try
    {
        audioReceiver = new UdpClient();
        audioReceiver.ExclusiveAddressUse = false;
        audioReceiver.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        audioReceiver.Client.Bind(new IPEndPoint(IPAddress.Any, AUDIO_PORT));
        audioReceiver.JoinMulticastGroup(IPAddress.Parse(MULTICAST_IP));

        audioPlayer = new SimpleAudioPlayer();
        audioPlayer.Start();

        audioTask = Task.Run(() => ReceiveAudioLoop());
        Log("[INFO] Audio receiver started on port " + AUDIO_PORT);
    }
    catch (Exception ex)
    {
        Log("[ERROR] Audio receiver: " + ex.Message);
    }
}
```

**Cambiar método `StartChat`:**
```csharp
private void StartChat()
{
    try
    {
        // Enviar mensajes a servidor
        chatSender = new UdpClient();
        chatSenderEndpoint = new IPEndPoint(IPAddress.Parse(MULTICAST_IP), CHAT_CLIENT_PORT);
        chatSender.JoinMulticastGroup(IPAddress.Parse(MULTICAST_IP));

        // Recibir mensajes del servidor
        chatReceiver = new UdpClient();
        chatReceiver.ExclusiveAddressUse = false;
        chatReceiver.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        chatReceiver.Client.Bind(new IPEndPoint(IPAddress.Any, CHAT_SERVER_PORT));
        chatReceiver.JoinMulticastGroup(IPAddress.Parse(MULTICAST_IP));

        chatTask = Task.Run(() => ReceiveChatLoop());
        Log("[INFO] Chat initialized.");
    }
    catch (Exception ex)
    {
        Log("[ERROR] Chat: " + ex.Message);
    }
}
```

### PASO 2: Configurar TTL en servidor

Archivo: `src/project/MultiCom.Server/ServerForm.cs`

**En método `OnStartClick`, después de crear videoSender:**
```csharp
videoSender = new UdpClient();
videoSender.JoinMulticastGroup(IPAddress.Parse(MULTICAST_IP));
videoSender.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 10);

audioSender = new UdpClient();
audioSender.JoinMulticastGroup(IPAddress.Parse(MULTICAST_IP));
audioSender.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 10);

chatSender = new UdpClient();
chatSender.JoinMulticastGroup(IPAddress.Parse(MULTICAST_IP));
chatSender.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 10);
```

---

## VERIFICACIÓN

### Test 1: Servidor envía

```bash
cd src\project\MultiCom.Server\bin\Release
.\MultiCom.Server.exe
```

1. Click "Start service"
2. Verifica logs:
   ```
   Iniciando cámara: ...
   ✅ Audio capturando
   ✅ Transmisión iniciada correctamente
   ```

### Test 2: Cliente recibe

```bash
cd src\project\MultiCom.Client\bin\Release
.\MultiCom.Client.exe
```

1. Click "Connect"
2. Deberías ver logs:
   ```
   [INFO] Video receiver started on port 8080
   [INFO] Audio receiver started on port 8081
   [INFO] Chat initialized
   [INFO] Connected to conference
   ```
3. **IMPORTANTE:** Deberías ver:
   - Video del servidor en pantalla
   - Escuchar audio del servidor
   - Ver mensajes de chat

### Test 3: Múltiples clientes

Ejecuta 2 o 3 clientes en la misma red:
- Todos deberían ver el mismo video
- Todos deberían escuchar el mismo audio
- Todos deberían ver los mensajes de chat

---

## PRÓXIMO PASO: VIDEOCONFERENCIA COMPLETA (PEER-TO-PEER)

Si quieres que CADA cliente transmita SU cámara (videoconferencia real):

### Opción A: Modelo híbrido (más simple)
- Mantener servidor/cliente
- Añadir captura de cámara en cliente
- Cliente envía a OTRO puerto/grupo
- Servidor retransmite

### Opción B: P2P puro (más complejo)
- Eliminar distinción servidor/cliente
- Todos son "peers"
- Cada peer captura Y recibe
- Múltiples tiles mostrando cada participante
- Identificar participantes por IP/ID

**¿Cuál prefieres implementar?**

---

## COMANDOS DE DIAGNÓSTICO

### Verificar multicast en red local

PowerShell:
```powershell
# Ver interfaces de red
Get-NetIPInterface | Where-Object {$_.InterfaceAlias -like "*Ethernet*" -or $_.InterfaceAlias -like "*Wi-Fi*"}

# Verificar rutas multicast
Get-NetRoute | Where-Object {$_.DestinationPrefix -like "224.*"}
```

### Capturar tráfico multicast

Wireshark:
```
Filtro: ip.dst == 224.0.0.1
```

Deberías ver paquetes UDP en puertos 8080, 8081, 8082, 8083.

---

**Fecha:** 2026-01-03 15:30  
**Estado:** Diagnóstico completo - Esperando correcciones
