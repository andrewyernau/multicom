# Guía de Simplificación - MultiCom

## 🎯 Objetivo
Reducir ~2000 líneas a ~600 líneas eliminando el relay server innecesario.

## 📋 Checklist de Cambios

### SERVIDOR (MultiCom.Server/ServerForm.cs)

#### ❌ ELIMINAR (líneas 278-350+)
```csharp
// ELIMINAR COMPLETO:
private void StartRelayServices()
private void StopRelayServices()
private Task StartRelayLoop(...)
private UdpClient CreateRelayReceiver(...)
private UdpClient CreateRelaySender(...)

// ELIMINAR variables:
private UdpClient videoRelayReceiver;
private UdpClient chatRelayReceiver;
private UdpClient audioRelayReceiver;
private UdpClient videoRelaySender;
private UdpClient chatRelaySender;
private UdpClient audioRelaySender;
private Task videoRelayTask;
private Task chatRelayTask;
private Task audioRelayTask;
private CancellationTokenSource relayToken;
```

#### ✅ MANTENER (esencial)
```csharp
// Solo mantener presencia:
private void StartPresenceService()
private void StopPresenceService()
private void ListenPresenceLoop(CancellationToken token)
private bool ApplyPresence(PresenceMessage message)
private async Task BroadcastSnapshotAsync()

// Variables necesarias:
private readonly Dictionary<Guid, PresenceRecord> roster;
private UdpClient snapshotSender;
private CancellationTokenSource presenceToken;
```

#### 🔧 MODIFICAR
En `StartPresenceService()`:
```csharp
// ANTES:
StartRelayServices(); // ELIMINAR esta línea

// DESPUÉS:
// (nada, solo comentarlo o borrarlo)
```

En `StopPresenceService()`:
```csharp
// ANTES:
StopRelayServices(); // ELIMINAR esta línea

// DESPUÉS:
// (nada, solo comentarlo o borrarlo)
```

---

### CLIENTE (MultiCom.Client/ClientForm.cs)

#### ❌ ELIMINAR/SIMPLIFICAR

**Conceptualmente:**
- El cliente NO necesita "relay"
- El cliente captura SU webcam y la envía directamente a multicast
- El cliente escucha multicast para recibir de OTROS

**Lógica actual incorrecta:**
```csharp
// INCORRECTO: Enviar a servidor para relay
unicastToServer.Send(videoData, ...); // ❌

// CORRECTO: Enviar directo a multicast
videoSender.Send(videoData, ..., videoMulticastEndpoint); // ✅
```

#### ✅ MANTENER/CORREGIR

**Captura de video (tu webcam):**
```csharp
private CameraFrameSource frameSource;
private UdpClient videoSender; // Para enviar MI video

private void OnImageCaptured(IFrameSource source, Frame frame, double fps)
{
    var bitmap = frame.Image;
    
    // Codificar JPEG
    using (var ms = new MemoryStream())
    {
        bitmap.Save(ms, ImageFormat.Jpeg);
        byte[] jpeg = ms.ToArray();
        
        // ENVÍO DIRECTO a multicast (sin servidor intermediario)
        var endpoint = MulticastChannels.BuildVideoEndpoint();
        videoSender.Send(jpeg, jpeg.Length, endpoint);
    }
}
```

**Recepción de video (de otros):**
```csharp
private UdpClient videoListener;

private void ListenVideoLoop()
{
    var endpoint = MulticastChannels.BuildVideoEndpoint();
    var udp = CreateMulticastListener(endpoint);
    udp.JoinMulticastGroup(IPAddress.Parse(MulticastChannels.VIDEO_ADDRESS));
    
    var remote = new IPEndPoint(IPAddress.Any, endpoint.Port);
    
    while (!videoCts.IsCancellationRequested)
    {
        try
        {
            byte[] buffer = udp.Receive(ref remote);
            
            // Reconstruir imagen
            using (var ms = new MemoryStream(buffer))
            {
                var img = Image.FromStream(ms);
                
                // Mostrar en UI
                Invoke(new Action(() => 
                {
                    pictureBoxOthers.Image = img;
                }));
            }
        }
        catch (Exception ex)
        {
            Log($"[ERROR] Video: {ex.Message}");
        }
    }
}
```

**Audio similar:**
```csharp
// Captura MI audio
private void OnAudioCaptured(object sender, WaveInEventArgs e)
{
    byte[] encoded = ALawEncoder.ALawEncode(e.Buffer);
    var endpoint = MulticastChannels.BuildAudioEndpoint();
    audioSender.Send(encoded, encoded.Length, endpoint); // DIRECTO
}

// Recibe audio de OTROS
private void ListenAudioLoop()
{
    var endpoint = MulticastChannels.BuildAudioEndpoint();
    var udp = CreateMulticastListener(endpoint);
    udp.JoinMulticastGroup(IPAddress.Parse(MulticastChannels.AUDIO_ADDRESS));
    
    var remote = new IPEndPoint(IPAddress.Any, endpoint.Port);
    
    while (!audioCts.IsCancellationRequested)
    {
        byte[] buffer = udp.Receive(ref remote);
        byte[] decoded = ALawDecoder.ALawDecode(buffer);
        
        // Reproducir
        waveProvider.AddSamples(decoded, 0, decoded.Length);
    }
}
```

**Chat:**
```csharp
// Enviar MI mensaje
private void SendChatMessage(string text)
{
    var envelope = ChatEnvelope.Create(clientId, displayName, text);
    byte[] packet = envelope.ToPacket();
    var endpoint = MulticastChannels.BuildChatEndpoint();
    chatSender.Send(packet, packet.Length, endpoint); // DIRECTO
}

// Recibir mensajes de TODOS
private void ListenChatLoop()
{
    var endpoint = MulticastChannels.BuildChatEndpoint();
    var udp = CreateMulticastListener(endpoint);
    udp.JoinMulticastGroup(IPAddress.Parse(MulticastChannels.CHAT_ADDRESS));
    
    var remote = new IPEndPoint(IPAddress.Any, endpoint.Port);
    
    while (!chatCts.IsCancellationRequested)
    {
        byte[] buffer = udp.Receive(ref remote);
        ChatEnvelope envelope;
        if (ChatEnvelope.TryParse(buffer, out envelope))
        {
            // Ignorar mis propios mensajes
            if (envelope.SenderId != clientId)
            {
                DisplayChatMessage(envelope.SenderName, envelope.Message);
            }
        }
    }
}
```

---

## 🔑 Principios Clave

### 1. Envío = Multicast DIRECTO
```csharp
// ❌ INCORRECTO:
unicastClient.Send(data, serverAddress); // Enviar a servidor

// ✅ CORRECTO:
multicastClient.Send(data, multicastEndpoint); // Enviar a grupo
```

### 2. Recepción = JoinMulticastGroup
```csharp
// ✅ CORRECTO:
var udp = new UdpClient();
udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
udp.Client.Bind(new IPEndPoint(IPAddress.Any, PORT));
udp.JoinMulticastGroup(IPAddress.Parse(MULTICAST_ADDRESS));

// Bucle de recepción
while (activo)
{
    byte[] buffer = udp.Receive(ref remote);
    // Procesar...
}
```

### 3. Servidor NO retransmite
```csharp
// ❌ ELIMINAR del servidor:
private void RelayVideoFromClientToMulticast() { ... }
private void RelayAudioFromClientToMulticast() { ... }
private void RelayChatFromClientToMulticast() { ... }

// ✅ Servidor solo mantiene:
private void ListenPresenceMessages() { ... }
private void BroadcastRosterSnapshot() { ... }
```

---

## 📊 Resultado Esperado

### Antes (incorrecto)
- **ServerForm.cs:** ~800 líneas
- **ClientForm.cs:** ~1200 líneas
- **Total:** ~2000 líneas
- **Flujo:** Cliente → Servidor relay → Multicast → Clientes
- **Problemas:** Latencia, no escala, complejo

### Después (correcto)
- **ServerForm.cs:** ~200 líneas (solo presencia)
- **ClientForm.cs:** ~400 líneas (captura + envío + recepción)
- **Total:** ~600 líneas
- **Flujo:** Cliente → Multicast ← Clientes
- **Ventajas:** Simple, directo, escalable

---

## 🚀 Pasos de Implementación

### Opción A: Modificar código actual
1. Backup del proyecto actual
2. En ServerForm.cs: eliminar todo StartRelayServices y métodos relacionados
3. En ClientForm.cs: cambiar envío de unicast a multicast directo
4. Probar con 2 clientes: deben verse entre sí sin servidor

### Opción B: Reescribir desde referencia (RECOMENDADO)
1. Copiar `context/Project_done/Skype/` como plantilla
2. Renombrar namespaces a MultiCom.*
3. Adaptar UI si es necesario
4. Probar

---

## 🧪 Cómo Probar

1. **Sin servidor:** Ejecutar 2 clientes, deben comunicarse vía multicast
2. **Con servidor:** Ejecutar servidor + 2 clientes, servidor muestra roster
3. **Validar:** Cada cliente ve video/oye audio de OTROS clientes

---

## ❓ Preguntas Frecuentes

**P: ¿El servidor es necesario?**
R: NO para video/audio/chat. Opcional solo para roster.

**P: ¿Cómo un cliente ve a otros sin servidor?**
R: Multicast = todos envían al mismo grupo, todos reciben de todos.

**P: ¿Por qué mi código actual no funciona?**
R: Porque intenta relay centralizado en vez de multicast distribuido.

**P: ¿Debo eliminar PresenceMessage?**
R: NO, es útil para el roster (opcional).

**P: ¿Debo eliminar VideoFrameAssembler?**
R: Depende. Si fragmentas frames grandes, úsalo. Si envías frames completos (recomendado), no lo necesitas.

---

## 📖 Referencias Finales

- **ARQUITECTURA_CORRECTA.md** - Diagramas y flujos
- **lib/project/project-description.md** - Especificación oficial PDF
- **context/Project_done/Skype/** - Implementación de referencia correcta
