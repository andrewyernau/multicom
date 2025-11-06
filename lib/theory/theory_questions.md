# Preguntas teóricas exigentes (nivel universitario) — Respuestas detalladas

Este documento contiene preguntas complejas sobre los contenidos del curso (ASP.NET, .NET Remoting, streaming A/V, QoS, DiffServ/IntServ, DASH, CDNs, técnicas de mitigación, seguridad y diseño de servicios). Cada pregunta viene seguida de una respuesta fundamentada y, cuando procede, de recomendaciones prácticas.

---

## Pregunta 1 — ASP.NET y falta de newstate en e-commerce

Un diseñador propone una web ASP.NET para comercio electrónico que no implementa ningún mecanismo de persistencia de estado del lado servidor (por ejemplo, no usa sesiones server-side, ni persistencia transaccional). Si la red se ralentiza bruscamente durante la fase de compra (pago con tarjeta), ¿qué problemas concretos podría experimentar el cliente? ¿Cómo se pueden solventar estos problemas a nivel de arquitectura y de implementación?

### Respuesta

- Problemas esperables:
  - Pérdida parcial de interacción: si el cliente envía formularios y la petición se retrasa, el usuario puede reenviar (doble envío) o abandonar la operación.
  - Condiciones de carrera y inconsistencias: sin coordinación server-side, múltiples peticiones concurrentes (por reintentos del cliente) pueden generar cobros duplicados o pedidos duplicados.
  - Falta de atomicidad: operaciones compuestas (validar tarjeta, reservar stock, crear pedido) pueden quedar a medias si no hay un mecanismo transaccional.
  - Exposición de datos sensibles en el cliente: si el cliente confía todo al navegador (client-side state), se incrementa el riesgo ante pérdidas o reenvíos.

- Soluciones arquitectónicas y de implementación:
  1. Diseñar la operación de compra como una transacción idempotente: usar un identificador único de operación (orderId / paymentId) generado en el cliente o en el servidor para detectar reenvíos y evitar duplicados.
  2. Usar persistencia transaccional server-side: envolver los pasos críticos (reservar inventario, crear pedido, confirmar cobro) en una transacción ACID o en una saga bien diseñada que permita compensaciones en caso de fallo.
  3. Implementar confirmaciones asincrónicas y colas: aceptar la solicitud y encolar el trabajo (persistir pedido en estado PENDIENTE), procesarlo en background y notificar al cliente por polling/webhook/email cuando esté confirmado. Esto evita timeouts HTTP largos.
  4. Manejar retires con backoff exponencial y idempotencia: si la red es inestable, el cliente debe reintentar con backoff y el servidor debe detectar reintentos duplicados usando el token único.
  5. Asegurar la integridad y confidencialidad de los datos de la tarjeta: no almacenar PAN en texto; usar proveedores de pago y tokenización (PCI compliance). Delegar la captura de datos sensibles a un PSP (Payment Service Provider) mediante redirección o iframes seguros.

- Resumen de buenas prácticas:
  - Idempotencia + token de operación.
  - Persistencia transaccional o diseño de saga para flujos largos.
  - Delegación de procesamiento de datos sensibles a servicios certificados.

---

## Pregunta 2 — .NET Remoting: entidad responsable y uso de DLLs

En un servicio implementado con .NET Remoting (modelo clásico), ¿qué componente se encarga de la comunicación cliente-servidor? ¿Qué métodos o APIs se invocan para publicar/consumir el servicio? ¿Qué conviene implementar dentro de la librería `.dll` compartida y, en el caso de servicios de streaming, es razonable esperar buenas prestaciones usando remoting + DLLs?

### Respuesta

- Entidad responsable de la comunicación:
  - El canal de remoting (por ejemplo `HttpChannel` o `TcpChannel`) y la infraestructura de remoting (`RemotingConfiguration`, `ChannelServices`) realizan el transporte y la serialización/transparencia remota.

- Métodos/APIs principales:
  - En el servidor: `RemotingConfiguration.RegisterWellKnownServiceType(Type, string, WellKnownObjectMode)` o configuración equivalente en el archivo `.config` para exponer tipos `WellKnown`.
  - En el cliente: `Activator.GetObject(Type, url)` para obtener un proxy remoto; llamadas sobre el proxy se trasladan al servidor a través del canal.
  - Registro de canales: `ChannelServices.RegisterChannel(new TcpChannel(port), false)` o `ChannelServices.RegisterChannel(new HttpChannel(port), false)`.

- Qué implementar dentro de la `.dll`:
  - La definición de las interfaces y/o clases (contrato) que usan cliente y servidor. Tipos compartidos: interfaces, DTOs, excepciones específicas, y las clases derivadas de `MarshalByRefObject` si se pretende pasar por referencia.
  - Evitar que la DLL contenga detalles de infraestructura (configuración del canal, puertos) — mantener contratos y lógica de negocio.

- Prestaciones para streaming:
  - .NET Remoting añade overhead por serialización y por el modelo RPC: no está optimizado para streaming de alto rendimiento y baja latencia. Para flujos A/V continuos es preferible usar protocolos orientados a datagramas (RTP/UDP) o soluciones específicas (sockets TCP binarios, WebRTC, mediaserver). Remoting puede ser aceptable para control, señalización o transporte de metadatos, pero no como canal principal de audio/video a escala.

- Recomendación práctica:
  - Usar remoting (o RPC) solo para control (comandos, negociaciones, metadatos). Implementar el transporte de media con canales especializados (RTP, WebSockets binarios, HTTP streaming chunked) y usar la DLL para los contratos y codecs o utilidades de serialización.

---

## Pregunta 3 — Diferenciar flujos streaming y no-streaming en la red

En una red que transporta tráfico mixto (HTTP file downloads, SSH, VoIP, videoconferencia), ¿es conveniente tratar los flujos de streaming en categorías distintas? Si la respuesta es afirmativa, qué técnicas aprendidas en el curso aplicarías para mejorar la experiencia del usuario en la sesión de streaming en tiempo real?

### Respuesta

- Sí: es recomendable distinguir flujos streaming (sensibles a retardo y jitter) de flujos tolerantes a retrasos (descargas, backups). Motivos:
  - Priorizar latencia sobre integridad para tiempo real.
  - Proporcionar aislamiento para que los grandes transfers no destruyan la experiencia interactiva.

- Técnicas aplicables (nivel red y borde):
  1. Clasificación y marcaje en borde (DiffServ): marcar paquetes de streaming con DSCP (p. ej. EF para voz, AF* para vídeo) para que el núcleo aplique PHBs adecuados.
 2. Policing y shaping en el borde: aplicar token/leaky bucket para limitar ráfagas y garantizar perfiles (Tspec). Esto previene que un flujo malicioso consuma toda la cola.
 3. Scheduling en routers/switches: usar Priority + WFQ (o SRR) para garantizar que el tráfico en tiempo real obtenga acceso preferente y que no haya inanición de otras clases.
 4. Control de admisión (IntServ/RSVP) cuando se requieren garantías absolutas en dominios controlados: reservar recursos extremo a extremo para sesiones críticas.
 5. En el extremo: buffering inicial y playout control para absorber jitter; combinar con FEC o interleaving para mitigar pérdidas sin retransmisiones.
 6. Para redes de acceso con variabilidad: usar Adaptative Bitrate (DASH/HLS) en entornos HTTP, o ajustar el codec (reducción de bitrate, cambio de perfil) en tiempo real.

- Elección práctica:
  - En Internet pública: DiffServ + adaptative bitrate en el extremo (DASH/ABR) suele ser la estrategia más escalable.
  - En redes administradas (operadoras o campus): combinar RSVP para sesiones críticas con shaping y WFQ en el core para máxima garantía.

---

## Pregunta 4 — RTSP vs HTTP/DASH: cuándo elegir cada uno

Explique las ventajas e inconvenientes de usar RTSP/RTP frente a HTTP/DASH para un servicio que ofrece tanto videoconferencia en tiempo real como vídeo bajo demanda. Considere latencia, escalabilidad, atravesado de NAT/Proxy y facilidad de integración con CDN.

### Respuesta

- RTSP/RTP (ideal para tiempo real):
  - Ventajas: baja latencia (si se usa UDP), diseño pensado para reproducción continua y control de sesión (PLAY/PAUSE/SEEK), timestamps y secuencia con RTP.
  - Inconvenientes: NAT/Firewall más problemático (UDP), menor compatibilidad con infra HTTP/CDN, escalabilidad limitada sin infra adicional (MCU/SFU para multiparty).

- HTTP/DASH (ideal para VOD y entornos escalables):
  - Ventajas: gran compatibilidad (HTTP/TCP), fácil caching por CDNs, atraviesa proxies y firewalls sin problemas, altamente escalable.
  - Inconvenientes: mayor latencia (segmentación y buffering), adaptación por segmentos que introduce switching delays; no es óptimo para conversaciones en tiempo real.

- Recomendación práctica:
  - Para videoconferencia: usar soluciones basadas en WebRTC (RTP/SRTP sobre UDP/DTLS) o RTP con soporte de NAT traversal (ICE, STUN, TURN) y arquitecturas SFU/MCU para escalado.
  - Para VOD: usar DASH/HLS servidos desde CDN; diseñar segmentos cortos (2–4 s) para reducir latencia de adaptación.
  - Híbrido: usar RTSP/WebRTC para la fase de interacción en tiempo real y DASH para entregar contenidos pregrabados al público masivo.

---

## Pregunta 5 — RTP/RTCP: cálculo y uso de jitter y RTCP para adaptación

Explique cómo RTCP ayuda a estimar la calidad del enlace (pérdidas, jitter) y cómo un cliente puede usar esa información para adaptar la reproducción (p. ej. cambiar bitrate o activar FEC). Incluya fórmulas o mecanismos relevantes.

### Respuesta

- Qué informa RTCP:
  - Paquetes de recepción contienen: fracción de paquetes perdidos, número del último paquete recibido, y la estimación de jitter.
  - Sender reports incluyen timestamps sincronizados (NTP/RTP) y contadores de paquetes/bytes enviados.

- Estimación de jitter (RFC 3550, resumen):
  - Se computa un estimador exponencial del jitter J mediante la diferencia inter-arrival D:

    J = J + (|D| - J)/16

  - D es la variación entre los intervalos de tiempo de llegada y los timestamps RTP esperados.

- Uso para adaptación:
  - Si la fracción de pérdida aumenta por encima de un umbral, el cliente puede pedir una representación de menor bitrate (si el flujo es adaptativo) o activar FEC/ARQ local.
  - Si el jitter crece, aumentar el tamaño del buffer de playout para evitar underruns (trade-off: más latencia).
  - RTCP puede usarse para señalizar congestion control: los emisores reducen bitrate si varios receptores reportan alta pérdida.

- Implementación práctica:
  - Para multicast/one-to-many, usar RTCP para recopilar una vista agregada de calidad y aplicar control de tasa colaborativo.
  - En un esquema DASH, combinar mediciones de throughput con RTCP para una decisión más robusta.

---

## Pregunta 6 — FEC e interleaving: cuándo y cómo usar

Compare Forward Error Correction (FEC) y interleaving como técnicas para aumentar la robustez frente a pérdidas en UDP/RTP. ¿Qué ventajas/limitaciones tiene cada una y cómo combinarlas con buffering para optimizar calidad/latencia?

### Respuesta

- FEC (Forward Error Correction):
  - Añade paquetes de paridad que permiten recuperar pérdidas sin retransmisiones.
  - Ventajas: recuperación rápida sin coste de RTT; útil en enlaces con pérdidas aleatorias.
  - Inconvenientes: overhead de ancho de banda adicional; menos eficiente ante pérdidas muy altas o ráfagas largas.

- Interleaving:
  - Reordena temporalmente los bytes/packets de forma que las pérdidas concentradas en ráfagas se distribuyen entre diferentes bloques de datos, permitiendo que la FEC o la decodificación recuperen la información.
  - Ventajas: reduce el impacto de ráfagas de pérdida.
  - Inconvenientes: introduce retardo adicional proporcional al grado de interleaving.

- Combinación con buffering:
  - Usar buffering para absorber el retardo añadido por interleaving y para esperar la llegada de paquetes FEC.
  - Trade-off: más interleaving → mayor resiliencia ante ráfagas → mayor latencia.

- Recomendación:
  - Para voz (latencia crítica): usar FEC ligera y small interleaving; priorizar baja latencia.
  - Para vídeo en tiempo real con mayor tolerancia (p. ej. streaming multicámara): usar interleaving moderado + FEC con ratio ajustable.

---

## Pregunta 7 — Leaky Bucket / Token Bucket: impacto en la latencia y dimensionamiento

Explique con fórmulas cómo los parámetros de un token-bucket (r = tasa sostenida, b = ráfaga) afectan el dimensionamiento de las colas y el retardo máximo que puede observar un paquete en un enlace con capacidad R. ¿Cómo se traduce esto en garantías de servicio para streaming?

### Respuesta

- Modelo básico:
  - En un token-bucket con tasa r y ráfaga b, la cantidad máxima de bytes permitida en un intervalo t es r·t + b.

- Retardo máximo para una ráfaga b si el enlace tiene capacidad R (R > r):
  - Si un burst de tamaño b llega, el tiempo para drenar la ráfaga en exceso sobre r es aproximadamente b / (R - r). Esto contribuye al retardo en cola.

- Implicaciones para streaming:
  - Para garantizar retardo limitado, fijar r próximo al bitrate objetivo y mantener b pequeño. Si b es grande, el retardo en cola puede crecer y perjudicar la latencia.
  - El dimensionamiento debe considerar la agregación de flujos: múltiples buckets agregados pueden producir ráfagas simultáneas y saturación.

---

## Pregunta 8 — DiffServ vs IntServ: ventajas, límites y uso híbrido

Discuta por qué DiffServ escala mejor que IntServ, y en qué escenarios IntServ/RSVP siguen siendo adecuados. Proponga una arquitectura híbrida que aproveche lo mejor de ambos modelos en una operadora de campus que debe soportar videoconferencia crítica y tráfico best-effort.

### Respuesta

- Escalabilidad:
  - IntServ requiere mantener estado por flujo en cada router y realizar admisión por flujo → no escala cuando hay millones de flujos.
  - DiffServ opera por clases agregadas (sin estado por flujo en el core), por lo que escala en el núcleo de la red.

- Escenarios para IntServ:
  - Entornos controlados y con pocos flujos críticos (p. ej. red corporativa de una institución con sesiones médicas remotas) donde las reservas extremo a extremo son asequibles.

- Arquitectura híbrida sugerida:
  - Borde: usar IntServ/RSVP para sesiones críticas (hospital, sala de telemedicina) y asignar recursos en el borde; marcar el tráfico reservado con DSCP.
  - Núcleo: aplicar DiffServ en el backbone (PHBs predefinidos) para eficiencia y escalabilidad.
  - Políticas: en el borde validar/condicionar tráfico (policing/shaping) y traducir reservas RSVP en marcaje DSCP para el core.

---

## Pregunta 9 — DASH: tamaño de segmento y algoritmo de adaptación

Explique el trade-off en la elección del tamaño de segmento en DASH (2 s vs 10 s) y describa al menos dos estrategias de adaptación (reactiva y buffer-aware). ¿Cuál elegiría para una red móvil con variabilidad alta y por qué?

### Respuesta

- Trade-off tamaño de segmento:
  - Segmentos cortos (2 s): menor latencia de adaptación y mejor experiencia cuando la red cambia rápido; mayor overhead por más requests y más posibilidades de fallo de petición.
  - Segmentos largos (10 s): menos overhead y peticiones, pero mayor latencia para cambiar de representación → peor en condiciones de variabilidad.

- Estrategias de adaptación:
  1. Reactiva (throughput-based): estima throughput observado en peticiones recientes y elige la representación que cabe con un margen. Rápida para reaccionar a caídas, puede fluctuaciones bruscas.
  2. Buffer-aware (baseado en buffer occupancy): prioriza mantener un nivel de buffer objetivo; si buffer es alto elige calidad superior, si bajo degrada. Estable y evita conmutaciones frecuentes.

- Recomendación para red móvil con alta variabilidad:
  - Usar una estrategia híbrida: throughput-based para reaccionar a caídas súbitas + buffer-aware para evitar rebufferings frecuentes. Además elegir segmentos cortos (2–4 s) para reducir latencia de adaptación.

---

## Pregunta 10 — CDNs: consistencia, invalidación y balanceo geográfico

Describa cómo una CDN maneja la coherencia y la invalidación de contenido (cache invalidation) para material multimedia que se actualiza con frecuencia (p. ej. assets de una web de e-commerce) y cuáles son los criterios para seleccionar el PoP (Point of Presence) que servirá un usuario.

### Respuesta

- Coherencia / invalidación:
  - TTL y encabezados HTTP (Cache-Control, Expires): control básico de validez.
  - Purge/Invalidation API: APIs que permiten a la aplicación purgar objetos específicos de la CDN.
  - Versionado de URLs (cache-busting): cambiar el nombre de recurso al actualizar (p. ej. app.js?v=123) para evitar invalidaciones costosas.
  - Invalidation basada en eventos: el servicio de origen puede notificar a la CDN (webhooks o APIs) para purgar objetos críticos.

- Selección de PoP:
  - Proximidad geográfica y latencia (latency-based routing).
  - Carga y capacidad actuales del PoP.
  - Políticas de negocio (coste, regulaciones de datos, georestrictions).
  - Consistencia de contenido entre PoPs y salud del caché.

---

## Ejercicios propuestos (autoevaluación)

1. Diseña un flujo de compra web idempotente: dibuja las etapas y crea un esquema de estados (PENDIENTE, AUTORIZADO, CONFIRMADO, COMPENSADO) y describe las transiciones ante fallos de red.
2. Simula (conceptualmente) una topología DiffServ + RSVP para un campus con 3 edificios: indica dónde aplicarías policing, shaping y reserva de recursos.
3. Para un escenario de streaming móvil con 3 niveles de calidad, escribe un pseudocódigo simple de un adaptador híbrido (throughput + buffer-aware).

---

## Referencias y lecturas recomendadas

- RFC 3550 (RTP/RTCP)
- RFC 2326 (RTSP)
- Estándares DASH (ISO/IEC 23009)
- Documentación Microsoft: .NET Remoting (histórico) y ASP.NET patterns

---

Archivo generado por el equipo docente: preguntas complejas y respuestas fundamentadas para uso en exámenes y prácticas de laboratorio. Si desea que exporte estas preguntas como archivo de examen separable o que genere una versión de sólo preguntas (sin respuestas) para evaluación, indíquelo y lo creo en otra ruta.
