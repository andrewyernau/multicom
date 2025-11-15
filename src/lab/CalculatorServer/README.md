# CalculatorServer - Servidor de Calculadora Remota

Aplicación de consola que expone el servicio de calculadora mediante .NET Remoting sobre HTTP.

## Descripción

Este servidor utiliza la librería `CalculatorLib.dll` y la expone como un servicio remoto accesible a través de HTTP. Los clientes pueden conectarse al servidor y utilizar las operaciones de la calculadora de forma remota.

## Tecnologías Utilizadas

- **.NET Remoting**: Framework para comunicación entre procesos y máquinas
- **HTTP Channel**: Canal de comunicación basado en HTTP
- **System.Runtime.Remoting.dll**: Librería de .NET Framework para remoting
- **CalculatorLib.dll**: Librería con la lógica de operaciones

## Configuración del Servidor

### Canal HTTP
- **Puerto**: 8090
- **Protocolo**: HTTP

### Servicio Registrado
- **Clase**: `Operaciones` (de CalculatorLib)
- **URI**: `http://localhost:8090/CalculatorService/Operaciones`
- **Modo**: `Singleton` (una única instancia para todos los clientes)

### Operaciones Disponibles

```csharp
public double Sumar(double a, double b)
public double Restar(double a, double b)
public double Multiplicar(double a, double b)
public double Dividir(double a, double b)
```

## Modos de Activación

### Singleton (Implementado)
- **Características**:
  - Una única instancia del objeto para todos los clientes
  - El estado se comparte entre clientes
  - Más eficiente en uso de memoria
  - La instancia persiste mientras el servidor está activo

### SingleCall (Alternativa)
Para cambiar a modo SingleCall, modificar:
```csharp
RemotingConfiguration.RegisterWellKnownServiceType(
    typeof(Operaciones),
    "Operaciones",
    WellKnownObjectMode.SingleCall  // Una instancia por llamada
);
```

**Características SingleCall**:
- Nueva instancia para cada llamada de método
- No mantiene estado entre llamadas
- Se destruye después de cada operación
- Mayor aislamiento entre clientes

## Compilación

### Requisitos
- .NET Framework 4.7.2 o superior
- CalculatorLib.dll compilada
- System.Runtime.Remoting.dll (incluida en .NET Framework)

### Usando Visual Studio
1. Abrir `CalculatorServer.sln`
2. Menú: **Compilar → Compilar solución**
3. El ejecutable se genera en `bin\Debug\CalculatorServer.exe`

### Usando MSBuild
```bash
msbuild CalculatorServer.csproj /p:Configuration=Debug
```

## Ejecución

### Opción 1: Desde Visual Studio
1. Abrir el proyecto
2. Presionar **F5** o **Ctrl+F5**

### Opción 2: Desde línea de comandos
```bash
cd bin\Debug
CalculatorServer.exe
```

### Salida Esperada
```
===========================================
  Calculator Remote Server
===========================================

[INFO] HTTP Channel registered on port 8090
[INFO] Application name set: CalculatorService
[INFO] Service registered: Operaciones
[INFO] Mode: Singleton

Service URI: http://localhost:8090/CalculatorService/Operaciones

Available operations:
  - Sumar(double a, double b)
  - Restar(double a, double b)
  - Multiplicar(double a, double b)
  - Dividir(double a, double b)

===========================================
Server is running. Press ENTER to stop...
===========================================
```

## Uso del Servidor

### Desde un Cliente .NET Remoting

```csharp
using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Http;
using CalculatorLib;

// Registrar canal HTTP en el cliente
ChannelServices.RegisterChannel(new HttpChannel(), false);

// Obtener referencia al objeto remoto
Operaciones calc = (Operaciones)Activator.GetObject(
    typeof(Operaciones),
    "http://localhost:8090/CalculatorService/Operaciones"
);

// Usar las operaciones
double resultado = calc.Sumar(10, 5);
Console.WriteLine($"10 + 5 = {resultado}");
```

### Desde Navegador Web (Solo para inspección)
Navegar a: `http://localhost:8090/CalculatorService/Operaciones`

**Nota**: El navegador mostrará información sobre el servicio, pero no podrá invocar los métodos directamente.

## Arquitectura del Código

### Program.cs
Contiene la lógica del servidor:

1. **Registro del Canal HTTP**:
   ```csharp
   HttpChannel channel = new HttpChannel(8090);
   ChannelServices.RegisterChannel(channel, false);
   ```

2. **Configuración del Nombre de la Aplicación**:
   ```csharp
   RemotingConfiguration.ApplicationName = "CalculatorService";
   ```

3. **Registro del Servicio**:
   ```csharp
   RemotingConfiguration.RegisterWellKnownServiceType(
       typeof(Operaciones),
       "Operaciones",
       WellKnownObjectMode.Singleton
   );
   ```

4. **Mantener el Servidor Activo**:
   ```csharp
   Console.ReadLine();  // Espera entrada del usuario
   ```

## Referencias del Proyecto

### System.Runtime.Remoting.dll
- Proporciona clases para .NET Remoting
- Incluye `RemotingConfiguration`, `ChannelServices`
- Solo disponible en .NET Framework

### CalculatorLib.dll
- Referencia al proyecto de librería
- Contiene la clase `Operaciones` con herencia de `MarshalByRefObject`

## Estructura del Proyecto

```
CalculatorServer/
├── Program.cs                  # Lógica del servidor
├── Properties/
│   └── AssemblyInfo.cs        # Metadatos del ensamblado
├── CalculatorServer.csproj    # Archivo de proyecto
├── CalculatorServer.sln       # Archivo de solución
└── README.md                  # Esta documentación
```

## Solución de Problemas

### Error: "El puerto 8090 ya está en uso"

**Solución:**
1. Cambiar el puerto en el código:
   ```csharp
   HttpChannel channel = new HttpChannel(8091);
   ```
2. O cerrar la aplicación que está usando el puerto 8090

### Error: "No se puede cargar CalculatorLib.dll"

**Solución:**
1. Compilar CalculatorLib primero
2. Verificar que la DLL esté en la carpeta de salida
3. Copiar manualmente la DLL a `bin\Debug` si es necesario

### Error: "RemotingException: No se puede conectar al servidor"

**Solución en el cliente:**
1. Verificar que el servidor esté ejecutándose
2. Comprobar la URI del servicio
3. Verificar que el puerto no esté bloqueado por firewall

### Error: "System.Runtime.Remoting no está disponible"

**Causa**: Proyecto configurado para .NET Core/.NET 5+

**Solución:**
1. Usar .NET Framework 4.7.2
2. .NET Remoting solo está disponible en .NET Framework

## Seguridad

### Consideraciones
- El servidor está configurado sin autenticación
- No se usa encriptación en el canal HTTP
- Apropiado solo para desarrollo/aprendizaje

### Mejoras para Producción
1. Usar HTTPS en lugar de HTTP
2. Implementar autenticación de clientes
3. Agregar validación de entrada
4. Implementar logging de auditoría
5. Considerar alternativas modernas (gRPC, Web API)

## Diferencias con Enfoques Modernos

### .NET Remoting (Este proyecto)
- ✅ Fácil de implementar para escenarios simples
- ✅ Transparencia de ubicación
- ❌ Solo .NET Framework
- ❌ Considerado legacy/obsoleto

### Alternativas Modernas
- **gRPC**: Alto rendimiento, cross-platform
- **ASP.NET Core Web API**: REST APIs, amplio soporte
- **SignalR**: Comunicación en tiempo real
- **WCF Core**: Migración de WCF a .NET Core

## Convenciones Seguidas

- ✅ PascalCase para clases y métodos
- ✅ camelCase para variables locales
- ✅ Documentación XML en clases públicas
- ✅ Mensajes de error con prefijo [AGENT]
- ✅ Mensajes informativos con prefijo [INFO]
- ✅ Manejo de excepciones con try-catch

## Testing

### Test Manual
1. Iniciar el servidor
2. Verificar que aparece el mensaje de confirmación
3. Crear un cliente de prueba
4. Invocar operaciones remotas
5. Verificar resultados

### Test de Carga
Para probar con múltiples clientes simultáneos:
```csharp
// Cliente que hace llamadas concurrentes
Parallel.For(0, 10, i => {
    var resultado = calc.Sumar(i, i);
    Console.WriteLine($"Thread {i}: {resultado}");
});
```

## Logs y Debugging

### Habilitar Logging de Remoting
Agregar al archivo `app.config`:

```xml
<configuration>
  <system.diagnostics>
    <trace autoflush="true">
      <listeners>
        <add name="textWriter" 
             type="System.Diagnostics.TextWriterTraceListener" 
             initializeData="remoting.log" />
      </listeners>
    </trace>
  </system.diagnostics>
</configuration>
```

## Conclusión

Este servidor demuestra:
- ✅ Exposición de servicios mediante .NET Remoting
- ✅ Uso de canal HTTP para comunicación
- ✅ Registro de objetos como tipos conocidos (well-known types)
- ✅ Uso de modo Singleton para compartir instancia
- ✅ Integración con librería externa (CalculatorLib.dll)

El servidor está listo para ser usado por clientes remotos que necesiten realizar operaciones de calculadora de forma distribuida.
