import type {SidebarsConfig} from '@docusaurus/plugin-content-docs';

const sidebars: SidebarsConfig = {
  tutorialSidebar: [
    {
      type: 'doc',
      id: 'intro',
      label: 'Introducción',
    },
    {
      type: 'doc',
      id: 'getting-started',
      label: 'Getting Started',
    },
    {
      type: 'category',
      label: 'Arquitectura',
      items: [
        'architecture',
      ],
    },
    {
      type: 'category',
      label: 'Guías',
      items: [
        'framework-guide',
        'examples',
        'reporting-logging',
      ],
    },
    {
      type: 'category',
      label: 'CI/CD y DevOps',
      items: [
        'ci-cd',
      ],
    },
    {
      type: 'category',
      label: 'Ayuda',
      items: [
        'troubleshooting',
        'contributing',
      ],
    },
  ],
};

export default sidebars;
