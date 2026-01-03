# Plan de Implementación: Servidor Central + Clientes Capturadores

## ARQUITECTURA FINAL CONFIRMADA

```
CLIENTE 1:                    SERVIDOR CENTRAL:              TODOS LOS CLIENTES:
├─ Captura cámara            ├─ Escucha puerto 9001         ├─ Reciben de 8080
├─ Captura micrófono         ├─ "Captura" streams           ├─ Muestran N tiles
├─ Envía a 9001/9002/9003    ├─ Identifica por clientID     ├─ Reproducen audio
└─ Recibe de 8080/8081/8082  └─ Retransmite a 8080/8081     └─ Ven chat global
```

## PASOS DE IMPLEMENTACIÓN

### PASO 1: Modificar cabecera de paquetes (agregar ClientID)
- Cambiar de 28 bytes a 44 bytes
- Agregar `ClientID` (16 bytes) al inicio
- Servidor usa este ID para identificar origen

### PASO 2: Refactorizar SERVIDOR
- **ELIMINAR:** Captura de cámara/micrófono propios
- **AGREGAR:** Listeners en 9001 (video), 9002 (audio), 9003 (chat)
- **AGREGAR:** Diccionario de participantes activos
- **AGREGAR:** Lógica de retransmisión con ID de origen

### PASO 3: Refactorizar CLIENTE
- **AGREGAR:** Captura de cámara (igual que servidor actual)
- **AGREGAR:** Captura de micrófono (igual que servidor actual)
- **AGREGAR:** Envío a puertos upload (9001, 9002, 9003)
- **MANTENER:** Recepción de broadcasts (8080, 8081, 8082)
- **AGREGAR:** Sistema de tiles múltiples
- **AGREGAR:** Input de userName (clientID único)

### PASO 4: Implementar identificación de participantes
- Estructura `ParticipantInfo` con: `{ ClientID, IPAddress, LastSeen, IsActive }`
- Servidor mantiene lista de participantes
- Timeout para desconexión automática

### PASO 5: Implementar tiles múltiples en cliente
- FlowLayoutPanel con tiles dinámicos
- Uno por cada ClientID detectado
- Mostrar nombre del participante en cada tile

## ARCHIVOS A MODIFICAR

### MultiCom.Shared
- [NUEVO] `PacketHeader.cs` - Estructura de cabecera con ClientID
- [NUEVO] `ParticipantInfo.cs` - Info de participante

### MultiCom.Server
- [MODIFICAR] `ServerForm.cs`:
  - Eliminar: `CameraFrameSource`, `SimpleAudioCapture`
  - Agregar: `ReceiveVideoUploads()`, `ReceiveAudioUploads()`, `ReceiveChatUploads()`
  - Agregar: `RelayToAllClients()`
  - Agregar: `Dictionary<string, ParticipantInfo> participants`

### MultiCom.Client
- [MODIFICAR] `ClientForm.cs`:
  - Agregar: `CameraFrameSource` (mover del servidor)
  - Agregar: `SimpleAudioCapture` (mover del servidor)
  - Agregar: `SendVideoToServer()`, `SendAudioToServer()`
  - Agregar: `Dictionary<string, PictureBox> videoTiles`
  - Modificar: `ReceiveVideoLoop()` para identificar por ClientID

## TIEMPO ESTIMADO
- Paso 1: 10 min
- Paso 2: 20 min
- Paso 3: 25 min
- Paso 4: 10 min
- Paso 5: 15 min
- Testing: 10 min
**Total: ~90 minutos**

## ¿PROCEDO?
