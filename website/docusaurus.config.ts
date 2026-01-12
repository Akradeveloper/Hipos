import {themes as prismThemes} from 'prism-react-renderer';
import type {Config} from '@docusaurus/types';
import type * as Preset from '@docusaurus/preset-classic';

const config: Config = {
  title: 'Hipos Automation Framework',
  tagline: 'Windows UI Automation con FlaUI y NUnit',
  favicon: 'img/favicon.ico',

  // URL de producción de tu sitio
  url: 'https://yourusername.github.io',
  // Pathname base para tu proyecto (<projectName>/)
  baseUrl: '/Hipos/',

  // Configuración de GitHub Pages
  organizationName: 'yourusername', // Cambiar por tu usuario/organización de GitHub
  projectName: 'Hipos', // Nombre del repositorio

  onBrokenLinks: 'warn',
  onBrokenMarkdownLinks: 'warn',

  // Internacionalización
  i18n: {
    defaultLocale: 'es',
    locales: ['es', 'en'],
    localeConfigs: {
      es: {
        label: 'Español',
        direction: 'ltr',
      },
      en: {
        label: 'English',
        direction: 'ltr',
      },
    },
  },

  presets: [
    [
      'classic',
      {
        docs: {
          sidebarPath: './sidebars.ts',
          editUrl: 'https://github.com/yourusername/Hipos/tree/main/website/',
          showLastUpdateTime: false,
          showLastUpdateAuthor: false,
        },
        blog: false, // Deshabilitado por ahora
        theme: {
          customCss: './src/css/custom.css',
        },
      } satisfies Preset.Options,
    ],
  ],

  // Habilitar Mermaid para diagramas
  markdown: {
    mermaid: true,
  },
  themes: ['@docusaurus/theme-mermaid'],

  themeConfig: {
    image: 'img/hipos-social-card.jpg',
    navbar: {
      title: 'Hipos',
      logo: {
        alt: 'Hipos Logo',
        src: 'img/logo.svg',
      },
      items: [
        {
          type: 'docSidebar',
          sidebarId: 'tutorialSidebar',
          position: 'left',
          label: 'Documentación',
        },
        {
          type: 'localeDropdown',
          position: 'right',
        },
        {
          href: 'https://github.com/yourusername/Hipos',
          label: 'GitHub',
          position: 'right',
        },
      ],
    },
    footer: {
      style: 'dark',
      links: [
        {
          title: 'Documentación',
          items: [
            {
              label: 'Getting Started',
              to: '/docs/getting-started',
            },
            {
              label: 'Arquitectura',
              to: '/docs/architecture',
            },
            {
              label: 'Framework Guide',
              to: '/docs/framework-guide',
            },
          ],
        },
        {
          title: 'Recursos',
          items: [
            {
              label: 'CI/CD',
              to: '/docs/ci-cd',
            },
            {
              label: 'Troubleshooting',
              to: '/docs/troubleshooting',
            },
            {
              label: 'Contributing',
              to: '/docs/contributing',
            },
          ],
        },
        {
          title: 'Más',
          items: [
            {
              label: 'GitHub',
              href: 'https://github.com/yourusername/Hipos',
            },
            {
              label: 'FlaUI Documentation',
              href: 'https://github.com/FlaUI/FlaUI',
            },
            {
              label: 'NUnit Documentation',
              href: 'https://docs.nunit.org/',
            },
          ],
        },
      ],
      copyright: `Copyright © ${new Date().getFullYear()} Hipos Project. Built with Docusaurus.`,
    },
    prism: {
      theme: prismThemes.github,
      darkTheme: prismThemes.dracula,
      additionalLanguages: ['csharp', 'yaml', 'json', 'powershell', 'bash'],
    },
    // Configuración de Algolia Search (opcional, comentado)
    // algolia: {
    //   appId: 'YOUR_APP_ID',
    //   apiKey: 'YOUR_SEARCH_API_KEY',
    //   indexName: 'hipos',
    // },
  } satisfies Preset.ThemeConfig,
};

export default config;
