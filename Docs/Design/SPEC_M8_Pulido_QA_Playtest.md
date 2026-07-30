# SYNORA — SPEC M8: Pulido, QA y Playtest (v1.0 — Aprobada / Congelada)

> Especificación **técnica de ejecución** de M8. Traduce el **GDD M8 v1.0 (congelado, commit
> `9aa79fd`)** en fases, gates, tareas, validaciones y evidencias. **No amplía, reinterpreta ni
> modifica** el alcance del GDD M8. Estado: **Aprobada / Congelada**.
>
> **Base congelada:** DEC-001 (`dfdf200`) + GDD M8 v1.0 (`9aa79fd`). No modifica Biblia, CANON-001,
> DEC-001, GDD M8 ni el GDD Prototipo histórico.
>
> Jerarquía documental ante conflicto: Biblia v3 → CANON-001 → DEC-001 → GDD M8 v1.0 → GDD Prototipo
> → GDD/SPEC M1–M7 → estado del repo. **Ante una contradicción: citar, detenerse y pedir decisión
> del Director; no resolver por cuenta propia.**

## Constancia de aprobación

- **Aprobado por:** Director del proyecto.
- **Fecha de aprobación:** 2026-07-29.
- **Base documental:** DEC-001 y GDD M8 v1.0.
- **Alcance:** ejecución técnica de integración, QA, build, playtest y cierre del slice consolidado M1–M7.
- **Efecto:** autoriza comenzar la **Fase 0**, sin ampliar el alcance del GDD.
- **No modifica** la Biblia, CANON-001, DEC-001, el GDD M8 ni el GDD Prototipo.

---

## 1. Objetivo técnico

Definir el **cómo y el orden de ejecución** del cierre del prototipo: recorrer y estabilizar el
slice consolidado M1–M7, producir una **build candidata** para playtest externo, ejecutar el
playtest, corregir **todos los bloqueadores y críticos confirmados**, producir la **build de cierre**
y consolidar la evidencia — sin ampliar alcance ni violar las invariantes arquitectónicas.

## 2. Alcance

Integración final del flujo, QA (auditoría + regresión), preparación de build, playtest externo,
triage y corrección de incidencias obligatorias, build de cierre y cierre documental — todo sobre
el **flujo consolidado definido por DEC-001** (Despertar simplificado → Exploración y observación →
Encuentro/combate no letal → Contención → Restauración → Vinculación → Compañero).

## 3. No objetivos (fuera de alcance)

Reconstrucción narrativa A–C; celda de energía; gate por energía; ECO inicial; Verak atrapado; huida
dirigida; pistas ambientales originales; acople pista→combate; persistencia; guardado; progresión;
santuarios; campaña; nuevas criaturas/habilidades/mecánicas; arte/audio definitivo (salvo legibilidad
imprescindible aprobada); refactor general; optimización prematura; telemetría nueva; pipeline
comercial; CI/CD nuevo; preparación de publicación comercial; **resolución de DEC-P01–P05 dentro de
la SPEC**.

## 4. Baseline de referencia (nombres reales verificados)

- **Escenas en Build Settings** (orden): `Bootstrap`(0) → `CamaraPreservacion`(1) → `CorredorTecnico`(2)
  → `ClaroExterior`(3). `SampleScene` existe pero **no** está en build.
- **Punto de entrada:** `GameBootstrap` (en `Bootstrap`) → `SceneLoader.TryLoad("CamaraPreservacion", "")`.
- **Transiciones:** `AreaTransition` (campos `destinationScene`/`destinationSpawnId`) →
  `SceneLoader` + `SceneTransitionContext` + `SpawnPoint`/`PlayerSpawner`. Cadena verificada:
  `CamaraPreservacion→CorredorTecnico`; `CorredorTecnico→{CamaraPreservacion, ClaroExterior}`;
  `ClaroExterior→CorredorTecnico`. **El retorno se valida sólo donde está diseñado y soportado.**
- **Sistemas M1–M7 (owners):** `CreatureBrain` (transiciones), `CreatureContext` (contexto),
  `CreatureMovement` (movimiento), `CreatureSensor` (dual-radius), estados
  `Idle/Patrol/Alert/Chase/Attack/Subdued/Restoring/Restored/Bonding/Bonded`, `AlteredVerakSetup`,
  `CreatureAttackController`, `Health`, `PlayerControlGate`, `InteractionController` +
  `InteractionDetector/Selector` + `InteractionPromptPresenter` + `ObservationPanelPresenter`,
  `ExaminableInteractable` / `CreatureExaminableInteractable` / `CreatureObservationSource`,
  `CreatureRestorationInteractable`, `RestoredCreatureExaminableInteractable`,
  `CreatureBondingInteractable`, `CreatureBondingControlBlock`, `CreatureBondPresentation`,
  `CreatureBondedFeedback` + `BondEstablishedPresenter` + `EcoSignal`, `CreatureBondSessionCoordinator`
  + `BondSessionState`, `SpriteFlash`.
- **Prefabs:** `Assets/Prefabs/Creatures/{Verak, VerakAltered}.prefab`, `Assets/Prefabs/Player.prefab`.
- **Datos:** `Assets/Data/Creatures/Verak.asset` (`CreatureIdentity`).
- **UI de vínculo en `ClaroExterior`:** `InteractionCanvas` (existente) con `BondFichaPanel` y
  `EcoPanel` (`BondEstablishedPresenter`), objeto `EcoSignal`, objeto `BondSessionState`.
- **Regresión:** suite **EditMode** existente (601 tests al cierre de M7; la **Fase 0 registra el
  conteo real actual** y explica cualquier diferencia). **No** se crea framework nuevo.
- **Build Profile:** **la Fase 0 debe verificar el Build Profile actual; no se asume vigente la
  configuración histórica de M0.**

> Cualquier nombre no listado aquí debe **confirmarse en el repo antes de mencionarse** en una tarea.

---

## 5. Fases de ejecución

Cada fase declara: **objetivo · precondiciones · tareas · validaciones · evidencia · criterio de
salida (gate) · bloqueos/decisiones · archivos/sistemas potencialmente afectados · prohibiciones**.
Una fase **no** se cierra por "se revisó": requiere **evidencia verificable**.

### Fase 0 — Preparación y baseline
- **Objetivo:** referencia reproducible antes de modificar nada.
- **Precondiciones:** repo en el HEAD de trabajo; Unity abierto.
- **Tareas:** registrar `HEAD`/commit de trabajo; verificar árbol limpio o documentar cambios;
  confirmar versión de Unity (`ProjectSettings/ProjectVersion.txt`); confirmar plataforma objetivo
  **Windows PC**; **verificar el Build Profile actual**; confirmar escenas en build y su orden;
  confirmar punto de entrada (`GameBootstrap`); confirmar rutas reales del flujo consolidado;
  **confirmar la convención y ubicación real para los artefactos de QA antes de crearlos**
  (`Docs/Technical/QA/` permanece **sólo como sugerencia** hasta esa confirmación; **no se crea aún
  la carpeta ni ningún artefacto**); **registrar el conteo real actual de la suite EditMode** y
  explicar cualquier diferencia con el baseline histórico (601); preparar, una vez confirmada la
  ubicación, la plantilla de **matriz de incidencias** (§7) y el **checklist maestro de recorrido**;
  listar riesgos conocidos.
- **Validaciones:** el proyecto **inicia de forma reproducible** desde `Bootstrap` y el flujo es
  recorrible desde el punto de entrada (aunque sea con incidencias).
- **Evidencia:** baseline técnico (commit + versión Unity + Build Profile), lista de escenas, entry
  point confirmado, plantilla de incidencias vacía, checklist inicial, lista de riesgos.
- **Gate de salida (Gate A):** existe una forma reproducible de iniciar el proyecto y recorrer el
  flujo esperado desde su punto de entrada; si el Build Profile no puede verificarse, se registra
  como incidencia y no se avanza a build.
- **Bloqueos/decisiones:** si Unity/plataforma/Build Profile difieren de lo esperado → incidencia +
  decisión del Director antes de F4.
- **Archivos/sistemas (inspección):** `ProjectSettings/*`, Build Settings/Build Profile, `Bootstrap`,
  `GameBootstrap`, `SceneLoader`. **No se modifica nada en F0.**
- **Prohibiciones:** ningún cambio de código/escena; no asumir config de M0 vigente.

### Fase 1 — Auditoría de integración
- **Objetivo:** recorrer el slice completo **sin modificar** y detectar incidencias.
- **Precondiciones:** Gate A cumplido.
- **Tareas (recorrido y revisión):** inicio desde build o Play Mode equivalente; `GameBootstrap`;
  escena inicial `CamaraPreservacion`; transiciones requeridas; retornos sólo donde estén diseñados;
  control del jugador (`PlayerMotor`/`PlayerInputReader`), cámara (`CameraFollow`/`CameraBounds2D`),
  colisiones; interacción contextual y examinables; criatura ambiental (`Verak`), estados
  `Idle/Patrol/Alert`, dual-radius (`CreatureSensor`), patrulla PingPong; entrada al combate,
  combate no letal, Estabilidad, pulso, reinicio, contención (`Subdued`); restauración (`Restored`);
  vinculación (`Bonding→Bonded`), seguimiento del compañero, ficha (`BondEstablishedPresenter`), ECO
  (`EcoSignal`); `CreatureBondSessionCoordinator` + flag `verak_vinculado`; cierre del slice; reinicio
  de sesión; **ausencia de persistencia accidental**.
- **Validaciones / clasificación:** cada hallazgo se marca como **fallo reproducible · sospecha ·
  deuda técnica · observación de diseño · defecto fuera de alcance**, con severidad (§ GDD M8 §6:
  Bloqueador/Crítico/Mayor/Menor/Observación).
- **Evidencia:** **matriz de incidencias** poblada (§7) con al menos ID, título, severidad, sistema,
  escena, pasos, esperado, real, reproducibilidad, evidencia, owner, estado, decisión.
- **Gate de salida (Gate B):** recorrido completo intentado; **todas** las incidencias registradas;
  clasificación inicial hecha; **ningún hallazgo crítico queda en notas informales**.
- **Bloqueos/decisiones:** contradicción GDD↔repo → citar y elevar al Director.
- **Archivos/sistemas (inspección):** todos los del baseline (§4). **No se modifica nada en F1.**
- **Prohibiciones:** no corregir aún; no refactorizar; no cambiar alcance.

### Fase 2 — Corrección de bloqueadores y críticos
- **Objetivo:** corregir **todos** los bloqueadores y críticos **confirmados**, con causa mínima.
- **Precondiciones:** Gate B cumplido.
- **Tareas (por incidencia):** registrar ID, **causa raíz**, **cambio mínimo** propuesto, **owner del
  sistema**, archivos afectados, riesgos, prueba de regresión, resultado posterior, evidencia. Una
  incidencia por cambio lógico cuando sea razonable.
- **Validaciones (regresión obligatoria):** flujo completo; sistema corregido; sistemas adyacentes;
  estado de sesión; presentación; transiciones. **Ejecutar las pruebas dirigidas aplicables tras cada
  corrección; ejecutar la suite EditMode completa al terminar la Fase 2, conforme a §10.** Correr el
  recorrido del tramo afectado.
- **Evidencia:** diffs por incidencia + resultados de regresión (EditMode verde; recorrido del tramo).
- **Criterio de salida — Condición técnica previa a Gate C:** cero bloqueadores abiertos; cero
  críticos abiertos; **correcciones verificadas**; **regresiones dirigidas registradas**; **suite
  EditMode completa aprobada**; **ninguna violación arquitectónica** (§8). *(Gate C se concede en
  Fase 3, no aquí.)*
- **Bloqueos/decisiones:** cualquier **desviación arquitectónica** se documenta y se aprueba **antes**
  de implementarla; propuestas narrativas → Director; DEC-P01–P05 **no** se resuelven por código.
- **Archivos/sistemas (modificar sólo si hay incidencia):** los del sistema afectado (§4).
- **Prohibiciones:** no features; no refactor general; no reconstruir A–C; no persistencia; no
  contenido; no placeholders nuevos salvo legibilidad aprobada.

### Fase 3 — Validación interna del slice
- **Objetivo:** confirmar recorrido de inicio a fin **repetible** antes de la build candidata.
- **Precondiciones:** **Fase 2 completada y su condición técnica previa a Gate C satisfecha.**
- **Tareas:** **≥ 3 recorridos internos completos**; ≥ 1 desde estado limpio; ≥ 1 tras reiniciar
  sesión; revisar logs/excepciones, softlocks, prompts, feedback obligatorio, estados del Verak,
  flag de sesión, cierre, retornos permitidos, repetición de combate/interacciones cuando aplique.
- **Validaciones:** consola sin errores; sin softlock; el flag `verak_vinculado` se marca en `Bonded`
  y **no persiste** al reiniciar Play; el cierre se entiende.
- **Evidencia:** **checklist firmado** por recorrido; capturas/video cuando útil; logs; IDs de
  incidencias; resultado y tiempo por recorrido; observaciones.
- **Gate de salida (concede Gate C):** **tres recorridos internos completos**; cero bloqueadores;
  cero críticos; **ninguna incidencia sin clasificar**; **mayores y menores evaluados**; *readiness*
  de build candidata aprobada.
- **Bloqueos/decisiones:** si aparece un bloqueador/crítico nuevo → volver a Fase 2.
- **Archivos/sistemas (inspección; modificación sólo por incidencia):** flujo completo.
- **Prohibiciones:** no ampliar alcance por "mejorar la experiencia".

### Fase 4 — Preparación de build candidata
- **Objetivo:** producir la **build candidata** para playtest externo.
- **Precondiciones:** Gate C + validación interna (F3).
- **Tareas / verificación:** Build Profile actual; escenas incluidas y orden; plataforma Windows PC;
  resolución/modo de pantalla; input; dependencias y archivos requeridos; **sin herramientas de
  desarrollo visibles**; **sin referencias rotas**; **sin escenas de prueba** (`SampleScene` excluida);
  logs de build; arranque desde ejecutable; recorrido desde carpeta limpia; **correspondencia con el
  HEAD aprobado**.
- **Definir:** nomenclatura de build, identificador de versión, ubicación, hash/mecanismo de
  identificación, checklist de **smoke test**, evidencia de **qué commit produjo la build**.
- **Evidencia:** build candidata identificable + logs + smoke test + commit de origen.
- **Gate de salida (Gate D):** build candidata identificable; **arranca fuera del editor**; smoke test
  aprobado; representa el HEAD documentado; flujo principal iniciable; **cero bloqueadores y cero
  críticos conocidos**.
- **Bloqueos/decisiones:** Build Profile inválido → incidencia + decisión del Director.
- **Archivos/sistemas:** configuración de build (verificar; ajustar sólo si hay incidencia).
- **Prohibiciones:** **no** pipeline comercial; **no** CI/CD salvo que ya exista y sea indispensable.

### Fase 5 — Gate documental DEC-P05
- **Objetivo:** verificar que **DEC-P05** (texto final del cierre) esté resuelto **antes** de autorizar
  el playtest externo.
- **Precondiciones:** Gate D.
- **Tareas:** confirmar resolución de DEC-P05 (documento/decisión del Director); validar que la build
  candidata refleja la decisión; registrar responsable de autorización (**Director**).
- **Revalidación de build (si la resolución de DEC-P05 exige cualquier cambio en texto, presentación,
  escena, prefab, asset o configuración):**
  1. aplicar **exclusivamente** el cambio autorizado;
  2. registrar el **commit** que lo contiene;
  3. generar una **nueva build candidata**;
  4. actualizar **versión / hash / identificación**;
  5. repetir el **smoke test**;
  6. **volver a validar todas las condiciones de Gate D**;
  7. **sólo entonces** conceder Gate E.
- **Validaciones:** existe decisión documentada de DEC-P05; la build muestra el cierre acordado.
- **Evidencia:** referencia a la resolución de DEC-P05; nueva build + commit + smoke + re-validación de
  Gate D si hubo cambio.
- **Gate de salida (Gate E):** DEC-P05 resuelto y documentado; **la build autorizada para el playtest
  es exactamente la que refleja DEC-P05 y volvió a pasar Gate D** (cuando hubo cambio); **autorización
  explícita del Director para el playtest**.
- **Bloqueos/decisiones:** **la SPEC no resuelve DEC-P05**; **no** se redacta el texto por cuenta
  propia; **no** solución temporal silenciosa; **el playtest externo NO comienza mientras DEC-P05 esté
  pendiente.**
- **Archivos/sistemas:** ninguno de código; posible ajuste de texto de cierre según la decisión.
- **Prohibiciones:** iniciar F6 sin Gate E.

### Fase 6 — Playtest externo
- **Objetivo:** ejecutar el playtest definido por el GDD M8 §8.
- **Precondiciones:** Gate E.
- **Tareas / definiciones:** **objetivo: cinco sesiones válidas con participantes externos** (GDD
  Prototipo §36). Una **reducción sólo es admisible por decisión documental explícita del Director**,
  que **justifique la excepción y registre su efecto sobre la validez del resultado**. Participantes
  externos, sin conocimiento previo, **sin instrucciones verbales** de flujo/mecánicas (sólo lo mínimo
  para abrir la build y controles básicos si no se comunican); **registro manual, sin telemetría
  nueva, sin datos personales innecesarios**. La SPEC define: guion del moderador; información
  permitida vs prohibida; plantilla de observación; plantilla de entrevista posterior; forma de
  registrar tiempo/abandono/reinicios/dudas; criterio para intervenir por fallo técnico; criterio de
  invalidar/repetir sesión; tratamiento de incidencias; resguardo de privacidad.
- **Datos mínimos por sesión:** participante anonimizado; versión de build; resultado
  (completado/abandonado/invalidado); tiempo; punto de bloqueo; nº de reinicios; comprensión de
  objetivo/combate/restauración/vínculo/cierre; comentarios cualitativos.
- **Evidencia:** sesiones registradas; reporte preliminar; incidencias clasificadas.
- **Gate de salida (Gate F):** **cinco sesiones válidas** cumplidas (o reducción por **decisión
  documental del Director** que justifique la excepción y su efecto sobre la validez); sesiones válidas
  registradas; reporte preliminar; incidencias clasificadas; bloqueadores/críticos identificados.
- **Bloqueos/decisiones:** las respuestas de participantes **no** se convierten en decisiones
  automáticas de diseño.
- **Prohibiciones:** playtest guiado; telemetría; datos personales innecesarios.

#### 5.6.A — Guion mínimo del moderador
- **Presentación neutral:** "Vas a probar un prototipo. Jugá a tu ritmo; no hay respuestas correctas.
  Podés pensar en voz alta si querés."
- **Información permitida:** cómo ejecutar la build y los controles básicos **sólo si el juego no los
  comunica**.
- **Información prohibida:** objetivos, mecánicas, qué hacer, cómo avanzar, pistas, ayuda de flujo.
- **Respuesta neutral ante "¿qué tengo que hacer?":** *"Intentá continuar usando únicamente la
  información que te da el juego."* El moderador **no** explica objetivos ni mecánicas.
- **Intervención por fallo técnico:** sólo si un fallo impide continuar (crash, softlock, build rota);
  la intervención se limita a resolver el fallo técnico, no a guiar.
- **Registro obligatorio:** **toda** intervención se registra (motivo, momento, acción).
- **Cierre:** al terminar o abandonar, se agradece y se pasa a la entrevista (5.6.C).

#### 5.6.B — Plantilla de registro de sesión
| Campo | Valor |
|---|---|
| ID anonimizado | |
| Fecha | |
| Build | |
| Commit | |
| Moderador | |
| Hora de inicio | |
| Hora de fin | |
| Resultado | completado / abandonado / invalidado |
| Tiempo total | |
| Punto de abandono o bloqueo | |
| Nº de reinicios | |
| Intervenciones | (motivo/momento/acción) |
| Dudas espontáneas | |
| Comprensión observada | (objetivo/combate/restauración/vínculo/cierre) |
| Incidencias vinculadas | (IDs) |
| Notas | |

#### 5.6.C — Plantilla de entrevista posterior
- ¿Qué entendiste que debías hacer?
- ¿En qué momento dudaste o te trabaste?
- ¿Cómo entendiste el combate no letal?
- ¿Qué entendiste que ocurrió durante la restauración?
- ¿Qué significó para vos la vinculación?
- ¿Qué entendiste al final del slice?
- ¿Hubo algún feedback que no pudieras leer o interpretar?
- ¿Te interesaría continuar jugando? ¿Por qué?

> Las respuestas **no** se convierten en decisiones automáticas de diseño (son insumo, ver Fase 7).

#### 5.6.D — Criterios de validez de sesión
- **Sesión válida:** build correcta; participante externo; sin guía indebida; registro completo;
  recorrido o abandono **atribuible a la experiencia evaluada**.
- **Sesión abandonada:** el participante decide no continuar, o no puede avanzar, **sin** un fallo
  técnico que invalide la sesión. *(Una sesión abandonada **no** se invalida automáticamente.)*
- **Sesión invalidada:** build incorrecta/corrupta; fallo externo de hardware/sistema; intervención
  indebida del moderador; pérdida significativa del registro; el participante ya conocía material que
  invalida la condición de prueba.
- **Sesión repetible:** invalidación técnica; corrección de bloqueador/crítico; corrección **mayor**
  que cambie sustancialmente la comprensión evaluada.

### Fase 7 — Triage y correcciones post-playtest
- **Objetivo:** clasificar y resolver los hallazgos del playtest.
- **Precondiciones:** Gate F.
- **Tareas:** acto de **triage**; separar bloqueadores/críticos/mayores/menores/observaciones/
  sugerencias fuera de alcance; definir responsable de severidad y criterios de: confirmación,
  duplicado, **no reproducible**, **by design**, diferimiento, y evidencia de decisión.
- **Reglas:** **todos** los bloqueadores y críticos se corrigen; mayores/menores requieren decisión
  explícita; una observación **no** es automáticamente un bug; una sugerencia **no** amplía alcance;
  propuesta narrativa → Director; propuesta arquitectónica → evaluar contra invariantes (§8).
- **Validaciones (tras cada corrección):** repetir el tramo afectado; repetir el recorrido completo
  cuando afecte estado/transición/flujo; **repetir con participantes nuevos cuando una corrección
  cambie sustancialmente la comprensión evaluada**.
- **Evidencia:** matriz actualizada; decisiones registradas; regresiones.
- **Gate de salida (Gate G):** cero bloqueadores; cero críticos; mayores/menores decididos;
  regresiones completadas; resultados actualizados.
- **Archivos/sistemas (modificar sólo por incidencia):** los del sistema afectado.
- **Prohibiciones:** no ampliar alcance; no refactor general.

### Fase 8 — Build de cierre
- **Objetivo:** producir y validar la **build final de cierre** de M8.
- **Precondiciones:** Gate G.
- **Tareas / verificación:** build posterior a **todas** las correcciones obligatorias; identificación
  del commit; verificación del Build Profile; smoke test; recorrido completo; revisión de logs;
  verificación del **estado de sesión**, del **cierre**, de la **ausencia de persistencia** y de la
  **ausencia de artefactos de desarrollo**; confirmar **cero bloqueadores y cero críticos**.
- **Evidencia:** build final identificada + commit + smoke test + recorrido + logs archivados.
- **Gate de salida (Gate H — build de cierre validada técnicamente; responsable Dev/QA):** build
  final identificada; recorrido completo aprobado; cero bloqueadores; cero críticos; evidencia
  archivada; mayores/menores documentados. La **aprobación de cierre de M8** corresponde a **Gate I
  (Director)**.
- **Bloqueos/decisiones:** la build de cierre **no** es necesariamente comercial.
- **Prohibiciones:** no preparación de publicación comercial.

### Fase 9 — Cierre documental de M8
- **Objetivo:** consolidar evidencia y preparar la decisión de cierre del milestone.
- **Precondiciones:** Gate H.
- **Entregables:** build candidata; build de cierre; checklist de recorrido; **matriz final de
  incidencias**; reporte de playtest; lista de bloqueadores corregidos; lista de críticos corregidos;
  lista de mayores/menores diferidos; evidencia de regresión; referencia al commit de cierre; riesgos
  residuales; **recomendación de aceptación o no aceptación**.
- **Debe indicar:** qué se corrigió; qué se difirió y por qué puede diferirse; qué queda fuera de
  alcance; si el GDD M8 se cumplió; si hay contradicción pendiente.
- **Gate final (Gate I):** **M8 sólo puede proponerse como completado cuando se cumplen todos los
  criterios de aceptación del GDD M8** (§9 del GDD) y el **DoD técnico** (§9 de esta SPEC).
- **Prohibiciones:** declarar M8 completo sin la aprobación del Director según convención.

---

## 6. Gates globales

| Gate | Condición de entrada | Condición de salida | Evidencia | Responsable | Qué bloquea |
|---|---|---|---|---|---|
| **A — Baseline listo** | HEAD registrado | Proyecto iniciable/reproducible; Build Profile verificado o incidencia | baseline + escenas + entry | QA/Dev | inicia F1 |
| **B — Auditoría completa** | Gate A | Recorrido intentado; todas las incidencias registradas y clasificadas | matriz F1 | QA | inicia F2 |
| **C — Slice validado (pre-build)** | Condición técnica previa a Gate C (F2) satisfecha | 3 recorridos completos; 0 bloqueadores + 0 críticos; incidencias clasificadas; mayores/menores evaluados; readiness de build | 3 recorridos + regresión + checklists | Dev/QA | inicia F4 |
| **D — Build candidata aprobada** | Gate C | Build arranca fuera del editor; smoke ok; = HEAD; cero bloqueadores y cero críticos conocidos | build + smoke + commit | Dev | inicia F5 |
| **E — DEC-P05 resuelto** | Gate D | DEC-P05 documentado; build autorizada identificada; la build refleja DEC-P05; si hubo cambios volvió a cumplir Gate D; autorización explícita del Director | decisión DEC-P05 + build | **Director** | inicia F6 |
| **F — Playtest completado** | Gate E | Cinco sesiones válidas, o reducción mediante decisión documental explícita del Director que justifique la excepción y su efecto sobre la validez; reporte e incidencias registrados | reportes de sesión | QA | inicia F7 |
| **G — Triage cerrado** | Gate F | 0 bloqueadores + 0 críticos; mayores/menores decididos; regresión | matriz actualizada | Dev/owners | inicia F8 |
| **H — Build de cierre validada técnicamente** | Gate G | Build final; recorrido ok; 0 bloq/crít; evidencia | build cierre + smoke | Dev/QA | inicia F9 |
| **I — Evidencia documental completa** | Gate H | Todos los entregables (§Fase 9) + DoD cumplido | reporte de cierre | **Director** | propone M8 completo |

## 7. Matriz de incidencias (plantilla)

Plantilla obligatoria (gestión del milestone, **no** un sistema runtime):

| ID | Fecha | Build | Commit | Fase | Escena | Sistema | Severidad | Título | Pasos | Esperado | Real | Reproducibilidad | Evidencia | Owner | Estado | Resolución | Prueba de regresión | Decisión de diferimiento |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|

- **Severidad:** Bloqueador · Crítico · Mayor · Menor · Observación (GDD M8 §6).
- **Estado:** Nuevo · Confirmado · En corrección · Listo para verificar · Cerrado · Diferido · No
  reproducible · Duplicado · By design · Fuera de alcance.
- **Reglas:** bloqueadores/críticos **no** admiten estado "Diferido"; toda corrección exige "Prueba de
  regresión"; "Owner" debe ser el owner del sistema afectado (§8).

## 8. Restricciones arquitectónicas (verificación obligatoria por corrección)

Toda corrección que toque uno de estos sistemas exige **verificación explícita de ownership** en la
ficha de la incidencia:

- Independent State Pattern; **sin `BaseState`**.
- `CreatureBrain` = único dueño de transiciones.
- `CreatureContext` = contexto compartido.
- `CreatureMovement` = único dueño del movimiento.
- `Animator` = sólo presentación; **sin `AnimationEvents`**.
- Dual-radius sensing; patrulla PingPong.
- Reutilización de `ExaminableInteractable`.
- Criatura y jugador **sin colisión física**.
- `SpriteFlash` = único dueño de `SpriteRenderer.color`; pipeline `Base → persistent tint → flash →
  SpriteRenderer.color`.
- Presentación **no** muta estado de sesión ni gameplay.
- `CreatureBondSessionCoordinator` = owner de `verak_vinculado` (la presentación no lo escribe).

Cualquier excepción requiere **decisión previa del Director** documentada antes de implementarla.

## 9. Definition of Done técnico de M8

- build **ejecutable por terceros**; **inicio desde ejecutable**; **recorrido completo**; **sin
  intervención del desarrollador**;
- **cero bloqueadores**; **cero críticos**;
- **CANON-001 preservado** (progresión encuentro/observación → contención → restauración → vinculación
  → compañero; criatura única);
- **estado de sesión correcto** (`verak_vinculado` marcado en `Bonded`, no persiste);
- **`CreatureBondSessionCoordinator` conserva ownership**;
- **ausencia de persistencia accidental**;
- **cierre comprensible**;
- **playtest registrado**; **incidencias clasificadas**; **mayores/menores decididos**;
- **build de cierre identificada**; **evidencia archivada**;
- **ningún alcance excluido incorporado**.

> El criterio **no** es "cero bugs": es un slice jugable, completable y validado con cero bloqueadores
> y cero críticos, y las mayores/menores documentadas y decididas.

## 10. Estrategia de pruebas (proporcional)

Separada por tipo, **proporcional al proyecto**, **sin** automatización nueva de gran alcance ni
framework nuevo sólo para M8:

- **Inspección estática** (código/escena/prefab/config).
- **Pruebas manuales en editor** (recorrido por tramos, reproducibles y documentadas).
- **Play Mode** mediante la **infraestructura existente si está disponible**; en **ausencia de una
  suite PlayMode verificada**, usar **procedimientos manuales reproducibles y documentados**. No se
  crea un framework nuevo.
- **Pruebas de build** y **smoke test** (arranque, escenas, flujo iniciable).
- **Regresión EditMode:** suite existente. **La Fase 0 registra el conteo real actual** y explica
  cualquier diferencia con el baseline histórico (601 al cierre de M7); **el baseline histórico no se
  altera**.
- **Recorrido end-to-end** interno.
- **Playtest externo** (Fase 6).

**Puntos de ejecución obligatorios de la suite EditMode:**
- **pruebas dirigidas** después de **cada corrección**;
- **suite completa** al **terminar la Fase 2**;
- **suite completa** antes de **Gate D**;
- **suite completa** después de las **correcciones de la Fase 7**;
- **suite completa** antes de **Gate H**.

Si una incidencia afecta un **sistema sin cobertura automatizada**, se exige una **regresión manual
reproducible y documentada** (pasos, esperado, real, evidencia).

**Pruebas obligatorias tras cambios en:** escenas → smoke + recorrido del tramo; transiciones →
recorrido de la transición en ambos extremos soportados; `CreatureBrain`/estados → EditMode +
recorrido; `CreatureMovement` → EditMode + verificación de movimiento; combate → recorrido del
encuentro + EditMode; restauración → recorrido + EditMode; vinculación → recorrido `Bonding→Bonded` +
EditMode; sesión → verificación del flag + no-persistencia; UI → legibilidad de prompt/ficha/ECO;
build → smoke + recorrido desde ejecutable.

## 11. Impacto técnico (archivos y sistemas)

- **Probablemente se inspeccionarán:** todos los sistemas del baseline (§4); escenas
  `CamaraPreservacion/CorredorTecnico/ClaroExterior/Bootstrap`; prefabs `Verak/VerakAltered/Player`;
  `Verak.asset`; Build Settings/Build Profile; UI de `ClaroExterior`.
- **Sólo se modificarán si existe una incidencia confirmada:** el archivo/escena/prefab del sistema
  afectado, con owner verificado (§8).
- **Escenas:** modificación sólo por incidencia (wiring/transición/spawn).
- **Prefabs:** modificación sólo por incidencia.
- **ScriptableObjects:** `Verak.asset` u otros datos, sólo por incidencia.
- **Configuración de build:** verificar (F0/F4); ajustar sólo por incidencia.
- **Documentación de QA:** nueva (matriz, checklists, reporte de playtest, reporte de cierre) —
  ubicación sugerida `Docs/Technical/QA/` (a confirmar con la convención al crearla).
- **Artefactos de evidencia:** builds, logs, capturas.

> No se presenta como cambio obligatorio ningún archivo que sólo requiere inspección. No se inventan
> rutas: los nombres provienen del repo (§4).

## 12. Riesgos y mitigaciones

| Riesgo | Mitigación |
|---|---|
| Expansión de alcance durante el pulido | §3 No objetivos + regla de causa mínima (F2/F7); toda idea nueva → "después del prototipo" |
| Refactor accidental | Una incidencia por cambio; prohibido refactor general (F2/F7) |
| Correcciones que rompen ownership | Verificación de ownership obligatoria por corrección (§8) |
| Build distinta al HEAD validado | Evidencia de commit de origen + correspondencia con HEAD (F4/F8) |
| DEC-P05 sin resolver | Gate E bloquea el playtest hasta la decisión del Director |
| Playtest guiado involuntariamente | Guion del moderador + información prohibida (F6) |
| Muestra insuficiente | Objetivo documental de 5; excepción sólo por decisión del Director |
| Datos de playtest inconsistentes | Plantillas fijas + datos mínimos por sesión (F6) |
| Corrección mayor sin repetición de prueba | Regla de repetición ante cambio sustancial de comprensión (F7) |
| Persistencia accidental | Verificación de no-persistencia en F3/F8 (reinicio de Play → flag en false) |
| Divergencia editor ↔ build | Smoke test + recorrido desde ejecutable (F4/F8) |
| Fallos de transición | Validación de transiciones/retornos soportados (F1/F3) |
| Falsos positivos por placeholders | Distinguir "placeholder" de "defecto" en el triage (F1/F7) |

## 13. Criterios de calidad documental (meta)

Esta SPEC es **ejecutable, verificable y trazable al GDD M8**; específica sobre gates; prudente con
nombres técnicos (§4); consistente con el repo; clara sobre qué es **inspección** vs **modificación**
y sobre qué requiere **decisión del Director**. Evita frases ambiguas ("pulir lo necesario", "arreglar
bugs", "hacer pruebas", "mejorar la experiencia", "optimizar"); cada criterio es observable.

---

**Estado: Aprobada / Congelada.** Aprobada por el Director el 2026-07-29. La ejecución puede comenzar
en Fase 0. Esta SPEC no se modifica sin una revisión documental aprobada.
