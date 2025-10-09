# Laboratorio de Contenidos Digitales

Material docente y prácticas de la asignatura "Laboratorio de Contenidos Digitales" (Universidad Politécnica de Cartagena).

Este repositorio contiene notas de teoría y prácticas orientadas a tecnologías .NET (C#, .NET Remoting, ASP.NET, Windows Azure) y a técnicas de transmisión y procesamiento de contenidos digitales.

## Objetivo del repositorio

Proveer material formativo (apuntes, ejemplos y prácticas) para que el estudiante:

- Entienda y programe servicios y aplicaciones multimedia en .NET.
- Diseñe y evalúe arquitecturas de distribución de contenidos (streaming, notificaciones, colas).
- Experimente con implementaciones cliente/servidor (HTTP, TCP, UDP) y con despliegue en entornos cloud (Azure).

## Estructura del repositorio

- `lib/theory/` — Apuntes de teoría, cada unidad convertida a Markdown (`unit1.md`, `unit2.md`, ...). Algunos archivos fueron generados automáticamente a partir de fuentes TXT.
- `lib/practices/` — Guías de prácticas y ejercicios (por ejemplo `lab1.md`).
- `README.md` — Este fichero.

Si abres la carpeta `lib/theory` y `lib/practices` verás los documentos en formato Markdown listos para leer o publicar.

## Cómo usar el material

- Ver los archivos Markdown directamente en un editor (por ejemplo VS Code) o en GitHub para obtener la vista renderizada.
- Para ejecutar los ejemplos de C# incluidos:
	- Usa Visual Studio (recomendado) o `dotnet` CLI si los proyectos están disponibles.

## Contenidos principales (resumen)

- Introducción y fundamentos de C# y .NET
- Clases, objetos, herencia, polimorfismo y patrones de diseño para servicios distribuidos
- .NET Remoting: arquitectura, canales (HTTP/TCP), lifetime y versionado
- ASP.NET: WebForms, ciclo de vida, autenticación y gestión de estado
- Windows Azure: roles, SQL Azure, Azure Storage, AppFabric, WIF y notificaciones móviles
- Prácticas: conversión de imágenes, chat UDP, .NET Remoting, ASP.NET apps y más

## Buenas prácticas y recomendaciones

- Revisa los ficheros `.md` por unidad antes de ejecutar código generado automáticamente.
- Usa entornos virtuales / sandboxes para pruebas que impliquen servicios en red o Azure.
- Asegura manejo de excepciones y liberación de recursos (Dispose) en los ejemplos de C#.

## Contacto

Profesor: Antonio Javier García Sánchez
