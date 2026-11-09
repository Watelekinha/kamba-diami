import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { guestGuard } from './core/guards/guest.guard';
import { roleGuard } from './core/guards/role.guard';

export const routes: Routes = [
  {
    path: '',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/landing/landing.component').then((m) => m.LandingComponent)
  },
  {
    path: 'login',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/login/login.component').then((m) => m.LoginComponent)
  },
  {
    path: 'register',
    canActivate: [guestGuard],
    loadComponent: () =>
      import('./features/auth/register/register.component').then((m) => m.RegisterComponent)
  },
  {
    path: 'forgot-password',
    canActivate: [guestGuard],
    loadComponent: () =>
      import('./features/auth/forgot-password/forgot-password.component').then((m) => m.ForgotPasswordComponent)
  },
  {
    // Sem guarda: a ligação do email tem de funcionar mesmo com outra sessão aberta neste browser.
    path: 'reset-password',
    loadComponent: () =>
      import('./features/auth/reset-password/reset-password.component').then((m) => m.ResetPasswordComponent)
  },
  {
    path: 'feed',
    canActivate: [authGuard, roleGuard(['Familia', 'Secretaria'])],
    loadComponent: () => import('./features/feed/feed.component').then((m) => m.FeedComponent)
  },
  {
    path: 'profile',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/profile/my-profile/my-profile.component').then((m) => m.MyProfileComponent)
  },
  {
    path: 'users/:id',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/profile/public-profile/public-profile.component').then(
        (m) => m.PublicProfileComponent
      )
  },
  {
    path: 'job-postings/new',
    canActivate: [authGuard, roleGuard(['Familia', 'Secretaria'])],
    loadComponent: () =>
      import('./features/job-postings/create-posting/create-posting.component').then(
        (m) => m.CreatePostingComponent
      )
  },
  {
    path: 'job-postings/mine',
    canActivate: [authGuard, roleGuard(['Familia', 'Secretaria'])],
    loadComponent: () =>
      import('./features/job-postings/my-postings/my-postings.component').then((m) => m.MyPostingsComponent)
  },
  {
    path: 'job-postings/saved',
    canActivate: [authGuard, roleGuard(['Familia', 'Secretaria'])],
    loadComponent: () =>
      import('./features/job-postings/saved-postings/saved-postings.component').then(
        (m) => m.SavedPostingsComponent
      )
  },
  {
    path: 'job-postings/:id',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/job-postings/posting-detail/posting-detail.component').then(
        (m) => m.PostingDetailComponent
      )
  },
  {
    path: 'messages',
    canActivate: [authGuard, roleGuard(['Familia', 'Secretaria'])],
    loadComponent: () =>
      import('./features/messages/conversation-list/conversation-list.component').then(
        (m) => m.ConversationListComponent
      )
  },
  {
    path: 'messages/:id',
    canActivate: [authGuard, roleGuard(['Familia', 'Secretaria'])],
    loadComponent: () =>
      import('./features/messages/conversation-detail/conversation-detail.component').then(
        (m) => m.ConversationDetailComponent
      )
  },
  {
    path: 'contracts',
    canActivate: [authGuard, roleGuard(['Familia', 'Secretaria'])],
    loadComponent: () =>
      import('./features/contracts/contract-list/contract-list.component').then(
        (m) => m.ContractListComponent
      )
  },
  {
    path: 'contracts/match/:matchId',
    canActivate: [authGuard, roleGuard(['Familia', 'Secretaria'])],
    loadComponent: () =>
      import('./features/contracts/contract-workspace/contract-workspace.component').then(
        (m) => m.ContractWorkspaceComponent
      )
  },
  {
    path: 'admin',
    canActivate: [authGuard, roleGuard(['Admin'])],
    loadComponent: () =>
      import('./features/admin/admin-dashboard/admin-dashboard.component').then(
        (m) => m.AdminDashboardComponent
      )
  },
  { path: '**', redirectTo: '' }
];
