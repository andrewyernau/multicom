# VideoStreaming Unit Tests

Este proyecto contiene los tests unitarios para el sistema de videoconferencia UDP Multicast siguiendo metodología TDD.

## Estado TDD: 🔴 RED Phase

Todos los tests están definidos pero las implementaciones aún no existen. Este es el estado inicial esperado en TDD.

## Estructura

- `Server/` - Tests para componentes del servidor
  - `JpegEncoderTests.cs` - Tests para codificación JPEG
- `Client/` - Tests para componentes del cliente
  - `MetricsCollectorTests.cs` - Tests para colección de métricas
- `Shared/` - Tests para componentes compartidos
  - `PacketHeaderTests.cs` - Tests para serialización de cabeceras

## Ejecutar Tests

```bash
cd tests/VideoStreaming.UnitTests
dotnet test
```

**Resultado esperado:** Todos los tests fallan con `NotImplementedException` (esto es correcto en fase RED).

## Siguiente Paso (GREEN Phase)

Implementar las clases reales para hacer que los tests pasen:
1. Implementar `PacketHeader.Serialize()` y `Deserialize()`
2. Implementar `MetricsCollector` con toda su lógica
3. Implementar `JpegFrameEncoder.Encode()`

## Cobertura Objetivo

- Métodos públicos: 100%
- Lógica de negocio: >= 90%
- Total: >= 80%
