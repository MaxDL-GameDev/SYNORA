# SYNORA — Enmienda técnica TEC-001

**Documento base:** `SYNORA_SPEC_M0-M1_Movimiento_v0.4.md` (CONGELADA)
**Tipo:** Enmienda técnica (no reabre decisiones de diseño ni de arquitectura)
**Identificador:** TEC-001
**Alcance:** Sustitución de la versión base del Editor. Ningún otro punto de M0-M1 cambia.
**Estado del código:** No se ha escrito código.

> Esta enmienda **no modifica** el cuerpo congelado de la v0.4. Sólo **sobrescribe** las referencias a la versión del Editor allí presentes y añade una política de paquetes y un reporte de verificación de APIs. En caso de conflicto sobre la versión del Editor, prevalece TEC-001.

---

## 1. Cambio introducido

| Antes (v0.4) | Después (TEC-001) |
|---|---|
| Editor inicial: **Unity 6.3 LTS** | Editor base oficial del proyecto: **Unity 6.5, rama 6000.5**, usando el **parche estable más reciente** disponible en Unity Hub |

**Referencias de la v0.4 que quedan sobrescritas por esta enmienda** (sólo el valor de la versión; el resto del texto sigue vigente):
- Encabezado → campo "Editor inicial".
- A1 (tabla de stack) → fila "Editor".
- Parte C, paso 1 → "en Unity 6.3 LTS" pasa a "en Unity 6.5 (6000.5)".
- Parte F → decisión cerrada "Editor Unity 6.3 LTS" pasa a "Editor Unity 6.5 (6000.5)".

## 2. Versión exacta instalada (Regla 1)

Campo a **registrar en el momento de la instalación** (no puedo fijarlo desde aquí; debe copiarse literal desde Unity Hub → Installs):

```
Unity 6.5 — versión exacta instalada: 6000.5.___f___   (rellenar con el parche estable instalado)
Fecha de instalación: ____-__-__
Origen: Unity Hub (canal estable)
```

Una vez rellenado, este valor es el número de versión oficial del proyecto y debe reflejarse en el registro maestro de decisiones (GDD §33).

## 3. Reglas acatadas

- **Regla 2 — Sin alpha/beta/6.6+.** Sólo parche **estable** de la rama **6000.5**. Prohibido alpha, beta y cualquier 6.6+ (6000.6+).
- **Regla 4 — Paquetes.** Se usan las versiones de paquetes **compatibles recomendadas por el propio Editor 6.5** (Package Manager con "recommended/verified"). No se fijan versiones a mano.
- **Regla 5 — Sin actualizaciones manuales** de paquetes salvo que la SPEC lo exija. La SPEC v0.4 **no** exige ninguna versión de paquete concreta (ADR-002 ya dejó el Input System "en la versión recomendada por el Editor"), por lo que **no hay** actualizaciones manuales pendientes.
- **Regla 6 — Sin otros cambios.** Ninguna otra decisión de M0-M1 se toca (arquitectura de escenas, física, cámara, transición atómica, colisiones, nomenclatura, pruebas, ADRs, pendientes de la Parte F: todo permanece igual).
- **Regla 7 — v0.4 congelada.** El cuerpo de la v0.4 no se edita; esta enmienda se añade aparte.

## 4. Verificación de APIs (Reglas 3 y 8) — reporte

> **Aviso de fiabilidad.** No puedo confirmar detalles específicos de Unity 6.5 desde mi conocimiento (queda en el límite o por encima de mi corte de datos). La columna "Evaluación" es mi mejor juicio basado en que son APIs estables introducidas/consolidadas en Unity 6; **cada fila debe verificarse contra la documentación de Unity 6.5 o el Editor instalado antes de implementar.** No se ha detectado ninguna ruptura confirmada, pero tampoco puedo garantizar su ausencia sin esa verificación.

| API / propiedad usada en la SPEC | Dónde | Evaluación (a verificar en 6.5) |
|---|---|---|
| `Rigidbody2D.linearVelocity` | B3, B4, B8, D2 | Nombre vigente desde Unity 6 (sustituyó a `velocity`, deprecado). Se espera sin cambios. **Verificar.** |
| `Rigidbody2D.linearDamping` | B4 | Nombre vigente desde Unity 6 (sustituyó a `drag`). Se espera sin cambios. **Verificar.** |
| `Rigidbody2D.bodyType` = Dynamic / Static | B4, B5 | Estable. **Verificar.** |
| `Rigidbody2D.position` (reposicionar en spawn) | B8 | Estable. **Verificar.** |
| `TilemapCollider2D` → **Composite Operation = Merge** | B5 | Enum "Composite Operation" que reemplazó a "Used By Composite" en Unity 6. Confirmar que el nombre/enum sigue igual en 6.5. **Verificar (prioritario).** |
| `CompositeCollider2D` → **Geometry Type = Polygons** | B5 | Estable. **Verificar.** |
| Input System: `InputAction.Enable/Disable`, callbacks `performed`/`canceled`, `InputActionReference` | B3, B6 | Estable en Input System 1.x. **Verificar la versión recomendada por 6.5 y que la API no cambió.** |
| *Pixel Perfect Camera* (URP) + `Reference Resolution`/`PPU` | B7 | Componente de URP 2D. **Verificar nombres de propiedades y que sigue en el paquete URP recomendado por 6.5.** |
| `CameraFollow` en `LateUpdate` + `Camera` ortográfica | B7 | APIs base de Unity, sin cambios esperados. **Verificar.** |
| `Application.targetFrameRate` (prueba de frame rate) | D1, D3 | Estable, sin cambios esperados. |
| `SceneManager.LoadSceneAsync`, `LoadSceneMode.Single` | B8 | Estable. **Verificar.** |
| **Build Profiles** (sustituto de Build Settings) | A6, C | Introducido en Unity 6; presente en 6.x. **Verificar el flujo exacto en 6.5.** |
| `OnValidate` (validación de `SpawnPoint`) | B8 | API de editor estable. |

**Diferencias técnicas detectadas con impacto en la SPEC:** **ninguna confirmada.** Los puntos marcados "prioritario"/"Verificar" son tareas de comprobación previas a la implementación, no cambios ya conocidos. Si al verificar contra 6.5 alguna propiedad hubiera cambiado de nombre o comportamiento (candidatos más probables: el enum *Composite Operation* del `TilemapCollider2D` y los nombres de propiedades del *Pixel Perfect Camera* de URP), se registrará una enmienda **TEC-002** antes de tocar código; el resto de la SPEC no se vería afectado.

## 5. Efecto sobre el estado de la SPEC

La SPEC M0-M1 sigue **congelada en v0.4** salvo por esta enmienda **TEC-001**, que fija Unity 6.5 (6000.5) como versión base. No hay cambios de alcance ni de diseño. La implementación puede comenzar una vez: (a) registrada la versión exacta instalada (§2), y (b) completada la verificación de APIs de §4 contra el Editor 6.5.

**FIN DE LA ENMIENDA TEC-001.**
