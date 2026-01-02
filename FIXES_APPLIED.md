# Correcciones Aplicadas - MultiCom

## Versión 3.0 - Arquitectura Cliente-Servidor Pura

### Problema Original

**Síntomas:**
- Chat intermitente - unos mensajes llegan, otros no
- Video unidireccional - PC1 ve a PC2, pero PC2 NO ve a PC1
- Audio unidireccional

**Causa raíz:** Multicast no funciona de manera confiable en redes Windows domésticas.

---

## Solución Implementada

### Arquitectura: Relay Unicast Puro

**Antes (problemático):**
```
Cliente → Servidor → Multicast → Todos los clientes ❌
```

**Ahora (correcto):**
```
Cliente A ──unicast──→ Servidor ──unicast──→ Cliente B
                                 └─unicast──→ Cliente C
```

### Cambios Clave

#### 1. Servidor: Tracking de IPs + Relay Unicast Individual

**Código agregado:**
```csharp
// Trackear IP de cada cliente
private readonly Dictionary<Guid, IPEndPoint> clientEndpoints;

// En ListenPresenceLoop:
clientEndpoints[message.ClientId] = new IPEndPoint(remote.Address, 0);

// En StartRelayLoop:
foreach (var client in clientEndpoints.Values)
{
    var target = new IPEndPoint(client.Address, port);
    await sender.SendAsync(buffer, length, target);
}
```

#### 2. Cliente: Solo Multicast en Modo P2P

**Código modificado:**
```csharp
private UdpClient CreateMulticastListener(IPEndPoint endpoint)
{
    // ... crear socket ...
    
    // Solo unirse a multicast si NO hay servidor
    if (serverUnicastAddress == null)
    {
        JoinMulticastGroup(udp, endpoint.Address);
    }
    
    return udp;
}
```

#### 3. Socket Creation con ReuseAddress

**Para evitar errores de permisos:**
```csharp
private static UdpClient CreateRelayReceiver(int port)
{
    var socket = new Socket(...);
    socket.SetSocketOption(..., ReuseAddress, true);
    socket.Bind(new IPEndPoint(IPAddress.Any, port));
    
    var udp = new UdpClient();
    udp.Client = socket;
    return udp;
}
```

---

## Archivos Modificados

### MultiCom.Server/ServerForm.cs
- **Línea ~17:** Agregado `Dictionary<Guid, IPEndPoint> clientEndpoints`
- **Línea ~160:** Captura IP del cliente en `ListenPresenceLoop`
- **Línea ~365:** Relay unicast individual en `StartRelayLoop`
- **Línea ~355:** `CreateRelayReceiver` con Socket/ReuseAddress
- **Línea ~368:** `CreateRelaySender` con Socket/ReuseAddress

### MultiCom.Client/ClientForm.cs
- **Línea ~42:** Nombre por defecto cambiado a "Invitado"
- **Línea ~755:** `SendPresence` usa `ResolveTransmissionEndpoint`
- **Línea ~653:** `SendVideoSegmentAsync` solo unicast o solo multicast
- **Línea ~1347:** `BroadcastChatPayload` solo unicast o solo multicast
- **Línea ~1484:** `CreateMulticastListener` solo se une a multicast en modo P2P
- **Línea ~1443:** `CloseClient` con triple cierre (Client.Close + Close + Dispose)
- **Línea ~1469:** `WaitListenerTask` espera solo 50ms

---

## Flujo de Comunicación

### Video
```
PC1 cámara → Encode JPEG → Unicast a Server:5050
Server:5050 → Recibe → Unicast a PC2:5050
PC2:5050 → Recibe → Decode → Muestra tile
```

### Chat
```
PC1 texto → ChatEnvelope → Unicast a Server:5051
Server:5051 → Recibe → Unicast a PC2:5051
PC2:5051 → Recibe → Muestra en chat
```

### Audio
```
PC1 mic → A-law encode → Unicast a Server:5052
Server:5052 → Recibe → Unicast a PC2:5052
PC2:5052 → Recibe → A-law decode → Reproduce
```

### Presence/Control
```
PC1 → Heartbeat → Unicast a Server:5053
Server:5053 → Snapshot con todos → Unicast a todos los clientes
Clientes → Actualizan lista de miembros
```

---

## Ventajas

| Aspecto | Multicast | Unicast Relay |
|---------|-----------|---------------|
| Funciona en Windows | ❌ Problemático | ✅ Siempre |
| Atraviesa firewall | ❌ Difícil | ✅ Fácil |
| Requiere IGMP | ✅ Sí | ❌ No |
| Configuración router | ✅ Compleja | ❌ Ninguna |
| Escalabilidad | ✅ Excelente | ⚠️ Limitada (< 20) |

---

## Testing

### Test Básico (Misma PC)

1. Ejecutar `MultiCom.Server.exe`
2. Ejecutar `MultiCom.Client.exe` (1era instancia)
   - Server IP: `127.0.0.1`
   - Name: Usuario1
   - Connect
3. Ejecutar `MultiCom.Client.exe` (2da instancia)
   - Server IP: `127.0.0.1`
   - Name: Usuario2
   - Connect

**Verificar:**
- Ambos aparecen en lista de miembros
- Chat bidireccional funciona
- Video bidireccional funciona

### Test Real (2 PCs)

1. **PC1:** Ejecutar servidor, obtener IP con `ipconfig`
2. **PC1:** Ejecutar cliente, Server IP: `127.0.0.1`
3. **PC2:** Ejecutar cliente, Server IP: `<IP de PC1>`

**Verificar lo mismo.**

---

## Resolución de Problemas

### "PC2 no ve a PC1"

**Firewall bloqueando UDP.** Solución:

```powershell
# PowerShell como Admin (en AMBAS PCs)
New-NetFirewallRule -DisplayName "MultiCom" -Direction Inbound -Protocol UDP -LocalPort 5050-5053 -Action Allow
```

### "Unable to start relay services"

**Puerto en uso.** Solución:

```powershell
Get-Process | Where-Object { $_.ProcessName -like "*MultiCom*" } | Stop-Process -Force
```

### "Veo tile pero sin imagen"

**Verificar métricas del cliente:**
- Loss > 50 → Red saturada, reducir FPS/Quality
- Latency > 300ms → Red lenta

---

## Documentación

- `README_MULTICOM.md` - Guía de uso
- `UNICAST_RELAY_FIX.md` - Explicación técnica detallada
- `FIXES_APPLIED.md` - Este archivo

---

**Última actualización:** 2026-01-02 17:30 UTC


### Problemas Detectados en Primera Revisión:
1. **Control/Presence enviaba duplicado** (multicast + unicast siempre)
2. **Desconexión todavía lenta** - El cierre de sockets no abortaba Receive()
3. **Chat no funcionaba** - Enviaba siempre por multicast aunque hubiera servidor

### Correcciones Aplicadas en Revisión 2:

#### 1. **SendPresence unificado**
**Antes:** Enviaba SIEMPRE tanto a multicast como unicast
```csharp
controlSender.Send(payload, payload.Length, controlEndpoint);
if (serverUnicastAddress != null) {
    controlSender.Send(payload, payload.Length, unicast); // Duplicado
}
```

**Después:** Usa `ResolveTransmissionEndpoint()` (una sola transmisión)
```csharp
var target = ResolveTransmissionEndpoint(controlEndpoint);
controlSender.Send(payload, payload.Length, target);
```

#### 2. **Cierre AGRESIVO de sockets**
**Problema:** `client.Close()` no abortaba `Receive()` inmediatamente

**Solución:**
```csharp
private void CloseClient(ref UdpClient client)
{
    try
    {
        client.Client?.Close();  // Cierra socket subyacente
        client.Close();          // Cierra UdpClient
        client.Dispose();        // Libera recursos
    }
    catch { }
    client = null;
}
```

#### 3. **WaitListenerTask ultra-rápido**
**Antes:** Esperaba 300ms
**Después:** Espera solo 50ms y verifica si ya está completada

```csharp
private void WaitListenerTask(Task task)
{
    if (task == null || task.IsCompleted) return;
    task.Wait(50);  // Solo 50ms
}
```

#### 4. **Nombre por defecto cambiado**
- **Antes:** `"Agent"`
- **Después:** `"Invitado"`

Archivos modificados:
- Línea 42: `private string displayName = "Invitado";`
- Línea 1589: Fallback en `ApplyPreferences`

---

### Problemas Identificados y Solucionados

#### 1. **Problema de Video RTP: Cliente B no ve a Cliente A**

**Causa raíz:**
El cliente estaba enviando paquetes de video **dos veces**:
- Una vez por unicast al servidor (correcto)
- Una vez por multicast directo (incorrecto cuando hay servidor)

Esto causaba problemas porque:
- El servidor ya retransmite los paquetes unicast a multicast
- Enviar directo a multicast duplicaba tráfico y causaba conflictos
- Los clientes no veían correctamente los videos de otros clientes

**Solución aplicada:**
- Modificado `ClientForm.cs::SendVideoSegmentAsync()`: Ahora solo envía por **unicast al servidor** si está configurado, o por **multicast** si no hay servidor
- Aplicada la misma lógica a `BroadcastChatPayload()` para consistencia
- El audio ya usaba correctamente `ResolveTransmissionEndpoint()` que implementa esta lógica

**Archivos modificados:**
- `src/project/MultiCom.Client/ClientForm.cs` (líneas ~653-690, ~1347-1380)

**Código antes:**
```csharp
// Enviaba SIEMPRE tanto por unicast como multicast
if (unicastAddress != null) {
    await videoSender.SendAsync(..., unicastTarget);
}
await videoSender.SendAsync(..., multicastEndpoint); // ❌ Problema
```

**Código después:**
```csharp
// Envía SOLO por unicast si hay servidor, SOLO por multicast si no
if (unicastAddress != null) {
    await videoSender.SendAsync(..., unicastTarget);
} else {
    await videoSender.SendAsync(..., multicastEndpoint); // ✅ Correcto
}
```

---

#### 2. **Problema de Cierre Lento de la Aplicación**

**Causa raíz:**
- `WaitListenerTask()` esperaba 1 segundo por cada tarea (video, chat, control, audio)
- Total: 4+ segundos solo esperando tareas
- Las tareas bloqueadas en `Receive()` no terminaban inmediatamente
- El servidor también tenía el mismo problema con relay tasks

**Solución aplicada:**
1. **Cliente**: Reducido timeout de espera de 1000ms a 300ms en `WaitListenerTask()`
2. **Cliente**: Cerrar sockets **ANTES** de esperar tareas (para interrumpir `Receive()`)
3. **Servidor**: Reducido timeout de 1000ms a 300ms en `WaitRelayTask()`
4. **Servidor**: Agregado timeout polling en `StartRelayLoop()` usando `Task.WhenAny()` con delay de 500ms
5. **Servidor**: Configurado `ReceiveTimeout = 1000` en receptores relay
6. **Servidor**: Reordenado cierre para cancelar relay services antes de esperar presence task

**Archivos modificados:**
- `src/project/MultiCom.Client/ClientForm.cs` (líneas ~239-313, ~1461-1478)
- `src/project/MultiCom.Server/ServerForm.cs` (líneas ~72-113, ~339-429)

**Mejoras implementadas en el servidor:**
```csharp
// Antes: bloqueaba indefinidamente
result = await receiver.ReceiveAsync();

// Después: polling con timeout de 500ms
var receiveTask = receiver.ReceiveAsync();
var delayTask = Task.Delay(500, token);
var completedTask = await Task.WhenAny(receiveTask, delayTask);
if (completedTask == delayTask) continue;
result = await receiveTask;
```

---

### Resultados Esperados

✅ **Video RTP funcional**: Los clientes ahora pueden ver correctamente los videos de otros clientes a través del servidor

✅ **Cierre rápido**: La aplicación (cliente y servidor) se cierra en menos de 1 segundo en lugar de 4+ segundos

✅ **Arquitectura correcta**: 
- Sin servidor: comunicación P2P multicast directa
- Con servidor: comunicación cliente → servidor (unicast) → multicast relay

---

### Notas Técnicas

**Arquitectura de comunicación:**
```
Cliente A  ──unicast──→  Servidor  ──multicast──→  Grupo Multicast
                                                          ↓
Cliente B (listener multicast)  ←───────────────────────┘
```

**Gestión de desconexión mejorada:**
1. Enviar `Goodbye` presence
2. Detener heartbeat y captura
3. Cancelar tokens (señal a todas las tareas)
4. **CERRAR SOCKETS** (interrumpe Receive bloqueantes)
5. Esperar tareas con timeout corto (300ms)
6. Limpiar recursos

---

### Testing Recomendado

1. **Test de video bidireccional:**
   - Iniciar servidor
   - Conectar Cliente A y activar cámara
   - Conectar Cliente B y activar cámara
   - Verificar que ambos clientes ven ambas cámaras

2. **Test de cierre:**
   - Con aplicación conectada y cámara activa
   - Presionar "Disconnect" y medir tiempo
   - Debería ser < 1 segundo
   - Cerrar aplicación completa (X) y medir tiempo
   - Debería ser < 1.5 segundos

3. **Test sin servidor (P2P):**
   - No configurar server IP
   - Conectar múltiples clientes
   - Verificar que la comunicación multicast directa funciona

---

### Advertencia de Compilación

La advertencia sobre arquitectura x86 vs MSIL de WebCamLib es existente y no afecta la funcionalidad:
```
warning MSB3270: Arquitectura MSIL vs x86 no coinciden
```
Esto es esperado ya que WebCamLib es una DLL x86 y el proyecto es AnyCPU.
