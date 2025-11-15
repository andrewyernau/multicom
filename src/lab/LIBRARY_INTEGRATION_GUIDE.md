# Guía de Integración: Librería .dll y Aplicación Calculadora

Esta guía documenta cómo se creó la librería CalculatorLib.dll y cómo se integró con la aplicación Lab2Calculadora.

## Parte 1: Creación de la Librería CalculatorLib

### 1.1 Estructura del Proyecto

Se creó un proyecto de **Biblioteca de clases** con dos versiones:

```
CalculatorLib/
├── Operaciones.cs              # Versión .NET Framework 4.7.2
├── CalculatorLib.csproj        # Proyecto .NET Framework
└── Net6/
    ├── Operaciones.cs          # Versión .NET 6.0
    └── CalculatorLib.csproj    # Proyecto .NET 6.0
```

### 1.2 Clase Operaciones

La clase `Operaciones` hereda de `MarshalByRefObject` y proporciona cuatro métodos:

```csharp
public class Operaciones : MarshalByRefObject
{
    public double Sumar(double a, double b)
    {
        return a + b;
    }
    
    public double Restar(double a, double b)
    {
        return a - b;
    }
    
    public double Multiplicar(double a, double b)
    {
        return a * b;
    }
    
    public double Dividir(double a, double b)
    {
        if (b == 0)
        {
            throw new DivideByZeroException("[AGENT] Cannot divide by zero.");
        }
        return a / b;
    }
}
```

### 1.3 Compilación de la Librería

**Opción A: Visual Studio**
1. Abrir `CalculatorLib.sln`
2. Menú: **Compilar → Compilar solución**
3. La DLL se genera en:
   - .NET Framework: `bin\Debug\CalculatorLib.dll`
   - .NET 6.0: `Net6\bin\Debug\net6.0\CalculatorLib.dll`

**Opción B: Línea de comandos**
```bash
# .NET Framework (requiere MSBuild)
msbuild CalculatorLib.csproj /p:Configuration=Debug

# .NET 6.0
cd Net6
dotnet build
```

## Parte 2: Modificación de Lab2Calculadora

### 2.1 Agregar Referencia a la DLL

**Método 1: Usando Visual Studio (Referencia directa a DLL)**

1. En el **Explorador de soluciones**, hacer clic derecho sobre el proyecto `Lab2Calculadora`
2. Seleccionar **Agregar → Referencia**
3. En el diálogo, hacer clic en **Examinar** (botón inferior)
4. Navegar hasta la carpeta de la DLL compilada:
   ```
   CalculatorLib\Net6\bin\Debug\net6.0\CalculatorLib.dll
   ```
5. Seleccionar el archivo `CalculatorLib.dll`
6. Hacer clic en **Aceptar**

**Método 2: Usando Referencia de Proyecto (Recomendado)**

Editar `Lab2Calculadora.csproj` y agregar:

```xml
<ItemGroup>
  <ProjectReference Include="..\CalculatorLib\Net6\CalculatorLib.csproj" />
</ItemGroup>
```

### 2.2 Importar el Namespace

Agregar al inicio de `Form1.cs`:

```csharp
using CalculatorLib;
```

### 2.3 Instanciar el Objeto Operaciones

Modificar la clase `Form1`:

```csharp
public partial class Form1 : Form
{
    private readonly Operaciones operaciones;

    public Form1()
    {
        InitializeComponent();
        operaciones = new Operaciones();
    }
    
    // ... resto del código
}
```

### 2.4 Usar los Métodos de la Librería

Modificar el método `PerformOperation` para usar la instancia de `operaciones`:

**ANTES (sin librería):**
```csharp
switch (operation)
{
    case '+':
        result = operand1 + operand2;
        break;
    case '-':
        result = operand1 - operand2;
        break;
    // ... etc
}
```

**DESPUÉS (con librería):**
```csharp
try
{
    switch (operation)
    {
        case '+':
            result = operaciones.Sumar(operand1, operand2);
            break;
        case '-':
            result = operaciones.Restar(operand1, operand2);
            break;
        case '*':
            result = operaciones.Multiplicar(operand1, operand2);
            break;
        case '/':
            result = operaciones.Dividir(operand1, operand2);
            break;
    }
}
catch (DivideByZeroException ex)
{
    ShowError(ex.Message);
}
```

## Parte 3: Compilación y Ejecución

### 3.1 Orden de Compilación

**Importante:** Compilar primero la librería, luego la aplicación.

```bash
# 1. Compilar la librería
cd CalculatorLib/Net6
dotnet build

# 2. Compilar la aplicación
cd ../../Lab2Calculadora
dotnet build
```

### 3.2 Ejecutar la Aplicación

```bash
cd Lab2Calculadora
dotnet run
```

O presionar **F5** en Visual Studio.

## Parte 4: Verificación de la Integración

### 4.1 Comprobar Referencias

En Visual Studio, expandir el nodo **Dependencias → Proyectos** en el Explorador de soluciones. Debería aparecer `CalculatorLib`.

### 4.2 Verificar Compilación

Al compilar, la salida debe mostrar:

```
Compilando CalculatorLib -> bin\Debug\net6.0\CalculatorLib.dll
Compilando Lab2Calculadora -> bin\Debug\net6.0-windows\Lab2Calculadora.exe
```

### 4.3 Probar Funcionalidad

1. Ejecutar la aplicación
2. Ingresar valores en los TextBox
3. Hacer clic en cada botón de operación
4. Verificar que los resultados se muestren correctamente en el ListBox

### 4.4 Probar Manejo de Errores

1. Intentar dividir entre cero
2. Verificar que se muestre el mensaje: `[AGENT] Cannot divide by zero.`

## Parte 5: Beneficios de Usar la Librería

### 5.1 Separación de Responsabilidades
- **UI (Form1.cs)**: Maneja interfaz y validación de entrada
- **Lógica (CalculatorLib)**: Contiene operaciones aritméticas

### 5.2 Reutilización
La librería puede ser usada por:
- Otros proyectos Windows Forms
- Aplicaciones de consola
- Servicios web
- Aplicaciones WPF

### 5.3 Mantenibilidad
- Cambios en lógica de operaciones no afectan la UI
- Fácil actualización de la librería
- Testing independiente

### 5.4 Distribución
- La DLL puede distribuirse de forma independiente
- Versionado separado de la aplicación principal

## Parte 6: Estructura Completa de Archivos

```
src/lab/
├── CalculatorLib/
│   ├── Operaciones.cs                      # Clase de operaciones (.NET Framework)
│   ├── CalculatorLib.csproj                # Proyecto .NET Framework 4.7.2
│   ├── CalculatorLib.sln                   # Solución
│   ├── README.md                           # Documentación de la librería
│   ├── Properties/
│   │   └── AssemblyInfo.cs
│   ├── Net6/
│   │   ├── Operaciones.cs                  # Clase de operaciones (.NET 6)
│   │   ├── CalculatorLib.csproj            # Proyecto .NET 6.0
│   │   └── bin/Debug/net6.0/
│   │       └── CalculatorLib.dll           # DLL compilada ⭐
│   └── bin/Debug/
│       └── CalculatorLib.dll               # DLL compilada (.NET Framework)
│
└── Lab2Calculadora/
    ├── Form1.cs                            # Lógica de UI (usa CalculatorLib)
    ├── Form1.Designer.cs                   # Diseño de UI
    ├── Program.cs                          # Punto de entrada
    ├── Lab2Calculadora.csproj              # Proyecto (con referencia a CalculatorLib)
    ├── Lab2Calculadora.sln                 # Solución
    └── README.md                           # Documentación de la aplicación
```

## Parte 7: Solución de Problemas

### Error: "No se encuentra el tipo o namespace 'CalculatorLib'"

**Solución:**
1. Verificar que la referencia esté agregada correctamente
2. Compilar primero CalculatorLib
3. Reconstruir Lab2Calculadora

### Error: "No se puede cargar el archivo o ensamblado 'CalculatorLib'"

**Solución:**
1. Verificar que la DLL esté en la carpeta de salida
2. Usar referencia de proyecto en lugar de referencia de DLL
3. Verificar que ambos proyectos usen versiones compatibles de .NET

### Error: "No se encuentra el método 'Sumar'"

**Solución:**
1. Verificar que la clase `Operaciones` sea pública
2. Verificar que los métodos sean públicos
3. Recompilar CalculatorLib

## Parte 8: Mejoras Futuras

### 8.1 Operaciones Adicionales
Agregar a `Operaciones`:
- Potencia
- Raíz cuadrada
- Módulo
- Porcentaje

### 8.2 Testing
Crear proyecto de pruebas unitarias para CalculatorLib:

```csharp
[TestClass]
public class OperacionesTests
{
    [TestMethod]
    public void Sumar_DeberiaRetornarSumaCorrecta()
    {
        var ops = new Operaciones();
        Assert.AreEqual(15, ops.Sumar(10, 5));
    }
}
```

### 8.3 Logging
Agregar registro de operaciones:

```csharp
public double Sumar(double a, double b)
{
    var result = a + b;
    Logger.Log($"Suma: {a} + {b} = {result}");
    return result;
}
```

## Conclusión

La integración de CalculatorLib con Lab2Calculadora demuestra:
- ✅ Creación de librería .dll
- ✅ Clase con herencia de MarshalByRefObject
- ✅ Implementación de métodos de operaciones
- ✅ Referencia de proyecto en aplicación Windows Forms
- ✅ Uso de directiva `using`
- ✅ Instanciación y uso del objeto `Operaciones`
- ✅ Manejo de excepciones desde la librería

Esta arquitectura modular facilita el mantenimiento, testing y reutilización del código.
