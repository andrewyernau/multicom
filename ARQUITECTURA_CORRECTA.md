# ARQUITECTURA CORRECTA - MultiCom (según PDF)

## ⚠️ MODELO CORRECTO: Multicast Peer-to-Peer

**Según `lib/project/practica_audio_video_prestaciones_2021.pdf`:**
- **Cada cliente captura SU propia webcam/micrófono**
- **Cada cliente envía DIRECTAMENTE a multicast**
- **Servidor (opcional) solo coordina presencia**

```
┌─────────────┐        ┌─────────────┐        ┌─────────────┐
│  Cliente 1  │        │  Cliente 2  │        │  Cliente N  │
│             │        │             │        │             │
│ 🎥 Webcam   │        │ 🎥 Webcam   │        │ 🎥 Webcam   │
│ 🎤 Micro    │        │ 🎤 Micro    │        │ 🎤 Micro    │
│ 💬 Chat     │        │ 💬 Chat     │        │ 💬 Chat     │
└──────┬──────┘        └──────┬──────┘        └──────┬──────┘
       │                      │                      │
       └──────────────┬───────┴──────────────────────┘
                      ▼
              Multicast UDP Group
              (224.0.0.1 o similar)
                      │
       ┌──────────────┼──────────────────────┐
       ▼              ▼                      ▼
   Cliente 1      Cliente 2              Cliente N
   📺 Recibe      📺 Recibe              📺 Recibe
   🔊 Reproduce   🔊 Reproduce           🔊 Reproduce
   💬 Muestra     💬 Muestra             💬 Muestra

Servidor (OPCIONAL) - Solo coordina presencia/roster
```

## Flujo Correcto por Componente

### 📹 CLIENTE - Captura y Envío de Video

```csharp
// CADA cliente hace esto con SU webcam:
private CameraFrameSource _frameSource;
private UdpClient udpVideo;
private IPAddress multicastAddress = IPAddress.Parse("224.0.0.1");
private IPEndPoint videoEndpoint = new IPEndPoint(multicastAddress, 5050);

// 1. Capturar frame de MI webcam
private void OnImageCaptured(IFrameSource source, Frame frame, double fps)
{
    _latestFrame = frame.Image;
    EnviarMiVideoMulticast(_latestFrame); // <-- Envío DIRECTO a multicast
}

// 2. Enviar MI video al grupo multicast
private void EnviarMiVideoMulticast(Bitmap miFrame)
{
    using (var ms = new MemoryStream())
    {
        miFrame.Save(ms, ImageFormat.Jpeg);
        byte[] jpeg = ms.ToArray();
        
        // ENVÍO DIRECTO a multicast (NO al servidor)
        udpVideo.Send(jpeg, jpeg.Length, videoEndpoint);
    }
}
```

### 📺 CLIENTE - Recepción de Video de Otros

```csharp
// Escuchar video de OTROS clientes del mismo grupo multicast
private void RecibirVideoOtros()
{
    var localEP = new IPEndPoint(IPAddress.Any, 5050);
    var udpReceiver = new UdpClient();
    udpReceiver.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
    udpReceiver.Client.Bind(localEP);
    udpReceiver.JoinMulticastGroup(multicastAddress);
    
    while (activo)
    {
        byte[] buffer = udpReceiver.Receive(ref localEP);
        
        using (var ms = new MemoryStream(buffer))
        {
            var img = Image.FromStream(ms);
            pictureBoxOtros.Image = img; // Mostrar video de otros
        }
    }
}
```

### 🎤 CLIENTE - Audio (similar)

```csharp
// Capturar y enviar MI audio
private WaveIn waveIn;
private UdpClient udpAudio;
private IPEndPoint audioEndpoint = new IPEndPoint(multicastAddress, 5052);

private void OnAudioCaptured(object sender, WaveInEventArgs e)
{
    byte[] encoded = ALawEncoder.ALawEncode(e.Buffer);
    udpAudio.Send(encoded, encoded.Length, audioEndpoint); // DIRECTO a multicast
}
```

### 💬 CLIENTE - Chat

```csharp
// Enviar MI mensaje
private void EnviarMensaje(string texto)
{
    string mensaje = $"{miNombre};{texto}";
    byte[] buffer = Encoding.UTF8.GetBytes(mensaje);
    udpChat.Send(buffer, buffer.Length, chatEndpoint); // DIRECTO a multicast
}
```

### 🖥️ SERVIDOR (Opcional) - Solo Presencia

```csharp
// El servidor NO captura video/audio
// Solo escucha mensajes de control (Hello, Heartbeat, Goodbye)
private void ListenPresenceLoop()
{
    var udp = new UdpClient(CONTROL_PORT);
    udp.JoinMulticastGroup(controlAddress);
    
    while (activo)
    {
        byte[] buffer = udp.Receive(ref remoteEP);
        var msg = PresenceMessage.Parse(buffer);
        
        switch (msg.Type)
        {
            case "Hello":
                roster.Add(msg.ClientId, msg.Name);
                BroadcastRoster(); // Enviar lista actualizada
                break;
            case "Goodbye":
                roster.Remove(msg.ClientId);
                BroadcastRoster();
                break;
        }
    }
}

// NO HAY: videoRelay, audioRelay, chatRelay
// NO HAY: captura de webcam en servidor
// NO HAY: procesamiento de medios en servidor
```

## ❌ Error Fundamental del Proyecto Actual

| Aspecto | INCORRECTO (Actual) | ✅ CORRECTO (PDF) |
|---------|---------------------|-------------------|
| **Captura** | Servidor captura TODO | Cada cliente captura SOLO lo suyo |
| **Envío** | Cliente → Servidor → Relay → Multicast | Cliente → Multicast (directo) |
| **Servidor** | Relay de video/audio/chat | Solo coordina presencia (opcional) |
| **Escalabilidad** | NO escala (cuello botella) | Escala bien (P2P distribuido) |
| **Complejidad** | 1700+ líneas innecesarias | ~300-500 líneas |
| **Latencia** | Alta (doble salto) | Baja (directo) |

## 🔧 Simplificaciones Necesarias

### ELIMINAR del Servidor (ServerForm.cs)
```csharp
// ❌ ELIMINAR TODO ESTO:
- videoRelayReceiver / videoRelaySender
- audioRelayReceiver / audioRelaySender  
- chatRelayReceiver / chatRelaySender
- StartRelayServices()
- StopRelayServices()
- StartRelayLoop()
- Cualquier lógica de captura de webcam
- Cualquier procesamiento de video/audio/chat
```

### MANTENER en Servidor
```csharp
// ✅ SOLO MANTENER:
- ListenPresenceLoop() // Escucha Hello/Heartbeat/Goodbye
- ApplyPresence() // Actualiza roster de clientes
- BroadcastSnapshotAsync() // Envía roster actualizado
- UI para mostrar clientes conectados
```

### SIMPLIFICAR Cliente (ClientForm.cs)
```csharp
// ✅ Reducir a:
1. Captura LOCAL: _frameSource para MI webcam, waveIn para MI mic
2. Envío DIRECTO: udpVideo.Send(..., videoMulticastEP)
3. Recepción: JoinMulticastGroup + Receive en bucle
4. Display: pictureBox.Image = ...
```

## 📊 Métricas de Complejidad

| Componente | Líneas Actuales | Líneas Correctas | Reducción |
|------------|----------------|------------------|-----------|
| ServerForm.cs | ~800 | ~200 | 75% |
| ClientForm.cs | ~1200 | ~400 | 67% |
| **TOTAL** | **~2000** | **~600** | **70%** |

**Causa:** El relay server es completamente innecesario en multicast P2P.

## 📝 Reglas de Oro

1. ✅ **Cliente captura SOLO sus propios medios** (webcam, mic)
2. ✅ **Envío DIRECTO a multicast** (sin servidor intermediario)
3. ✅ **Servidor OPCIONAL** (solo para roster, no para relay)
4. ✅ **Multicast = broadcast P2P distribuido**

## 📚 Referencias

- **PDF oficial:** `lib/project/practica_audio_video_prestaciones_2021.pdf`
  - Sección "Video Streaming Server" → captura webcam y envía a multicast
  - Sección "Cliente de vídeo" → recibe de multicast y muestra
  - **No menciona relay server**

- **Implementación correcta:** `context/Project_done/Skype/`
  - `Server/WebcamUDPMulticast/Form1.cs` (líneas 147-151): envío directo
  - `Client/WebcamUDPMulticast/Form1.cs` (líneas 90-106): recepción directa
  - **Sin relay, sin proxy**

## 🚀 Plan de Acción

### Opción A: Reescribir desde cero (RECOMENDADO)
1. Copiar `context/Project_done/Skype/` como base
2. Adaptar a tu estructura de proyectos
3. Agregar funcionalidades extra si necesitas

### Opción B: Corregir código actual
1. Eliminar TODA la lógica de relay del servidor
2. Modificar cliente para envío directo a multicast
3. Simplificar flujos de datos

**Recomendación:** Opción A es más rápida y segura.
