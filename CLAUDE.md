# SYNORA — Instrucciones para Claude Code

## 1. Autoridad del proyecto

El usuario es el Director del proyecto y tiene la decisión final sobre cualquier cambio.

### Responsabilidades

**ChatGPT**
- Dirección creativa.
- Canon y lore.
- Historia.
- Game design.
- Biblia del proyecto.
- GDD.
- Alcance funcional.
- Revisión de documentos y coherencia global.

**Claude Code**
- Arquitectura técnica aprobada.
- Implementación en Unity.
- Código C#.
- Configuración técnica del proyecto.
- Pruebas.
- Documentación técnica.
- Mantenimiento del repositorio.

**Gemini**
- Referencias visuales.
- Concept art.
- Sprites.
- Fondos.
- UI y exploración artística bajo brief aprobado.

Claude Code no debe modificar gameplay, narrativa, canon, dirección artística ni alcance sin aprobación expresa del Director.

---

## 2. Estado actual del proyecto

- Proyecto: **SYNORA**
- Tipo: RPG 2D para un jugador.
- Perspectiva: cenital en tres cuartos.
- Estilo visual: pixel art.
- Plataforma inicial: Windows PC.
- Plataformas futuras: Android e iOS.
- Cooperativo: posibilidad posterior, fuera del alcance actual.
- Editor base: **Unity 6000.5.3f1**.
- Render pipeline: **Universal Render Pipeline 2D**.
- Desarrollo actual: offline.
- MCP aprobado: **MCP for Unity v10.1.0**.

La versión exacta del Editor registrada en `ProjectSettings/ProjectVersion.txt` es la fuente técnica de verdad.

No actualizar Unity, paquetes principales ni MCP sin aprobación expresa y un registro técnico de la decisión.

---

## 3. Documentos de autoridad

Antes de planear o implementar, leer los documentos disponibles bajo `Docs/`.

Orden de autoridad:

1. `Docs/Canon/SYNORA_Biblia_del_Proyecto_v3.0.docx`
2. `Docs/Design/SYNORA_GDD_Prototipo_Mecanico_v0.1.docx`
3. `Docs/Technical/SYNORA_SPEC_M0-M1_Movimiento_v0.4.md`
4. `Docs/Technical/SYNORA_SPEC_M0-M1_Movimiento_v0.4_ENMIENDA_TEC-001.md`
5. Este archivo `CLAUDE.md`

### Regla de conflicto

- La Biblia manda sobre canon, historia, tono y universo.
- El GDD manda sobre gameplay y alcance funcional.
- La SPEC manda sobre implementación de M0-M1.
- La enmienda TEC-001 sustituye únicamente la versión de Unity indicada por la SPEC.
- Este archivo define cómo debe trabajar Claude Code dentro del repositorio.

Si dos documentos parecen contradecirse, detenerse y pedir aprobación. No resolver silenciosamente.

---

## 4. Milestone autorizado

### Autorizado actualmente: M0 — Fundación del proyecto

M0 incluye únicamente:

- Verificar Unity 6000.5.3f1.
- Mantener URP 2D.
- Mantener Input System.
- Mantener MCP for Unity.
- Inicializar Git y Git LFS.
- Crear `.gitignore`.
- Crear `.gitattributes`.
- Configurar serialización Force Text y meta files visibles.
- Corregir Enter Play Mode a opciones predeterminadas.
- Configurar Company Name y Product Name.
- Crear la estructura mínima de carpetas aprobada.
- Crear el preset de importación de pixel art.
- Crear `Bootstrap.unity`.
- Configurar Build Profile para Windows.
- Generar y verificar una build vacía.
- Dejar la consola con cero errores y cero warnings.
- Registrar ADR y enmiendas técnicas aprobadas.

### Fuera de alcance de M0

No implementar:

- Movimiento.
- `PlayerInputReader`.
- `PlayerMotor`.
- `PlayerOrientation`.
- Cámara funcional.
- Colisiones de gameplay.
- Escenas de áreas.
- Transiciones.
- Criaturas.
- Interacción.
- Observación.
- Combate.
- Restauración.
- Vinculación.
- Inventario.
- Guardado.
- UI funcional.
- Audio.
- Networking.
- Cooperativo.
- Arte definitivo.
- Monetización.

No comenzar M1 sin autorización expresa.

---

## 5. Reglas de implementación

### Principios

- Implementar únicamente lo aprobado.
- Priorizar claridad y mantenibilidad.
- No agregar funcionalidades “aprovechando” un cambio.
- No crear abstracciones para necesidades hipotéticas.
- No optimizar prematuramente.
- No instalar paquetes sin autorización.
- No modificar documentos canónicos.
- No eliminar paquetes por limpieza general.
- No cambiar Project Settings fuera del alcance aprobado.

### Código C#

Cuando se autorice código:

- Una responsabilidad clara por componente.
- Dependencias explícitas mediante referencias serializadas cuando sea apropiado.
- Evitar singletons.
- Evitar Service Locator.
- Evitar objetos globales ocultos.
- Evitar `GameObject.Find`.
- Evitar `FindObjectOfType` y variantes cuando una referencia explícita sea suficiente.
- Evitar números mágicos de balance o configuración.
- Evitar código no utilizado.
- Evitar `TODO` dentro del alcance declarado como terminado.
- No usar reflexión, `unsafe`, ECS, Jobs o Burst sin una necesidad aprobada.
- No mover objetos de física mediante `Transform` cuando la SPEC indique `Rigidbody2D`.
- No introducir dependencias de terceros sin aprobación.

### Nomenclatura

- Clases, métodos y propiedades: `PascalCase`.
- Campos privados: `camelCase`.
- Interfaces: prefijo `I` solamente cuando exista una abstracción real y más de un uso razonable.
- Nombres descriptivos, sin prefijos redundantes como `PF_`, `SO_`, `CFG_` o `SYN_`.
- La carpeta debe indicar el tipo del asset.

---

## 6. Reglas de Unity y MCP

- Unity debe permanecer abierto cuando se use MCP.
- Antes de una operación destructiva, indicar exactamente qué se modificará.
- Usar MCP para inspeccionar, configurar y validar el proyecto cuando sea apropiado.
- No crear, eliminar o renombrar assets fuera del plan aprobado.
- No modificar escenas o prefabs canónicos sin enumerar los cambios.
- No editar archivos YAML de Unity manualmente salvo necesidad excepcional y aprobación.
- Preferir APIs y herramientas del Editor sobre modificaciones ciegas de archivos.
- Si MCP y el sistema de archivos muestran estados distintos, detenerse y reportarlo.
- Si aparece un error de compilación, detener la cadena de cambios y resolverlo antes de continuar.

---

## 7. Paquetes

Paquetes aprobados actualmente:

- Universal Render Pipeline.
- Input System.
- Unity Test Framework.
- Paquetes 2D incluidos o requeridos por la plantilla.
- MCP for Unity v10.1.0.

### Unity AI

Se autoriza eliminar únicamente los paquetes directos de Unity AI que provoquen errores `NoSubscription`.

Antes de eliminarlos:

1. Enumerar sus nombres exactos.
2. Revisar si son dependencias directas.
3. Usar Package Manager o MCP.
4. Permitir que Unity resuelva dependencias.
5. Confirmar que no se eliminó nada fuera de Unity AI.

No eliminar Visual Scripting, Timeline, Test Framework, paquetes 2D, URP, Input System, MCP, multiplayer.center, collab-proxy u otros paquetes solamente porque aún no se usan.

---

## 8. Git y commits

### Reglas

- No reescribir historial sin aprobación.
- No usar `git reset --hard`.
- No usar `git clean -fd`.
- No hacer force push.
- No eliminar archivos no rastreados sin confirmar.
- Mostrar `git status` antes y después de cambios importantes.
- Mantener commits pequeños y descriptivos.
- No incluir `Library/`, `Temp/`, `Logs/`, `Obj/`, `Build/`, `Builds/` ni archivos generados por IDE.

### Commits aprobados para M0

**Commit 1**

```text
chore: initialize Unity 6.5 project
```

Debe contener:

- Estado base de la plantilla.
- `.gitignore`.
- `.gitattributes`.
- Configuración inicial de Git y Git LFS.

**Commit 2**

```text
chore: complete M0 project foundation
```

Solo se crea cuando M0 cumpla su Definition of Done.

---

## 9. Flujo obligatorio antes de modificar

Antes de cualquier cambio:

1. Leer este archivo.
2. Leer la SPEC y documentos aplicables.
3. Inspeccionar el repositorio.
4. Inspeccionar la consola de Unity.
5. Presentar un plan breve.
6. Enumerar archivos, assets, escenas y configuraciones a modificar.
7. Identificar riesgos y discrepancias.
8. Confirmar que el plan no invade otro milestone.
9. Esperar aprobación cuando la tarea lo indique.

No asumir autorización implícita por haber discutido una idea anteriormente.

---

## 10. Flujo obligatorio después de modificar

Al finalizar una tarea:

1. Resumir lo implementado.
2. Enumerar archivos creados, modificados y eliminados.
3. Enumerar ajustes de Unity modificados.
4. Enumerar paquetes agregados o eliminados.
5. Reportar errores y warnings.
6. Mostrar `git status`.
7. Mostrar commits creados.
8. Explicar cómo verificar manualmente el resultado.
9. Comparar el resultado con la Definition of Done.
10. Detenerse al concluir el milestone autorizado.

No declarar un milestone terminado si alguna validación falla.

---

## 11. Definition of Done de M0

M0 solo está terminado cuando:

- Unity abre con cero errores y cero warnings.
- El Editor es 6000.5.3f1.
- URP 2D está activo.
- Input System está activo.
- MCP for Unity permanece instalado.
- Domain Reload y Scene Reload están activos.
- Company Name es `SynoraDev`.
- Product Name es `SYNORA Prototipo`.
- Force Text está activo.
- Visible Meta Files está activo.
- Git y Git LFS funcionan.
- `.gitignore` y `.gitattributes` existen.
- La estructura mínima aprobada existe.
- El preset de pixel art existe.
- `Bootstrap.unity` existe.
- El Build Profile de Windows está configurado.
- La build de Windows compila.
- La build se ejecuta fuera del Editor.
- Los ADR y la enmienda técnica están registrados.
- El repositorio queda limpio después del commit final.

---

## 12. Condiciones de detención obligatoria

Detenerse y pedir aprobación si:

- La SPEC contradice al proyecto real.
- Una API cambió en Unity 6000.5.3f1.
- Se requiere instalar o actualizar un paquete.
- Se requiere modificar gameplay o diseño.
- Se requiere cambiar el alcance.
- Se requiere eliminar un asset no creado en la tarea actual.
- Aparecen errores no relacionados con el cambio.
- Una prueba o build falla repetidamente.
- La solución propuesta necesita un singleton, Service Locator o estado global no aprobado.
- El cambio afectaría M1 o milestones posteriores.
- El cambio puede invalidar una decisión canónica o técnica congelada.

---

## 13. Regla final

> Diseñar para crecer, pero implementar únicamente lo que existe y está aprobado hoy.

Claude Code debe actuar como implementador técnico del proyecto, no como autoridad creativa ni de producto.
