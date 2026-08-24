// Shared sidebar navigation, rendered into <div id="app-sidebar"></div>.
// Requires auth.js (getSession, getAccessibleTabs, logout) to be loaded first.

const SB_ICONS = {
  '/player.html': '<svg width="18" height="18" viewBox="0 0 24 24" fill="none"><path d="M12 12a4.5 4.5 0 1 0 0-9 4.5 4.5 0 0 0 0 9Z" stroke="currentColor" stroke-width="2"/><path d="M4 20c1.4-3.6 4.4-5.5 8-5.5s6.6 1.9 8 5.5" stroke="currentColor" stroke-width="2" stroke-linecap="round"/></svg>',
  '/observer.html': '<svg width="18" height="18" viewBox="0 0 24 24" fill="none"><path d="M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7-10-7-10-7Z" stroke="currentColor" stroke-width="2"/><circle cx="12" cy="12" r="3" stroke="currentColor" stroke-width="2"/></svg>',
  '/dashboard.html': '<svg width="18" height="18" viewBox="0 0 24 24" fill="none"><rect x="3" y="3" width="7" height="9" rx="1.5" stroke="currentColor" stroke-width="2"/><rect x="14" y="3" width="7" height="5" rx="1.5" stroke="currentColor" stroke-width="2"/><rect x="14" y="12" width="7" height="9" rx="1.5" stroke="currentColor" stroke-width="2"/><rect x="3" y="16" width="7" height="5" rx="1.5" stroke="currentColor" stroke-width="2"/></svg>',
  '/replay.html': '<svg width="18" height="18" viewBox="0 0 24 24" fill="none"><path d="M3 12a9 9 0 1 0 3-6.7" stroke="currentColor" stroke-width="2" stroke-linecap="round"/><path d="M3 4v5h5" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>',
  '/admin.html': '<svg width="18" height="18" viewBox="0 0 24 24" fill="none"><path d="M12 2 4 5v6c0 5 3.4 8.7 8 11 4.6-2.3 8-6 8-11V5l-8-3Z" stroke="currentColor" stroke-width="2" stroke-linejoin="round"/></svg>'
};

const SB_LABELS = { Player: 'Player', Observer: 'Team Leader', Dashboard: 'Commander', Admin: 'Admin Panel', Replay: 'Replay' };

function initials(name) {
  if (!name) return '?';
  const parts = name.trim().split(/[\s._-]+/).filter(Boolean);
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase();
  return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
}

function renderSidebar(activePath) {
  const container = document.getElementById('app-sidebar');
  if (!container) return;
  const session = getSession();
  if (!session) return;

  document.body.classList.add('app');

  const tabs = getAccessibleTabs(session.role);
  const navHtml = tabs.map(t => `
    <a class="sb-link ${t.url === activePath ? 'active' : ''}" href="${t.url}">
      <span class="sb-icon">${SB_ICONS[t.url] || ''}</span>
      <span>${SB_LABELS[t.label] || t.label}</span>
    </a>
  `).join('');

  container.innerHTML = `
    <div class="sb-logo"><span class="sb-logo-dot"></span>TACNET</div>
    <div class="sb-nav">${navHtml}</div>
    <div class="sb-spacer"></div>
    <a class="sb-link ${activePath === '/menu.html' ? 'active' : ''}" href="/menu.html">
      <span class="sb-icon"><svg width="18" height="18" viewBox="0 0 24 24" fill="none"><circle cx="12" cy="12" r="3" stroke="currentColor" stroke-width="2"/><path d="M19.4 15a1.7 1.7 0 0 0 .3 1.9l.1.1a2 2 0 1 1-2.8 2.8l-.1-.1a1.7 1.7 0 0 0-1.9-.3 1.7 1.7 0 0 0-1 1.6V21a2 2 0 1 1-4 0v-.2a1.7 1.7 0 0 0-1.1-1.6 1.7 1.7 0 0 0-1.9.3l-.1.1a2 2 0 1 1-2.8-2.8l.1-.1a1.7 1.7 0 0 0 .3-1.9 1.7 1.7 0 0 0-1.6-1H3a2 2 0 1 1 0-4h.2a1.7 1.7 0 0 0 1.6-1 1.7 1.7 0 0 0-.3-1.9l-.1-.1a2 2 0 1 1 2.8-2.8l.1.1a1.7 1.7 0 0 0 1.9.3H9a1.7 1.7 0 0 0 1-1.6V3a2 2 0 1 1 4 0v.2a1.7 1.7 0 0 0 1 1.6 1.7 1.7 0 0 0 1.9-.3l.1-.1a2 2 0 1 1 2.8 2.8l-.1.1a1.7 1.7 0 0 0-.3 1.9V9a1.7 1.7 0 0 0 1.6 1H21a2 2 0 1 1 0 4h-.2a1.7 1.7 0 0 0-1.6 1Z" stroke="currentColor" stroke-width="1.6"/></svg></span>
      <span>Menu</span>
    </a>
    <div class="sb-user">
      <div class="sb-avatar">${initials(session.username)}</div>
      <div class="sb-user-info">
        <div class="sb-user-name">${session.username}</div>
        <div class="sb-user-role">${session.role}</div>
      </div>
      <button class="sb-logout" title="Log out" onclick="logout()">
        <svg width="16" height="16" viewBox="0 0 24 24" fill="none"><path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4" stroke="currentColor" stroke-width="2" stroke-linecap="round"/><path d="M16 17l5-5-5-5M21 12H9" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>
      </button>
    </div>
  `;
}
