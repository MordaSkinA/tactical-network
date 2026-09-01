const I18N_LANG_KEY = 'gvg_lang';
const I18N_DEFAULT_LANG = 'en';

const I18N_STRINGS = {
    en: {
        langName: 'EN',
        common: {
            connecting: 'connecting...',
            connected: 'connected',
            reconnecting: 'reconnecting...',
            connectionFailed: 'connection failed: ',
            loading: 'Loading...',
            logout: 'Log out',
            menu: 'Menu',
            backToLogin: 'Back to login',
            noAccess: 'Your role ({role}) does not have access to this page.'
        },
        player: {
            player: 'Player',
            team: 'Team',
            reserves: 'Reserves',
            none: 'None',
            currentOrder: 'Current order',
            needHelp: 'NEED HELP',
            statusPrefix: 'STATUS: ',
            bossSpawning: 'BOSS SPAWNING',
            spawningSuffix: ' SPAWNING',
            and: ' AND ',
            eventFallback: 'EVENT'
        },
        orderTypes: {
            PushBot: 'PUSH BOT', PushMid: 'PUSH MID', PushTop: 'PUSH TOP', AttackGoose: 'ATTACK GOOSE',
            KillBoss: 'KILL BOSS', Bomb: 'BOMB',
            DefendBot: 'DEFEND BOT', DefendMid: 'DEFEND MID', DefendTop: 'DEFEND TOP', DefendGoose: 'DEFEND GOOSE', DefendTree: 'DEFEND TREE',
            Hold: 'HOLD',
            BotJungle: 'BOT JUNGLE', TopJungle: 'TOP JUNGLE'
        },
        jungleTags: {
            JungleTopOwn: 'TOP JUNGLE (OWN)',
            JungleBotOwn: 'BOTTOM JUNGLE (OWN)',
            JungleTopEnemy: 'TOP JUNGLE (ENEMY)',
            JungleBotEnemy: 'BOTTOM JUNGLE (ENEMY)',
            Jungle: 'JUNGLE CAMPS'
        },
        statusTypes: { SquadWiped: 'TEAM WIPED', Regroup: 'REGROUP', NeedHelp: 'NEED HELP', Retreating: 'RETREATING', Autonomous: 'AUTONOMOUS' },
        sidebar: {
            downloadApp: 'Download app',
            download: 'Download {tag}',
            siteGuide: 'Site guide',
            overlayGuide: 'Overlay guide',
            labels: { Player: 'Player', Observer: 'Team Leader', Dashboard: 'Commander', Admin: 'Admin Panel', Replay: 'Replay' }
        },
        observer: {
            pageTitle: 'Team Leader',
            pageSubtitle: "Issue orders to your team and report its status.",
            loadingProfile: 'Loading profile...',
            signedInAs: 'Signed in as:',
            team: 'Team:',
            unassigned: 'Unassigned',
            enemyHeading: 'Enemy',
            teamStatusHeading: 'Team status',
            currentStatus: 'Current status',
            autoSwitchHint: "If you don't update this, it automatically switches to Autonomous after 25 seconds.",
            incomingOrders: 'Incoming orders',
            enemyRoles: { TwinBlades: 'ENEMY TB', Healer: 'ENEMY HEALER', Tank: 'ENEMY TANK', Nameless: 'ENEMY NAMELESS' }
        },
        menu: {
            brandEyebrow: 'Tactical Network',
            pageTitle: 'Home',
            signedInAs: 'Signed in as {user} ({role})',
            activeRoster: 'Active roster',
            noTeams: 'No teams set up yet.',
            reserves: 'Reserves',
            noReserves: 'Nobody in reserves.',
            account: 'Account',
            linkDiscord: 'Link Discord account',
            relinkDiscord: 'Re-link Discord account',
            discordLinked: 'Discord: linked as {name}',
            discordNotLinked: 'Discord: not linked',
            changePassword: 'Change password',
            currentPassword: 'Current password',
            newPassword: 'New password',
            fillBothFields: 'Fill in both fields.',
            passwordChanged: 'Password changed.',
            errorPrefix: 'Error: ',
            empty: 'Empty',
            stats: {
                battle: 'Battle', teams: 'Teams', inRoster: 'In roster', reserves: 'Reserves',
                active: 'Active', inactive: 'Inactive',
                noActiveBattle: 'No active battle', since: 'Since {time}', inProgress: 'In progress'
            },
            roles: { Dps: 'DPS', Tank: 'TANK', Healer: 'HEALER' },
            bulwark: { Top: 'Top bulwark', Center: 'Center bulwark', Bottom: 'Bottom bulwark' }
        },
        dashboard: {
            pageTitle: 'Main Commander. Live Feed',
            startBattle: 'Start Battle',
            endBattle: 'End Battle',
            endBattleConfirm: 'History will be archived to a file on the server, then cleared for everyone. Continue?',
            battleStarted: 'battle started',
            battleEnded: 'battle ended, history archived',
            errorPrefix: 'error: ',
            battleInProgressSince: 'Battle in progress since {time}',
            orderMacros: 'Order macros',
            macroNamePlaceholder: 'Macro name, e.g. Fall back everyone',
            chooseOrder: 'Choose order…',
            allTeams: 'All teams',
            saveAsMacro: 'Save as macro',
            macroSaveHint: 'Uses the teams currently selected below, unless "All teams" is checked.',
            macroListEmpty: 'No macros saved yet - build a target + order below, then "Save as macro".',
            issueOrder: 'Issue order',
            target: 'Target:',
            noneSelected: 'none selected',
            selectAll: 'Select all',
            clear: 'Clear',
            selectAttack: 'Attack teams',
            selectDefense: 'Defense teams',
            currentOrderPrefix: 'Order: ',
            eventFeed: 'Event feed',
            feedEmpty: 'No events yet.',
            failedLoadMacros: 'Failed to load macros: ',
            deleteMacroConfirm: 'Delete macro "{name}"?',
            deleteMacroTitle: 'Delete macro',
            giveMacroName: 'Give the macro a name first.',
            selectTeamOrCheckAll: 'Select at least one team, or check "All teams".',
            selectTeamFirst: 'Select at least one team first',
            reservesCount: 'Reserves ({count})',
            online: 'Online',
            offline: 'Offline',
            noRolePrefix: '⚠ No ',
            missingHealer: 'healer',
            missingTank: 'tank',
            orderWord: 'ORDER',
            statusWord: 'STATUS',
            sides: { Attack: 'Attack', Defense: 'Defense', Flex: 'Flex' },
            tagLabels: {
                Jungle: 'Jungle', Boss: 'Boss', Backup: 'Backup',
                JungleTopOwn: 'Top jungle (own)', JungleBotOwn: 'Bot jungle (own)',
                JungleTopEnemy: 'Top jungle (enemy)', JungleBotEnemy: 'Bot jungle (enemy)'
            }
        },
        login: {
            heading: 'Sign in to GvG Tactical Network',
            username: 'Username',
            password: 'Password',
            logIn: 'Log in',
            or: 'or',
            loginWithDiscord: 'Login with Discord',
            tooManyAttempts: 'Too many attempts. Please wait a moment.',
            invalidCredentials: 'Invalid username or password.',
            connectionError: 'Connection error: '
        },
        register: {
            heading: 'Almost ready',
            defaultHint: 'Discord verified. Please specify your nickname, admins will use it to add you to a team.',
            sessionNotFound: 'Session not found. Please return to the login page and try again.',
            verifiedHint: 'Discord verified: {name}. Please specify your nickname — admins will use it to add you to a team.',
            nickname: 'Nickname',
            continue: 'Continue',
            failedCreateAccount: 'Failed to create account.'
        }
    },
    vi: {
        langName: 'Tiếng Việt',
        common: {
            connecting: 'đang kết nối...',
            connected: 'đã kết nối',
            reconnecting: 'đang kết nối lại...',
            connectionFailed: 'kết nối thất bại: ',
            loading: 'Đang tải...',
            logout: 'Đăng xuất',
            menu: 'Menu',
            backToLogin: 'Quay lại đăng nhập',
            noAccess: 'Vai trò của bạn ({role}) không có quyền truy cập trang này.'
        },
        player: {
            player: 'Người chơi',
            team: 'Đội',
            reserves: 'Dự bị',
            none: 'Không có',
            currentOrder: 'Mệnh lệnh hiện tại',
            needHelp: 'CẦN HỖ TRỢ',
            statusPrefix: 'TRẠNG THÁI: ',
            bossSpawning: 'BOSS SẮP XUẤT HIỆN',
            spawningSuffix: ' SẮP XUẤT HIỆN',
            and: ' và ',
            eventFallback: 'sự kiện'
        },
        orderTypes: {
            PushBot: 'Đẩy Hạ Lộ', PushMid: 'Đẩy Trung Lộ', PushTop: 'Đẩy Thượng Lộ', AttackGoose: 'Tấn công Ngỗng',
            KillBoss: 'Hạ Boss', Bomb: 'Đặt bom',
            DefendBot: 'Thủ Hạ Lộ', DefendMid: 'Thủ Trung Lộ', DefendTop: 'Thủ Thượng Lộ', DefendGoose: 'Thủ Ngỗng', DefendTree: 'Thủ Đại Thụ',
            Hold: 'Giữ vị trí',
            BotJungle: 'Rừng Hạ Lộ', TopJungle: 'Rừng Thượng Lộ'
        },
        jungleTags: {
            JungleTopOwn: 'Rừng Thượng Lộ (phe ta)',
            JungleBotOwn: 'Rừng Hạ Lộ (phe ta)',
            JungleTopEnemy: 'Rừng Thượng Lộ (địch)',
            JungleBotEnemy: 'Rừng Hạ Lộ (địch)',
            Jungle: 'Quái rừng hồi sinh'
        },
        statusTypes: { SquadWiped: 'Cả đội bị hạ', Regroup: 'Tập hợp', NeedHelp: 'Cần hỗ trợ', Retreating: 'Đang rút lui', Autonomous: 'Tự do tác chiến' },
        sidebar: {
            downloadApp: 'Tải ứng dụng',
            download: 'Tải {tag}',
            siteGuide: 'Hướng dẫn trang web',
            overlayGuide: 'Hướng dẫn overlay',
            labels: { Player: 'Người chơi', Observer: 'Đội trưởng', Dashboard: 'Chỉ huy', Admin: 'Bảng quản trị', Replay: 'Xem lại' }
        },
        observer: {
            pageTitle: 'Đội trưởng',
            pageSubtitle: 'Ra lệnh cho đội của bạn và báo cáo tình trạng đội.',
            loadingProfile: 'Đang tải hồ sơ...',
            signedInAs: 'Đăng nhập với tư cách: ',
            team: 'Đội: ',
            unassigned: 'Chưa phân đội',
            enemyHeading: 'Tình hình địch',
            teamStatusHeading: 'Trạng thái đội',
            currentStatus: 'Trạng thái hiện tại',
            autoSwitchHint: 'Sẽ tự động chuyển sang "Tự do tác chiến" sau 25 giây nếu không cập nhật.',
            incomingOrders: 'Mệnh lệnh nhận được',
            enemyRoles: { TwinBlades: 'Song Kiếm địch', Healer: 'Trị liệu địch', Tank: 'Đỡ đòn địch', Nameless: 'Vô Danh địch' }
        },
        menu: {
            brandEyebrow: 'Mạng lưới chiến thuật',
            pageTitle: 'Trang chủ',
            signedInAs: 'Đăng nhập: {user} ({role})',
            activeRoster: 'Đội hình hiện tại',
            noTeams: 'Chưa có đội nào được thiết lập.',
            reserves: 'Dự bị',
            noReserves: 'Chưa có quân dự bị.',
            account: 'Tài khoản',
            linkDiscord: 'Liên kết tài khoản Discord',
            relinkDiscord: 'Liên kết lại tài khoản Discord',
            discordLinked: 'Discord: đã liên kết với {name}',
            discordNotLinked: 'Discord: chưa liên kết',
            changePassword: 'Đổi mật khẩu',
            currentPassword: 'Mật khẩu hiện tại',
            newPassword: 'Mật khẩu mới',
            fillBothFields: 'Vui lòng điền đầy đủ cả hai trường.',
            passwordChanged: 'Đã đổi mật khẩu.',
            errorPrefix: 'Lỗi: ',
            empty: 'Trống',
            stats: {
                battle: 'Trận đấu', teams: 'Đội', inRoster: 'Số người trong đội hình', reserves: 'Dự bị',
                active: 'Đang diễn ra', inactive: 'Chưa bắt đầu',
                noActiveBattle: 'Hiện không có trận nào', since: 'Bắt đầu lúc {time}', inProgress: 'Đang diễn ra'
            },
            roles: { Dps: 'DPS', Tank: 'ĐỠ ĐÒN', Healer: 'TRỊ LIỆU' },
            bulwark: { Top: 'Chắn Thượng Lộ', Center: 'Chắn Trung Lộ', Bottom: 'Chắn Hạ Lộ' }
        },
        dashboard: {
            pageTitle: 'Chỉ huy trưởng · Trực tiếp',
            startBattle: 'Bắt đầu trận',
            endBattle: 'Kết thúc trận',
            endBattleConfirm: 'Lịch sử sẽ được lưu trữ vào file trên server, sau đó xoá cho tất cả mọi người. Tiếp tục?',
            battleStarted: 'trận đấu đã bắt đầu',
            battleEnded: 'trận đấu đã kết thúc, lịch sử đã được lưu trữ',
            errorPrefix: 'lỗi: ',
            battleInProgressSince: 'Trận đấu đang diễn ra từ {time}',
            orderMacros: 'Mệnh lệnh nhanh',
            macroNamePlaceholder: 'Tên mệnh lệnh nhanh, vd: Toàn đội rút lui',
            chooseOrder: 'Chọn mệnh lệnh…',
            allTeams: 'Tất cả các đội',
            saveAsMacro: 'Lưu thành mệnh lệnh nhanh',
            macroSaveHint: 'Dùng các đội đang được chọn bên dưới, trừ khi tick "Tất cả các đội".',
            macroListEmpty: 'Chưa có mệnh lệnh nhanh nào — chọn mục tiêu + mệnh lệnh bên dưới rồi bấm "Lưu thành mệnh lệnh nhanh".',
            issueOrder: 'Ra lệnh',
            target: 'Mục tiêu:',
            noneSelected: 'chưa chọn',
            selectAll: 'Chọn tất cả',
            clear: 'Bỏ chọn',
            selectAttack: 'Đội tấn công',
            selectDefense: 'Đội phòng thủ',
            currentOrderPrefix: 'Lệnh: ',
            eventFeed: 'Nhật ký sự kiện',
            feedEmpty: 'Chưa có sự kiện nào.',
            failedLoadMacros: 'Không tải được mệnh lệnh nhanh: ',
            deleteMacroConfirm: 'Xoá mệnh lệnh nhanh "{name}"?',
            deleteMacroTitle: 'Xoá mệnh lệnh nhanh',
            giveMacroName: 'Hãy đặt tên cho mệnh lệnh nhanh trước.',
            selectTeamOrCheckAll: 'Chọn ít nhất một đội, hoặc tick "Tất cả các đội".',
            selectTeamFirst: 'Hãy chọn ít nhất một đội trước',
            reservesCount: 'Dự bị ({count})',
            online: 'Trực tuyến',
            offline: 'Ngoại tuyến',
            noRolePrefix: '⚠ Thiếu ',
            missingHealer: 'trị liệu',
            missingTank: 'đỡ đòn',
            orderWord: 'LỆNH',
            statusWord: 'TRẠNG THÁI',
            sides: { Attack: 'Tấn công', Defense: 'Phòng thủ', Flex: 'Linh hoạt' },
            tagLabels: {
                Jungle: 'Rừng', Boss: 'Boss', Backup: 'Dự bị',
                JungleTopOwn: 'Rừng Thượng Lộ (phe ta)', JungleBotOwn: 'Rừng Hạ Lộ (phe ta)',
                JungleTopEnemy: 'Rừng Thượng Lộ (địch)', JungleBotEnemy: 'Rừng Hạ Lộ (địch)'
            }
        },
        login: {
            heading: 'Đăng nhập Mạng lưới chiến thuật',
            username: 'Tên đăng nhập',
            password: 'Mật khẩu',
            logIn: 'Đăng nhập',
            or: 'hoặc',
            loginWithDiscord: 'Đăng nhập bằng Discord',
            tooManyAttempts: 'Quá nhiều lần thử, vui lòng thử lại sau.',
            invalidCredentials: 'Sai tên đăng nhập hoặc mật khẩu.',
            connectionError: 'Lỗi kết nối: '
        },
        register: {
            heading: 'Sắp xong rồi',
            defaultHint: 'Xác minh Discord thành công. Vui lòng nhập nickname — quản trị viên sẽ dùng nó để thêm bạn vào đội.',
            sessionNotFound: 'Không tìm thấy phiên làm việc, vui lòng quay lại trang đăng nhập và thử lại.',
            verifiedHint: 'Xác minh Discord thành công: {name}. Vui lòng nhập nickname — quản trị viên sẽ dùng nó để thêm bạn vào đội.',
            nickname: 'Nickname',
            continue: 'Tiếp tục',
            failedCreateAccount: 'Tạo tài khoản thất bại.'
        }
    }
};

function getLang() {
    const stored = localStorage.getItem(I18N_LANG_KEY);
    return I18N_STRINGS[stored] ? stored : I18N_DEFAULT_LANG;
}

function setLang(lang) {
    if (!I18N_STRINGS[lang]) return;
    localStorage.setItem(I18N_LANG_KEY, lang);
    location.reload();
}

function toggleLang() {
    setLang(getLang() === 'en' ? 'vi' : 'en');
}




function t(key, vars) {
    const dict = I18N_STRINGS[getLang()] || I18N_STRINGS[I18N_DEFAULT_LANG];
    let val = key.split('.').reduce((o, k) => (o && o[k] !== undefined) ? o[k] : undefined, dict);
    if (val === undefined) {
        const fallback = I18N_STRINGS[I18N_DEFAULT_LANG];
        val = key.split('.').reduce((o, k) => (o && o[k] !== undefined) ? o[k] : undefined, fallback);
    }
    if (val === undefined) return key;
    if (vars && typeof val === 'string') {
        Object.keys(vars).forEach(k => { val = val.replace(`{${k}}`, vars[k]); });
    }
    return val;
}

function applyI18n(root) {
    const scope = root || document;
    scope.querySelectorAll('[data-i18n]').forEach(el => {
        el.textContent = t(el.getAttribute('data-i18n'));
    });
    scope.querySelectorAll('[data-i18n-placeholder]').forEach(el => {
        el.setAttribute('placeholder', t(el.getAttribute('data-i18n-placeholder')));
    });
    scope.querySelectorAll('[data-i18n-title]').forEach(el => {
        el.setAttribute('title', t(el.getAttribute('data-i18n-title')));
    });
    document.documentElement.lang = getLang();
}

document.addEventListener('DOMContentLoaded', () => applyI18n());
