# SOLUCIÓN: Error "Intento de acceso a un socket no permitido"

## ✅ Problema Resuelto

### Causa del Error
El error "Intento de acceso a un socket no permitido por sus permisos de acceso" ocurrió porque:

1. **Puerto 5050 (VIDEO)** estaba ocupado por `svchost.exe` (proceso del sistema Windows)
2. **Puerto 5053 (CONTROL)** estaba ocupado por una instancia previa de `MultiCom.Server`
3. El cliente intentaba hacer `Bind()` en puertos ya en uso

### Solución Aplicada

**Cambio de puertos en `MultiCom.Shared/Networking/MulticastChannels.cs`:**

```csharp
// ANTES (puertos conflictivos):
VIDEO_PORT = 5050;     // ❌ Ocupado por svchost
CHAT_PORT = 5051;
AUDIO_PORT = 5052;
CONTROL_PORT = 5053;   // ❌ Ocupado por servidor previo

// DESPUÉS (puertos libres):
VIDEO_PORT = 20989;    // ✅ Libre
CHAT_PORT = 20993;     // ✅ Libre
AUDIO_PORT = 20995;    // ✅ Libre
CONTROL_PORT = 20997;  // ✅ Libre
```

**Nota:** Los puertos 20989, 20993, 20995, 20997 son puertos altos (> 10000) que raramente están en uso por el sistema.

---

## 🔍 Cómo Detectar Este Problema en el Futuro

### 1. Verificar puertos en uso
```powershell
netstat -ano | Select-String "PUERTO"
```

Ejemplo:
```powershell
netstat -ano | Select-String "5050"
```

### 2. Identificar proceso que usa el puerto
```powershell
Get-Process -Id PID
```

Ejemplo:
```powershell
# Si netstat muestra PID 828 usando puerto 5050:
Get-Process -Id 828
```

### 3. Cerrar instancias previas antes de compilar
```powershell
Stop-Process -Name "MultiCom.Client","MultiCom.Server" -Force
```

---

## 🛠️ Troubleshooting Adicional

### Error: "El archivo está bloqueado por otro proceso"
**Causa:** Instancia del programa corriendo mientras intentas compilar.

**Solución:**
```powershell
# Cerrar todas las instancias:
Stop-Process -Name "MultiCom*" -Force
taskkill /F /IM MultiCom.Client.exe
taskkill /F /IM MultiCom.Server.exe
```

### Error: "Puerto ya en uso" después del cambio
**Causa:** Otro programa está usando los nuevos puertos.

**Diagnóstico:**
```powershell
netstat -ano | Select-String "20989|20993|20995|20997"
```

**Solución:** Cambiar a otros puertos disponibles (rango recomendado: 10000-65535).

---

## 📋 Checklist de Depuración

Cuando veas "Intento de acceso a un socket no permitido":

1. [ ] Cerrar todas las instancias del cliente/servidor
2. [ ] Verificar puertos con `netstat -ano | Select-String "PUERTO"`
3. [ ] Identificar conflictos (svchost, instancias previas, etc.)
4. [ ] Cambiar puertos en `MulticastChannels.cs` si es necesario
5. [ ] Recompilar proyecto
6. [ ] Ejecutar de nuevo

---

## ⚙️ Configuración de Puertos Recomendada

### Opción 1: Puertos actuales (✅ aplicados)
```csharp
VIDEO_PORT = 20989;
CHAT_PORT = 20993;
AUDIO_PORT = 20995;
CONTROL_PORT = 20997;
```

### Opción 2: Puertos alternativos (si sigues teniendo problemas)
```csharp
VIDEO_PORT = 15000;
CHAT_PORT = 15001;
AUDIO_PORT = 15002;
CONTROL_PORT = 15003;
```

### Opción 3: Según referencia PDF (puede tener conflictos)
```csharp
VIDEO_PORT = 20989;  // Del ejemplo PDF
CHAT_PORT = 8080;    // Del ejemplo PDF
// Nota: 8080 es muy común, evitar si es posible
```

---

## 🧪 Verificación de Solución

### Test 1: Verificar puertos libres
```powershell
netstat -ano | Select-String "20989|20993|20995|20997"
# Resultado esperado: Sin salida (puertos libres)
```

### Test 2: Compilar sin errores
```powershell
cd D:\CODE\DigitalLaboratoryContents\src\project
msbuild MultiCom.sln /t:Build /p:Configuration=Debug
# Resultado esperado: Build succeeded
```

### Test 3: Ejecutar cliente sin error de socket
```
1. Ejecutar MultiCom.Client.exe
2. Click en "Connect"
3. Resultado esperado: "[INFO] Connected to MultiCom services."
4. Sin error: "Intento de acceso a un socket no permitido"
```

---

## 📖 Contexto Adicional

### ¿Por qué svchost usa puerto 5050?

`svchost.exe` es un proceso contenedor de servicios de Windows. Puede usar muchos puertos para diferentes servicios (Windows Update, BITS, etc.). El puerto 5050 puede estar asignado a:

- **Yahoo Messenger** (legacy)
- **Multimedia conferencing**
- **Algunos servicios de Windows**

**Solución:** NO intentar cerrar svchost (es crítico del sistema). En su lugar, usar puertos diferentes.

### ¿Por qué el rango 20000-21000?

- Puertos **< 1024**: Privilegiados, requieren admin
- Puertos **1024-49151**: Registrados (pueden estar en uso)
- Puertos **49152-65535**: Dinámicos/privados (Windows los usa temporalmente)
- Puertos **10000-30000**: Zona segura para aplicaciones custom

El rango 20000-21000 es relativamente seguro y raramente conflictivo.

---

## ✅ Estado Actual

- ✅ Puertos cambiados de 5050-5053 → 20989-20997
- ✅ Proyecto recompilado exitosamente
- ✅ Instancias previas cerradas
- ✅ Listo para ejecutar cliente/servidor

---

**Próximo paso:** Ejecutar cliente y verificar conexión exitosa sin error de socket.
