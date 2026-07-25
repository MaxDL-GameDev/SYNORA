# SYNORA — SPEC M6: Restauración (v1.1 — FINAL)

> Especificación **funcional** de M6. Describe QUÉ debe hacer el sistema en términos de
> comportamiento **observable**, no CÓMO. Sin arquitectura ni decisiones técnicas (eso es M6 F1).
> Todas las decisiones de diseño fueron resueltas por el Director (D-01 a D-08) e incorporadas aquí.

## Objetivo
Permitir que el jugador **restaure** a una criatura alterada que previamente **contuvo** en el
combate no letal de M5, resolviendo positivamente ese encuentro. Restaurar significa **liberar a la
criatura del estado de alteración que rompió su equilibrio y la volvió amenazante**; tras hacerlo, la
criatura recupera su calma y su comportamiento natural. La restauración cierra el ciclo
"contener → restablecer el equilibrio" que M5 dejó pendiente.

## Alcance
- La posibilidad de restaurar una **criatura alterada contenida** presente en la escena.
- La restauración como **acción deliberada del jugador**, breve y perceptible.
- El cambio observable: **la criatura deja de representar una amenaza para el jugador** y vuelve a ser
  una presencia ambiental observable.
- Una **señal visual sutil, funcional y provisional** que identifica a la criatura como restaurada durante la escena.
- El feedback que comunica al jugador que la restauración comenzó, está ocurriendo y terminó correctamente.

## No alcance
- Vinculación / bonding.
- Persistencia o guardado del resultado.
- Recursos, coste, inventario, crafting, loot, experiencia, progresión numérica.
- Estado persistente o acumulativo del ecosistema / contadores del mundo.
- Recompensa material.
- Re-corrupción de criaturas restauradas.
- Definición del origen último de la alteración (queda como canon abierto del Director).
- Nuevas especies, diálogos, misiones, cinemáticas.
- Arte, animación y audio definitivos.
- Cualquier cambio al combate no letal o a la criatura ambiental existente.

## Motivación
M5 introdujo el combate no letal y dejó a la **criatura alterada contenida**, sin resolución. El
tono de SYNORA es de cuidado, no de exterminio (GDD_M5). M6 aporta el desenlace coherente: el
jugador **libera a la criatura de su alteración y restablece el equilibrio** en lugar de eliminar la amenaza.

## Estado inicial
Existe en la escena una **criatura alterada contenida** durante el combate no letal de M5: presente,
**sin representar una amenaza activa** y actualmente **sin poder ser observada** por el jugador. El
jugador conserva su control normal fuera de combate.

## Estado final esperado
- La **criatura contenida deja de representar una amenaza** y la **restauración se completa correctamente**.
- La criatura **vuelve a integrarse como una presencia ambiental pacífica** y **puede volver a ser observada**.
- Durante la escena, la criatura **conserva una señal visual sutil, funcional y provisional** que permite
  reconocer que fue restaurada; no constituye una transformación canónica permanente ni exige arte definitivo.
- El resultado **no persiste**: se conserva únicamente mientras la escena permanezca activa; recargar la
  escena o reiniciar el juego restablece el estado inicial.
- El jugador percibe con claridad que la restauración se realizó.
- El resto del mundo y toda la experiencia de M2–M5 permanecen intactos.

## Actores
- **Jugador**: inicia y realiza la restauración.
- **Criatura alterada contenida**: sujeto de la restauración.
- **Mundo / ecosistema**: contexto donde ocurre (el efecto es un cierre local del encuentro).

## Casos de uso
1. El jugador restaura una **criatura alterada contenida** dentro de las condiciones válidas → la criatura deja de representar una amenaza.
2. El jugador intenta restaurar una criatura que **no** está contenida → no es posible.
3. El jugador intenta restaurar sin estar suficientemente cerca o sin estar en condiciones de actuar → no ocurre.
4. El jugador intenta restaurar una criatura ya restaurada → no vuelve a ocurrir (sin efecto adicional).

## Flujo principal
1. El jugador contiene a la criatura alterada (combate M5) → queda contenida.
2. El jugador se aproxima a la criatura contenida.
3. El juego comunica que la restauración es posible.
4. El jugador realiza la acción de restaurar.
5. Se verifica que se cumplen las condiciones para restaurar (contenida, jugador en condiciones de actuar, suficientemente cerca).
6. Se desarrolla un **proceso breve y perceptible** (duración objetivo aproximada: **1,25 segundos**); una vez
   iniciado **no puede interrumpirse ni fallar**. El juego comunica que la restauración está ocurriendo.
7. **La restauración se completa correctamente**: la criatura deja de representar una amenaza y vuelve a ser
   una presencia ambiental observable, con la señal provisional de restaurada.
8. El jugador recupera su exploración normal.

## Reglas de gameplay
- La restauración es **no letal** (coherente con M5).
- Solo es posible sobre una **criatura contenida** previamente.
- Es una **acción deliberada del jugador**, nunca automática.
- Es un **proceso breve y perceptible** (~1,25 s): ni instantáneo, ni una canalización prolongada, ni un minijuego.
- Es **gratuita** y **segura**: no consume recursos.
- Solo puede iniciarse cuando la criatura está contenida, el jugador está en condiciones normales de actuar y está suficientemente cerca.
- Una vez iniciada, **no puede interrumpirse ni fallar**.
- Una criatura se restaura **una sola vez**.

## Requisitos funcionales
- RF1: El sistema permite restaurar únicamente **criaturas alteradas contenidas**.
- RF2: La restauración es iniciada por el jugador de forma deliberada.
- RF3: Tras la restauración, **la criatura deja de representar una amenaza para el jugador**.
- RF4: La restauración es segura ante repetición (no se puede re-restaurar; sin efectos acumulados).
- RF5: La restauración no altera a las criaturas ambientales ni a la experiencia del jugador fuera de este encuentro.
- RF6: El jugador recibe feedback claro de que la restauración comenzó, está ocurriendo y terminó correctamente.
- RF7: Restaurar **no** equivale a curar/recuperar la vitalidad de la criatura; de cara al diseño son cosas distintas.
- RF8: La restauración solo puede iniciarse cuando la criatura está contenida, el jugador está en condiciones normales de actuar y está suficientemente cerca.
- RF9: Es un proceso breve y perceptible (duración objetivo ~1,25 s) que, una vez iniciado, no puede interrumpirse ni fallar.
- RF10: Tras restaurar, la criatura vuelve a ser una presencia ambiental **observable** y muestra una **señal provisional** que la identifica como restaurada mientras la escena esté activa.
- RF11: El resultado **no persiste**: recargar la escena o reiniciar el juego restablece el estado inicial.
- RF12: La restauración es **gratuita** (no consume recursos) y **no otorga recompensa material ni progresión**.

## Requisitos no funcionales
- Claridad: el jugador debe entender cuándo puede restaurar y que lo logró.
- Coherencia de tono: la experiencia transmite alivio/cuidado, no violencia.
- Estabilidad: sin degradar la experiencia ni introducir regresiones en M2–M5.
- Verificabilidad: el comportamiento observable debe poder validarse de forma automatizada y manual.

## Restricciones
- Se mantiene el combate no letal de M5 sin cambios.
- No se introduce persistencia ni recursos.
- No se define el origen último de la alteración (canon abierto del Director).
- No se modifica la criatura ambiental ni la derrota temporal del jugador.
- Se respeta el tono y el canon del Director; ninguna afirmación de lore se inventa aquí.

## Dependencias funcionales
- El jugador debe poder **iniciar la restauración** cuando la criatura esté contenida, esté en condiciones de actuar y suficientemente cerca.
- La restauración requiere que el encuentro de combate previo haya dejado a la **criatura contenida**.

## Riesgos
- Que la experiencia se perciba como "curar vitalidad" en lugar de "liberar de la alteración / restablecer el equilibrio".
- Que el jugador espere que el resultado persista entre sesiones (M6 no persiste): mitigar con feedback claro.
- Que la señal provisional de "restaurada" se confunda con arte definitivo o con una transformación permanente.
- Contaminar la percepción de la criatura ambiental (que se sienta afectada por M6).

## Compatibilidad con M5
- El **desenlace de contención** de M5, que quedaba sin resolución, debe poder **resolverse
  positivamente** con la restauración. El combate, la contención y la derrota temporal del jugador
  se conservan sin cambios observables.

## Impacto esperado sobre el proyecto
- Nueva experiencia jugable (la acción de restaurar) y su feedback.
- Una señal visual provisional de "criatura restaurada" durante la escena.
- Sin impacto en el combate, en la criatura ambiental ni en el control/derrota del jugador.

## Estrategia de pruebas
Verificar el comportamiento **observable**: solo se restaura una **criatura contenida**; no se
restaura una no contenida ni fuera de las condiciones de cercanía/estado del jugador; la restauración
es una sola vez; una vez iniciada no se interrumpe ni falla; tras restaurar la criatura deja de
representar una amenaza y vuelve a ser observable con su señal provisional; recargar la escena
restablece el estado inicial (sin persistencia); no otorga recompensa material; el mundo y la
experiencia de M2–M5 quedan intactos; el feedback de inicio/desarrollo/fin se percibe. Parte será
automatizable y parte requerirá validación manual en ejecución.

## Criterios de aceptación
1. El jugador puede restaurar una **criatura alterada contenida** mediante una acción deliberada, breve y perceptible.
2. Tras restaurar, la criatura deja de representar una amenaza y la **restauración se completa correctamente**.
3. La restauración es única e idempotente.
4. La restauración es gratuita, segura, no interrumpible y no puede fallar.
5. La criatura restaurada vuelve a ser una presencia ambiental **observable**, con una **señal provisional** que la identifica como restaurada durante la escena.
6. El resultado **no persiste**: recargar la escena o reiniciar el juego restablece el estado inicial.
7. La criatura ambiental, el combate y la experiencia del jugador permanecen intactos.
8. El jugador recibe feedback claro del inicio, el desarrollo y el fin de la restauración.
