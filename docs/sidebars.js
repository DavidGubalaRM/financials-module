/**
 * Creating a sidebar enables you to:
 - create an ordered group of docs
 - render a sidebar for each doc of that group
 - provide next/previous navigation

 The sidebars can be generated from the filesystem, or explicitly defined here.

 Create as many sidebars as you want.
 */

// @ts-check

/** @type {import('@docusaurus/plugin-content-docs').SidebarsConfig} */
const sidebars = {
  docsSidebar: [
    'intro',
    {
      type: 'category',
      label: 'Architecture',
      items: [
        'architecture/overview',
        'architecture/data-model',
        'architecture/core-components',
        'architecture/event-model',
      ],
    },
    {
      type: 'category',
      label: 'Finance Gateway',
      items: [
        'finance-gateway/overview',
        'finance-gateway/actions',
        'finance-gateway/messages',
      ],
    },
    {
      type: 'category',
      label: 'Modules',
      items: [
        'modules/placements',
        'modules/rates',
        'modules/service-request',
        'modules/deposits-funds',
        'modules/invoice',
        'modules/approvals',
        'modules/adoptions-guardianship',
      ],
    },
    {
      type: 'category',
      label: 'Reference',
      items: [
        'reference/service-catalog',
        'reference/constants',
        'reference/event-index',
      ],
    },
  ],
};

module.exports = sidebars;
