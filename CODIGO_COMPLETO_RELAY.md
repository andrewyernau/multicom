# IMPLEMENTACIÓN COMPLETA: Servidor Central + Clientes Capturadores

## ESTADO ACTUAL
✅ Paso 1 completado: Estructuras de datos creadas
- `PacketHeader.cs` - Cabecera con ClientID (44 bytes)
- `ParticipantInfo.cs` - Info de participantes

## PRÓXIMOS PASOS DETALLADOS

### PASO 2: Refactorizar SERVIDOR

El servidor debe convertirse en un **RELAY PURO** que:
1. Escucha uploads de clientes (puertos 9001, 9002, 9003)
2. Identifica streams por ClientID
3. Retransmite a todos (puertos 8080, 8081, 8082)

**Código necesario en `ServerForm.cs`:**

```csharp
// CONSTANTES ACTUALIZADAS
private const string MULTICAST_IP = "224.0.0.1";
private const int PORT_VIDEO_UPLOAD = 9001;    // Clientes envían aquí
private const int PORT_AUDIO_UPLOAD = 9002;
private const int PORT_CHAT_UPLOAD = 9003;
private const int PORT_VIDEO_BROADCAST = 8080;  // Servidor retransmite aquí
private const int PORT_AUDIO_BROADCAST = 8081;
private const int PORT_CHAT_BROADCAST = 8082;

// NUEVOS CAMPOS
private Dictionary<string, ParticipantInfo> participants = new Dictionary<string, ParticipantInfo>();
private UdpClient videoUploadReceiver;
private UdpClient audioUploadReceiver;
private UdpClient chatUploadReceiver;
private UdpClient videoBroadcaster;
private UdpClient audioBroadcaster;
private UdpClient chatBroadcaster;

// ELIMINAR ESTOS CAMPOS (ya no los necesita el servidor):
// private CameraFrameSource frameSource;  ❌
// private SimpleAudioCapture audioCapture; ❌

// MÉTODO OnStartClick SIMPLIFICADO:
private void OnStartClick(object sender, EventArgs e)
{
    try
    {
        isStreaming = true;
        cts = new CancellationTokenSource();

        // Configurar RECEIVERS (escuchar uploads de clientes)
        videoUploadReceiver = new UdpClient();
        videoUploadReceiver.ExclusiveAddressUse = false;
        videoUploadReceiver.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        videoUploadReceiver.Client.Bind(new IPEndPoint(IPAddress.Any, PORT_VIDEO_UPLOAD));
        videoUploadReceiver.JoinMulticastGroup(IPAddress.Parse(MULTICAST_IP));

        audioUploadReceiver = new UdpClient();
        audioUploadReceiver.ExclusiveAddressUse = false;
        audioUploadReceiver.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        audioUploadReceiver.Client.Bind(new IPEndPoint(IPAddress.Any, PORT_AUDIO_UPLOAD));
        audioUploadReceiver.JoinMulticastGroup(IPAddress.Parse(MULTICAST_IP));

        chatUploadReceiver = new UdpClient();
        chatUploadReceiver.ExclusiveAddressUse = false;
        chatUploadReceiver.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        chatUploadReceiver.Client.Bind(new IPEndPoint(IPAddress.Any, PORT_CHAT_UPLOAD));
        chatUploadReceiver.JoinMulticastGroup(IPAddress.Parse(MULTICAST_IP));

        // Configurar BROADCASTERS (retransmitir a todos)
        videoBroadcaster = new UdpClient();
        videoBroadcaster.JoinMulticastGroup(IPAddress.Parse(MULTICAST_IP));
        videoBroadcaster.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 10);

        audioBroadcaster = new UdpClient();
        audioBroadcaster.JoinMulticastGroup(IPAddress.Parse(MULTICAST_IP));
        audioBroadcaster.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 10);

        chatBroadcaster = new UdpClient();
        chatBroadcaster.JoinMulticastGroup(IPAddress.Parse(MULTICAST_IP));
        chatBroadcaster.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 10);

        // Iniciar tareas de relay
        Task.Run(() => RelayVideoLoop(cts.Token));
        Task.Run(() => RelayAudioLoop(cts.Token));
        Task.Run(() => RelayChatLoop(cts.Token));
        Task.Run(() => CleanupInactiveParticipants(cts.Token));

        btnStart.Enabled = false;
        btnStop.Enabled = true;

        Log("✅ Servidor relay iniciado - Esperando clientes...");
    }
    catch (Exception ex)
    {
        Log($"❌ ERROR: {ex.Message}");
        StopStreaming();
    }
}

// NUEVOS MÉTODOS DE RELAY:
private void RelayVideoLoop(CancellationToken token)
{
    IPEndPoint remoteEp = new IPEndPoint(IPAddress.Any, PORT_VIDEO_UPLOAD);
    IPEndPoint broadcastEp = new IPEndPoint(IPAddress.Parse(MULTICAST_IP), PORT_VIDEO_BROADCAST);

    while (!token.IsCancellationRequested)
    {
        try
        {
            byte[] packet = videoUploadReceiver.Receive(ref remoteEp);
            if (packet.Length < PacketHeader.HEADER_SIZE) continue;

            // Extraer ClientID
            var (header, payload) = PacketHeader.ParsePacket(packet);

            // Registrar participante
            lock (participants)
            {
                if (!participants.ContainsKey(header.ClientID))
                {
                    participants[header.ClientID] = new ParticipantInfo(header.ClientID, remoteEp.Address.ToString());
                    Log($"📹 Nuevo participante: {header.ClientID}");
                }
                participants[header.ClientID].UpdateActivity();
                participants[header.ClientID].FrameCount++;
            }

            // Retransmitir a TODOS
            videoBroadcaster.Send(packet, packet.Length, broadcastEp);
        }
        catch (SocketException) { }
        catch (Exception ex)
        {
            if (!token.IsCancellationRequested)
                Log($"ERROR video relay: {ex.Message}");
        }
    }
}

private void RelayAudioLoop(CancellationToken token)
{
    IPEndPoint remoteEp = new IPEndPoint(IPAddress.Any, PORT_AUDIO_UPLOAD);
    IPEndPoint broadcastEp = new IPEndPoint(IPAddress.Parse(MULTICAST_IP), PORT_AUDIO_BROADCAST);

    while (!token.IsCancellationRequested)
    {
        try
        {
            byte[] packet = audioUploadReceiver.Receive(ref remoteEp);
            
            // Retransmitir directamente (audio no usa PacketHeader, solo lleva clientID prefix)
            audioBroadcaster.Send(packet, packet.Length, broadcastEp);
        }
        catch (SocketException) { }
        catch (Exception ex)
        {
            if (!token.IsCancellationRequested)
                Log($"ERROR audio relay: {ex.Message}");
        }
    }
}

private void RelayChatLoop(CancellationToken token)
{
    IPEndPoint remoteEp = new IPEndPoint(IPAddress.Any, PORT_CHAT_UPLOAD);
    IPEndPoint broadcastEp = new IPEndPoint(IPAddress.Parse(MULTICAST_IP), PORT_CHAT_BROADCAST);

    while (!token.IsCancellationRequested)
    {
        try
        {
            byte[] packet = chatUploadReceiver.Receive(ref remoteEp);
            string mensaje = Encoding.UTF8.GetString(packet);
            
            Log($"💬 Chat: {mensaje}");
            
            // Retransmitir a todos
            chatBroadcaster.Send(packet, packet.Length, broadcastEp);
        }
        catch (SocketException) { }
        catch (Exception ex)
        {
            if (!token.IsCancellationRequested)
                Log($"ERROR chat relay: {ex.Message}");
        }
    }
}

private void CleanupInactiveParticipants(CancellationToken token)
{
    while (!token.IsCancellationRequested)
    {
        try
        {
            Thread.Sleep(5000); // Check cada 5 segundos

            lock (participants)
            {
                var inactives = participants.Where(p => p.Value.IsTimedOut(30)).ToList();
                foreach (var inactive in inactives)
                {
                    Log($"⏹️ Participante desconectado: {inactive.Key}");
                    participants.Remove(inactive.Key);
                }
            }
        }
        catch { }
    }
}
```

---

### PASO 3: Refactorizar CLIENTE

El cliente debe:
1. **CAPTURAR** su cámara y micrófono (código que antes tenía el servidor)
2. **ENVIAR** a puertos upload (9001, 9002, 9003) con su ClientID
3. **RECIBIR** de puertos broadcast (8080, 8081, 8082)
4. **MOSTRAR** múltiples tiles (uno por cada ClientID recibido)

**Código necesario en `ClientForm.cs`:**

```csharp
// AGREGAR CAMPOS PARA CAPTURA
private CameraFrameSource frameSource;
private SimpleAudioCapture audioCapture;
private string myClientID;  // Identificador único de este cliente
private int myFrameNumber = 0;

// AGREGAR SENDERS
private UdpClient videoUploadSender;
private UdpClient audioUploadSender;
private UdpClient chatUploadSender;

// SISTEMA DE TILES
private Dictionary<string, PictureBox> videoTiles = new Dictionary<string, PictureBox>();
private Dictionary<string, byte[]> frameBuffers = new Dictionary<string, byte[]>();
private Dictionary<string, int> receivedPackets = new Dictionary<string, int>();

// MODIFICAR OnConnect:
private void OnConnect(object sender, EventArgs e)
{
    // Pedir nombre de usuario
    myClientID = Microsoft.VisualBasic.Interaction.InputBox(
        "Ingresa tu nombre (máx 15 caracteres):", 
        "Identificación", 
        "User" + new Random().Next(100, 999)
    ).Trim().Substring(0, Math.Min(15, myClientID.Length));

    if (string.IsNullOrEmpty(myClientID))
        return;

    isConnected = true;

    // Iniciar recepción de broadcasts
    StartVideoReceiver();      // Puerto 8080
    StartAudioReceiver();      // Puerto 8081
    StartChat();               // Puerto 8082

    // Iniciar captura y envío
    StartCameraCapture();      // Captura y envía a 9001
    StartAudioCapture();       // Captura y envía a 9002

    btnConnect.Enabled = false;
    btnDisconnect.Enabled = true;
    Log($"[INFO] Conectado como: {myClientID}");
}

// NUEVO: Capturar y enviar cámara
private void StartCameraCapture()
{
    try
    {
        // Buscar primera cámara
        Camera cam = null;
        foreach (Camera c in CameraService.AvailableCameras)
        {
            cam = c;
            break;
        }

        if (cam == null)
        {
            Log("[WARN] No se encontró cámara");
            return;
        }

        frameSource = new CameraFrameSource(cam);
        frameSource.Camera.CaptureWidth = 320;
        frameSource.Camera.CaptureHeight = 240;
        frameSource.Camera.Fps = 15;
        frameSource.NewFrame += OnMyCameraFrame;
        frameSource.StartFrameCapture();

        // Crear sender
        videoUploadSender = new UdpClient();
        videoUploadSender.JoinMulticastGroup(IPAddress.Parse(MULTICAST_IP));

        Log("[INFO] Cámara capturando");
    }
    catch (Exception ex)
    {
        Log($"[ERROR] Camera: {ex.Message}");
    }
}

private void OnMyCameraFrame(Touchless.Vision.Contracts.IFrameSource source, 
                             Touchless.Vision.Contracts.Frame frame, double fps)
{
    if (!isConnected) return;

    try
    {
        Bitmap bitmap = (Bitmap)frame.Image.Clone();
        Task.Run(() => SendMyVideoFrame(bitmap));
    }
    catch (Exception ex)
    {
        Log($"[ERROR] Frame: {ex.Message}");
    }
}

private void SendMyVideoFrame(Bitmap bitmap)
{
    try
    {
        // Convertir a JPEG
        byte[] imageData;
        using (var ms = new MemoryStream())
        {
            bitmap.Save(ms, ImageFormat.Jpeg);
            imageData = ms.ToArray();
        }
        bitmap.Dispose();

        // Split en chunks
        int chunkSize = 2500;
        int totalChunks = (int)Math.Ceiling((double)imageData.Length / chunkSize);

        for (int i = 0; i < totalChunks; i++)
        {
            int offset = i * chunkSize;
            int length = Math.Min(chunkSize, imageData.Length - offset);

            byte[] chunk = new byte[length];
            Array.Copy(imageData, offset, chunk, 0, length);

            // Crear cabecera con MI clientID
            var header = new PacketHeader
            {
                ClientID = myClientID,
                Timestamp = DateTime.Now.ToBinary(),
                ImageNumber = myFrameNumber,
                SequenceNumber = i,
                TotalPackets = totalChunks,
                TotalSize = imageData.Length,
                ChunkSize = chunkSize
            };

            byte[] packet = PacketHeader.CreatePacket(header, chunk);

            // Enviar al servidor (puerto 9001)
            IPEndPoint uploadEp = new IPEndPoint(IPAddress.Parse(MULTICAST_IP), 9001);
            videoUploadSender.Send(packet, packet.Length, uploadEp);
        }

        myFrameNumber++;
    }
    catch (Exception ex)
    {
        Log($"[ERROR] Send video: {ex.Message}");
    }
}

// NUEVO: Capturar y enviar audio
private void StartAudioCapture()
{
    try
    {
        audioCapture = new SimpleAudioCapture();
        audioCapture.DataAvailable += OnMyAudioData;
        audioCapture.StartRecording(8000, 16, 1);

        audioUploadSender = new UdpClient();
        audioUploadSender.JoinMulticastGroup(IPAddress.Parse(MULTICAST_IP));

        Log("[INFO] Micrófono capturando");
    }
    catch (Exception ex)
    {
        Log($"[ERROR] Audio capture: {ex.Message}");
    }
}

private void OnMyAudioData(object sender, AudioDataEventArgs e)
{
    if (!isConnected) return;

    try
    {
        // Codificar A-Law
        byte[] audioData = new byte[e.BytesRecorded];
        Array.Copy(e.Buffer, audioData, e.BytesRecorded);
        byte[] encoded = ALawEncoder.ALawEncode(audioData);

        // Prefijo con clientID (16 bytes) + encoded
        byte[] packet = new byte[16 + encoded.Length];
        byte[] idBytes = Encoding.UTF8.GetBytes(myClientID.PadRight(16).Substring(0, 16));
        Buffer.BlockCopy(idBytes, 0, packet, 0, 16);
        Buffer.BlockCopy(encoded, 0, packet, 16, encoded.Length);

        // Enviar al servidor (puerto 9002)
        IPEndPoint uploadEp = new IPEndPoint(IPAddress.Parse(MULTICAST_IP), 9002);
        audioUploadSender.Send(packet, packet.Length, uploadEp);
    }
    catch (Exception ex)
    {
        Log($"[ERROR] Send audio: {ex.Message}");
    }
}

// MODIFICAR ReceiveVideoLoop para tiles múltiples:
private void ReceiveVideoLoop()
{
    IPEndPoint remoteEp = new IPEndPoint(IPAddress.Any, VIDEO_PORT); // 8080

    while (isConnected)
    {
        try
        {
            byte[] packet = videoReceiver.Receive(ref remoteEp);
            if (packet.Length < PacketHeader.HEADER_SIZE) continue;

            var (header, chunk) = PacketHeader.ParsePacket(packet);
            string clientId = header.ClientID;

            // Ignorar mis propios paquetes
            if (clientId == myClientID) continue;

            // Crear key única por cliente e imagen
            string bufferKey = $"{clientId}_{header.ImageNumber}";

            // Primer chunk
            if (header.SequenceNumber == 0)
            {
                frameBuffers[bufferKey] = new byte[header.TotalSize];
                receivedPackets[bufferKey] = 1;
                Array.Copy(chunk, 0, frameBuffers[bufferKey], 0, chunk.Length);
            }
            else if (frameBuffers.ContainsKey(bufferKey))
            {
                // Chunks intermedios/finales
                Array.Copy(chunk, 0, frameBuffers[bufferKey], 
                          header.SequenceNumber * header.ChunkSize, chunk.Length);
                receivedPackets[bufferKey]++;

                // Imagen completa
                if (receivedPackets[bufferKey] == header.TotalPackets)
                {
                    try
                    {
                        using (var ms = new MemoryStream(frameBuffers[bufferKey]))
                        {
                            Bitmap bitmap = new Bitmap(ms);
                            ShowFrameInTile(clientId, bitmap);
                        }
                    }
                    catch { }

                    frameBuffers.Remove(bufferKey);
                    receivedPackets.Remove(bufferKey);
                }
            }
        }
        catch (ObjectDisposedException) { break; }
        catch (SocketException) { break; }
        catch (Exception ex)
        {
            if (isConnected)
                Log($"[ERROR] Video receive: {ex.Message}");
        }
    }
}

private void ShowFrameInTile(string clientId, Bitmap bitmap)
{
    try
    {
        BeginInvoke(new Action(() =>
        {
            if (!videoTiles.ContainsKey(clientId))
            {
                // Crear nuevo tile
                var pb = new PictureBox
                {
                    Size = new Size(320, 240),
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BackColor = Color.Black,
                    Margin = new Padding(5)
                };

                var label = new Label
                {
                    Text = clientId,
                    ForeColor = Color.White,
                    BackColor = Color.FromArgb(47, 49, 54),
                    Dock = DockStyle.Bottom,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Height = 25
                };

                var panel = new Panel
                {
                    Size = new Size(320, 265),
                    BackColor = Color.FromArgb(47, 49, 54)
                };
                panel.Controls.Add(pb);
                panel.Controls.Add(label);

                if (flowVideo != null)
                {
                    flowVideo.Controls.Add(panel);
                    videoTiles[clientId] = pb;
                }
            }

            // Actualizar imagen
            var old = videoTiles[clientId].Image;
            videoTiles[clientId].Image = bitmap;
            old?.Dispose();
        }));
    }
    catch
    {
        bitmap?.Dispose();
    }
}
```

---

## COMPILACIÓN Y TESTING

Una vez aplicados todos los cambios:

```bash
cd src\project
msbuild MultiCom.sln /p:Configuration=Release /t:Rebuild
```

**Testing:**
1. Ejecutar 1 servidor
2. Ejecutar 2-3 clientes en diferentes PCs (o mismo PC)
3. Cada cliente debe ver los tiles de los DEMÁS clientes
4. Chat debe funcionar para todos

---

## RESUMEN DE ARCHIVOS A MODIFICAR

| Archivo | Acción |
|---------|--------|
| `MultiCom.Shared/PacketHeader.cs` | ✅ Creado |
| `MultiCom.Shared/ParticipantInfo.cs` | ✅ Creado |
| `MultiCom.Shared/MultiCom.Shared.csproj` | ✅ Actualizado |
| `MultiCom.Server/ServerForm.cs` | ⏳ Refactorizar (código arriba) |
| `MultiCom.Client/ClientForm.cs` | ⏳ Refactorizar (código arriba) |

---

**Tiempo estimado para aplicar cambios manualmente:** 60-90 minutos  
**¿Procedo con modificar automáticamente los archivos o prefieres hacerlo manualmente con este código de referencia?**
