# MultiCom - Sistema de Videoconferencia

## Arquitectura Cliente-Servidor

```
┌─────────────┐                    ┌──────────────┐
│  Cliente A  │◄──────────────────►│   Servidor   │
│             │    Unicast UDP     │              │
│  - Video    │                    │   - Relay    │
│  - Audio    │                    │   - Presence │
│  - Chat     │                    │   - Control  │
└─────────────┘                    └──────────────┘
                                          ▲
                                          │ Unicast UDP
                                          ▼
                                   ┌──────────────┐
                                   │  Cliente B   │
                                   │              │
                                   │  - Video     │
                                   │  - Audio     │
                                   │  - Chat      │
                                   └──────────────┘
```

## Funcionamiento

### 1. Servidor (MultiCom.Server.exe)
- Escucha en puertos UDP 5050-5053
- Recibe datos de TODOS los clientes (video, audio, chat, presence)
- Reenvía a CADA cliente individualmente por unicast
- Mantiene lista de clientes conectados

### 2. Cliente (MultiCom.Client.exe)
- **Envía TODO al servidor** (unicast)
- **Recibe TODO del servidor** (unicast)
- NO usa multicast cuando hay servidor configurado
- Modo P2P (sin servidor): usa multicast directo

## Puertos UDP

| Servicio | Puerto |
|----------|--------|
| Video    | 5050   |
| Chat     | 5051   |
| Audio    | 5052   |
| Control  | 5053   |

## Configuración Básica

### PC1 (Servidor + Cliente)

1. **Ejecutar MultiCom.Server.exe**
   - Debe mostrar: "Relay services started"

2. **Ejecutar MultiCom.Client.exe**
   - Settings → Server IP: `127.0.0.1`
   - Settings → Name: Usuario1
   - Connect

### PC2 (Solo Cliente)

1. **Obtener IP de PC1**
   ```powershell
   ipconfig
   ```
   Ejemplo: `192.168.1.105`

2. **Ejecutar MultiCom.Client.exe**
   - Settings → Server IP: `192.168.1.105` (IP de PC1)
   - Settings → Name: Usuario2
   - Connect

## Firewall (Windows)

```powershell
# PowerShell como Administrador
New-NetFirewallRule -DisplayName "MultiCom" -Direction Inbound -Protocol UDP -LocalPort 5050-5053 -Action Allow
```

## Verificación

### Servidor
```
[INFO] Presence service started
[INFO] Relay services started
[INFO] Presence updated by Usuario1
[INFO] Presence updated by Usuario2
```

### Cliente
```
[INFO] Connected to MultiCom services
[INFO] Synchronized with server
```

Lista de miembros debe mostrar ambos usuarios.

## Características

- ✅ **Video bidireccional** - Cada cliente ve a todos los demás
- ✅ **Audio bidireccional** - Comunicación de voz en tiempo real
- ✅ **Chat bidireccional** - Mensajes de texto entre todos
- ✅ **Presence** - Lista de usuarios conectados
- ✅ **Métricas** - FPS, latencia, jitter, pérdidas

## Resolución de Problemas

### "PC2 no ve a PC1"

**Causa:** Firewall bloqueando UDP

**Solución:**
```powershell
# En AMBAS PCs como Admin
New-NetFirewallRule -DisplayName "MultiCom" -Direction Inbound -Protocol UDP -Action Allow
New-NetFirewallRule -DisplayName "MultiCom OUT" -Direction Outbound -Protocol UDP -Action Allow
```

### "Veo el tile pero sin imagen"

**Causas posibles:**
1. Pérdida de paquetes (verificar métricas)
2. Firewall bloqueando selectivamente
3. Calidad JPEG muy alta para la red

**Solución:** Settings → Quality: Reducir a 50-60

### "Error: Intento de acceso a un socket"

**Causa:** Puerto en uso por otro proceso

**Solución:**
```powershell
Get-Process | Where-Object { $_.ProcessName -like "*MultiCom*" } | Stop-Process -Force
```

Luego reiniciar servidor.

## Requisitos de Red

- **Mínimo:** Red local 100 Mbps
- **Recomendado:** Red local Gigabit o Wi-Fi 5GHz
- **Máximo clientes:** 10-15 (limitación del relay unicast)

## Compilación

```powershell
cd src\project
dotnet build MultiCom.sln
```

Ejecutables en:
- `MultiCom.Server\bin\Debug\MultiCom.Server.exe`
- `MultiCom.Client\bin\Debug\MultiCom.Client.exe`

---

**Versión:** 3.0 (Unicast Relay)  
**Fecha:** 2026-01-02
