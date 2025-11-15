# Lab2 - Calculadora Windows Forms

Aplicación de calculadora básica implementada como Windows Forms Application con las cuatro operaciones aritméticas fundamentales. **Utiliza la librería CalculatorLib.dll para las operaciones.**

## Componentes de la Interfaz

### Controles de Entrada
- **txtOperand1**: TextBox para ingresar el primer valor numérico
- **txtOperand2**: TextBox para ingresar el segundo valor numérico

### Botones de Operación
- **btnAdd** (+): Realiza suma
- **btnSubtract** (−): Realiza resta
- **btnMultiply** (×): Realiza multiplicación
- **btnDivide** (÷): Realiza división

### Visualización de Resultados
- **lstResults**: ListBox que muestra el historial de operaciones realizadas
- **btnClear**: Botón para limpiar resultados y campos de entrada

### Elementos Adicionales
- **Labels**: Etiquetas descriptivas para guiar al usuario
- **Validación de Entrada**: Verificación de valores numéricos válidos
- **Manejo de Errores**: Prevención de división entre cero
- **Formato de Resultados**: Muestra resultados con 2 decimales
- **Colores**: Cada botón de operación tiene un color distintivo

## Funcionalidades

### Operaciones Básicas
```
Suma:           a + b = resultado
Resta:          a - b = resultado
Multiplicación: a × b = resultado
División:       a ÷ b = resultado
```

### Validaciones
- Verifica que ambos campos contengan valores numéricos válidos
- Previene división entre cero (manejado por CalculatorLib)
- Muestra mensajes de error descriptivos con prefijo [AGENT]

### Características Adicionales
- Historial de operaciones en la ListBox
- Auto-scroll al último resultado
- Botón de limpieza para reiniciar la calculadora
- Selección automática de texto en caso de error
- Formato de salida con dos decimales

## ⚠️ Versión con Cliente Remoto

### Modificación para Usar Objeto Remoto

El proyecto ha sido modificado para usar un **objeto remoto** en lugar de un objeto local. Los cambios principales son:

#### 1. Instanciación Remota
```csharp
// En lugar de: operaciones = new Operaciones();
// Ahora usa:
operaciones = (Operaciones)Activator.GetObject(
    typeof(Operaciones),
    "http://localhost:8090/CalculatorService/Operaciones"
);
```

#### 2. Dependencia del Servidor
- ✅ **Con servidor activo**: Todas las operaciones funcionan normalmente
- ❌ **Sin servidor**: Muestra error de conexión

#### 3. Requisitos
- El servidor `CalculatorServer` debe estar ejecutándose
- Puerto 8090 debe estar disponible
- CalculatorLib.dll debe estar disponible

### Pruebas de Funcionamiento

**Prueba 1 - Servidor Activo**:
1. Iniciar `CalculatorServer.exe`
2. Iniciar `Lab2Calculadora.exe`
3. Realizar operaciones (Suma, Resta, Multiplicación, División)
4. ✅ Resultado: Operaciones funcionan correctamente, mostrando `[REMOTE]`

**Prueba 2 - Servidor Inactivo**:
1. Detener `CalculatorServer.exe`
2. Intentar realizar una operación
3. ❌ Resultado: Error "Server connection error"

## Integración con CalculatorLib

### Paso 1: Agregar Referencia
Se agregó una referencia al proyecto CalculatorLib en el archivo `.csproj`:

```xml
<ItemGroup>
  <ProjectReference Include="..\CalculatorLib\Net6\CalculatorLib.csproj" />
</ItemGroup>
```

**Alternativa usando DLL directa:**
1. En el Explorador de Soluciones, clic derecho sobre el proyecto
2. Seleccionar **Agregar → Referencia**
3. Clic en **Examinar**
4. Navegar a `CalculatorLib\Net6\bin\Debug\net6.0\CalculatorLib.dll`
5. Seleccionar el archivo y hacer clic en **Aceptar**

### Paso 2: Importar Namespace
```csharp
using CalculatorLib;
```

### Paso 3: Instanciar Objeto Operaciones
```csharp
private readonly Operaciones operaciones;

public Form1()
{
    InitializeComponent();
    operaciones = new Operaciones();
}
```

### Paso 4: Usar Métodos de la Librería
```csharp
// Suma
result = operaciones.Sumar(operand1, operand2);

// Resta
result = operaciones.Restar(operand1, operand2);

// Multiplicación
result = operaciones.Multiplicar(operand1, operand2);

// División (con manejo de excepción)
try
{
    result = operaciones.Dividir(operand1, operand2);
}
catch (DivideByZeroException ex)
{
    ShowError(ex.Message);
}
```

## Compilación y Ejecución

### Requisitos
- .NET 6.0 SDK o superior
- Windows (debido a Windows Forms)
- CalculatorLib compilada

### Compilar Ambos Proyectos
```bash
# Compilar la librería primero
cd ../CalculatorLib/Net6
dotnet build

# Compilar la calculadora
cd ../../Lab2Calculadora
dotnet build
```

### Ejecutar
```bash
dotnet run
```

O abrir la solución en Visual Studio y ejecutar con F5.

## Uso

1. Ingrese el primer valor en el campo "Primer Valor"
2. Ingrese el segundo valor en el campo "Segundo Valor"
3. Haga clic en el botón de la operación deseada (+, -, ×, ÷)
4. El resultado se mostrará en la lista de resultados
5. Use el botón "Limpiar" para resetear la calculadora

## Ejemplo de Uso

```
Entrada:
  Primer Valor: 10
  Segundo Valor: 5

Clic en "+": 10 + 5 = 15.00
Clic en "−": 10 − 5 = 5.00
Clic en "×": 10 × 5 = 50.00
Clic en "÷": 10 ÷ 5 = 2.00
```

## Arquitectura del Código

### Uso de CalculatorLib.dll
El proyecto hace uso de la librería externa `CalculatorLib.dll` que contiene la implementación de las operaciones aritméticas en la clase `Operaciones`.

**Beneficios de usar la librería:**
- Separación de responsabilidades (UI vs. lógica de negocio)
- Reutilización del código en otros proyectos
- Facilita testing unitario de operaciones
- Permite actualizar lógica sin modificar UI

### Form1.cs
Contiene la lógica de la interfaz:
- `operaciones`: Instancia de la clase `Operaciones` de CalculatorLib
- `PerformOperation(char)`: Ejecuta operaciones usando la librería CalculatorLib
- `TryParseOperands()`: Valida y parsea los valores de entrada
- `ShowError(string)`: Muestra mensajes de error al usuario
- Event handlers para botones: `BtnAdd_Click`, `BtnSubtract_Click`, etc.

### Form1.Designer.cs
Contiene la definición de la interfaz gráfica generada por el diseñador de Windows Forms.

## Convenciones Seguidas

- Nombres en `PascalCase` para métodos públicos y controles
- Nombres en `camelCase` para variables locales
- Documentación XML para métodos públicos
- Mensajes de error con prefijo [AGENT]
- Manejo estructurado de excepciones
- Uso de librería externa para operaciones

## Estructura del Proyecto

```
Lab2Calculadora/
├── Form1.cs                  # Lógica de la UI
├── Form1.Designer.cs         # Diseño de la UI
├── Program.cs                # Punto de entrada
├── Lab2Calculadora.csproj    # Archivo de proyecto (incluye referencia a CalculatorLib)
└── README.md                 # Esta documentación

Dependencias:
└── CalculatorLib/Net6/
    ├── Operaciones.cs        # Clase con métodos de operaciones
    └── CalculatorLib.dll     # Librería compilada
```
