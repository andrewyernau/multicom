# CalculatorLib - Biblioteca de Operaciones

Biblioteca de clases (.NET Framework) que implementa operaciones aritméticas básicas. Esta librería se compila como un archivo .dll que puede ser referenciado por otros proyectos.

## Clase: Operaciones

La clase `Operaciones` hereda de `MarshalByRefObject` para permitir capacidades de remoting y proporciona cuatro métodos para operaciones aritméticas básicas.

### Métodos Implementados

#### Sumar
```csharp
public double Sumar(double a, double b)
```
Suma dos números.
- **Parámetros:**
  - `a`: Primer operando
  - `b`: Segundo operando
- **Retorna:** La suma de a y b

#### Restar
```csharp
public double Restar(double a, double b)
```
Resta el segundo número del primero.
- **Parámetros:**
  - `a`: Minuendo
  - `b`: Sustraendo
- **Retorna:** La diferencia de a y b

#### Multiplicar
```csharp
public double Multiplicar(double a, double b)
```
Multiplica dos números.
- **Parámetros:**
  - `a`: Primer factor
  - `b`: Segundo factor
- **Retorna:** El producto de a y b

#### Dividir
```csharp
public double Dividir(double a, double b)
```
Divide el primer número por el segundo.
- **Parámetros:**
  - `a`: Dividendo
  - `b`: Divisor
- **Retorna:** El cociente de a y b
- **Excepciones:** Lanza `DivideByZeroException` si b es cero

## Compilación

### Usando Visual Studio
1. Abrir la solución `CalculatorLib.sln`
2. Menú: **Compilar → Compilar solución**
3. El archivo .dll se generará en `bin\Debug\CalculatorLib.dll` o `bin\Release\CalculatorLib.dll`

### Usando MSBuild (línea de comandos)
```bash
msbuild CalculatorLib.csproj /p:Configuration=Debug
```

## Uso en Otros Proyectos

### Paso 1: Agregar Referencia
1. En el proyecto que desea usar la librería, hacer clic derecho en el **Explorador de Soluciones**
2. Seleccionar **Agregar → Referencia**
3. Hacer clic en **Examinar**
4. Navegar a la carpeta `bin\Debug` de CalculatorLib
5. Seleccionar `CalculatorLib.dll`
6. Hacer clic en **Aceptar**

### Paso 2: Agregar Directiva Using
```csharp
using CalculatorLib;
```

### Paso 3: Instanciar y Usar
```csharp
// Crear instancia de la clase Operaciones
Operaciones calc = new Operaciones();

// Usar los métodos
double suma = calc.Sumar(10, 5);           // 15
double resta = calc.Restar(10, 5);         // 5
double multiplicacion = calc.Multiplicar(10, 5);  // 50
double division = calc.Dividir(10, 5);     // 2
```

## Ejemplo Completo

```csharp
using System;
using CalculatorLib;

namespace MiAplicacion
{
    class Program
    {
        static void Main(string[] args)
        {
            Operaciones operaciones = new Operaciones();
            
            double a = 20;
            double b = 4;
            
            Console.WriteLine($"{a} + {b} = {operaciones.Sumar(a, b)}");
            Console.WriteLine($"{a} - {b} = {operaciones.Restar(a, b)}");
            Console.WriteLine($"{a} * {b} = {operaciones.Multiplicar(a, b)}");
            Console.WriteLine($"{a} / {b} = {operaciones.Dividir(a, b)}");
            
            // Manejo de división por cero
            try
            {
                operaciones.Dividir(a, 0);
            }
            catch (DivideByZeroException ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
```

## Características Técnicas

- **Target Framework:** .NET Framework 4.7.2
- **Tipo de Proyecto:** Biblioteca de clases
- **Herencia:** MarshalByRefObject (para soporte de remoting)
- **Documentación:** XML documentation comments
- **Convenciones:** PascalCase para nombres públicos

## Estructura del Proyecto

```
CalculatorLib/
├── Operaciones.cs           # Clase con métodos de operaciones
├── Properties/
│   └── AssemblyInfo.cs     # Metadatos del ensamblado
├── CalculatorLib.csproj    # Archivo de proyecto
├── CalculatorLib.sln       # Archivo de solución
└── bin/
    └── Debug/
        └── CalculatorLib.dll  # Librería compilada
```

## Integración con Lab2Calculadora

Esta librería está diseñada para ser utilizada por el proyecto Lab2Calculadora, reemplazando la lógica de operaciones directa con llamadas a los métodos de la clase `Operaciones`.

## Notas

- La clase hereda de `MarshalByRefObject` para permitir el uso en escenarios de remoting y comunicación entre dominios de aplicación
- Todos los métodos están documentados con XML comments para IntelliSense
- La división por cero lanza una excepción con mensaje prefijado con [AGENT] según convenciones del proyecto
