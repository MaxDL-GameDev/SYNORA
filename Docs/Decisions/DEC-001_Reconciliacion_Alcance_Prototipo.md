# DEC-001 — Reconciliación del alcance del prototipo

| Campo | Valor |
|---|---|
| **ID** | DEC-001 |
| **Título** | Reconciliación del alcance de entrega efectivo del vertical slice (M1–M7) |
| **Fecha** | 2026-07-29 |
| **Estado** | **Aprobado / Congelado** |
| **Tipo** | Decisión de planificación / arquitectura de alcance. **No es canon.** No es un ADR. |
| **Autoridad** | Director del proyecto |
| **Decisión reemplazada** | Ninguna |
| **Documentos afectados** | Roadmap del prototipo (GDD Prototipo §34), planificación de M8. **No modifica** la Biblia v3.0, el GDD Prototipo (histórico), CANON-001, ni ningún SPEC/GDD de milestone. |

> DEC-001 documenta y fija el **alcance de entrega efectivo** del prototipo tras los rescopes formalizados durante M2–M7. **No modifica ni crea canon, ni reemplaza el GDD Prototipo**: describe la evolución real del alcance de entrega. El GDD Prototipo v0.1 sigue siendo un documento histórico válido de intención original.

## Constancia de aprobación

- **Aprobado por:** Director del proyecto.
- **Fecha de aprobación:** 2026-07-29.
- **Alcance:** reconciliación del alcance de entrega efectivo del prototipo (M1–M7).
- **Efecto:** M8 queda definido como integración final, pulido, QA y playtest del slice consolidado.
- **No modifica** la Biblia, CANON-001 ni el GDD Prototipo histórico.

---

## 1. Contexto

**Objetivo original del GDD Prototipo.** El GDD Prototipo Mecánico v0.1 (`Docs/Design/SYNORA_GDD_Prototipo_Mecanico_v0.1.docx`) describe un vertical slice narrativo de 10–15 minutos organizado en seis segmentos (§7): **A** Despertar, **B** Ayudar a Verak, **C** Explorar el claro, **D** Encuentro/Combate, **E** Restauración, **F** Vinculación/cierre. El roadmap de implementación (§34) mapea esos segmentos a los hitos M0–M8, con **M8 = Pulido/pruebas**.

**Milestones implementados.** M0–M7 fueron implementados y probados; sus tags `m0-complete` … `m7-complete` están verificados en el remoto. Cada milestone se especificó con su propio GDD/SPEC delta.

**Auditoría realizada.** Antes de iniciar M8 se ejecutaron dos auditorías de solo lectura: (a) documental del canon y de los GDD/SPEC de milestones, y (b) del estado real del repositorio (censo de componentes por escena, encadenado de `AreaTransition`, punto de entrada `GameBootstrap`). Ninguna auditoría modificó código, escenas, prefabs, assets ni tests.

**Evidencia encontrada.** El endgame (D–E–F) está integrado y jugable en `ClaroExterior`, y las escenas encadenan correctamente (`Bootstrap → CamaraPreservacion → CorredorTecnico → ClaroExterior`). El contenido narrativo temprano de los Segmentos A, B y C difiere de la descripción del GDD Prototipo: A está simplificado, B fue reinterpretado y C fue parcialmente reinterpretado. Esas diferencias **no fueron omisiones accidentales**: son **rescopes deliberados, formalizados en los GDD de milestone y consolidados mediante la implementación de M2–M4**.

---

## 2. Evidencia (trazabilidad GDD Prototipo ↔ implementación)

Todas las afirmaciones citan documentos revisados; no se inventa información.

### Segmento A — Despertar
- **Qué decía el GDD Prototipo** (§8): moverse en la cámara; inspeccionar cápsula y panel; **recoger/reinsertar una celda de energía**; **activar parcialmente para abrir la salida**; escuchar una **señal fragmentada de ECO**.
- **Qué implementó realmente M2**: un único objeto graybox **examinable** ("Terminal de diagnóstico") que sólo muestra texto (GDD M2 §4.1). El movimiento y el encadenado de escenas provienen de M1.
- **Por qué cambió**: el GDD de M2 declaró **explícitamente fuera de su alcance** la *"Recolección"* y la *"Activación o reparación de los objetos examinables"* (GDD M2 §5). La celda de energía, el gate de salida por energía y el ECO inicial no se construyeron en M2. *(El GDD de M2 registra además que su inicio partió de un "Contrato funcional entregado por el Director"; GDD M2, encabezado.)*

### Segmento B — Ayudar a Verak
- **Qué decía el GDD Prototipo** (§9; roadmap §34 "M3 — Verak dirigido"): un **Verak joven atrapado** en el corredor que retrocede ante el jugador, se libera observando una raíz luminosa / desactivando una fuente de ruido, y **huye** al exterior.
- **Qué implementó realmente M3**: **Verak ambiental** (2 ejemplares) en `ClaroExterior`, que patrullan (PingPong), pausan, detectan y observan al jugador sin agresión (GDD M3 §1–§3). El corredor no contiene criatura (evidencia: censo `CorredorTecnico → CreatureBrain = 0`).
- **Por qué cambió**: el GDD de M3 redefinió el hito hacia "criatura ambiental viva" y declaró **fuera de su alcance** la *"huida"* y la *"captura"*, con estado narrativo *"ambos libres; no atrapados"* (GDD M3 §3–§4). La secuencia del Verak atrapado no se construyó en M3.

### Segmento C — Explorar el claro
- **Qué decía el GDD Prototipo** (§10; roadmap §34 "M4 — Observación: tres pistas"): **tres pistas ambientales** (anillos apagados, huellas de Verak, panel exterior con patrón) que **almacenan estado** y cuyo patrón **informa el combate**.
- **Qué implementó realmente M4**: **observación de la criatura** — examinar al Verak ambiental y leer una lectura breve según su estado (Calm/Roaming/Watchful), reutilizando el panel de M2 (GDD M4 §1–§5). Evidencia: la única examinable ambiental de `ClaroExterior` es el "Nodo inactivo" de M2; no hay examinables de las tres pistas.
- **Por qué cambió**: M4 se formalizó como *"Observación de Criaturas"* en lugar del sistema de pistas ambientales del roadmap. El acople pista→combate no fue especificado por ningún delta y quedó pendiente.

### Segmentos D–F — confirmación de implementación
- **D (Combate no letal)**: integrado en `ClaroExterior` (Verak Alterado + combate no letal + Estabilidad + **contención**), SPEC/GDD M5. Evidencia: censo `AlteredVerakSetup = 1`.
- **E (Restauración)**: integrado (interactuable de restauración, estado `Restored`), SPEC/GDD M6. Evidencia: `RestoreInteract = 1`.
- **F (Vinculación)**: integrado (interactuable de vínculo, `Bonding → Bonded`, compañero que sigue, ficha "Vínculo establecido", señal de ECO provisional, bandera de sesión `verak_vinculado`), SPEC/GDD M7 + CANON-001. Evidencia: `BondInteract = 1`, `BondedFeedback = 1`, `BondSessionCoord = 1`, `BondSessionState = 1`.

---

## 3. Decisión

Se documenta y fija formalmente, sin modificar el canon:

1. Durante M2–M7 el proyecto **evolucionó** desde un vertical slice **principalmente narrativo** (la descripción por segmentos del GDD Prototipo) hacia un vertical slice **centrado en validar las mecánicas principales del juego** (interacción, criatura ambiental, observación, combate no letal con contención, restauración, vinculación y compañero).

2. Las diferencias entre el GDD Prototipo y la implementación actual **fueron rescopes deliberados, formalizados en los GDD de M2, M3 y M4 y consolidados mediante su implementación** — no omisiones accidentales ni errores del GDD histórico. La **aprobación del alcance de entrega final ocurre ahora, mediante DEC-001.**

3. El **GDD Prototipo histórico NO se modifica**: conserva su validez como documento de **intención original**. **DEC-001 es el registro formal del contenido diferido**; el GDD histórico no formaliza ese diferimiento.

4. El **alcance de entrega efectivo del prototipo** queda definido por la **implementación consolidada de M1–M7**.

5. **Como decisión de DEC-001, el prototipo no reincorporará antes de su cierre el contenido narrativo temprano que quedó fuera durante M2–M4. Dicho contenido queda fuera del alcance de entrega de M8 y se difiere para una posible etapa posterior.**

### 3.1 Alternativas consideradas
- **A — Reconstrucción completa de los Segmentos A–C** (celda/gate/ECO inicial, Verak atrapado/huida, pistas ambientales + acople al combate) antes del cierre del prototipo. *(Descartada: revertiría los rescopes consolidados y ampliaría el alcance mucho más allá de M8.)*
- **B — Solución híbrida**: reconstruir sólo la apertura/cierre de mayor valor (p. ej. gate de salida + ECO inicial y el texto de cierre) y aceptar la sustitución ambiental de B/C. *(Descartada para el alcance de entrega del prototipo; puede retomarse como contenido posterior.)*
- **C — Consolidación del slice efectivo M1–M7** como alcance de entrega del prototipo, con el contenido temprano diferido. *(**Decisión adoptada**.)*

---

## 4. Alcance de entrega efectivo del vertical slice (consolidado M1–M7)

Flujo de entrega real del slice. Incluye un **onboarding previo** (despertar simplificado, exploración y observación) y, dentro del flujo, el **arco de resolución de la criatura preserva la progresión conceptual**: encuentro/observación → contención → restauración → vinculación → compañero.

```
Despertar simplificado
    ↓   [mecánicas consolidadas: movimiento M1 + examinar M2]
Exploración y observación
    ↓   [mecánicas consolidadas: recorrido/transiciones M1 + criatura ambiental M3 + observación M4]
Encuentro / combate no letal
    ↓   [mecánica consolidada: combate no letal M5]
Contención
    ↓   [mecánica consolidada: estado contenido (Subdued) M5]
Restauración
    ↓   [mecánica consolidada: restauración M6]
Vinculación
    ↓   [mecánica consolidada: vínculo voluntario M7 + CANON-001]
Resultado: compañero
        [seguimiento + ficha + ECO provisional + verak_vinculado (sesión, no persistente)]
```

> **Compañero no constituye un Segmento G**; es el **resultado estable** del Segmento F y de CANON-001.

**Mecánicas consolidadas** (implementadas, probadas, integradas): movimiento y cámara con colisiones y transiciones (M1); interacción/examinar contextual (M2); criatura ambiental con patrulla/percepción (M3); observación de criatura por estado (M4); combate no letal con contención (M5); restauración de la criatura contenida (M6); vinculación voluntaria y compañero que sigue, con feedback y bandera de sesión (M7).

---

## 5. Contenido diferido

Se distinguen tres categorías con **orígenes distintos**. Ningún elemento se elimina ni se invalida.

### 5.1 Contenido narrativo o de escena diferido por DEC-001
Elementos que quedaron **fuera del alcance de cada milestone** durante M2–M4 (origen histórico de la exclusión) y que **DEC-001 decide ahora no reincorporar dentro de M8** (decisión actual). Su exclusión histórica fue por milestone; su diferimiento para el resto del prototipo lo decide este DEC:
- celda de energía;
- gate/activación de salida;
- señal ECO inicial (al despertar);
- Verak atrapado;
- liberación y huida dirigida;
- pistas ambientales originales (anillos apagados, huellas, panel con patrón);
- acople pista → combate.

### 5.2 Sistemas ya definidos como post-prototipo por Biblia/GDD
Diferidos **por las fuentes**, con cita explícita (no por DEC-001):
- persistencia / guardado (GDD Prototipo §37 DEC-P06 *"Sistema de guardado definitivo — después de validar el flujo"*; Biblia §57–58);
- progresión, santuarios y sistemas de campaña posteriores (Biblia §57–58, "Después del prototipo").

### 5.3 Producción audiovisual y decisiones pendientes
- arte/audio **representativo o definitivo**: diferido a "después de M7" (GDD Prototipo §35), bajo autoridad de Gemini/Director;
- decisiones pendientes del GDD Prototipo §37: **DEC-P01** (diseño de la criatura enemiga), **DEC-P02** (afinidad de Verak), **DEC-P03** (nombre de ECO), **DEC-P04** (Resonador), **DEC-P05** (texto final del cierre — límite documental: *antes de la prueba externa*). Estado: pendientes; su resolución corresponde al Director conforme a la autoridad documental y creativa del proyecto. No son resueltas por DEC-001.

---

## 6. Impacto

- **El roadmap vuelve a ser consistente**: el estado real del repositorio deja de contradecir la planificación, porque el alcance de entrega efectivo queda explícitamente documentado.
- **M8 recupera su definición original**: con el alcance efectivo definido por M1–M7, **M8 = Pulido / QA / Playtest** del slice consolidado (coherente con GDD Prototipo §34/§36), y **no** una reconstrucción del contenido narrativo original.
- **El GDD histórico permanece intacto**: sigue siendo la referencia de intención original y de contenido diferido.
- **El prototipo queda correctamente documentado**: un lector futuro entiende qué se construyó, qué se difirió y con qué origen.

---

## 7. Consecuencias

### 7.1 Positivas
- Planificación de M8 acotada y sin ambigüedad (Pulido/QA/Playtest del slice consolidado).
- Coherencia entre documentación y repositorio.
- Contenido diferido preservado y trazable para etapas futuras.
- Sin cambios de canon ni de reglas jugables.

### 7.2 Negativas
- El prototipo **no reproduce íntegramente la intención narrativa A–F original**.
- El **playtest validará principalmente las mecánicas consolidadas**, no el onboarding narrativo completo.
- Parte del **onboarding narrativo permanece simplificado** (apertura y ayuda inicial).
- El **contenido diferido requerirá planificación futura** si se decide reincorporarlo.

### 7.3 Para líneas de trabajo
- **Para M8**: puede documentarse (GDD/SPEC/fases) como hito de integración final + pulido + QA + playtest del slice consolidado, con foco en "build jugable por terceros" y corrección de bloqueadores. DEC-P05 (texto de cierre) debería resolverse antes de la prueba externa.
- **Para el post-prototipo**: el contenido diferido de §5.1 y §5.2 es la base del trabajo posterior a la validación del prototipo (Biblia §57–58), sin compromiso de fecha.
- **Para la persistencia**: sigue siendo post-prototipo (DEC-P06). Nada en M8 la introduce.

---

## 8. Auditoría de consistencia

| Verificación | Resultado |
|---|---|
| No contradice la Biblia v3.0 | ✔ La Biblia sitúa persistencia y los sistemas explícitamente señalados en §57–58 después del prototipo; DEC-001 difiere separadamente el contenido narrativo temprano no incorporado. |
| No contradice CANON-001 | ✔ CANON-001 rige la vinculación de criatura única; este DEC no la toca y su flujo la respeta (contención explícita). |
| No contradice M7 | ✔ El endgame D–F queda confirmado como implementado. |
| No introduce mecánicas nuevas | ✔ Sólo documenta el estado existente. |
| No redefine reglas jugables | ✔ |
| No modifica el GDD histórico | ✔ Permanece intacto; sólo se referencia. |
| Únicamente documenta la evolución del alcance de entrega | ✔ |

---

**Estado: Aprobado / Congelado.** Aprobado por el Director el 2026-07-29 (ver *Constancia de aprobación*). Congelado: no se modifica sin un nuevo DEC.
