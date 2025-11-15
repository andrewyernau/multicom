# Guía de Implementación del Servidor - Lab2 Parte 3

Este documento detalla la implementación del servidor de calculadora usando .NET Remoting con HTTP.

## ✅ Requisitos Completados

### 1. Proyecto de Aplicación de Consola
- ✅ Tipo: Aplicación de Consola (.NET Framework)
- ✅ Ubicación: `src/lab/CalculatorServer/`
- ✅ Target Framework: .NET Framework 4.7.2

### 2. Referencia a System.Runtime.Remoting.dll
- ✅ Agregada en el archivo `.csproj`
- ✅ Proporciona acceso a protocolos HTTP/TCP
- ✅ Clases utilizadas:
  - `System.Runtime.Remoting.RemotingConfiguration`
  - `System.Runtime.Remoting.Channels.ChannelServices`
  - `System.Runtime.Remoting.Channels.Http.HttpChannel`

### 3. Referencia a CalculatorLib.dll
- ✅ Agregada como referencia al proyecto
- ✅ Apunta a: `..\CalculatorLib\bin\Debug\CalculatorLib.dll`
- ✅ Permite usar la clase `Operaciones`

## Implementación del Servidor

### Código Completo

```csharp
using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Http;
using CalculatorLib;

namespace CalculatorServer
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                // 1. Registrar canal HTTP en puerto 8090
                HttpChannel channel = new HttpChannel(8090);
                ChannelServices.RegisterChannel(channel, false);
                
                // 2. Configurar nombre de aplicación
                RemotingConfiguration.ApplicationName = "CalculatorService";
                
                // 3. Registrar servicio como tipo conocido (Singleton)
                RemotingConfiguration.RegisterWellKnownServiceType(
                    typeof(Operaciones),
                    "Operaciones",
                    WellKnownObjectMode.Singleton
                );
                
                Console.WriteLine("Server running. Press ENTER to stop...");
                Console.ReadLine();
                
                // 4. Limpiar al cerrar
                ChannelServices.UnregisterChannel(channel);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[AGENT] Error: " + ex.Message);
                Console.ReadLine();
            }
        }
    }
}
```

## Análisis Detallado del Código

### Paso 1: Registro del Canal HTTP

```csharp
HttpChannel channel = new HttpChannel(8090);
ChannelServices.RegisterChannel(channel, false);
```

**¿Qué hace?**
- Crea un canal de comunicación HTTP en el puerto 8090
- `false` = No usar seguridad (apropiado para desarrollo)

**Alternativas:**
- `TcpChannel`: Para comunicación TCP (más rápido)
- `IpcChannel`: Para comunicación entre procesos en la misma máquina

### Paso 2: Nombre de la Aplicación

```csharp
RemotingConfiguration.ApplicationName = "CalculatorService";
```

**¿Qué hace?**
- Define el nombre base de la aplicación remota
- Forma parte de la URI del servicio
- URI completa: `http://localhost:8090/CalculatorService/Operaciones`

### Paso 3: Registro del Servicio

```csharp
RemotingConfiguration.RegisterWellKnownServiceType(
    typeof(Operaciones),      // Tipo de objeto a exponer
    "Operaciones",            // URI relativa del servicio
    WellKnownObjectMode.Singleton  // Modo de activación
);
```

**¿Qué hace?**
- Registra la clase `Operaciones` como servicio remoto
- La expone en la URI especificada
- Usa modo Singleton (una instancia para todos los clientes)

**Modos de Activación:**

#### Singleton
```csharp
WellKnownObjectMode.Singleton
```
- ✅ Una única instancia para todos los clientes
- ✅ Estado compartido entre clientes
- ✅ Más eficiente en memoria
- ❌ Problemas de concurrencia si no se sincroniza

#### SingleCall
```csharp
WellKnownObjectMode.SingleCall
```
- ✅ Nueva instancia por cada llamada
- ✅ Sin estado compartido
- ✅ Más seguro para concurrencia
- ❌ Más uso de memoria

### Paso 4: Mantener Servidor Activo

```csharp
Console.ReadLine();
```

**¿Qué hace?**
- Mantiene la aplicación ejecutándose
- El servidor procesa peticiones mientras espera
- Presionar ENTER detiene el servidor

### Paso 5: Limpieza

```csharp
ChannelServices.UnregisterChannel(channel);
```

**¿Qué hace?**
- Libera el puerto 8090
- Cierra el canal HTTP correctamente
- Permite reiniciar el servidor sin errores

## Configuración del Proyecto (.csproj)

### Referencias Necesarias

```xml
<ItemGroup>
  <!-- Referencia al framework de Remoting -->
  <Reference Include="System.Runtime.Remoting" />
  
  <!-- Referencia a la librería de calculadora -->
  <Reference Include="CalculatorLib">
    <HintPath>..\CalculatorLib\bin\Debug\CalculatorLib.dll</HintPath>
  </Reference>
</ItemGroup>
```

## Comparación con el Ejemplo de Microsoft

### Ejemplo de Microsoft (Documentación)

```csharp
// Del ejemplo oficial
ChannelServices.RegisterChannel(new HttpChannel(8090));

WellKnownServiceTypeEntry wkste = 
    new WellKnownServiceTypeEntry(
        typeof(RemoteObject),
        "RemoteObject",
        WellKnownObjectMode.Singleton
    );

RemotingConfiguration.RegisterWellKnownServiceType(wkste);
```

### Nuestra Implementación

```csharp
// Versión simplificada equivalente
HttpChannel channel = new HttpChannel(8090);
ChannelServices.RegisterChannel(channel, false);

RemotingConfiguration.RegisterWellKnownServiceType(
    typeof(Operaciones),
    "Operaciones",
    WellKnownObjectMode.Singleton
);
```

**Diferencias:**
1. ✅ Usamos método directo en lugar de `WellKnownServiceTypeEntry`
2. ✅ Guardamos referencia al canal para poder limpiarlo
3. ✅ Agregamos `ApplicationName` para mejor organización
4. ✅ Solo implementamos la parte del SERVIDOR (como se solicitó)
5. ✅ Usamos `CalculatorLib.dll` en lugar de clase inline

## URI del Servicio

### Estructura Completa

```
http://localhost:8090/CalculatorService/Operaciones
│      │         │     │                  │
│      │         │     │                  └── Nombre del objeto (URI relativa)
│      │         │     └───────────────────── Nombre de la aplicación
│      │         └─────────────────────────── Puerto del canal HTTP
│      └───────────────────────────────────── Host (localhost)
└──────────────────────────────────────────── Protocolo
```

### Ejemplos de URI Válidas

```
http://localhost:8090/CalculatorService/Operaciones
http://192.168.1.100:8090/CalculatorService/Operaciones
http://miservidor.com:8090/CalculatorService/Operaciones
```

## Flujo de Ejecución

### 1. Inicio del Servidor

```
[Usuario] Ejecuta CalculatorServer.exe
    ↓
[Servidor] Carga System.Runtime.Remoting.dll
    ↓
[Servidor] Carga CalculatorLib.dll
    ↓
[Servidor] Registra HttpChannel en puerto 8090
    ↓
[Servidor] Configura ApplicationName
    ↓
[Servidor] Registra tipo Operaciones como Singleton
    ↓
[Servidor] Muestra "Server running..."
    ↓
[Servidor] Espera peticiones (Console.ReadLine())
```

### 2. Petición de Cliente

```
[Cliente] Solicita objeto remoto via Activator.GetObject()
    ↓
[Servidor] Recibe petición HTTP en puerto 8090
    ↓
[Servidor] Extrae URI del mensaje
    ↓
[Servidor] Busca "Operaciones" en tabla de servicios
    ↓
[Servidor] Retorna proxy del objeto Operaciones
    ↓
[Cliente] Recibe proxy
```

### 3. Invocación de Método

```
[Cliente] calc.Sumar(10, 5)
    ↓
[Proxy] Serializa llamada a mensaje HTTP
    ↓
[HTTP] Envía mensaje al servidor (puerto 8090)
    ↓
[Servidor] Deserializa mensaje
    ↓
[Servidor] Invoca Operaciones.Sumar(10, 5)
    ↓
[Operaciones] Ejecuta: return 10 + 5;
    ↓
[Servidor] Serializa resultado (15)
    ↓
[HTTP] Envía respuesta al cliente
    ↓
[Proxy] Deserializa respuesta
    ↓
[Cliente] Recibe resultado: 15
```

## Testing del Servidor

### Verificación Manual

1. **Compilar y Ejecutar**:
   ```bash
   cd CalculatorServer\bin\Debug
   CalculatorServer.exe
   ```

2. **Verificar Salida**:
   ```
   ===========================================
     Calculator Remote Server
   ===========================================
   
   [INFO] HTTP Channel registered on port 8090
   [INFO] Application name set: CalculatorService
   [INFO] Service registered: Operaciones
   [INFO] Mode: Singleton
   
   Service URI: http://localhost:8090/CalculatorService/Operaciones
   
   Server is running. Press ENTER to stop...
   ===========================================
   ```

3. **Probar con Cliente** (próxima sección):
   - Crear aplicación cliente
   - Conectar a la URI del servicio
   - Invocar operaciones

## Solución de Problemas Comunes

### Error: "El puerto 8090 ya está en uso"

**Causa**: Otra aplicación está usando el puerto 8090

**Solución**:
```csharp
// Cambiar a otro puerto
HttpChannel channel = new HttpChannel(8091);
```

### Error: "No se puede cargar CalculatorLib"

**Causa**: La DLL no está en la ubicación esperada

**Solución**:
1. Compilar CalculatorLib primero
2. Verificar ruta en el .csproj:
   ```xml
   <HintPath>..\CalculatorLib\bin\Debug\CalculatorLib.dll</HintPath>
   ```
3. Copiar manualmente la DLL a `bin\Debug`

### Error: "System.Runtime.Remoting no encontrado"

**Causa**: Proyecto configurado para .NET Core/.NET 5+

**Solución**:
- Usar .NET Framework 4.7.2
- Remoting solo existe en .NET Framework

### Advertencia: "RegisterChannel is obsolete"

**Causa**: API antigua en versiones recientes de .NET Framework

**Solución**:
```csharp
// Agregar segundo parámetro 'false'
ChannelServices.RegisterChannel(channel, false);
```

## Diferencias con el Requisito Original

### Solicitado en el Lab
```
"implementar SOLAMENTE las partes de ese ejemplo requeridas 
(aquellas en la que se ofrece el servicio y NO aquellas en 
las que se usa el servicio)"
```

### Implementado
✅ **Solo código del SERVIDOR**:
- Registro del canal HTTP
- Configuración del servicio
- Exposición del objeto remoto
- Mantener servidor activo

❌ **NO incluido (según requisito)**:
- Código del cliente
- `Activator.GetObject()`
- Invocación de métodos remotos
- Parte de consumo del servicio

## Mejoras Implementadas

### Más Allá del Requisito Mínimo

1. **Mensajes Informativos**:
   ```csharp
   Console.WriteLine("[INFO] HTTP Channel registered on port 8090");
   Console.WriteLine("[INFO] Service registered: Operaciones");
   ```

2. **Manejo de Excepciones**:
   ```csharp
   try {
       // Código del servidor
   } catch (Exception ex) {
       Console.WriteLine("[AGENT] Error: " + ex.Message);
   }
   ```

3. **Limpieza Apropiada**:
   ```csharp
   ChannelServices.UnregisterChannel(channel);
   ```

4. **Información Detallada**:
   - URI completa del servicio
   - Lista de operaciones disponibles
   - Modo de activación usado

5. **Documentación Completa**:
   - README.md con guía de uso
   - Comentarios XML en el código
   - Ejemplos de cliente

## Convenciones Seguidas

- ✅ PascalCase para clases y métodos
- ✅ camelCase para variables locales
- ✅ Documentación XML en clases públicas
- ✅ Mensajes [INFO] para información
- ✅ Mensajes [AGENT] para errores
- ✅ Código limpio y bien estructurado

## Comparación con Tecnologías Modernas

### .NET Remoting (Este proyecto)
```csharp
// Servidor
RemotingConfiguration.RegisterWellKnownServiceType(
    typeof(Operaciones), "Operaciones", WellKnownObjectMode.Singleton
);

// Cliente
var calc = (Operaciones)Activator.GetObject(
    typeof(Operaciones), "http://localhost:8090/..."
);
```

### gRPC (Alternativa moderna)
```csharp
// Servidor
var server = new Server {
    Services = { Calculator.BindService(new CalculatorImpl()) },
    Ports = { new ServerPort("localhost", 8090, ServerCredentials.Insecure) }
};
server.Start();

// Cliente
var channel = new Channel("localhost:8090", ChannelCredentials.Insecure);
var client = new Calculator.CalculatorClient(channel);
```

### ASP.NET Core Web API (Alternativa REST)
```csharp
// Servidor
[ApiController]
[Route("api/calculator")]
public class CalculatorController : ControllerBase {
    [HttpGet("sum")]
    public double Sum(double a, double b) => a + b;
}

// Cliente
var client = new HttpClient();
var result = await client.GetStringAsync(
    "http://localhost:8090/api/calculator/sum?a=10&b=5"
);
```

## Conclusión

La implementación del servidor cumple con todos los requisitos:

✅ **Aplicación de Consola**: Tipo de proyecto correcto
✅ **System.Runtime.Remoting.dll**: Referencia agregada
✅ **CalculatorLib.dll**: Referencia al proyecto de librería
✅ **Canal HTTP**: Configurado en puerto 8090
✅ **Servicio Registrado**: Clase Operaciones como Singleton
✅ **Solo Servidor**: No incluye código de cliente
✅ **Basado en Documentación**: Sigue ejemplo de Microsoft
✅ **Librería Externa**: Usa CalculatorLib.dll (no inline)

El servidor está listo para:
- Aceptar conexiones de clientes remotos
- Exponer las operaciones de la calculadora
- Procesar peticiones HTTP
- Mantener estado con modo Singleton

Próximo paso: Implementar el cliente remoto (Parte 4 del Lab).
