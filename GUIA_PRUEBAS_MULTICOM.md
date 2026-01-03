# 🧪 GUÍA DE PRUEBAS - SISTEMA MULTICOM

## 📋 PREREQUISITOS

### Hardware
- ✅ Cámara web conectada y funcionando
- ✅ Micrófono disponible
- ✅ Altavoces o auriculares
- ✅ Al menos 2 PCs en la misma red (o usar localhost para pruebas básicas)

### Software
- ✅ Windows con .NET Framework 4.6.1 o superior
- ✅ Ejecutables compilados:
  - `MultiCom.Server\bin\Debug\MultiCom.Server.exe`
  - `MultiCom.Client\bin\Debug\MultiCom.Client.exe`

---

## 🚀 PRUEBA BÁSICA (1 Servidor + 1 Cliente)

### Paso 1: Iniciar Servidor
1. Ejecutar `MultiCom.Server.exe`
2. **Verificar** que aparece lista de cámaras detectadas
3. Hacer clic en **"Start"**
4. **Verificar logs:**
   - ✅ "Iniciando cámara: [nombre_cámara]"
   - ✅ "Iniciando captura de audio..."
   - ✅ "✅ Audio capturando"
   - ✅ "✅ Transmisión iniciada correctamente"

### Paso 2: Iniciar Cliente
1. Ejecutar `MultiCom.Client.exe`
2. **Verificar:** `[INFO] Ready. Press Connect to join.`
3. Hacer clic en **"Connect"**
4. **Verificar logs:**
   - ✅ "[INFO] Video receiver started on port 8080"
   - ✅ "[INFO] Audio receiver started on port 8081"
   - ✅ "[INFO] Chat initialized."
   - ✅ "[INFO] Connected to conference."

### Paso 3: Verificar Video
1. **Observar:** Panel de video debe mostrar imagen de la cámara del servidor
2. **Esperar:** 3-5 segundos para estabilización
3. **Verificar:** Imagen se actualiza continuamente

### Paso 4: Verificar Audio
1. **Hablar** cerca del micrófono del servidor
2. **Escuchar:** Audio debe reproducirse en cliente con ligero delay
3. **Calidad esperada:** 8kHz, puede sonar comprimido (A-Law)

### Paso 5: Verificar Chat
1. En el cliente, escribir mensaje en cuadro de texto
2. Presionar **Enter** o botón **"Send"**
3. **Verificar:** Mensaje aparece en lista de chat del cliente
4. **Verificar:** Mensaje aparece en lista de chat del servidor

### Paso 6: Verificar Métricas
**Ubicación:** Panel superior derecho del cliente

Esperar 5-10 segundos y verificar:

✅ **FPS:** 10-15 fps (depende de cámara y red)
✅ **Latency:** 10-100 ms (red local)
✅ **Jitter:** 5-50 ms (normal)
✅ **Loss:** 0 pkts (ideal)

---

## 🌐 PRUEBA MULTICLIENTE (1 Servidor + N Clientes)

### Configuración
1. Servidor en PC_A
2. Cliente_1 en PC_B
3. Cliente_2 en PC_C
4. ...

### Procedimiento
1. Iniciar servidor en PC_A
2. Iniciar clientes en todos los PCs
3. Conectar todos los clientes
4. **Verificar:**
   - ✅ Todos reciben video del servidor
   - ✅ Todos reciben audio del servidor
   - ✅ Chat funciona entre todos

### Prueba de Chat Multiusuario
1. Cliente_1 envía mensaje "Hola desde Cliente 1"
2. **Verificar:** Mensaje aparece en:
   - Lista de chat del servidor
   - Listas de chat de TODOS los clientes
3. Repetir desde otros clientes

---

## 📊 PRUEBAS DE MÉTRICAS

### Prueba 1: FPS Baseline
**Objetivo:** Establecer FPS normal de la red

1. Conectar cliente
2. Esperar 30 segundos
3. Anotar FPS promedio: ________ fps
4. **Esperado:** 10-15 fps

### Prueba 2: Latencia Baseline
**Objetivo:** Medir latencia de red

1. Conectar cliente
2. Esperar 30 segundos
3. Anotar Latency promedio: ________ ms
4. **Esperado:**
   - Red local (LAN): 5-50 ms
   - WiFi: 10-100 ms
   - Internet local: 20-200 ms

### Prueba 3: Detección de Pérdidas
**Objetivo:** Verificar que se detectan paquetes perdidos

**Método 1 - Simulación por desconexión momentánea:**
1. Conectar cliente
2. Esperar 10 segundos
3. Desconectar cable de red por 2 segundos
4. Reconectar
5. **Verificar:** Loss > 0 pkts

**Método 2 - Saturación de red:**
1. Conectar cliente
2. Iniciar descarga pesada en el mismo PC
3. **Observar:** Loss puede aumentar, FPS puede disminuir

### Prueba 4: Jitter
**Objetivo:** Medir estabilidad de latencia

1. Conectar cliente
2. **Sin carga de red:** Anotar Jitter: ________ ms
3. **Con carga de red:** Anotar Jitter: ________ ms
4. **Esperado:** Jitter aumenta con carga

---

## 🔍 VALIDACIÓN DE PROTOCOLO

### Verificar Consistencia Servidor-Cliente

**En Servidor (revisar código):**
```csharp
// ServerForm.cs líneas 306-314
byte[] timestamp = BitConverter.GetBytes(DateTime.Now.ToBinary());  // 8 bytes
byte[] frameBytes = BitConverter.GetBytes(frameNumber);             // 4 bytes
byte[] chunkIndexBytes = BitConverter.GetBytes(i);                  // 4 bytes
byte[] totalChunksBytes = BitConverter.GetBytes(totalChunks);       // 4 bytes
byte[] totalSizeBytes = BitConverter.GetBytes(imageData.Length);    // 4 bytes
byte[] chunkSizeBytes = BitConverter.GetBytes(CHUNK_SIZE);          // 4 bytes
// = 28 bytes
```

**En Cliente (revisar código):**
```csharp
// ClientForm.cs líneas 176-182
long timestampBinary = BitConverter.ToInt64(packet, 0);      // offset 0, 8 bytes
int imageNum = BitConverter.ToInt32(packet, 8);              // offset 8, 4 bytes
int seqNum = BitConverter.ToInt32(packet, 12);               // offset 12, 4 bytes
int totalPackets = BitConverter.ToInt32(packet, 16);         // offset 16, 4 bytes
int totalSize = BitConverter.ToInt32(packet, 20);            // offset 20, 4 bytes
int chunkSize = BitConverter.ToInt32(packet, 24);            // offset 24, 4 bytes
// = 28 bytes
```

✅ **CONSISTENTE:** Ambos usan cabecera de 28 bytes con mismos campos y offsets

---

## 🐛 TROUBLESHOOTING

### Problema: Cliente no recibe video
**Causas posibles:**
- ❌ Firewall bloqueando puerto 8080 UDP
- ❌ Red no permite multicast
- ❌ Dirección multicast 224.0.0.1 no ruteable

**Solución:**
1. Verificar firewall: permitir `MultiCom.Client.exe` y `MultiCom.Server.exe`
2. En Windows: `netsh advfirewall firewall add rule name="MultiCom" dir=in action=allow protocol=UDP localport=8080-8083`
3. Verificar que ambos estén en misma subnet

### Problema: FPS = 0.0
**Causas:**
- ❌ No se completan frames (todos los paquetes se pierden)
- ❌ Timer de métricas no iniciado

**Solución:**
1. Verificar logs: debe aparecer "[INFO] Video receiver started"
2. Verificar que aparezca imagen (aunque sea intermitente)
3. Revisar Loss: si es muy alto, problema de red

### Problema: Latency > 500ms
**Causas:**
- ❌ Red muy congestionada
- ❌ Sincronización de reloj entre servidor y cliente

**Solución:**
1. Verificar que servidor y cliente tengan hora sincronizada (NTP)
2. Reducir tráfico de red
3. Usar red cableada en lugar de WiFi

### Problema: Loss muy alto (>100 pkts en 30 seg)
**Causas:**
- ❌ Paquetes UDP muy grandes (fragmentación)
- ❌ Red saturada
- ❌ Buffer de recepción pequeño

**Solución:**
1. Reducir CHUNK_SIZE en servidor (de 2500 a 1400)
2. Reducir FPS de cámara (de 15 a 10)
3. Reducir resolución de cámara (de 320x240 a 160x120)

### Problema: Audio cortado
**Causas:**
- ❌ Pérdida de paquetes de audio
- ❌ Buffer de audio muy pequeño

**Solución:**
1. Verificar que puerto 8081 no esté bloqueado
2. Audio es más sensible a pérdidas que video
3. Considerar usar TCP para audio (modificación mayor)

---

## 📈 VALORES DE REFERENCIA

### Red Local Cableada (Gigabit)
- FPS: 14-15 fps
- Latency: 5-15 ms
- Jitter: 2-10 ms
- Loss: 0-2 pkts/minuto

### WiFi 802.11n (5GHz)
- FPS: 10-15 fps
- Latency: 10-50 ms
- Jitter: 5-30 ms
- Loss: 0-10 pkts/minuto

### WiFi 802.11g (2.4GHz)
- FPS: 8-12 fps
- Latency: 20-100 ms
- Jitter: 10-50 ms
- Loss: 5-30 pkts/minuto

---

## ✅ CHECKLIST DE PRUEBAS COMPLETAS

### Funcionalidad Básica
- [ ] Servidor inicia correctamente
- [ ] Cliente conecta correctamente
- [ ] Video se visualiza en cliente
- [ ] Audio se reproduce en cliente
- [ ] Chat funciona bidireccional

### Métricas
- [ ] FPS muestra valores entre 5-15
- [ ] Latency muestra valores entre 5-200ms
- [ ] Jitter muestra valores razonables
- [ ] Loss = 0 en red estable
- [ ] Métricas se resetean al reconectar

### Múltiples Clientes
- [ ] 2+ clientes conectan simultáneamente
- [ ] Todos reciben mismo video
- [ ] Chat se propaga a todos
- [ ] Desconexión de uno no afecta a otros

### Robustez
- [ ] Reconexión funciona correctamente
- [ ] Pérdidas de paquetes se detectan
- [ ] Frames incompletos se manejan correctamente
- [ ] No hay memory leaks (probar 10+ minutos)

---

## 📝 REPORTE DE PRUEBAS

**Fecha:** _______________  
**Configuración de red:** _______________  
**Número de clientes:** _______________

### Resultados

| Métrica | Valor Mínimo | Valor Máximo | Promedio |
|---------|--------------|--------------|----------|
| FPS     |              |              |          |
| Latency |              |              |          |
| Jitter  |              |              |          |
| Loss    |              |              |          |

### Observaciones
```
[Anotar aquí cualquier comportamiento anormal o problema encontrado]
```

### Conclusión
- [ ] ✅ Todas las pruebas pasaron
- [ ] ⚠️ Algunas pruebas con issues menores
- [ ] ❌ Fallos críticos encontrados

---

**Elaborado por:** Sistema MultiCom  
**Última actualización:** 2026-01-03
