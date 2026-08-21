function getSession() {
    const raw = localStorage.getItem('gvg_session');
    if (!raw) return null;
    try { return JSON.parse(raw); } catch { return null; }
}

function requireAuth(allowedRoles) {
    const session = getSession();
    if (!session) {
        location.href = '/index.html';
        return null;
    }
    if (!allowedRoles.includes(session.role)) {
        document.body.innerHTML =
            '<div style="padding:40px;text-align:center;font-family:sans-serif;color:#eee;background:#111;min-height:100vh;">' +
            'У вашей роли (' + session.role + ') нет доступа к этой странице.<br><br>' +
            '<a href="/index.html" style="color:#6cf;">Назад к логину</a></div>';
        throw new Error('access denied for role ' + session.role);
    }
    return session;
}


const TABS = [
    { url: '/player.html', label: 'Player', roles: ['Player', 'Leader', 'Commander', 'Admin'] },
    { url: '/observer.html', label: 'Observer', roles: ['Leader', 'Admin'] },
    { url: '/dashboard.html', label: 'Dashboard', roles: ['Commander', 'Admin'] },
    { url: '/admin.html', label: 'Admin', roles: ['Admin'] }
];

function getAccessibleTabs(role) {
    return TABS.filter(t => t.roles.includes(role));
}

function logout() {
    const session = getSession();
    if (session) {
        fetch('/api/auth/logout', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ token: session.token })
        }).catch(() => { });
    }
    localStorage.removeItem('gvg_session');
    location.href = '/index.html';
}