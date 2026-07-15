# SYNORA — Especificación Técnica M0 + M1

**Configuración de proyecto · Estructura inicial · Movimiento · Cámara · Colisiones · Transición entre áreas**

| Campo | Valor |
|---|---|
| Documento | `SYNORA_SPEC_M0-M1_Movimiento_v0.4.md` |
| Versión | **0.4 — APROBADA Y CONGELADA para M0-M1** (parche final aplicado; v0.1–v0.3 se conservan) |
| Documento padre | SYNORA — GDD del Prototipo Mecánico v0.1 |
| Rol de autor | Technical Lead / Senior Unity Developer |
| Editor inicial | **Unity 6.3 LTS** (último parche disponible en Unity Hub), C# |
| Plataforma | PC (Windows), desarrollo **offline** |
| Estado | **Congelada.** Lista para implementar M0 y M1. No se ha escrito código. |
| Hitos cubiertos | M0 (Proyecto base) y M1 (Movimiento, cámara, colisiones y transición entre áreas) |
| Fuera de alcance | Combate, criaturas, interacción, observación, narrativa, restauración, vinculación, representación visual y animación del jugador, arte definitivo, networking y cualquier sistema futuro |

> **Versiones de paquetes.** El Editor se fija en Unity 6.3 LTS (último parche en Unity Hub). Las versiones de paquetes **no se fijan a mano**: se usa la versión compatible que recomiende el Editor (incluido el Input System).

---

## 0. Límites de autoridad de esta especificación

- No cambia gameplay, narrativa, dirección artística ni alcance del prototipo. Sólo define *cómo* se implementa lo aprobado en M0 y M1.
- No implementa sistemas futuros, no instala paquetes adicionales, no optimiza prematuramente y prioriza claridad y mantenibilidad.
- Toda decisión que roce diseño, sensación o aspecto visual se marca **`⚠ Requiere aprobación del Game Director`** y se recoge en la Parte F.
- No se escribe código en esta versión. La Parte C entrega la secuencia exacta de implementación. Al estar la spec **congelada**, la implementación puede comenzar tras la confirmación de este parche.

---

## Registro de cambios (v0.3 → v0.4) — parche final

| # | Corrección aplicada |
|---|---|
| 1 | **Transición atómica.** `AreaTransition` ya no escribe en el contexto; llama a `SceneLoader.TryLoad(destinationScene, destinationSpawnId)`. `SceneLoader` comprueba primero si hay carga activa: si rechaza, **no** toca el contexto; si acepta, escribe el spawn ID, bloquea nuevas solicitudes e inicia la carga. |
| 2 | **Config de colisiones Tilemap** explícita: `TilemapCollider2D` Composite Operation = **Merge**; `CompositeCollider2D` Geometry Type = **Polygons**; `Rigidbody2D` Body Type = **Static**. |
| 3 | **`.gitattributes` simplificado:** `.unity`/`.prefab`/`.asset` como `text eol=lf`; **eliminado** `merge=unityyamlmerge`. `UnityYAMLMerge` se documenta como *mergetool* configurado **por separado**. |
| 4 | **Editor:** Unity **6.3 LTS** (último parche en Unity Hub). No se fija Input System 1.8+; se usa la versión compatible recomendada por el Editor. |
| 5 | **"Build Settings" → "Build Profiles"** donde corresponde. |
| 6 | **Validación de `SpawnPoint`:** IDs no vacíos y únicos por escena; exactamente un punto Default; lista serializada en `PlayerSpawner`; sin búsquedas globales. |
| 7 | **Cardinalización diagonal** definida: eje dominante; en empate, conservar la orientación anterior si coincide; sin orientación previa, priorizar vertical. |
| 8 | **`CameraFollow` centra** la cámara si la habitación es menor que el viewport en cualquier eje. Pruebas M1 iniciales en **16:9** a **1280×720** y **1920×1080**. |
| 9 | **Pruebas automáticas obligatorias reducidas a 4:** normalización diagonal, cálculo de velocidad, bloqueo de cargas duplicadas, consumo y limpieza de `SceneTransitionContext`. Colisiones, cámara, transiciones reales y frame rate quedan como **pruebas manuales documentadas**. |

---

# PARTE A — M0: Fundación del proyecto

> Resultado verificable (GDD §34): *repositorio, Unity configurado y build vacía ejecutable*. Estimación: 4–6 h.

## A1. Stack (acordado, sin paquetes adicionales)

| Elemento | Decisión | ADR |
|---|---|---|
| Editor | **Unity 6.3 LTS** (último parche en Unity Hub) | — |
| Plantilla | 2D (URP) | ADR-001 |
| Input | Input System (versión recomendada por el Editor) | ADR-002 |
| Movimiento | Rigidbody2D (dinámico) por velocidad | ADR-003 |
| Cámara | `CameraFollow` propio + *Pixel Perfect Camera* de URP | ADR-008 |
| Colisiones de escenario | Tilemap + `CompositeCollider2D` | ADR-007 |
| Control de versiones | Git + Git LFS | ADR-010 |
| Espacio de color | Linear (provisional, revisión con arte real) | ADR-011 |

No se añade Cinemachine, Addressables ni ningún otro paquete. El único cambio respecto a la plantilla es habilitar el Input System (ADR-002).

## A2. Estructura de carpetas de assets

Nombres sin prefijos; la carpeta indica el tipo. Sólo carpetas con contenido real en M0-M1.

```
Assets/
├─ Scripts/
│  ├─ Core/
│  ├─ Gameplay/
│  │  └─ Player/
│  ├─ Systems/
│  └─ Data/
├─ Prefabs/
│  ├─ Player.prefab
│  ├─ CameraRig.prefab
│  ├─ AreaTransition.prefab
│  └─ SpawnPoint.prefab
├─ Configuration/                 # config de diseño/ajuste (ScriptableObject)
│  └─ PlayerMovement.asset
├─ RuntimeData/                   # contexto transitorio de ejecución (ScriptableObject)
│  └─ SceneTransitionContext.asset
├─ Input/
│  └─ Controls.inputactions
├─ Scenes/
│  ├─ Bootstrap.unity
│  ├─ CamaraPreservacion.unity
│  ├─ CorredorTecnico.unity
│  └─ ClaroExterior.unity
├─ Art/
│  ├─ Sprites/                    # placeholders
│  └─ Tiles/
├─ Settings/                      # UniversalRP y 2D Renderer Data
└─ Tests/
   ├─ EditMode/
   └─ PlayMode/
```

**Carpetas de scripts que NO se crean todavía** (se crearán con su contenido en sus hitos): `Gameplay/Creatures`, `Gameplay/Interaction`, `Gameplay/Exploration`, `Gameplay/Combat`, `Gameplay/Restoration`, `UI`, `Debug`, `Editor`. Se documentan sólo como mapa de la arquitectura escalable.

`Configuration/` guarda datos de diseño/ajuste (persistentes). `RuntimeData/` guarda contexto transitorio (no persistente). Assets de terceros aislados en `Assets/ThirdParty/`.

## A3. Git, LFS y serialización

En *Project Settings → Editor*: Asset Serialization → **Force Text**; Version Control → **Visible Meta Files**.

**`.gitignore`** (raíz):

```gitignore
[Ll]ibrary/
[Tt]emp/
[Oo]bj/
[Bb]uild/
[Bb]uilds/
[Ll]ogs/
[Uu]ser[Ss]ettings/
[Mm]emoryCaptures/
.vs/
.idea/
.vscode/
*.csproj
*.sln
*.user
```

**`.gitattributes`** — escenas/prefabs/assets como texto con `eol=lf`; binarios y `.aseprite` por LFS:

```gitattributes
*.cs       text diff=csharp

*.unity    text eol=lf
*.prefab   text eol=lf
*.asset    text eol=lf

*.png      filter=lfs diff=lfs merge=lfs -text
*.psd      filter=lfs diff=lfs merge=lfs -text
*.aseprite filter=lfs diff=lfs merge=lfs -text
*.wav      filter=lfs diff=lfs merge=lfs -text
*.ogg      filter=lfs diff=lfs merge=lfs -text
```

**`UnityYAMLMerge`** se configura **por separado** como *mergetool* en la configuración de Git del desarrollador (p. ej. `merge.tool` + su ruta), **no** como atributo `merge=` en `.gitattributes`. Así se resuelven conflictos de escenas/prefabs con la herramienta de Unity sin acoplar el repositorio a una ruta local.

## A4. Project Settings clave

| Ajuste | Valor | Motivo |
|---|---|---|
| Editor → Enter Play Mode Options | **Predeterminadas** (Domain Reload y Scene Reload activados) | Comportamiento estándar; no se depende de estado estático entre sesiones |
| Player → Color Space | Linear (provisional; ADR-011) | Coherencia con futura iluminación 2D; revisión con arte real |
| Player → Default resolution | 1920×1080, ventana + fullscreen | Legibilidad a 1080p (GDD §23) |
| Player → Company/Product | SynoraDev / SYNORA Prototipo | Identidad de build |
| Time → Fixed Timestep | 0.02 (50 Hz) | Base del paso de física |
| Physics 2D → Simulation Mode | FixedUpdate | Movimiento consistente y desacoplado de la tasa de cuadros |

## A5. Preset de importación pixel art

Preset de textura por defecto para `Art/Sprites` y `Art/Tiles` (GDD §28):

| Propiedad | Valor |
|---|---|
| Texture Type | Sprite (2D and UI) |
| Pixels Per Unit | 32 (1 tile = 1 unidad) |
| Filter Mode | Point (no filter) |
| Compression | None |
| Mip Maps | Off |

## A6. Criterios de aceptación de M0

- Repo con `.gitignore`, `.gitattributes` y LFS operativo.
- Proyecto abre sin errores ni warnings; URP 2D e Input System activos.
- Serialización Force Text; meta files visibles.
- Existe `Bootstrap.unity`.
- **Build de Windows** (vía Build Profiles) compila y arranca fuera del editor.
- ADRs registradas en el registro de decisiones del proyecto.

---

# PARTE B — M1: Movimiento, cámara, colisiones y transición

> Resultado verificable (GDD §34): *el protagonista recorre las tres áreas con cámara y colisiones*. Estimación: 8–12 h.
> Criterio de sensación no negociable (GDD §14): *moverse debe sentirse preciso antes que bonito*. Sin dash, salto, stamina ni física compleja.

## B1. Arquitectura de escenas — provisional vs extensible

| Aspecto | Decisión M0-M1 | Clasificación |
|---|---|---|
| Escena de arranque | `Bootstrap.unity` con inicialización mínima; carga la primera área | Extensible (punto de entrada estable) |
| Áreas del prototipo | Tres escenas autocontenidas, cada una jugable en aislamiento | Aprobada (provisional del prototipo) |
| Carga entre áreas | `SceneManager` en modo Single (corte directo, sin fundido en M1) | Aprobada |
| Carga aditiva / streaming | No ahora; la arquitectura la permite después sin tocar gameplay | Extensible (objetivo futuro) |
| Addressables | No se introduce | — |

Cada área contiene su propio `Player` y `CameraRig` (instancias de prefab), tilemaps, límites de sala y puntos de aparición: así cada escena es jugable en aislamiento (contrato GDD §30). El único acoplamiento entre áreas es el nombre de escena destino y el id de spawn, pasados por datos.

## B2. Descomposición por sistemas y dependencias

```
[Input]  →  [Movimiento (Rigidbody2D)]  →  [Orientación]
                     │
                     ▼
              [Colisiones 2D]  ←  [Escenario / Tilemap]
                     │
                     ▼
          [Cámara (follow + límites de sala + pixel perfect)]
                     │
                     ▼
   [Transición de áreas]  ←  [SceneLoader (atómico) + SpawnPoint + SceneTransitionContext]
```

Orden de construcción: **Input → Movimiento → Colisiones/Escenario → Cámara → Transición.**

## B3. Componentes del jugador

Sin `PlayerController` monolítico. Responsabilidades separadas en `Player.prefab`, comunicadas por referencias serializadas (sin `GameObject.Find`, `FindObjectOfType` ni tags). La representación visual y la animación quedan fuera de M1.

| Componente | Carpeta | Responsabilidad única (M1) | Referencias serializadas |
|---|---|---|---|
| `PlayerInputReader` | `Gameplay/Player` | Fuente única de input. Habilita la acción `Move` en `OnEnable` y la deshabilita en `OnDisable`. Guarda el último `Vector2` recibido por callbacks y lo pone a **cero** al cancelar, deshabilitarse o perder el control. Expone ese valor. | `InputActionReference` (Move) |
| `PlayerMotor` | `Gameplay/Player` | Locomoción física. En `FixedUpdate` consume el valor del reader, **limita su magnitud a 1** y aplica `Rigidbody2D.linearVelocity = valor * MoveSpeed`. Nunca escribe `Transform`. | `PlayerInputReader`, `PlayerMovement`, `Rigidbody2D` (RequireComponent) |
| `PlayerOrientation` | `Gameplay/Player` | Observa el input expuesto por el reader y conserva la **última dirección no nula** (cardinalizada a 4 direcciones, provisional). No duplica el input ni mueve nada. | `PlayerInputReader` |

**Cardinalización diagonal (regla exacta):** a partir del vector de entrada, la orientación de 4 direcciones se resuelve así:

1. **Eje dominante:** se elige el eje con mayor magnitud (`|x|` vs `|y|`).
2. **En empate** (`|x| == |y|`, ambos no nulos): se **conserva la orientación anterior** si coincide con alguno de los dos ejes empatados.
3. **Sin orientación previa aplicable** (primer movimiento o la anterior no coincide): se **prioriza el eje vertical**.

**Notas de contrato** (sin implementación en esta versión): el reader es el único que toca el Input System y no hay segunda lectura en `Update`; el motor no multiplica por `Time.deltaTime` (la física integra por el paso fijo) y limita la magnitud a 1 para que la diagonal no sea más rápida (GDD §14).

## B4. Física del jugador

| Propiedad del `Rigidbody2D` | Valor recomendado | Motivo |
|---|---|---|
| Body Type | Dynamic | Deslizamiento y respuesta de colisión sin física compleja |
| Gravity Scale | 0 | Perspectiva cenital |
| Freeze Rotation Z | Activado | El personaje no rota por colisión |
| Collision Detection | Continuous | Evita atravesar muros a velocidad |
| Interpolation | Interpolate | Movimiento visual suave entre pasos de física |
| Sleeping Mode | Never Sleep | La velocidad se aplica siempre |
| Linear Damping | 0 | Velocidad constante (sin inercia) |

- **Colisor del jugador:** `CapsuleCollider2D` pequeño desplazado a la base del sprite (sensación cenital 3/4).
- Reglas duras: el jugador nunca modifica `Transform` para desplazarse; el input se lee independientemente del movimiento físico (ADR-004).

## B5. Colisiones y escenario (ADR-007)

Cada escena tiene, sobre un `Grid`:

- **Tilemap "Ground"** — sólo visual.
- **Tilemap "Collision"** — con la siguiente configuración exacta:
  - `TilemapCollider2D` → **Composite Operation = Merge**.
  - `CompositeCollider2D` → **Geometry Type = Polygons**.
  - `Rigidbody2D` → **Body Type = Static**.

Aquí van muros, mobiliario y vegetación densa (GDD §14). El composite fusiona los tiles en pocos polígonos: eficiente y sin costuras.

**Capas y matriz de colisión (mínimo M1):**

| Layer | Uso | Colisiona con |
|---|---|---|
| `Player` | Protagonista | `Environment`; overlap (trigger) con `Transitions` |
| `Environment` | Muros/mobiliario/vegetación | `Player` |
| `Transitions` | Volúmenes de transición | `Player` (sólo trigger) |

Todo cruce no listado se desactiva. La capa `Transitions` hace que sólo el jugador dispare la transición, evitando comprobaciones por tag. La capa `Creatures` no se crea todavía.

## B6. Input

Asset `Controls.inputactions`, Action Map **Gameplay**, **una sola acción en M1**:

| Acción | Tipo | Bindings PC |
|---|---|---|
| `Move` | Value / Vector2 | WASD (2D Vector Composite) + Flechas |

`Interact` y `Pause` (GDD §14) no se definen aún: pertenecen a hitos posteriores.

## B7. Cámara (ADR-008)

Requisito GDD §14: *seguimiento suave con límites por habitación*. Sin Cinemachine.

- **Unity Camera** en `CameraRig.prefab` con *Pixel Perfect Camera* de URP: Reference Resolution **480×270**, PPU **32** (escala entera ×4 a 1080p). Alternativas de prueba: 384×216 y 320×180. `⚠ Requiere aprobación del Game Director` la resolución definitiva (pendiente de pruebas).
- **`CameraFollow`** (`Systems/`): en `LateUpdate`, sigue la posición interpolada del jugador y **acota** el centro de cámara a los límites rectangulares de la sala.
  - **Suavizado configurable, valor inicial 0** (seguimiento inmediato). Primero se valida el seguimiento **pixel-perfect sin jitter**; el suavizado se sube sólo si las pruebas lo permiten.
  - **Habitación menor que el viewport:** si la sala es más pequeña que el viewport en algún eje, la cámara se **centra** en ese eje (no fuerza el borde ni deja ver fuera de la sala).
  - Referencias serializadas: objetivo (Player de la escena) y límites de sala.
- **Contexto de pruebas M1:** inicialmente en **16:9**, a **1280×720** y **1920×1080**.

Las salas del GDD §24 son rectangulares, así que un límite rectangular por escena basta y evita añadir paquetes.

## B8. Transición entre áreas (atómica)

| Pieza | Carpeta | Responsabilidad única |
|---|---|---|
| `SceneLoader` | `Systems/` | Único punto que decide y ejecuta la carga. Expone `TryLoad(destinationScene, destinationSpawnId)`. Comprueba **primero** si hay carga activa: si la hay, **rechaza** la solicitud y **no** modifica el contexto; si no, **escribe el spawn ID** en el contexto, **bloquea** nuevas solicitudes e **inicia** la carga (Single, asíncrona). |
| `AreaTransition` | `Systems/` | Volumen *trigger* (capa `Transitions`). Al entrar el jugador, llama a `SceneLoader.TryLoad(escenaDestino, spawnDestino)`. **No** escribe en el contexto. Sólo detecta y solicita. | 
| `SpawnPoint` | `Systems/` | Marcador en escena con un `id` y bandera de *Default*. |
| `PlayerSpawner` | `Systems/` | Mantiene la **lista serializada** de los `SpawnPoint` de su escena. Al iniciar, coloca al `Player` en el spawn correspondiente **mediante `Rigidbody2D.position`**, pone `linearVelocity` a **cero**, **no** usa el movimiento normal de `PlayerMotor` y luego **consume (limpia)** el id del contexto. |
| `SceneTransitionContext` | `Data/` (instancia en `RuntimeData/`) | Contexto transitorio compartido (ScriptableObject) que transporta el id de spawn entre cargas Single. |

**Flujo atómico:** el jugador entra en un `AreaTransition` → éste llama a `SceneLoader.TryLoad(...)` (referencia serializada, misma escena) → `SceneLoader` comprueba si hay carga activa: **rechazo** → no cambia nada; **aceptación** → escribe el spawn ID, bloquea e inicia la carga → en la escena destino, `PlayerSpawner` lee el id, reposiciona vía `Rigidbody2D.position`, limpia velocidad y **consume** el id. Así el estado del contexto y el bloqueo de carga cambian **juntos** o **no cambian**.

**Validación de `SpawnPoint`** (en editor, vía `OnValidate` de `PlayerSpawner`, sin búsquedas globales):

- IDs **no vacíos** y **únicos por escena**.
- **Exactamente un** punto marcado como Default.
- La lista de `SpawnPoint` es **serializada** en `PlayerSpawner` (referencias explícitas de la propia escena; no se busca en runtime).

**Naturaleza de `SceneTransitionContext` (honesta):** es **estado compartido transitorio**, no "ausencia de estado global". Se mantiene manejable con tres reglas explícitas: id de spawn **no serializado**; **se limpia tras consumirse** (lo borra `PlayerSpawner`); y **`Reset()` explícito** invocado desde `Bootstrap` al arrancar. Sólo `SceneLoader` lo escribe (atómicamente) y sólo `PlayerSpawner` lo lee y limpia. En la migración aditiva futura se sustituiría por el paso de un argumento en memoria.

## B9. Configuración de movimiento

`PlayerMovement` (ScriptableObject en `Data/`; instancia en `Configuration/PlayerMovement.asset`), **sólo** con lo que M1 necesita:

| Campo | Tipo | Descripción |
|---|---|---|
| `MoveSpeed` | float (u/seg) | Velocidad constante. **Valor inicial de prueba aprobado: 4.5.** El definitivo se ajusta con pruebas (no bloqueante). |

Sin campos especulativos. `MoveSpeed` vive en el asset para no dejar un número mágico de balance en el código.

## B10. Criterios de aceptación de M1

- Movimiento en 4 direcciones (WASD y flechas); la diagonal no es más rápida.
- Velocidad constante e independiente del frame rate.
- El jugador no atraviesa muros, mobiliario ni vegetación; desliza limpio.
- Cámara con seguimiento (suavizado 0 inicial) **sin jitter**, sin salir de la sala, y **centrada** cuando la sala es menor que el viewport.
- Imagen pixel-perfect a 1080p (sin suavizado).
- Recorrido Cámara → Corredor → Claro y regreso, con aparición correcta en ambos sentidos.
- Cada escena de área jugable en aislamiento.
- Build de Windows recorre las tres áreas fuera del editor.

---

# PARTE C — Secuencia exacta de implementación (sin código)

## M0
1. Crear proyecto con la plantilla **2D (URP)** en **Unity 6.3 LTS**.
2. Confirmar/activar el **Input System** (versión recomendada por el Editor). No añadir otros paquetes.
3. Aplicar Project Settings de A4 (Enter Play Mode predeterminadas, Color Space provisional, resolución, Fixed Timestep).
4. Crear `.gitignore` y `.gitattributes` (A3); configurar `UnityYAMLMerge` como *mergetool* aparte; `git init`; `git lfs install`; commit inicial.
5. Crear el árbol de carpetas de A2 (incluyendo `Configuration/` y `RuntimeData/`).
6. Crear el Preset de importación pixel art (A5).
7. Crear `Bootstrap.unity` mínima. **Build de Windows** (Build Profiles) y ejecutar fuera del editor → **M0 terminado**.

## M1
8. Crear `Controls.inputactions` con la acción `Move` (WASD + flechas).
9. Crear `Configuration/PlayerMovement.asset` con `MoveSpeed = 4.5`.
10. Crear `Player.prefab`: `Rigidbody2D` (B4), `CapsuleCollider2D`, `SpriteRenderer` placeholder y los componentes `PlayerInputReader`, `PlayerMotor`, `PlayerOrientation`; cablear referencias serializadas.
11. Validar el movimiento en una escena gris (diagonal = cardinal, en `FixedUpdate`, sin asignaciones por frame).
12. Crear `CameraRig.prefab`: cámara con *Pixel Perfect Camera* (**480×270** / PPU 32) + `CameraFollow` (**suavizado = 0**, centrado si la sala es menor que el viewport). Validar seguimiento pixel-perfect sin jitter a 1280×720 y 1920×1080 (16:9).
13. Crear las tres escenas de área a tamaño del GDD §24, con `Grid` + Tilemap Ground + Tilemap Collision (B5), límites de sala y `SpawnPoint`s. Colocar instancias de `Player` y `CameraRig` y cablear.
14. Crear `RuntimeData/SceneTransitionContext.asset`; colocar `AreaTransition` en los umbrales (con escena y spawn destino, llamando a `SceneLoader.TryLoad`); añadir `PlayerSpawner` (con su lista serializada de `SpawnPoint`) y `SceneLoader` por escena. `Bootstrap` invoca `SceneTransitionContext.Reset()` al arrancar.
15. Configurar capas (`Player`, `Environment`, `Transitions`) y su matriz; añadir escenas a **Build Profiles**; `Bootstrap` carga la primera área.
16. Recorrer Cámara → Corredor → Claro y regreso; validar aparición correcta en ambos sentidos.
17. Escribir y pasar las **4 pruebas automáticas** de D2; ejecutar las pruebas **manuales** documentadas de D3.
18. Verificar la Definition of Done y las métricas; **build de Windows** que recorre las tres áreas → **M1 terminado**.

---

# PARTE D — Métricas, pruebas y Definition of Done

## D1. Métricas y validación

| Métrica | Cómo se comprueba | Umbral M1 |
|---|---|---|
| Movimiento consistente y desacoplado de la tasa de cuadros | Recorrer **10 s** en línea recta a **30, 60 y 120 FPS** (fijando `Application.targetFrameRate`) y comparar la distancia recorrida | Diferencia máxima **≈ 1 %** |
| Igualdad cardinal / diagonal | Medir la velocidad resultante en `(1,0)` y `(1,1)` | Misma magnitud |
| Colisiones | Empujar contra muros y esquinas | Sin atravesar en condiciones normales |
| Cámara sin jitter | Inspección visual en movimiento a 1280×720 y 1920×1080 (suavizado 0) | Sin temblor de píxeles |
| Consola limpia | Reproducir el recorrido completo | 0 errores y 0 warnings |
| Asignaciones por frame | Profiler (GC Alloc), jugador en movimiento ya inicializado | 0 bytes/frame atribuibles al movimiento cuando sea razonable |
| Rendimiento en escena de prueba | Estable durante el recorrido | Estable (sin objetivos comerciales todavía) |

## D2. Pruebas automáticas obligatorias (Unity Test Framework) — exactamente cuatro

1. **Normalización diagonal:** una entrada `(1,1)` produce una velocidad de magnitud ≈ 1 (la diagonal no acelera).
2. **Cálculo de velocidad:** para cualquier entrada de magnitud ≥ 1, `|linearVelocity| == MoveSpeed`.
3. **Bloqueo de cargas duplicadas:** con una carga activa, una segunda llamada a `SceneLoader.TryLoad(...)` se **rechaza** y **no** modifica `SceneTransitionContext`.
4. **Consumo y limpieza de `SceneTransitionContext`:** tras aparecer, `PlayerSpawner` deja el contexto **limpio** (id consumido).

## D3. Pruebas manuales documentadas (M1)

- **Colisiones:** empujar contra muros, esquinas y vegetación densa; sin atravesar.
- **Cámara:** seguimiento sin jitter; sin salir de la sala; **centrado** cuando la sala es menor que el viewport; a 1280×720 y 1920×1080.
- **Transiciones reales:** recorrido bidireccional Cámara ↔ Corredor ↔ Claro con aparición correcta.
- **Frame rate:** métrica D1 (30/60/120 FPS, 10 s, ≤ ~1 %).

## D4. Definition of Done

M0 o M1 **no** están terminados si ocurre cualquiera de estos casos:

- Hay errores o warnings en consola.
- Se usan `GameObject.Find`, `FindObjectOfType` o búsquedas globales evitables.
- Existen números mágicos relevantes (valores de balance/ajuste fuera de la config).
- Hay responsabilidades mezcladas en un único componente.
- Hay código no utilizado.
- Quedan `TODO` pendientes dentro del alcance del milestone.
- El movimiento depende del frame rate.
- No se han probado diagonales, colisiones, bordes y transición entre zonas.

---

# PARTE E — Decisiones técnicas registradas (ADR)

**ADR-001 — Unity 2D con URP** · Contexto: prototipo 2D pixel art, se anticipa iluminación 2D (GDD §25, §28). · Decisión: plantilla 2D (URP) con 2D Renderer. · Razón: habilita luces 2D y *Pixel Perfect Camera* sin rehacer el pipeline. · Consecuencias: obliga a decidir el espacio de color (ADR-011). · Estado: **Aceptado**.

**ADR-002 — Input System** · Contexto: control de PC con posible reasignación futura. · Decisión: Input System nuevo (versión recomendada por el Editor). · Razón: acciones desacopladas, rebinding. · Consecuencias: único paquete a habilitar. · Estado: **Aceptado**.

**ADR-003 — Movimiento con Rigidbody2D** · Contexto: movimiento cenital con colisiones y deslizamiento, sin física compleja (GDD §14). · Decisión: `Rigidbody2D` dinámico (gravedad 0), por velocidad en `FixedUpdate`; nunca `Transform`. · Razón: deslizamiento correcto; movimiento consistente y desacoplado de la tasa de cuadros. · Consecuencias: colisores 2D bien configurados; velocidad por config. · Estado: **Aceptado**.

**ADR-004 — Separación entre lectura de input y locomoción** · Contexto: mantenibilidad y testabilidad. · Decisión: `PlayerInputReader` (fuente única de input) separado de `PlayerMotor`; flujo único sin copia en `Update`. · Razón: probar el motor sin dispositivos y cambiar la fuente sin tocar la locomoción. · Consecuencias: una referencia serializada entre ambos. · Estado: **Aceptado**.

**ADR-005 — Desarrollo offline y PC primero** · Contexto: prototipo local (GDD §5). · Decisión: offline, PC (Windows); sin backend ni paquetes online. · Razón: alcance y simplicidad. · Consecuencias: sin dependencias de red. · Estado: **Aceptado**.

**ADR-006 — Escenas autocontenidas en modo Single con carga aditiva futura** · Contexto: transición entre las tres áreas y prueba en aislamiento. · Decisión: escenas autocontenidas en Single (corte directo en M1); Bootstrap mínimo; sin Addressables; arquitectura preparada para aditivo. · Razón: simplicidad y testabilidad ahora; extensibilidad después. · Consecuencias: paso de spawn vía `SceneTransitionContext` (ADR-009). · Estado: **Aceptado** (provisional para el prototipo).

**ADR-007 — Colisiones de escenario con Tilemap + CompositeCollider2D** · Contexto: escenario de tiles. · Decisión: `TilemapCollider2D` (Composite Operation = Merge) + `CompositeCollider2D` (Geometry Type = Polygons) en `Rigidbody2D` Static. · Razón: colisión eficiente y sin costuras. · Consecuencias: tilemap de colisión dedicado. · Estado: **Aceptado**.

**ADR-008 — Cámara propia + Pixel Perfect (sin Cinemachine)** · Contexto: seguimiento suave con límites por sala sin instalar paquetes. · Decisión: `CameraFollow` propio (suavizado configurable, inicial 0; centrado si la sala es menor que el viewport) con *Pixel Perfect Camera* de URP. · Razón: cumple el requisito sin dependencias; salas rectangulares no necesitan confinador poligonal. · Consecuencias: Cinemachine queda como opción futura (requiere aprobación de paquete). · Estado: **Aceptado**.

**ADR-009 — Paso de spawn por contexto transitorio compartido (transición atómica)** · Contexto: transiciones bidireccionales sin singletons ni `Find`. · Decisión: `SceneTransitionContext` (ScriptableObject) como contexto transitorio compartido; id no serializado, limpiado tras consumirse, con `Reset()` explícito. **Sólo `SceneLoader` lo escribe, de forma atómica junto con el bloqueo de carga** (`TryLoad`). · Razón: estado compartido acotado, cambios atómicos, sin ocultar estado global. · Consecuencias: en migración aditiva se sustituiría por argumento en memoria. · Estado: **Aceptado** (provisional).

**ADR-010 — Git + Git LFS** · Contexto: versionado con binarios y escenas/prefabs. · Decisión: LFS para binarios (incluido `.aseprite`); escenas/prefabs/assets como texto con `eol=lf`; **`UnityYAMLMerge` configurado por separado como mergetool** (no como atributo `merge=`); Force Text. · Razón: repositorio portable y merges de escena seguros sin acoplar rutas locales. · Consecuencias: requiere `git lfs install` y configurar el mergetool en cada entorno. · Estado: **Aceptado**.

**ADR-011 — Espacio de color Linear (provisional)** · Contexto: URP 2D y futura iluminación 2D favorecen Linear. · Decisión: Linear, provisional. · Razón: coherencia con luces 2D futuras. · Consecuencias: cambiarlo tarde re-tinta todo; se revisará con arte real. · Estado: **Aceptado (provisional, revisión con arte real)**.

---

# PARTE F — Lista final de decisiones pendientes

Ya aprobadas (cerradas): orientación provisional a 4 direcciones; transiciones por corte directo en M1; Linear provisional; `MoveSpeed` inicial 4.5; escenas autocontenidas en Single; sin Cinemachine/Addressables/paquetes adicionales; Editor Unity 6.3 LTS.

**Pendientes (no bloquean el inicio de M0-M1):**

1. **Resolución de referencia Pixel Perfect definitiva** — inicial 480×270; alternativas de prueba 384×216 y 320×180. Se decide tras pruebas. `⚠ Requiere aprobación del Game Director`.
2. **Suavizado final de `CameraFollow`** — inicial 0; se subirá sólo si las pruebas de jitter lo permiten.
3. **Valor definitivo de `MoveSpeed`** — inicial 4.5 aprobado; el final se afina con pruebas.
4. **Confirmación definitiva del espacio de color** — Linear provisional; revisión con arte real. `⚠ Requiere aprobación del Game Director`.

---

# PARTE G — Fuera de alcance y puentes a hitos futuros

- **Representación visual y animación del jugador** → hito posterior. `PlayerOrientation` ya expone la última dirección no nula para consumirla sin refactor.
- Interacción contextual y prompt → **M2** (acción `Interact`).
- Criaturas / estados de Verak → **M3** (capa `Creatures` y carpeta `Gameplay/Creatures` con su contenido).
- Observación, combate, restauración, vinculación → **M4–M7**.
- Checkpoint, pausa y guardado → P1 (acción `Pause`).
- Arte definitivo → hasta **M7**; los placeholders de M1 se sustituyen sin reprogramar.

Ningún sistema posterior crea dependencias rígidas sobre M1: se conectan por componentes pequeños y referencias serializadas, coherente con el contrato del GDD §30.

---

**FIN DE LA ESPECIFICACIÓN — v0.4 CONGELADA PARA M0-M1.**
