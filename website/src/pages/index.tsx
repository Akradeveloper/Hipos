import clsx from 'clsx';
import Link from '@docusaurus/Link';
import useDocusaurusContext from '@docusaurus/useDocusaurusContext';
import Layout from '@theme/Layout';
import Heading from '@theme/Heading';

import styles from './index.module.css';

function HomepageHeader() {
  const {siteConfig} = useDocusaurusContext();
  return (
    <header className={clsx('hero hero--primary', styles.heroBanner)}>
      <div className="container">
        <Heading as="h1" className="hero__title">
          {siteConfig.title}
        </Heading>
        <p className="hero__subtitle">{siteConfig.tagline}</p>
        <div className={styles.buttons}>
          <Link
            className="button button--secondary button--lg"
            to="/docs/getting-started">
            Comenzar - 5 minutos ⏱️
          </Link>
        </div>
      </div>
    </header>
  );
}

function HomepageFeatures() {
  const features = [
    {
      title: '🚀 Fácil de Usar',
      description: (
        <>
          Framework diseñado desde cero para ser intuitivo y productivo.
          Incluye Page Objects, waits inteligentes y manejo automático de errores.
        </>
      ),
    },
    {
      title: '📊 Reporting Completo',
      description: (
        <>
          Integración con Allure para reportes HTML profesionales.
          Screenshots automáticos al fallar, logs detallados y artifacts para CI.
        </>
      ),
    },
    {
      title: '⚙️ CI/CD Ready',
      description: (
        <>
          Configuración lista para GitHub Actions y Azure DevOps.
          Soporte para categorización de tests (smoke/regression) y ejecución paralela.
        </>
      ),
    },
  ];

  return (
    <section className={styles.features}>
      <div className="container">
        <div className="row">
          {features.map((feature, idx) => (
            <div key={idx} className={clsx('col col--4')}>
              <div className="text--center padding-horiz--md">
                <Heading as="h3">{feature.title}</Heading>
                <p>{feature.description}</p>
              </div>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}

export default function Home(): JSX.Element {
  const {siteConfig} = useDocusaurusContext();
  return (
    <Layout
      title={`${siteConfig.title}`}
      description="Framework enterprise para automatización de aplicaciones Windows">
      <HomepageHeader />
      <main>
        <HomepageFeatures />
      </main>
    </Layout>
  );
}
