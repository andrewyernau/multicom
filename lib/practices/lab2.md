# LABORATORIO DE CONTENIDOS DIGITALES

## Práctica 2 — .NET Remoting. Aplicación: Calculadora

**Escuela Técnica Superior de Ingeniería de Telecomunicación**

**Profesores:**
- Antonio Javier García Sánchez
- Rubén Martínez Sandoval

---

## 0. Objetivos de la práctica

En esta práctica el alumno se familiarizará con los conceptos de .NET Remoting implementando una aplicación que haga uso de llamadas a métodos remotos. Estas llamadas seguirán el paradigma Request-Reply para proporcionar las funcionalidades típicas de una calculadora.

Se crearán dos proyectos:

- **Servidor:** realiza las operaciones matemáticas de la calculadora, exponiendo un objeto remoto llamable.
- **Cliente:** interfaz gráfica para la calculadora; instancia el objeto remoto proporcionado por el servidor y ejecuta las llamadas sobre él.

La práctica se divide en 5 etapas:

1. Generación de la aplicación calculadora que funcione localmente (100%).
2. Portar los métodos de cálculo (suma/resta/etc.) a una biblioteca (.dll).
3. Implementación del Servidor (exposición del objeto remoto).
4. Modificar el proyecto cliente para consumir el objeto remoto (usar Activator.GetObject).
5. Adaptación final utilizando utilidades de .NET Remoting.

---

## 1. Generación de la aplicación calculadora en local

La interfaz de usuario debe implementarse como un proyecto de tipo "Aplicación de Windows Form" y contener como mínimo:

- Dos `TextBox` para los valores a operar.
- Cuatro `Button` para las operaciones básicas: sumar, restar, multiplicar y dividir.
- Una `ListBox` para visualizar los resultados (historial de operaciones).
- Otros elementos opcionales que el alumno quiera añadir (mejoras de interfaz, utilidades, etc.).

Ilustración 1: ejemplo de interfaz gráfica (el alumno puede adaptarla a su gusto).

### Comportamiento esperado

- Al pulsar un botón de operación, la aplicación debe leer los valores de los `TextBox`, convertirlos a `double`, ejecutar la operación y añadir el resultado al `ListBox`.
- Si uno o ambos valores no son numéricos, debe mostrarse un aviso al usuario y no realizar la operación.

---

## 2. Generación de la librería .dll

Crear un nuevo proyecto de tipo **Biblioteca de clases (.NET Framework)** y definir una clase que represente el objeto `operaciones` con los métodos básicos:

```csharp
public class operaciones : MarshalByRefObject
{
    public double sumar(double a, double b)
    {
        // implementación
    }
    public double restar(double a, double b)
    {
        // implementación
    }
    public double multiplicar(double a, double b)
    {
        // implementación
    }
    public double dividir(double a, double b)
    {
        // implementación
    }
}
```

Rellenar los métodos con la lógica correspondiente. Al compilar la solución se generará la `.dll` en la carpeta `bin\Debug` del proyecto.

### Referenciar la librería desde el cliente

1. En el Explorador de soluciones del proyecto cliente, hacer clic derecho y seleccionar **Agregar -> Referencia**.
2. Pulsar **Examinar** y seleccionar la `.dll` generada (por ejemplo, `bin\Debug\tuProyecto.dll`).
3. Añadir la directiva `using` correspondiente (p. ej. `using calculadora;`) y crear una instancia de `operaciones` para llamar a los métodos.

---

## 3. Implementación del servidor (exponer servicio remoto)

Crear un proyecto de tipo **Aplicación de Consola** para alojar el código del servidor. Para usar los protocolos HTTP o TCP y las funcionalidades de remoting, agregar la referencia a `System.Runtime.Remoting.dll`.

También agregar como referencia la biblioteca con la clase `operaciones` creada en el apartado anterior.

.NET permite registrar servicios accesibles de forma remota. En este ejercicio se ofrecerá el servicio `calculadora` mediante un canal HTTP.

Consultar la documentación de Microsoft para ejemplos y la clase `RemotingConfiguration.RegisterWellKnownServiceType`:

https://docs.microsoft.com/eses/dotnet/api/system.runtime.remoting.remotingconfiguration.registerwellknownservicetype

> Nota: en la documentación de Microsoft el ejemplo define la clase en el propio código; en esta práctica la clase estará en la `.dll` externa y el servidor la cargará desde allí.

---

## 4. Modificación del proyecto cliente para usar el objeto remoto

Modificar el cliente para instanciar el objeto remoto (en vez de instanciar `new operaciones()`) usando `Activator.GetObject` apuntando al servicio HTTP publicado por el servidor.

Prueba de funcionamiento remoto:

1. Iniciar el servidor (proyecto del apartado 3).
2. Iniciar el cliente (interfaz gráfica).
3. Ejecutar operaciones (suma, resta, multiplicación, división) y comprobar que funcionan correctamente.
4. Detener el servidor y ejecutar una operación; comprobar que deja de funcionar (comportamiento esperado cuando el servidor no está disponible).

---

## Notas adicionales y recomendaciones

- Evitar bloquear el hilo de la UI: usar `Task` o `Thread` para operaciones de red o procesos que puedan tardar.
- Documentar claramente el formato del servicio y la URL donde se publica (por ejemplo `http://localhost:8080/Calculadora.rem`).
- Para la evaluación, comprobar la correcta gestión de errores cuando el servidor no está disponible.

---

## Entrega

Adjuntar la solución completa con los tres proyectos (cliente, servidor y biblioteca) y una pequeña nota que describa:

- Cómo ejecutar el servidor y el cliente.
- La URL/puerto elegido para el servicio remoto.
- Observaciones de implementación y posibles mejoras.
