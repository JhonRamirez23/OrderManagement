# Order Management API

API REST para gestionar pedidos, validar inventario, procesar pagos simulados y notificar el resultado de la operación.

La solución está implementada como una prueba técnica en .NET 10 y utiliza persistencia en memoria.

## Tecnologías

- .NET 10.
- C# y ASP.NET Core Web API.
- Clean Architecture.
- Inyección de dependencias nativa de .NET.
- Persistencia en memoria con colecciones thread-safe.
- xUnit.
- Swagger/OpenAPI en desarrollo.

No se utilizan bases de datos reales, Entity Framework, Redis, RabbitMQ, Kafka, microservicios ni proveedores externos de pago o notificación.

## Arquitectura

```text
src/
  OrderManagement.Domain/
  OrderManagement.Application/
  OrderManagement.Infrastructure/
  OrderManagement.Api/

tests/
  OrderManagement.Tests/
```

- `OrderManagement.Domain`: entidades, estados, invariantes y excepciones de dominio.
- `OrderManagement.Application`: DTOs, interfaces, validaciones y casos de uso.
- `OrderManagement.Infrastructure`: repositorios en memoria, inventario, Observer, pagos y notificaciones simuladas.
- `OrderManagement.Api`: controladores REST, middleware de errores, Swagger y configuración de dependencias.
- `OrderManagement.Tests`: pruebas unitarias, de infraestructura y de integración HTTP.

Las dependencias apuntan hacia Domain y Application. Application no depende de Infrastructure ni de Api, e Infrastructure no depende de Api.

## Requisitos

- .NET SDK 10.
- PowerShell, Bash o una terminal compatible.

## Ejecución

Desde la raíz de la solución:

```text
dotnet restore
dotnet build
dotnet test
dotnet run --project src/OrderManagement.Api
```

Swagger está disponible en desarrollo en `https://localhost:7137/swagger/index.html`. La persistencia es volátil: los pedidos, reservas, productos y notificaciones se pierden al detener la aplicación. Esta persistencia en memoria no equivale a una transacción distribuida ni ACID.

## Endpoints REST

### POST /api/orders

Crea un pedido. El cliente solo envía identificadores y cantidades; el precio siempre se obtiene del catálogo.

Solicitud:

```json
{
  "customerEmail": "cliente@ejemplo.com",
  "items": [
    {
      "productId": "11111111-1111-1111-1111-111111111111",
      "quantity": 2
    }
  ]
}
```

Respuesta `201 Created`:

```json
{
  "id": "00000000-0000-0000-0000-000000000001",
  "customerEmail": "cliente@ejemplo.com",
  "items": [
    {
      "productId": "11111111-1111-1111-1111-111111111111",
      "productName": "Teclado",
      "unitPrice": 25.50,
      "quantity": 2,
      "subtotal": 51.00
    }
  ],
  "totalAmount": 51.00,
  "status": "Confirmed",
  "createdAt": "2026-01-01T12:00:00+00:00"
}
```

### GET /api/orders/{id}

Consulta un pedido existente. Devuelve `200 OK` con el mismo formato de pedido. Si el identificador no existe devuelve `404 Not Found` con `application/problem+json`.

### GET /api/products

Devuelve `200 OK` con el catálogo e inventario actual:

```json
[
  {
    "id": "11111111-1111-1111-1111-111111111111",
    "name": "Keyboard",
    "price": 25.50,
    "stock": 10
  }
]
```

### Códigos HTTP

| Código | Significado |
|---:|---|
| 201 | Pedido registrado correctamente. |
| 200 | Consulta exitosa. |
| 400 | Datos inválidos o transición no permitida. |
| 404 | Producto o pedido inexistente. |
| 409 | Inventario insuficiente. |
| 503 | Resultado técnico desconocido del proveedor de pagos. |
| 500 | Error inesperado sin detalles internos. |

## Productos iniciales

| Id | Nombre | Precio | Stock inicial |
|---|---|---:|---:|
| `11111111-1111-1111-1111-111111111111` | Keyboard | 25.50 | 10 |
| `22222222-2222-2222-2222-222222222222` | Mouse | 12.75 | 20 |
| `33333333-3333-3333-3333-333333333333` | Monitor | 199.99 | 5 |

## Flujo de pedidos

1. Se validan el correo, los productos y las cantidades.
2. Se consultan los productos y los precios vigentes del catálogo.
3. Se reserva todo el inventario de forma atómica.
4. Se registra el pedido como `PendingPayment`.
5. Se procesa el pago sin mantener el bloqueo de inventario.
6. Un pago aprobado confirma el pedido.
7. Un pago rechazado libera la reserva y marca el pedido como `PaymentFailed`.
8. Un error técnico deja el pedido pendiente y no libera automáticamente el inventario, porque el resultado del cobro es desconocido.
9. La notificación se intenta sin provocar rollback si falla.

## Pagos y notificaciones

`FakePaymentGateway` permite simular de forma determinista `Approved`, `Rejected` y `TechnicalError`. No utiliza datos bancarios ni conexiones externas.

`FakeNotificationService` registra en memoria el correo, el identificador del pedido y el estado. Un fallo de notificación no revierte un pago aprobado.

## Inventario, Observer y rollback

- `SemaphoreSlim` protege la sección crítica de reserva y liberación dentro de una instancia.
- La reserva verifica todos los productos y cantidades antes de descontar cualquiera de ellos.
- Una reserva tiene un identificador único y su liberación es idempotente.
- `IInventorySubject` publica cambios de reserva y liberación después de completar la operación.
- `IInventoryObserver` no modifica reglas de negocio ni controla la concurrencia.
- El rollback restaura las cantidades reservadas y evita compensaciones duplicadas.
- La persistencia y el rollback son en memoria; no representan una transacción distribuida o ACID.
- La garantía de concurrencia aplica a una única instancia de la aplicación.

## Configuración de dependencias

Los repositorios de productos, pedidos e inventario se registran como singleton para compartir el estado volátil de la instancia. Los casos de uso se registran como transient y los proveedores simulados y observadores como singleton.

## Pruebas automatizadas

Las pruebas se ejecutan con xUnit mediante `dotnet test`. La suite cubre:

- Reglas de dominio, cálculo de subtotales y totales y transiciones de estado.
- Validación de Application y uso del precio del catálogo.
- Pagos aprobados, rechazados y con resultado técnico.
- Liberación de inventario tras pagos rechazados.
- Conservación de la reserva ante incertidumbre técnica del pago.
- Fallos de notificación sin rollback de pagos aprobados.
- Reservas atómicas con varios productos.
- Liberación idempotente de una misma reserva.
- Notificaciones del Observer.
- Consultas de pedidos existentes e inexistentes.
- Endpoints HTTP mediante `WebApplicationFactory`.

### Prueba crítica de concurrencia

Se crea un producto con una unidad disponible y se ejecutan múltiples reservas simultáneas mediante `Task.WhenAll`, sin `Thread.Sleep`. El resultado esperado es una sola reserva exitosa, las demás rechazadas, stock final igual a cero y ningún inventario negativo.

Para ejecutar todas las pruebas:

```text
dotnet test
```

## Limitaciones y decisiones técnicas

- La persistencia es volátil y local a la instancia.
- No existe autenticación porque está fuera del alcance.
- Los pagos y notificaciones son simulaciones deterministas.
- No se implementan transacciones SQL, sagas, CQRS ni Event Sourcing.
- El resultado técnico desconocido del pago no se compensa automáticamente para evitar liberar inventario que podría corresponder a un cobro real.
- Los errores internos se registran en el middleware, pero las respuestas no exponen stack traces.

## Estado de validación

La solución se valida con restore, build, test y ejecución local de la API. Los criterios de aceptación se comprueban mediante pruebas de dominio, Application, Infrastructure y HTTP.
