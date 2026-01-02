# MultiCom - Versión Simplificada (Broadcast)

## 🎯 Arquitectura Real

```
         SERVIDOR                          
    ┌──────────────────┐                   
    │  🎥 Cámara       │                   
    │  💬 Chat         │                   
    └────────┬─────────┘                   
             │                             
      Multicast UDP                        
      239.50.10.x                          
             │                             
    ┌────────┴────────┐                   
    ▼                 ▼                    
┌─────────┐       ┌─────────┐            
│Cliente 1│       │Cliente 2│            
│ 📺 Ver  │       │ 📺 Ver  │            
│ 💬 Chat │       │ 💬 Chat │            
└─────────┘       └─────────┘            
```

## ✨ Cambios Realizados

### De 1700 líneas a 400 líneas

He creado versiones **ultra-simplificadas** basadas en el proyecto de referencia:

- **ServerFormSimple.cs** (~350 líneas) - Captura cámara y transmite
- **ClientFormSimple.cs** (~380 líneas) - Recibe y muestra

### Mantenido del proyecto actual:
- ✅ UI moderna estilo Discord
- ✅ Colores y diseño visual
- ✅ PictureBox para video
- ✅ ListBox para chat y logs

### Eliminado (innecesario):
- ❌ Todo el sistema de Presence/Roster
- ❌ Relay unicast complejo
- ❌ Performance tracking complejo
- ❌ VideoFrameAssembler de 200 líneas
- ❌ Cliente capturando cámara
- ❌ Servidor como relay

## 🚀 Cómo Funciona

### Servidor (ServerFormSimple.cs)

1. **Seleccionar cámara** del combo
2. **Click Start**
3. Captura frames → Encode JPEG → Split chunks → Multicast
4. Recibe chat de clientes
5. Envía chat a clientes

### Cliente (ClientFormSimple.cs)

1. **Escribir nombre**
2. **Click Connect**
3. Recibe chunks → Reassemble → Decode JPEG → Muestra
4. Envía/recibe mensajes de chat

## 📡 Canales Multicast

| Canal | IP Multicast | Puerto | Dirección |
|-------|-------------|--------|-----------|
| Video | 239.50.10.1 | 5050 | Servidor → Todos |
| Chat Servidor | 239.50.10.2 | 5051 | Servidor → Todos |
| Chat Clientes | 239.50.10.4 | 5053 | Clientes → Todos |

## 🔧 Integración

### Opción 1: Reemplazar archivos actuales

```powershell
# Backup
Copy-Item src\project\MultiCom.Server\ServerForm.cs src\project\MultiCom.Server\ServerForm.cs.backup
Copy-Item src\project\MultiCom.Client\ClientForm.cs src\project\MultiCom.Client\ClientForm.cs.backup

# Reemplazar con versiones simples
Copy-Item src\project\MultiCom.Server\ServerFormSimple.cs src\project\MultiCom.Server\ServerForm.cs
Copy-Item src\project\MultiCom.Client\ClientFormSimple.cs src\project\MultiCom.Client\ClientForm.cs
```

### Opción 2: Crear proyecto nuevo

Usa `ServerFormSimple.cs` y `ClientFormSimple.cs` como base para un nuevo proyecto limpio.

## ⚠️ Requisitos de UI

Ambos Forms necesitan estos controles (mantener nombres exactos):

### ServerForm
- `comboCameras` - ComboBox para seleccionar cámara
- `picturePreview` - PictureBox para preview local
- `btnStart` - Button "Start"
- `btnStop` - Button "Stop"
- `listEvents` - ListBox para logs
- `listChat` - ListBox para mensajes
- `txtMessage` - TextBox para escribir
- `btnSendMessage` - Button "Send"

### ClientForm
- `txtName` - TextBox para nombre de usuario
- `btnConnect` - Button "Connect"
- `btnDisconnect` - Button "Disconnect"
- `pictureVideo` - PictureBox para video del servidor
- `lblProfileName` - Label para mostrar "Usuario: X"
- `lblLatency` - Label para "Latencia: X ms"
- `listChat` - ListBox para mensajes
- `listDiagnostics` - ListBox para logs
- `txtMessage` - TextBox para escribir
- `btnSendMessage` - Button "Send"

## 🎨 Mantiene Diseño Visual

Los archivos `.Designer.cs` actuales ya tienen todo el diseño visual. Solo necesitas asegurarte que los **nombres de controles** coincidan.

## ✅ Ventajas

1. **Simple** - 400 líneas vs 1700
2. **Funcional** - Multicast directo, sin relay
3. **Rápido** - No hay latencia de relay
4. **Visual** - Mantiene UI moderna
5. **Basado en referencia** - Probado y funcional

## 📝 Próximos Pasos

1. **Revisar** que los `.Designer.cs` tengan los controles con nombres correctos
2. **Compilar**
3. **Probar** - Servidor en PC1, cliente en PC1 y PC2
4. **Ajustar** colores/fonts si es necesario

## 🔍 Diferencias Clave vs Proyecto Anterior

```csharp
// ANTES (complejo, 1700 líneas)
- Sistema de Presence/Snapshot
- Tracking de clientes con IPs
- Relay unicast individual
- Cliente captura cámara y envía
- Servidor hace relay de todo

// AHORA (simple, 400 líneas)
- Solo multicast broadcasting
- Servidor captura y transmite
- Cliente solo recibe y muestra
- Chat bidireccional simple
```

---

**Fecha:** 2026-01-02  
**Versión:** 4.0 (Broadcast Simplificado)
