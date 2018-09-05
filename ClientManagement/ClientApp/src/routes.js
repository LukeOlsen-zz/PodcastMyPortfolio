import React from 'react';
import Loadable from 'react-loadable';
import DefaultLayout from './containers/DefaultLayout';

function Loading() {
  return <div>Loading...</div>;
}

const Users = Loadable({
  loader: () => import('./views/Users/Users'),
  loading: Loading
});

const User = Loadable({
  loader: () => import('./views/Users/User'),
  loading: Loading
});

const UserProfile = Loadable({
  loader: () => import('./views/Users/UserProfile'),
  loading: Loading
});


const Firm = Loadable({
  loader: () => import('./views/Setup/Firm'),
  loading: Loading
});

const FirmPodcastSettings = Loadable({
  loader: () => import('./views/Setup/FirmPodcastSettings'),
  loading: Loading
});

const FirmPodcastSegments = Loadable({
  loader: () => import('./views/Podcasts/FirmPodcastSegments'),
  loading: Loading
});

const FirmPodcastSegment = Loadable({
  loader: () => import('./views/Podcasts/FirmPodcastSegment'),
  loading: Loading
});

const ClientGroupPodcastSegments = Loadable({
  loader: () => import('./views/Podcasts/ClientGroupPodcastSegments'),
  loading: Loading
});

const ClientGroupPodcastSegment = Loadable({
  loader: () => import('./views/Podcasts/ClientGroupPodcastSegment'),
  loading: Loading
});

const ClientGroups = Loadable({
  loader: () => import('./views/Directory/ClientGroups'),
  loading: Loading
});

const ClientGroup = Loadable({
  loader: () => import('./views/Directory/ClientGroup'),
  loading: Loading
});

const ClientGroupsImport = Loadable({
  loader: () => import('./views/Directory/ClientGroupsImport'),
  loading: Loading
});


const Clients = Loadable({
  loader: () => import('./views/Directory/Clients'),
  loading: Loading
});

const Client = Loadable({
  loader: () => import('./views/Directory/Client'),
  loading: Loading
});

const ClientsImport = Loadable({
  loader: () => import('./views/Directory/ClientsImport'),
  loading: Loading
});

const ClientMessagesImport = Loadable({
  loader: () => import('./views/Import/ClientMessagesImport'),
  loading: Loading
});

const ClientAccountsImport = Loadable({
  loader: () => import('./views/Import/ClientAccountsImport'),
  loading: Loading
});

const ClientAccountPeriodicDataImport = Loadable({
  loader: () => import('./views/Import/ClientAccountPeriodicDataImport'),
  loading: Loading
});

const ClientAccountActivityImport = Loadable({
  loader: () => import('./views/Import/ClientAccountActivityImport'),
  loading: Loading
});

const ClientAccount = Loadable({
  loader: () => import('./views/Directory/ClientAccount'),
  loading: Loading
});


const routes = [
  { path: '/', name: 'Home', component: DefaultLayout, exact: true },
  { path: '/users', exact: true,  name: 'Users', component: Users },
  { path: '/users/:id', exact: true, name: 'User Details', component: User },
  { path: '/userprofile', exact: true, name: 'Profile', component: UserProfile },
  { path: '/firm', exact: true, name: 'Firm', component: Firm },
  { path: '/firmpodcastsettings', exact: true, name: 'Firm Podcast Settings', component: FirmPodcastSettings },
  { path: '/podcasts/firmpodcastsegments', exact: true, name: 'Firm Podcast Segments', component: FirmPodcastSegments },
  { path: '/podcasts/firmpodcastsegments/:id', exact: true, name: 'Segment', component: FirmPodcastSegment },
  { path: '/podcasts/clientgrouppodcastsegments', exact: true, name: 'Group Podcast Segments', component: ClientGroupPodcastSegments },
  { path: '/podcasts/clientgrouppodcastsegments/:id', exact: true, name: 'Segment', component: ClientGroupPodcastSegment },
  { path: '/directory/clientgroups', exact: true, name: 'Client Groups', component: ClientGroups },
  { path: '/directory/clientgroups/clientgroupsimport', exact: true, name: 'Client Groups Import', component: ClientGroupsImport },
  { path: '/directory/clientgroups/:id', exact: true, name: 'Client Group' , component: ClientGroup },
  { path: '/directory/clientgroups/:clientgroupid/:id', exact: true, name: 'Client', component: Client },

  { path: '/directory/clients', exact: true, name: 'Clients', component: Clients },
  { path: '/directory/clients/clientsimport', exact: true, name: 'Client Import', component: ClientsImport },
  { path: '/directory/clients/clientaccount/:id', exact: true, name: 'Client Account', component: ClientAccount },
  { path: '/directory/clients/:id', exact: true, name: 'Client', component: Client },

  { path: '/import/clientmessages', exact: true, name: 'Client Message Import', component: ClientMessagesImport },
  { path: '/import/clientaccounts', exact: true, name: 'Client Accounts Import', component: ClientAccountsImport },
  { path: '/import/clientaccountperiodicdata', exact: true, name: 'Client Account Periodic Data Import', component: ClientAccountPeriodicDataImport },
  { path: '/import/clientaccountactivity', exact: true, name: 'Client Account Activity Import', component: ClientAccountActivityImport }
];

export default routes;
