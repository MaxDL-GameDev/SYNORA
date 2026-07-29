# SYNORA — GDD M8: Pulido, QA y Playtest (v1.0 — Aprobado / Congelado)

> Documento de diseño del hito **M8 — Pulido / QA / Playtest**, el **cierre del prototipo**.
> Estado: **Aprobado / Congelado**.
> No modifica la Biblia, CANON-001, DEC-001 ni el GDD Prototipo histórico. Autoridad creativa
> y de canon: el Director.
>
> **Base congelada:** el alcance de entrega de M8 está fijado por **DEC-001** (aprobado y
> congelado, commit `dfdf200`). M8 opera sobre el **slice consolidado M1–M7**, no reconstruye
> los Segmentos A–C originales ni introduce contenido, persistencia ni mecánica nueva.

## Constancia de aprobación

- **Aprobado por:** Director del proyecto.
- **Fecha de aprobación:** 2026-07-29.
- **Base documental:** DEC-001.
- **Alcance:** integración final, pulido, QA y playtest del slice consolidado M1–M7.
- **Efecto:** la SPEC de M8 podrá definir fases y ejecución técnica sin ampliar el alcance.
- **No modifica** la Biblia, CANON-001, DEC-001 ni el GDD Prototipo histórico.

---

## 1. Propósito del milestone

M8 **cierra el prototipo** mediante **integración final, estabilización y validación externa**
del slice consolidado M1–M7. No aporta mecánicas, contenido narrativo ni sistemas nuevos: toma
lo ya construido (movimiento, interacción, criatura ambiental, observación, combate no letal con
contención, restauración, vinculación y compañero) y lo lleva a una **build jugable de principio
a fin por terceros, sin bloqueadores conocidos**, con su resultado de prueba registrado.

Fuente del propósito: GDD Prototipo §34 (*"M8 — Pulido/pruebas: Build jugable por terceros y
corrección de bloqueadores"*), §36 (plan de pruebas), y DEC-001 (§6 Impacto: *"M8 = Pulido / QA /
Playtest del slice consolidado"*).

**M8 NO es** un milestone de contenido, de persistencia, de progresión ni de expansión narrativa
(ver §5 y §13).

## 2. Resultado esperado

Una **build del prototipo** que sea:

- **jugable de inicio a fin por un tercero**, sin intervención del desarrollador;
- **sin bloqueadores conocidos** (ver §6);
- con el **flujo principal comprensible** sin instrucciones verbales;
- con los **sistemas M1–M7 integrados** y funcionando en secuencia;
- con **presentación suficientemente clara** para un playtest (prompts, feedback de combate,
  ficha de vínculo, señal de ECO provisional legibles);
- con los **resultados del playtest registrados** y clasificados.

## 3. Flujo bajo prueba

El flujo efectivo que M8 debe integrar y validar es el **congelado por DEC-001** (§4 de DEC-001):

```
Despertar simplificado
   → Exploración y observación
   → Encuentro / combate no letal
   → Contención
   → Restauración
   → Vinculación
   → Resultado: compañero
```

Aclaraciones (coherentes con DEC-001 y CANON-001):

- El **onboarding previo** (despertar simplificado, exploración y observación) **no sustituye ni
  modifica CANON-001**: es la entrada al slice.
- Dentro del flujo, el **arco de resolución de la criatura preserva la progresión conceptual**:
  **encuentro/observación → contención → restauración → vinculación → compañero**.
- **Compañero es el resultado estable** del vínculo (Segmento F + CANON-001), **no un Segmento G**.

## 4. Alcance de M8

M8 incluye **únicamente trabajo de cierre** sobre el slice consolidado. Esta lista es de **diseño**;
**no** se convierte automáticamente en tareas técnicas (eso corresponde a la SPEC y a las fases):

- integración final de las escenas y del **flujo consolidado definido por DEC-001**;
- validación del **punto de entrada** (`GameBootstrap` → primera escena) y de **todas las
  transiciones de área requeridas por el flujo aprobado, incluyendo el retorno únicamente donde
  esté diseñado y soportado**;
- revisión de **bloqueadores y softlocks** a lo largo del recorrido;
- revisión de la **interacción contextual** (prompts, examinar) en las tres escenas;
- revisión de los **estados de criatura** (ambiental: Idle/Patrol/Alert; alterada: combate/contención);
- revisión del **combate no letal y la contención** (Estabilidad, pulso, reinicio, `Subdued`);
- revisión de la **restauración** (interacción, estado `Restored`);
- revisión de la **vinculación y el compañero** (`Bonding → Bonded`, seguimiento, ficha, ECO);
- revisión del **estado de sesión** (`verak_vinculado` como bandera de sesión, no persistente);
- **claridad básica de UI y feedback** suficiente para el playtest;
- **estabilidad** de cámara, movimiento y colisiones;
- **consistencia de los placeholders** imprescindibles para el playtest;
- **preparación de la build**;
- **ejecución del playtest** externo;
- **registro y clasificación de incidencias**;
- **corrección de todos los bloqueadores y defectos críticos confirmados**.

## 5. Fuera de alcance

Queda **expresamente fuera de M8** (contenido diferido y sistemas post-prototipo, según DEC-001 §5
y la Biblia §57–58):

- celda de energía;
- gate de salida por energía;
- señal ECO inicial (al despertar);
- Verak atrapado;
- liberación y huida dirigida;
- pistas ambientales originales (anillos apagados, huellas, panel con patrón);
- acople pista → combate;
- persistencia y guardado;
- progresión;
- santuarios;
- sistemas de campaña;
- nuevas criaturas;
- nuevas habilidades;
- nuevas mecánicas;
- reconstrucción narrativa de los Segmentos A–C;
- arte/audio definitivo, **salvo correcciones mínimas imprescindibles para la legibilidad** del playtest;
- refactors generales **no** requeridos por un bloqueador.

## 6. Definición de bloqueador

Un **bloqueador de M8** es un problema que **impide, rompe o invalida** alguna de estas condiciones
obligatorias del recorrido:

- iniciar la build;
- avanzar entre escenas;
- completar el flujo de principio a fin;
- activar una mecánica obligatoria (interacción, combate, contención, restauración, vinculación);
- recuperar el control del jugador cuando corresponde;
- comprender una acción obligatoria sin instrucciones externas;
- finalizar el slice (llegar a "compañero");
- obtener resultados fiables del playtest.

**Clasificación de incidencias** (funcional, sin sistema técnico complejo de severidades):

| Nivel | Definición |
|---|---|
| **Bloqueador** | Impide iniciar, avanzar o completar el flujo, o invalida el playtest. **Debe corregirse en M8.** |
| **Crítico** | No bloquea el recorrido, pero rompe una mecánica obligatoria o el estado esperado (p. ej. el compañero no queda como debe). **Debe corregirse en M8.** |
| **Mayor** | Afecta claridad o experiencia de forma seria pero el flujo se completa. Corrección **evaluada**; puede diferirse con decisión explícita. |
| **Menor** | Defecto cosmético o de pulido que no afecta la comprensión ni el flujo. **Diferible.** |
| **Observación de playtest** | Comentario, duda o fricción reportada por un tester; **insumo de diseño**, no necesariamente un defecto. |

## 7. Principios de corrección

Toda corrección en M8 se rige por:

- **corregir la causa mínima**, no el síntoma ni "de paso" otra cosa;
- **no ampliar el alcance** ni añadir features como solución;
- **preservar la arquitectura congelada** (ver *Restricciones arquitectónicas*);
- **respetar el ownership** de cada sistema;
- **la presentación no muta estado** (de sesión, de brain ni de gameplay);
- **no introducir AnimationEvents**;
- **no mover responsabilidades** de `CreatureBrain` (transiciones), `CreatureMovement` (movimiento),
  `SpriteFlash` (color) ni otros owners definidos;
- cualquier **excepción arquitectónica requiere decisión previa del Director** (no se resuelve dentro
  de una corrección).

## 8. Playtest externo

Basado en GDD Prototipo §36:

- **Perfil del participante:** persona **externa** al desarrollo, sin conocimiento previo del proyecto;
  no se requiere experiencia específica en juegos. **Objetivo documental (GDD Prototipo §36): cinco participantes.**
- **Condiciones de prueba:** juega **sin instrucciones verbales** del desarrollador; la build debe
  ejecutarse por sí sola desde el inicio.
- **Información previa entregada:** sólo la mínima para abrir la build (cómo ejecutarla y los controles
  básicos si el juego no los comunica); **no** se explica el flujo ni las mecánicas.
- **Observaciones a registrar:** tiempo total de finalización (objetivo 10–15 min; máximo aceptable
  inicial 20); punto y causa de abandono si ocurre; comprensión de acciones obligatorias; número de
  reinicios de combate; fricciones y dudas espontáneas; si el cierre ("compañero") se entiende.
- **Preguntas posteriores (cualitativas):** ¿entendió qué debía hacer?, ¿en qué momento se trabó o
  dudó?, ¿el combate y la resolución se sintieron comprensibles?, ¿el vínculo se leyó como resultado
  del cuidado y no como recompensa?, ¿le interesaría seguir jugando?
- **Datos cualitativos esperados:** claridad del onboarding, legibilidad del feedback, comprensión del
  arco de resolución, sensación de cierre.
- **Criterio para repetir la prueba:** si se corrige un bloqueador o crítico que altera el recorrido,
  **o una corrección mayor de comprensión que cambie sustancialmente la experiencia evaluada**, la
  prueba de ese tramo se **repite** con participantes que no lo hayan visto.
- **Privacidad:** no se recogen datos personales innecesarios. **No** se diseñan herramientas de
  telemetría en M8 (registro manual/observacional).

## 9. Criterios de aceptación de M8

M8 se considera cumplido cuando, como mínimo:

- la **build es ejecutable por un tercero**;
- el **recorrido completo** se realiza **sin intervención del desarrollador**;
- hay **cero bloqueadores abiertos**;
- hay **cero defectos críticos abiertos**;
- **cero errores que invaliden el resultado del playtest**;
- el **flujo M1–M7** se completa de inicio a fin;
- **CANON-001 se preserva**: la criatura restaurada atraviesa la vinculación y se convierte en
  compañero, respetando la progresión congelada *encuentro/observación → contención → restauración
  → vinculación → compañero* (criatura única, sin colección ni intercambio). El **vínculo
  voluntario** y la **ausencia de captura** provienen de la **Biblia §24** (glosario: *"Vinculación:
  Relación voluntaria entre criatura y Restaurador; no implica propiedad"*), conservados por CANON-001;
- el **estado de sesión es correcto**: `verak_vinculado` se marca al llegar a `Bonded` y **no
  persiste**; el ownership del flag lo conserva `CreatureBondSessionCoordinator` (la presentación
  **no** escribe el flag);
- el **cierre es comprensible** (se entiende que se obtuvo un compañero);
- las **incidencias quedan registradas** y clasificadas;
- el **resultado del playtest queda documentado**;
- existe una **decisión explícita sobre los defectos mayores y menores pendientes** (diferir o
  corregir). Los **bloqueadores y críticos no pueden diferirse**: deben estar corregidos (ver §6).

> Nota: el criterio **no** es "cero bugs". Es un slice **jugable, completable y validado**, con
> **cero bloqueadores y cero críticos abiertos**, y las incidencias **mayores/menores** documentadas
> y decididas.

## 10. Criterios de no aceptación

M8 **no** se acepta si, por ejemplo:

- la build **sólo funciona desde escenas intermedias**;
- el recorrido **requiere intervención manual** del desarrollador;
- existe un **softlock**;
- el jugador puede **saltarse una transición obligatoria** y romper el estado;
- **combate, restauración o vinculación no pueden completarse**;
- el **compañero no queda en el estado esperado** tras el vínculo;
- el **feedback obligatorio es ilegible** (prompt, combate, ficha, ECO);
- la **build no representa el HEAD aprobado**;
- se **incorporó alcance excluido** (§5) sin decisión explícita del Director.

## 11. Dependencias y decisiones pendientes

- **DEC-001** — base congelada del alcance de M8 (commit `dfdf200`). No se reabre.
- **DEC-P05** (GDD Prototipo §37) — **texto final del cierre narrativo**. Es una **dependencia de
  entrada del playtest externo**: **debe resolverlo el Director antes de autorizar la prueba**, y
  **la prueba externa no comienza mientras DEC-P05 permanezca pendiente**. Este GDD **no** lo resuelve.
- **Placeholders de arte/audio** — se conservan; sólo se ajustan por legibilidad imprescindible.
- **Plataforma y build** — plataforma objetivo: **Windows PC**. La **SPEC deberá verificar el Build
  Profile actual** (no se asume que la configuración histórica de M0 siga vigente). Cualquier
  dependencia adicional de build se registra aquí cuando aparezca.

> Este GDD **no resuelve DEC-P01–P05**. DEC-P05 es dependencia de entrada del playtest (arriba); las
> demás **permanecen fuera de M8 salvo resolución expresa del Director**.

## 12. Entregables de M8

A nivel de diseño, M8 produce:

- **build candidata para playtest externo** (ejecutable por terceros, previa a la prueba);
- **checklist de recorrido** (pasos obligatorios del **flujo consolidado definido por DEC-001**,
  verificables sin instrucciones);
- **registro/matriz de incidencias** (con la clasificación de §6);
- **reporte de playtest** (observaciones y datos cualitativos de §8);
- **lista de bloqueadores corregidos**;
- **lista de defectos diferidos** (mayores/menores, con decisión explícita);
- **build de cierre de M8** (posterior a corregir **todos** los bloqueadores y críticos confirmados);
- **evidencia de cierre del milestone** (build de cierre + recorrido + resultados).

> El **versionado, los nombres técnicos y el pipeline de build no se definen aquí**; corresponden a la SPEC.

## 13. No objetivos

M8 **no** debe convertirse en:

- una revisión total del proyecto;
- un refactor general;
- una optimización prematura;
- una reescritura narrativa;
- un milestone de contenido;
- una preparación comercial;
- un sistema de guardado;
- un vertical slice distinto del consolidado M1–M7.

Cualquier trabajo que no sea integración de cierre, pulido mínimo, QA, playtest o corrección de
incidencias conforme a la clasificación y reglas de §6 y §9 queda **fuera** de M8.

---

## Restricciones arquitectónicas (a preservar en toda corrección de M8)

Las correcciones de M8 **no pueden** violar estas invariantes ya congeladas en M1–M7; ningún objetivo
de M8 puede contradecirlas:

- **Independent State Pattern**, **sin `BaseState`**.
- **`CreatureBrain` es el único dueño de las transiciones** de estado.
- **`CreatureContext`** es el contexto compartido por la criatura.
- **`CreatureMovement` es el único dueño del movimiento** (nadie más escribe `Transform`/`Rigidbody2D`/velocidad).
- **`Animator` es sólo presentación**; **sin `AnimationEvents`**.
- **Dual-radius sensing** (detección/pérdida con histéresis) y **patrulla PingPong** se conservan.
- **Reutilización de `ExaminableInteractable`** para observación/examen; no se crean sistemas paralelos.
- **Criatura y jugador sin colisión física** (capas separadas).
- **`SpriteFlash` es el único dueño de `SpriteRenderer.color`.**
- **Pipeline de color:** `Base → persistent tint → flash → SpriteRenderer.color`.
- **La presentación no muta estado de sesión** ni de gameplay.
- **`CreatureBondSessionCoordinator` conserva el ownership** del flag de sesión establecido en M7
  (la presentación no escribe `verak_vinculado`).

Este documento es de **diseño**, no una SPEC técnica; pero ningún objetivo de M8 puede formularse en
contra de estas restricciones.

---

**Estado: Aprobado / Congelado.** Aprobado por el Director el 2026-07-29. No se modifica sin una
nueva decisión documental o una revisión aprobada del GDD.
