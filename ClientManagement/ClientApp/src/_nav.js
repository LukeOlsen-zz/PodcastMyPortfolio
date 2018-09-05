export default {
  items: [
    {
      name: 'Dashboard',
      url: '/directory/clients',
      icon: 'icon-speedometer',
      badge: {
        variant: 'info'
      }
    },
    {
      name: 'Directory',
      url: '/directory',
      icon: 'icon-notebook',
      children: [
        {
          name: 'Groups',
          url: '/directory/clientgroups',
          icon: 'icon-people'
        },
        {
          name: 'Clients',
          url: '/directory/clients',
          icon: 'icon-user'
        }
      ]
    },
    {
      name: 'Podcasts',
      url: '/podcasts',
      icon: 'icon-earphones',
      children: [
        {
          name: 'Firm Segments',
          url: '/podcasts/firmpodcastsegments',
          icon: 'icon-globe'
        },
        {
          name: 'Group Segments',
          url: '/podcasts/clientgrouppodcastsegments',
          icon: 'icon-people'
        }
      ]
    },
    {
      name: 'Import',
      url: '/import',
      icon: 'icon-plus',
      children: [
        {
          name: 'Messages',
          url: '/import/clientmessages',
          icon: 'icon-arrow-right'
        },
        {
          name: 'Accounts',
          url: '/import/clientaccounts',
          icon: 'icon-arrow-right'
        },
        {
          name: 'Account Periodic Data',
          url: '/import/clientaccountperiodicdata',
          icon: 'icon-arrow-right'
        },
        {
          name: 'Account Activity',
          url: '/import/clientaccountactivity',
          icon: 'icon-arrow-right'
        }
      ]
    },
    {
      name: 'Setup',
      url: '/setup',
      icon: 'icon-settings',
      children: [
        {
          name: 'Firm',
          url: '/firm',
          icon: 'icon-globe'
        },
        {
          name: 'Podcast Settings',
          url: '/firmpodcastsettings',
          icon: 'icon-equalizer'
        }
      ]
    },
    {
      divider: true,
      class: 'm-2'
    }
  ]
};
