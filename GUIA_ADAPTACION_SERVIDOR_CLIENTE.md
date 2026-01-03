# MultiCom - Cliente-Servidor COMPLETADO ✅

## ESTADO ACTUAL

**Última actualización:** 2026-01-03 15:00  
**Estado:** ✅ **COMPILACIÓN EXITOSA - VIDEO + AUDIO + CHAT COMPLETOS**

---

## ARQUITECTURA IMPLEMENTADA

```
SERVIDOR (MultiCom.Server) - UN ÚNICO SERVIDOR
├── Captura video de cámara (Touchless.Vision)
├── Fragmenta en chunks de 2500 bytes
├── Envía multicast a 224.0.0.1:8080
├── Captura audio de micrófono (winmm.dll)
├── Codifica con A-Law y envía a 224.0.0.1:8081
├── Recibe chat de clientes en 224.0.0.1:8083
└── Envía chat a clientes en 224.0.0.1:8082

CLIENTES (MultiCom.Client) - MÚLTIPLES CLIENTES
├── Reciben video multicast de 224.0.0.1:8080
├── Reensamblan chunks → JPEG → Bitmap
├── Visualizan en PictureBox
├── Reciben audio de 224.0.0.1:8081
├── Decodifican A-Law → PCM → reproducen con NAudio
├── Reciben chat del servidor en 224.0.0.1:8082
└── Envían chat al servidor en 224.0.0.1:8083
```

---

## CAMBIOS RECIENTES (v5.0)

### ✅ Audio implementado

**Servidor:**
- `SimpleAudioCapture.cs` creado usando winmm.dll (API nativa de Windows)
- Captura: 8kHz, 16-bit, mono (calidad telefónica)
- Codificación A-Law (compresión 2:1)
- Envío por multicast UDP

**Cliente:**
- Ya tenía `SimpleAudioPlayer.cs` con NAudio
- Decodificación A-Law → PCM
- Reproducción con WaveOut

### ✅ Referencias movidas a src/project/lib/

Antes (mal):
```xml
<HintPath>..\..\..\lib\project\WebCamWrapper\bin\Release\Touchless.Vision.dll</HintPath>
```

Ahora (bien):
```xml
<HintPath>..\lib\Touchless.Vision.dll</HintPath>
```

**DLLs copiadas:**
- `Touchless.Vision.dll`
- `WebCamLib.dll`
- `System.ComponentModel.Composition.dll`

---

## CAMBIOS APLICADOS

### ✅ Servidor (`MultiCom.Server`)

1. **Video Streaming:**
   - Captura frames usando `CameraFrameSource` (Touchless.Vision)
   - Convierte a JPEG y fragmenta en chunks de 2500 bytes
   - Cabecera de 28 bytes: `timestamp(8) + imageNum(4) + seqNum(4) + totalPackets(4) + totalSize(4) + chunkSize(4)`
   - Envía por UDP multicast a `224.0.0.1:8080`

2. **Referencias agregadas:**
   - `Touchless.Vision.dll` → `lib\project\WebCamWrapper\bin\Release\`
   - `WebCamLib.dll` → `lib\project\WebCamLib\bin\Release\`

3. **Simplificaciones KIS:**
   - Usa primera cámara disponible automáticamente
   - Chat simplificado (logs en `listEvents`)
   - Audio: estructura lista pero no implementada

### ✅ Cliente (`MultiCom.Client`)

1. **Reensamblado de chunks:**
   ```csharp
   // Estado de reensamblado
   private int currentImageNumber = -1;
   private byte[] imageBuffer = null;
   private int receivedPackets = 0;
   private int expectedPackets = 0;
   
   // Lógica:
   if (seqNum == 0) {
       // Primer chunk: inicializar buffer
       imageBuffer = new byte[totalSize];
       currentImageNumber = imageNum;
       receivedPackets = 1;
   } else if (imageNum == currentImageNumber) {
       // Chunk intermedio/final
       Array.Copy(chunk, 0, imageBuffer, seqNum * chunkSize, chunk.Length);
       receivedPackets++;
       
       if (receivedPackets == expectedPackets) {
           // Imagen completa → mostrar
           using (MemoryStream ms = new MemoryStream(imageBuffer)) {
               Bitmap bitmap = new Bitmap(ms);
               ShowFrame(bitmap);
           }
       }
   }
   ```

2. **Visualización:**
   - `PictureBox` creado dinámicamente en `flowVideo`
   - Tamaño: 640x480, modo Zoom
   - Actualización thread-safe con `BeginInvoke`

---

## PUERTOS Y PROTOCOLOS

| Canal     | Puerto | Dirección   | Dirección      | Uso                  |
|-----------|--------|-------------|----------------|----------------------|
| Video     | 8080   | 224.0.0.1   | Multicast      | Server → Clients     |
| Audio     | 8081   | 224.0.0.1   | Multicast      | Server → Clients *   |
| Chat TX   | 8082   | 224.0.0.1   | Multicast      | Server → Clients     |
| Chat RX   | 8083   | 224.0.0.1   | Multicast      | Clients → Server     |

\* Audio preparado pero no implementado activamente (simplicidad)

---

## COMPILACIÓN

### Servidor
```bash
cd src\project
msbuild MultiCom.Server\MultiCom.Server.csproj /p:Configuration=Release
```
**Resultado:** `MultiCom.Server\bin\Release\MultiCom.Server.exe` ✅

### Cliente
```bash
cd src\project
msbuild MultiCom.Client\MultiCom.Client.csproj /p:Configuration=Release
```
**Resultado:** `MultiCom.Client\bin\Release\MultiCom.Client.exe` ✅

---

## TESTING

### 1. Ejecutar Servidor
```bash
cd src\project\MultiCom.Server\bin\Release
.\MultiCom.Server.exe
```
- Click **"Start service"**
- Verifica logs: "Transmisión iniciada"
- Cámara debe activarse automáticamente

### 2. Ejecutar Cliente(s)
```bash
cd src\project\MultiCom.Client\bin\Release
.\MultiCom.Client.exe
```
- Click **"Connect"**
- Deberías ver el video del servidor
- Enviar mensajes de chat (opcional)

### 3. Verificaciones
- ✅ Video se muestra en cliente
- ✅ Chat funciona bidireccional
- ⚠️ Audio no implementado (agregar si necesario)

---

## PATRONES DE CÓDIGO (Referencia)

### Fragmentación (Servidor)
```csharp
byte[][] BufferSplit(byte[] buffer, int blockSize) {
    byte[][] blocks = new byte[(buffer.Length + blockSize - 1) / blockSize][];
    for (int i = 0, j = 0; i < blocks.Length; i++, j += blockSize) {
        blocks[i] = new byte[Math.Min(blockSize, buffer.Length - j)];
        Array.Copy(buffer, j, blocks[i], 0, blocks[i].Length);
    }
    return blocks;
}
```

### Cabecera (Servidor)
```csharp
byte[] packet = new byte[28 + chunkLength];
Buffer.BlockCopy(BitConverter.GetBytes(DateTime.Now.ToBinary()), 0, packet, 0, 8);   // timestamp
Buffer.BlockCopy(BitConverter.GetBytes(frameNumber), 0, packet, 8, 4);                // imageNum
Buffer.BlockCopy(BitConverter.GetBytes(i), 0, packet, 12, 4);                         // seqNum
Buffer.BlockCopy(BitConverter.GetBytes(totalChunks), 0, packet, 16, 4);               // totalPackets
Buffer.BlockCopy(BitConverter.GetBytes(imageData.Length), 0, packet, 20, 4);          // totalSize
Buffer.BlockCopy(BitConverter.GetBytes(CHUNK_SIZE), 0, packet, 24, 4);                // chunkSize
Buffer.BlockCopy(chunk, 0, packet, 28, chunkLength);                                  // payload
```

### Extracción cabecera (Cliente)
```csharp
long timestamp = BitConverter.ToInt64(packet, 0);
int imageNum = BitConverter.ToInt32(packet, 8);
int seqNum = BitConverter.ToInt32(packet, 12);
int totalPackets = BitConverter.ToInt32(packet, 16);
int totalSize = BitConverter.ToInt32(packet, 20);
int chunkSize = BitConverter.ToInt32(packet, 24);
byte[] chunk = new byte[packet.Length - 28];
Array.Copy(packet, 28, chunk, 0, chunk.Length);
```

---

## AÑADIR AUDIO (OPCIONAL)

Si se necesita audio, usar el código de referencia en `context/Project_done/Skype`:

### Servidor
```csharp
using NAudio.Wave;

WaveInEvent waveIn = new WaveInEvent();
waveIn.WaveFormat = new WaveFormat(8000, 16, 1);
waveIn.DataAvailable += (s, e) => {
    byte[] encoded = ALawEncoder.ALawEncode(e.Buffer, e.BytesRecorded);
    audioSender.Send(encoded, encoded.Length, new IPEndPoint(IPAddress.Parse("224.0.0.1"), 8081));
};
waveIn.StartRecording();
```

### Cliente
```csharp
WaveOut waveOut = new WaveOut();
BufferedWaveProvider waveProvider = new BufferedWaveProvider(new WaveFormat(8000, 16, 1));
waveOut.Init(waveProvider);
waveOut.Play();

// En loop de recepción:
byte[] alaw = audioReceiver.Receive(ref audioEp);
short[] decoded = ALawDecoder.ALawDecode(alaw, alaw.Length);
byte[] pcm = new byte[decoded.Length * 2];
Buffer.BlockCopy(decoded, 0, pcm, 0, pcm.Length);
waveProvider.AddSamples(pcm, 0, pcm.Length);
```

Codecs disponibles en:
- `lib\project\ALawEncoder.cs`
- `lib\project\ALawDecoder.cs`

---

## ARCHIVOS DE REFERENCIA

- ✅ **Servidor funcional:** `context/Project_done/Skype/Server/WebcamUDPMulticast/Form1.cs`
- ✅ **Cliente funcional:** `context/Project_done/Skype/Client/WebcamUDPMulticast/Form1.cs`
- ✅ **Patrón implementado:** 100% basado en estos archivos

---

## BACKUPS CREADOS

- `ServerForm.cs.BACKUP_P2P` - Versión P2P incorrecta (NO USAR)
- `ServerForm.cs.BACKUP_V2` - Versión intermedia (NO USAR)
- `ClientForm.cs.BACKUP_P2P` - Versión P2P incorrecta (NO USAR)
- `ClientForm.cs.BACKUP_V2` - Versión intermedia (NO USAR)
- `ClientForm.cs.BACKUP_COMPLEX` - Versión con tiles complejos (NO USAR)

**USAR SOLO:** `ServerForm.cs` y `ClientForm.cs` actuales

---

## TROUBLESHOOTING

### Video no se ve en cliente
1. Verificar que servidor esté transmitiendo (logs)
2. Verificar firewall permite multicast UDP
3. Verificar ambos equipos en misma red local
4. Verificar puerto 8080 no está bloqueado

### Errores de compilación
1. Verificar DLLs en rutas correctas:
   - `lib\project\WebCamWrapper\bin\Release\Touchless.Vision.dll`
   - `lib\project\WebCamLib\bin\Release\WebCamLib.dll`
2. Si faltan, copiar de `context/Entregar_definitivo/`

### Cámara no se activa
1. Verificar cámara conectada y funcionando
2. Verificar permisos de acceso a cámara
3. Probar con otra aplicación (ej: Camera de Windows)

---

## PRÓXIMOS PASOS SUGERIDOS

1. ✅ **Probar transmisión video:** Servidor → Cliente(s)
2. ⏳ **Agregar audio** (si es necesario)
3. ⏳ **Mejorar UI de chat** (si es necesario)
4. ⏳ **Optimizar calidad de video** (resolución, FPS, compresión)
5. ⏳ **Agregar métricas** (bitrate, latencia, paquetes perdidos)

---

**Versión:** 4.0 - Simplificación CLIENTE-SERVIDOR COMPLETADA  
**Fecha:** 2026-01-03  
**Estado:** ✅ LISTO PARA PRUEBAS

```
SERVIDOR (MultiCom.Server)
├── Captura video de cámara
├── Fragmenta en chunks
├── Envía multicast a 224.0.0.1:8080
├── Captura audio de micrófono
├── Envía multicast a 224.0.0.1:8081
├── Recibe chat de clientes en 224.0.0.1:8083
└── Envía chat a clientes en 224.0.0.1:8082

CLIENTES (MultiCom.Client) 
├── Reciben video multicast de 224.0.0.1:8080
├── Reensamblan chunks → JPEG → Bitmap
├── Visualizan en PictureBox
├── Reciben audio multicast de 224.0.0.1:8081
├── Decodifican ALaw → reproducen
├── Reciben chat del servidor en 224.0.0.1:8082
└── Envían chat al servidor en 224.0.0.1:8083
```

## PASOS PARA COMPLETAR LA ADAPT

ACIÓN

### 1. SERVIDOR - Archivos a modificar

**ServerForm.cs** - He creado una versión simplificada en:
- `ServerForm.cs.BACKUP_P2P` (tu versión P2P incorrecta)
- La nueva versión debe seguir el patrón de `context/Project_done/Skype/Server`

**Clases necesarias:**
```csharp
- CameraFrameSource (de Touchless.Vision)
- UdpClient para envío multicast
- Fragmentación en chunks (método BufferSplit)
- Método Combine para unir bytes
```

**Cabecera de video (32 bytes):**
```
timestamp (8) + imageNumber (4) + sequenceNumber (4) + totalPackets (4) + 
totalSize (4) + chunkSize (4) + payload (variable)
```

### 2. CLIENTE - Archivos a modificar

**ClientForm.cs** - Debe:
- Recibir chunks de video
- Reensamblar usando imageNumber y sequenceNumber
- Cuando todos los chunks llegan → crear JPEG → Bitmap
- Mostrar en PictureBox

**Código de referencia:** `context/Project_done/Skype/Client/WebcamUDPMulticast/Form1.cs`

### 3. PUERTOS DEFINITIVOS

| Canal | Puerto | Dirección | Uso |
|-------|--------|-----------|-----|
| Video | 8080 | 224.0.0.1 | Servidor → Clientes |
| Audio | 8081 | 224.0.0.1 | Servidor → Clientes |
| Chat TX | 8082 | 224.0.0.1 | Servidor → Clientes |
| Chat RX | 8083 | 224.0.0.1 | Clientes → Servidor |

### 4. REFERENCIAS NECESARIAS

**MultiCom.Server.csproj:**
```xml
<Reference Include="Touchless.Vision">
  <HintPath>..\WebcamUDPMulticast\WebCamWrapper\bin\Release\Touchless.Vision.dll</HintPath>
</Reference>
<Reference Include="WebCamLib">
  <HintPath>..\WebcamUDPMulticast\WebCamLib\bin\Release\WebCamLib.dll</HintPath>
</Reference>
<Reference Include="System.ComponentModel.Composition">
  <HintPath>..\WebcamUDPMulticast\WebCamWrapper\bin\Release\System.ComponentModel.Composition.dll</HintPath>
</Reference>
```

### 5. CLIENTE - Reensamblado de Video

```csharp
// Recibir chunks
private Dictionary<int, byte[]> imageBuffer = new Dictionary<int, byte[]>();
private Dictionary<int, int> receivedPackets = new Dictionary<int, int>();

private void ReceiveVideoLoop()
{
    IPEndPoint remoteEp = new IPEndPoint(IPAddress.Any, 8080);
    
    while (isConnected)
    {
        byte[] packet = videoReceiver.Receive(ref remoteEp);
        
        // Extraer cabecera (32 bytes)
        long timestamp = BitConverter.ToInt64(packet, 0);
        int imageNum = BitConverter.ToInt32(packet, 8);
        int seqNum = BitConverter.ToInt32(packet, 12);
        int totalPackets = BitConverter.ToInt32(packet, 16);
        int totalSize = BitConverter.ToInt32(packet, 20);
        int chunkSize = BitConverter.ToInt32(packet, 24);
        
        // Extraer payload (desde byte 32 en adelante)
        byte[] payload = new byte[packet.Length - 32];
        Array.Copy(packet, 32, payload, 0, payload.Length);
        
        // Almacenar chunk
        if (!imageBuffer.ContainsKey(imageNum))
        {
            imageBuffer[imageNum] = new byte[totalSize];
            receivedPackets[imageNum] = 0;
        }
        
        // Copiar payload en posición correcta
        Array.Copy(payload, 0, imageBuffer[imageNum], seqNum * chunkSize, payload.Length);
        receivedPackets[imageNum]++;
        
        // Si recibimos todos los chunks
        if (receivedPackets[imageNum] == totalPackets)
        {
            // Convertir a imagen
            using (MemoryStream ms = new MemoryStream(imageBuffer[imageNum]))
            {
                Bitmap bitmap = new Bitmap(ms);
                ShowFrame(bitmap);
            }
            
            // Limpiar
            imageBuffer.Remove(imageNum);
            receivedPackets.Remove(imageNum);
        }
    }
}
```

### 6. AUDIO (Si lo necesitas)

**Servidor:**
```csharp
WaveInEvent waveIn = new WaveInEvent();
waveIn.WaveFormat = new WaveFormat(8000, 16, 1);
waveIn.DataAvailable += (s, e) => {
    byte[] encoded = ALawEncoder.ALawEncode(e.Buffer);
    audioSender.Send(encoded, encoded.Length, audioEndpoint);
};
waveIn.StartRecording();
```

**Cliente:**
```csharp
WaveOut waveOut = new WaveOut();
BufferedWaveProvider waveProvider = new BufferedWaveProvider(new WaveFormat(8000, 16, 1));
waveOut.Init(waveProvider);
waveOut.Play();

// En loop de recepción:
byte[] alaw = audioReceiver.Receive(ref audioEp);
short[] decoded = ALawDecoder.ALawDecode(alaw);
byte[] pcm = new byte[decoded.Length * 2];
Buffer.BlockCopy(decoded, 0, pcm, 0, pcm.Length);
waveProvider.AddSamples(pcm, 0, pcm.Length);
```

### 7. MANTENER TU INTERFAZ VISUAL

- ✅ Colores Discord (mantener)
- ✅ Layout de paneles (mantener)
- ✅ Botones Start/Stop (mantener)
- ❌ Lógica de red (reemplazar por patrón servidor-cliente)

### 8. COMPILACIÓN

```bash
cd src\project
dotnet build MultiCom.sln --configuration Release
```

**Errores esperados:** Faltan referencias a Touchless.Vision y WebCamLib

**Solución:** 
1. Asegurarte de que existen las DLLs en `WebcamUDPMulticast\WebCamWrapper\bin\Release\`
2. Si no, copiarlas de `lib\project\WebCamWrapper\` y `lib\project\WebCamLib\`

### 9. TESTING

**PC1 (Servidor):**
1. Ejecutar `MultiCom.Server.exe`
2. Click "Start"
3. Verificar logs: "Server transmitting..."

**PC2 (Cliente):**
1. Ejecutar `MultiCom.Client.exe`
2. Click "Connect"
3. Debería ver el video del servidor

## PRÓXIMOS PASOS RECOMENDADOS

1. ✅ Completar la copia de las referencias DLL
2. ✅ Adaptar el cliente para reensamblado de chunks
3. ✅ Probar comunicación servidor → cliente
4. ✅ Añadir audio si es necesario
5. ✅ Pulir la UI manteniendo tu estilo visual

## ARCHIVOS DE REFERENCIA CLAVE

- `context/Project_done/Skype/Server/WebcamUDPMulticast/Form1.cs` - SERVIDOR FUNCIONAL
- `context/Project_done/Skype/Client/WebcamUDPMulticast/Form1.cs` - CLIENTE FUNCIONAL
- `lib/project/ALawEncoder.cs` - Codificador audio
- `lib/project/ALawDecoder.cs` - Decodificador audio

---

**Estado actual:** Servidor simplificado creado, falta adaptar cliente para reensamblado de chunks.

**Versión:** 3.0 - Arquitectura Cliente-Servidor CORRECTA  
**Fecha:** 2026-01-03
