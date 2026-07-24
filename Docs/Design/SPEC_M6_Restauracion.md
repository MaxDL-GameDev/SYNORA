# SYNORA — SPEC M6: Restauración (v1.0 — FINAL)

> Especificación **funcional** de M6. Describe QUÉ debe hacer el sistema en términos de
> comportamiento **observable**, no CÓMO. Sin arquitectura ni decisiones técnicas (eso es M6 F1).
> Lo no definido por el canon se marca "PENDIENTE DE DEFINICIÓN DEL DIRECTOR".

## Objetivo
Permitir que el jugador **restaure** a una criatura alterada que previamente **contuvo** en el
combate no letal de M5, resolviendo positivamente ese encuentro. La restauración cierra el ciclo
"contener → restablecer el equilibrio" que M5 dejó pendiente.

## Alcance
- La posibilidad de restaurar una **criatura alterada contenida** presente en la escena.
- La restauración como **acción deliberada del jugador**.
- El cambio observable: **la criatura deja de representar una amenaza para el jugador**.
- El feedback que comunica al jugador que la restauración ocurrió.

## No alcance
- Vinculación / bonding.
- Persistencia o guardado del resultado. *(PENDIENTE si M6 lo requiere.)*
- Recursos, coste, inventario, crafting, loot, experiencia, progresión numérica.
- Estado persistente del ecosistema / contadores del mundo.
- Re-corrupción de criaturas restauradas.
- Nuevas especies, diálogos, misiones, cinemáticas.
- Arte, animación y audio definitivos.
- Cualquier cambio al combate no letal o a la criatura ambiental existente.

## Motivación
M5 introdujo el combate no letal y dejó a la **criatura alterada contenida**, sin resolución. El
tono de SYNORA es de cuidado, no de exterminio (GDD_M5). M6 aporta el desenlace coherente: el
jugador **restablece el equilibrio** en lugar de eliminar la amenaza.

## Estado inicial
Existe en la escena una **criatura alterada contenida** durante el combate no letal de M5: presente,
**sin representar una amenaza activa** y actualmente **sin poder ser observada** por el jugador. El
jugador conserva su control normal fuera de combate.

## Estado final esperado
- La **criatura contenida deja de representar una amenaza** y la **restauración se completa correctamente**.
- El jugador percibe con claridad que la restauración se realizó.
- El resto del mundo y toda la experiencia de M2–M5 permanecen intactos.
- Estado observable de la criatura tras restaurar: **PENDIENTE DE DEFINICIÓN DEL DIRECTOR**
  (¿se percibe idéntica a la criatura ambiental?, ¿queda diferenciada como "restaurada"?,
  ¿puede volver a observarse?).
- Persistencia del resultado: **PENDIENTE DE DEFINICIÓN DEL DIRECTOR**.

## Actores
- **Jugador**: inicia y realiza la restauración.
- **Criatura alterada contenida**: sujeto de la restauración.
- **Mundo / ecosistema**: contexto donde ocurre (alcance del efecto: PENDIENTE).

## Casos de uso
1. El jugador restaura una **criatura alterada contenida** → la criatura deja de representar una amenaza.
2. El jugador intenta restaurar una criatura que **no** está contenida → no es posible.
3. El jugador intenta restaurar sin cumplir las condiciones establecidas → no ocurre.
4. El jugador intenta restaurar una criatura ya restaurada → no vuelve a ocurrir (sin efecto adicional).

## Flujo principal
1. El jugador contiene a la criatura alterada (combate M5) → queda contenida.
2. El jugador se aproxima a la criatura contenida.
3. El juego comunica que la restauración es posible.
4. El jugador realiza la acción de restaurar.
5. Se verifica que se cumplen las condiciones para restaurar.
6. El juego comunica el proceso de restauración al jugador (feedback).
7. **La restauración se completa correctamente**: la criatura deja de representar una amenaza para el jugador.
8. El jugador recupera su exploración normal.
- Si el proceso es instantáneo o gradual/interrumpible: **PENDIENTE DE DEFINICIÓN DEL DIRECTOR**.

## Reglas de gameplay
- La restauración es **no letal** (coherente con M5).
- Solo es posible sobre una **criatura contenida** previamente.
- Es una **acción deliberada del jugador**, nunca automática.
- Una criatura se restaura **una sola vez**.
- Coste de la restauración (recurso/tiempo/riesgo): **PENDIENTE DE DEFINICIÓN DEL DIRECTOR**.
- Posibilidad de fallar/interrumpirse: **PENDIENTE DE DEFINICIÓN DEL DIRECTOR**.

## Requisitos funcionales
- RF1: El sistema permite restaurar únicamente **criaturas alteradas contenidas**.
- RF2: La restauración es iniciada por el jugador de forma deliberada.
- RF3: Tras la restauración, **la criatura deja de representar una amenaza para el jugador**.
- RF4: La restauración es segura ante repetición (no se puede re-restaurar; sin efectos acumulados).
- RF5: La restauración no altera a las criaturas ambientales ni a la experiencia del jugador fuera de este encuentro.
- RF6: El jugador recibe feedback claro de que la restauración ocurrió.
- RF7: Restaurar **no** equivale a curar/recuperar la vitalidad de la criatura; de cara al diseño son cosas distintas.

## Requisitos no funcionales
- Claridad: el jugador debe entender cuándo puede restaurar y que lo logró.
- Coherencia de tono: la experiencia transmite alivio/cuidado, no violencia.
- Estabilidad: sin degradar la experiencia ni introducir regresiones en M2–M5.
- Verificabilidad: el comportamiento observable debe poder validarse de forma automatizada y manual.

## Restricciones
- Se mantiene el combate no letal de M5 sin cambios.
- No se introduce persistencia ni recursos salvo definición explícita del Director.
- No se modifica la criatura ambiental ni la derrota temporal del jugador.
- Se respeta el tono y el canon del Director; ninguna afirmación de lore se inventa aquí.

## Dependencias funcionales
- El jugador debe poder **iniciar la restauración** cuando se cumplan las condiciones establecidas.
- La restauración requiere que el encuentro de combate previo haya dejado a la **criatura contenida**.
- El cierre del alcance depende de las definiciones pendientes del Director.

## Riesgos
- Alcance creativo sin definir (mitigado con "PENDIENTE" + preguntas).
- Que la experiencia se perciba como "curar vitalidad" en lugar de "restablecer el equilibrio".
- Abuso/trivialización si no hay condiciones o coste (PENDIENTE).
- Expectativa de persistencia no cumplida si el Director la asume y M6 no la incluye.
- Contaminar la percepción de la criatura ambiental (que se sienta afectada por M6).

## Compatibilidad con M5
- El **desenlace de contención** de M5, que quedaba sin resolución, debe poder **resolverse
  positivamente** con la restauración. El combate, la contención y la derrota temporal del jugador
  se conservan sin cambios observables.

## Impacto esperado sobre el proyecto
- Nueva experiencia jugable (la acción de restaurar) y su feedback.
- Posible contenido de observación/feedback nuevo (texto/visual) — su contenido es canon del Director.
- Sin impacto en el combate, en la criatura ambiental ni en el control/derrota del jugador.

## Estrategia de pruebas
Verificar el comportamiento **observable**: solo se restaura una **criatura contenida**; no se
restaura una no contenida; la restauración es una sola vez; tras restaurar la criatura deja de
representar una amenaza; el mundo y la experiencia de M2–M5 quedan intactos; el feedback se percibe.
Parte será automatizable y parte requerirá validación manual en ejecución. El detalle se definirá
junto con el alcance cerrado.

## Criterios de aceptación
1. El jugador puede restaurar una **criatura alterada contenida** mediante una acción deliberada.
2. Tras restaurar, la criatura deja de representar una amenaza y la **restauración se completa correctamente**.
3. La restauración es única e idempotente.
4. La criatura ambiental, el combate y la experiencia del jugador permanecen intactos.
5. El jugador recibe feedback claro del resultado.
6. Todas las decisiones marcadas "PENDIENTE" fueron resueltas por el Director antes de cerrar M6 F0.
