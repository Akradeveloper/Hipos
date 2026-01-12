# Documentación Hipos - Docusaurus

Esta carpeta contiene la documentación completa del framework Hipos construida con Docusaurus 3.

## 🚀 Quick Start

### Instalar Dependencias

```bash
cd website
npm install
```

### Desarrollo Local

```bash
npm run start
```

Esto abrirá `http://localhost:3000` con hot-reload automático.

### Build para Producción

```bash
npm run build
```

Genera archivos estáticos en `build/`.

### Preview del Build

```bash
npm run serve
```

Sirve el build localmente para verificar antes de deploy.

## 📚 Contenido de la Documentación

### 9 Páginas Completas

1. **intro.md** - Introducción al framework
   - Características principales
   - Stack tecnológico
   - Estado del proyecto (11 tests, 100% success)

2. **getting-started.md** - Guía de inicio rápido
   - Instalación y configuración
   - Primer test
   - Estructura del proyecto
   - Troubleshooting básico

3. **architecture.md** - Arquitectura del framework
   - Diagramas Mermaid
   - Capas del sistema
   - Flujo de ejecución

4. **framework-guide.md** - Guía detallada del framework
   - AppLauncher (búsqueda híbrida ⭐)
   - BaseTest (OneTimeSetUp/TearDown)
   - WaitHelper, ElementWrapper
   - Page Objects
   - ConfigManager

5. **examples.md** ⭐ NUEVO
   - Tests básicos de verificación
   - Tests complejos con operaciones matemáticas
   - Page Objects completos
   - Patrón Arrange-Act-Assert
   - Configuración para diferentes apps

6. **reporting-logging.md** - Reportes y logging
   - Allure Reports
   - Serilog
   - Screenshots automáticos
   - Artifacts para CI

7. **ci-cd.md** - Integración continua
   - GitHub Actions workflows
   - Azure DevOps guide
   - Limitaciones de runners (interactive desktop)

8. **troubleshooting.md** - Resolución de problemas
   - TimeoutException (UWP vs Win32) ⭐
   - Element Not Found
   - Flaky tests
   - CI issues
   - Cursor/VS Code se cierra ⭐

9. **contributing.md** - Cómo contribuir
   - Convenciones de código
   - Pull requests
   - Testing guidelines

## 🎨 Características

- ✅ **Mermaid Diagrams** - Diagramas de arquitectura y flujo
- ✅ **Syntax Highlighting** - Código C#, JSON, YAML, Bash
- ✅ **Multiidioma** - Español (default) + English
- ✅ **Dark/Light Mode** - Tema adaptable
- ✅ **Mobile Responsive** - Funciona en todos los dispositivos
- ✅ **Search** - Búsqueda integrada

## 📝 Cambios Recientes

### Actualizaciones Importantes (Enero 2026)

1. **Nueva página `examples.md`**
   - Ejemplos completos de tests
   - Code snippets de CalculatorPage
   - Patrón Arrange-Act-Assert
   - Configuración para diferentes apps

2. **Actualización de `intro.md`**
   - Estado actual: 11 tests, 100% success rate
   - Métricas de ejecución
   - Búsqueda híbrida destacada

3. **Actualización de `getting-started.md`**
   - Cambio de DemoApp a Calculator
   - Nuevas categorías (Demo vs Complex)
   - Troubleshooting para UWP apps

4. **Actualización de `framework-guide.md`**
   - Explicación detallada de búsqueda híbrida
   - Strict mode (5s) + Relaxed mode (10s)
   - OneTimeSetUp/TearDown pattern
   - Ejemplos de logs

5. **Actualización de `troubleshooting.md`**
   - Sección de TimeoutException para UWP
   - Cursor/VS Code crashes
   - Diferencias UWP vs Win32

6. **Actualización de `architecture.md`**
   - Diagrama actualizado con hybrid search
   - 11 tests (4 Demo + 7 Complex)
   - Apps under test: Calculator, Notepad, Custom

## 🔧 Configuración

### docusaurus.config.ts

- **URL Base**: `/Hipos/`
- **Locales**: Español (default), English
- **Theme**: Dark/Light auto-switch
- **Mermaid**: Habilitado
- **Git Metadata**: Deshabilitado (sin `showLastUpdateTime`)

### sidebars.ts

Estructura de navegación:
- Introducción
- Getting Started
- Arquitectura
- Guías (Framework Guide, Examples, Reporting)
- CI/CD y DevOps
- Ayuda (Troubleshooting, Contributing)

## 🚀 Deploy

### GitHub Pages (Automático)

El workflow `.github/workflows/docs.yml` automatiza el deploy:

```yaml
name: Deploy Docs
on:
  push:
    branches: [main]
    paths:
      - 'website/**'

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-node@v3
      - run: cd website && npm install && npm run build
      - uses: peaceiris/actions-gh-pages@v3
        with:
          github_token: ${{ secrets.GITHUB_TOKEN }}
          publish_dir: ./website/build
```

### Deploy Manual

```bash
npm run build
# Subir contenido de build/ a tu servidor web
```

## 📦 Scripts NPM

```json
{
  "start": "docusaurus start",
  "build": "docusaurus build",
  "serve": "docusaurus serve",
  "clear": "docusaurus clear",
  "deploy": "docusaurus deploy"
}
```

## 🐛 Troubleshooting

### Error: Git not found

Si ves errores sobre Git metadata:
```ts
// En docusaurus.config.ts
docs: {
  showLastUpdateTime: false,
  showLastUpdateAuthor: false,
}
```

### Build Warnings

```
[WARNING] onBrokenMarkdownLinks is deprecated
```

Esto se resolverá automáticamente en Docusaurus v4. Por ahora, es solo un warning.

## 📧 Soporte

Para problemas con la documentación:
1. Verificar que Node.js >= 18
2. Ejecutar `npm install` limpio
3. Borrar `.docusaurus/` y `build/`
4. Ejecutar `npm run clear && npm run build`

---

**Última actualización**: Enero 2026
**Docusaurus Version**: 3.9.2
**Node Version**: 22.x
