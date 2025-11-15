# Guía: Modificación para Cliente Remoto

Este documento explica las modificaciones realizadas al proyecto Lab2Calculadora para usar un objeto remoto en lugar de un objeto local.

## Cambios Realizados

### 1. Referencias Agregadas

```csharp
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Http;
using CalculatorLib;
```

**Importante**: .NET Remoting requiere .NET Framework. Si el proyecto usa .NET 6/Core, necesitará:
- Migrar a .NET Framework 4.7.2, O
- Usar una alternativa moderna (gRPC, Web API)

### 2. Campo para Objeto Remoto

```csharp
private Operaciones? operaciones;
private const string SERVER_URL = "http://localhost:8090/CalculatorService/Operaciones";
```

**Cambio clave**: En lugar de instanciar localmente:
```csharp
// ANTES (local):
operaciones = new Operaciones();

// DESPUÉS (remoto):
operaciones = (Operaciones)Activator.GetObject(typeof(Operaciones), SERVER_URL);
```

### 3. Método InitializeRemoteConnection

```csharp
private void InitializeRemoteConnection()
{
    try
    {
        // Registrar canal HTTP para el cliente
        HttpChannel channel = new HttpChannel();
        ChannelServices.RegisterChannel(channel, false);

        // Obtener referencia al objeto remoto usando Activator.GetObject
        operaciones = (Operaciones)Activator.GetObject(
            typeof(Operaciones),
            SERVER_URL
        );

        lstResults.Items.Add("[INFO] Connected to remote server");
    }
    catch (Exception ex)
    {
        MessageBox.Show($"[AGENT] Could not connect: {ex.Message}");
    }
}
```

**Componentes**:
1. `HttpChannel`: Canal de comunicación HTTP (sin puerto específico en cliente)
2. `ChannelServices.RegisterChannel`: Registra el canal
3. `Activator.GetObject`: Obtiene proxy al objeto remoto

### 4. Modificación del Método PerformOperation

```csharp
private void PerformOperation(char operation)
{
    // ... validaciones ...
    
    try
    {
        switch (operation)
        {
            case '+':
                result = operaciones.Sumar(operand1, operand2);  // LLAMADA REMOTA
                break;
            case '-':
                result = operaciones.Restar(operand1, operand2);  // LLAMADA REMOTA
                break;
            // ... etc ...
        }
    }
    catch (System.Runtime.Remoting.RemotingException ex)
    {
        ShowError($"[AGENT] Server connection error: {ex.Message}");
    }
}
```

**Diferencias clave**:
- Mismo código de invocación
- Añadido manejo de `RemotingException`
- Etiqueta `[REMOTE]` en los resultados

## Comparación: Local vs Remoto

### Instanciación Local (Original)

```csharp
public Form1()
{
    InitializeComponent();
    operaciones = new Operaciones();  // Objeto local
}

private void PerformOperation(char operation)
{
    result = operaciones.Sumar(a, b);  // Llamada local (in-process)
}
```

### Instanciación Remota (Modificado)

```csharp
public Form1()
{
    InitializeComponent();
    InitializeRemoteConnection();  // Conexión remota
}

private void InitializeRemoteConnection()
{
    HttpChannel channel = new HttpChannel();
    ChannelServices.RegisterChannel(channel, false);
    
    // Obtener proxy al objeto remoto
    operaciones = (Operaciones)Activator.GetObject(
        typeof(Operaciones),
        "http://localhost:8090/CalculatorService/Operaciones"
    );
}

private void PerformOperation(char operation)
{
    result = operaciones.Sumar(a, b);  // Llamada remota (HTTP)
}
```

## Flujo de Comunicación

### Sin Servidor (Falla)
```
[Cliente] operaciones.Sumar(10, 5)
    ↓
[Proxy] Intenta conectar a http://localhost:8090...
    ↓
[Error] No se puede conectar al servidor
    ↓
[Cliente] RemotingException lanzada
    ↓
[UI] Mensaje: "Server connection error"
```

### Con Servidor (Éxito)
```
[Cliente] operaciones.Sumar(10, 5)
    ↓
[Proxy] Serializa llamada a mensaje HTTP
    ↓
[HTTP] POST a http://localhost:8090/CalculatorService/Operaciones
    ↓
[Servidor] Recibe petición
    ↓
[Servidor] Ejecuta: Operaciones.Sumar(10, 5)
    ↓
[Servidor] Retorna: 15
    ↓
[HTTP] Respuesta al cliente
    ↓
[Proxy] Deserializa respuesta
    ↓
[Cliente] result = 15
    ↓
[UI] Muestra: "10 + 5 = 15.00 [REMOTE]"
```

## Pruebas de Funcionamiento

### Prueba 1: Servidor Activo
1. **Iniciar Servidor**: Ejecutar `CalculatorServer.exe`
2. **Iniciar Cliente**: Ejecutar `Lab2Calculadora.exe`
3. **Verificar Conexión**: Debe mostrar "[INFO] Connected to remote server"
4. **Realizar Operaciones**:
   - 10 + 5 = 15.00 [REMOTE] ✅
   - 20 - 8 = 12.00 [REMOTE] ✅
   - 6 × 7 = 42.00 [REMOTE] ✅
   - 100 ÷ 5 = 20.00 [REMOTE] ✅

### Prueba 2: Servidor Inactivo
1. **Detener Servidor**: Cerrar `CalculatorServer.exe`
2. **Intentar Operación**: Click en botón de suma
3. **Resultado Esperado**: 
   ```
   [AGENT] Server connection error:
   No connection could be made because the target machine actively refused it.
   
   Is the server running?
   ```

### Prueba 3: División por Cero
1. **Con Servidor Activo**
2. **Ingresar**: 10 ÷ 0
3. **Resultado Esperado**:
   ```
   [AGENT] Cannot divide by zero.
   ```
   (Excepción manejada por el servidor y propagada al cliente)

## Ventajas del Enfoque Remoto

### Separación Física
- Cliente y servidor pueden estar en máquinas diferentes
- Actualizar servidor sin redistribuir cliente
- Escalar servidor independientemente

### Centralización
- Una sola instancia de lógica de negocio
- Actualizaciones centralizadas
- Monitoreo centralizado

### Transparencia
- Código cliente casi idéntico a versión local
- Solo cambia la instanciación del objeto
- Misma interfaz (`Sumar`, `Restar`, etc.)

## Desventajas y Consideraciones

### Latencia
```
Local:  < 1 ms
Remoto: 10-100 ms (dependiendo de red)
```

### Disponibilidad
- Dependencia del servidor
- Si servidor cae, cliente no funciona

### Seguridad
- Sin autenticación en este ejemplo
- Sin encriptación (HTTP, no HTTPS)
- Apropiado solo para desarrollo/aprendizaje

## Migración a .NET Framework

Si el proyecto original está en .NET 6/Core, necesita migración:

### Crear Nuevo Proyecto .NET Framework

```xml
<Project ToolsVersion="15.0" xmlns="...">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion>
    <UseWindowsForms>true</UseWindowsForms>
  </PropertyGroup>
  
  <ItemGroup>
    <Reference Include="System.Runtime.Remoting" />
    <Reference Include="CalculatorLib">
      <HintPath>..\CalculatorLib\bin\Debug\CalculatorLib.dll</HintPath>
    </Reference>
  </ItemGroup>
</Project>
```

### Copiar Archivos
1. Copiar `Form1.cs` con modificaciones remotas
2. Copiar `Form1.Designer.cs` (sin cambios)
3. Copiar `Form1.resx` (sin cambios)
4. Actualizar `Program.cs` si es necesario

## Alternativas Modernas

### gRPC (Recomendado para .NET 6+)

**Servidor**:
```csharp
builder.Services.AddGrpc();
app.MapGrpcService<CalculatorService>();
```

**Cliente**:
```csharp
var channel = GrpcChannel.ForAddress("http://localhost:8090");
var client = new Calculator.CalculatorClient(channel);
var result = await client.SumAsync(new SumRequest { A = 10, B = 5 });
```

### ASP.NET Core Web API

**Servidor**:
```csharp
[ApiController]
[Route("api/calculator")]
public class CalculatorController : ControllerBase
{
    [HttpGet("sum")]
    public double Sum(double a, double b) => a + b;
}
```

**Cliente**:
```csharp
var client = new HttpClient();
var response = await client.GetStringAsync(
    "http://localhost:8090/api/calculator/sum?a=10&b=5"
);
```

## Código Completo de Cambios Mínimos

### Cambio 1: Constructor

```csharp
// ANTES:
public Form1()
{
    InitializeComponent();
}

// DESPUÉS:
public Form1()
{
    InitializeComponent();
    InitializeRemoteConnection();
}
```

### Cambio 2: Agregar Método de Conexión

```csharp
// NUEVO MÉTODO:
private void InitializeRemoteConnection()
{
    HttpChannel channel = new HttpChannel();
    ChannelServices.RegisterChannel(channel, false);
    operaciones = (Operaciones)Activator.GetObject(
        typeof(Operaciones),
        "http://localhost:8090/CalculatorService/Operaciones"
    );
}
```

### Cambio 3: Actualizar Llamadas

```csharp
// ANTES:
result = operand1 + operand2;

// DESPUÉS:
result = operaciones.Sumar(operand1, operand2);
```

## Resumen

✅ **Completado**: Cliente modificado para usar objeto remoto
✅ **Método usado**: `Activator.GetObject()`
✅ **Protocolo**: HTTP sobre .NET Remoting
✅ **Funcionamiento**: 100% remoto cuando servidor está activo
✅ **Manejo de errores**: Detecta cuando servidor no está disponible

El cliente ahora depende completamente del servidor para realizar operaciones, demostrando el funcionamiento de .NET Remoting en un escenario real.
