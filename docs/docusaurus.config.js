// @ts-check
// `@type` JSDoc annotations allow editor autocompletion and type checking
// (when paired with `@ts-check`).
// There are two equivalent config styles: `js` and `ts`.
// You can switch between them by changing the `configType` below.
// You can write your Docusaurus config whether in `.js`, `.ts` or `.tsx` file.
// (If you are writing in `.tsx`, remember to include: import type {Config} from 'docusaurus';)

/** @type {import('@docusaurus/types').Config} */
const config = {
  title: 'Financials Module',
  tagline: 'Technical Documentation for the mCase Financials Module',
  favicon: 'img/favicon.ico',

  // Set your site URL for deployment
  url: 'https://davidgubalarm.github.io',       // TODO: Replace with your actual site URL
  baseUrl: '/',

  organizationName: 'your-org',             // TODO: Replace with your GitHub org or username
  projectName: 'financials_module',
  trailingSlash: false,

  onBrokenLinks: 'throw',
  onBrokenMarkdownLinks: 'warn',

  markdown: {
    mermaid: true,
  },
  themes: ['@docusaurus/theme-mermaid'],

  i18n: {
    defaultLocale: 'en',
    locales: ['en'],
  },

  presets: [
    [
      'classic',
      /** @type {import('@docusaurus/preset-classic').Options} */
      ({
        docs: {
          routeBasePath: '/',
          sidebarPath: './sidebars.js',
          editUrl: undefined,
        },
        blog: false,
        theme: {
          customCss: './src/css/custom.css',
        },
      }),
    ],
  ],

  themeConfig:
    /** @type {import('@docusaurus/preset-classic').ThemeConfig} */
    ({
      navbar: {
        title: 'Financials Module',
        logo: {
          alt: 'Financials Module',
          src: 'img/logo.svg',
        },
        items: [
          {
            type: 'docSidebar',
            sidebarId: 'docsSidebar',
            position: 'left',
            label: 'Documentation',
          },
        ],
      },
      footer: {
        style: 'dark',
        copyright: `Copyright © ${new Date().getFullYear()} Financials Module. Built with Docusaurus.`,
      },
      prism: {
        additionalLanguages: ['csharp'],
      },
    }),
};

module.exports = config;
