// Shared i18n: language switching between English and Chinese.


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
    zh: {
        langName: '中文',
        common: {
            connecting: '连接中...',
            connected: '已连接',
            reconnecting: '重新连接中...',
            connectionFailed: '连接失败: ',
            loading: '加载中...',
            logout: '退出登录',
            menu: '菜单',
            backToLogin: '返回登录',
            noAccess: '您的角色（{role}）无权访问此页面。'
        },
        player: {
            player: '玩家',
            team: '小队',
            reserves: '替补',
            none: '无',
            currentOrder: '当前指令',
            needHelp: '需要支援',
            statusPrefix: '状态：',
            bossSpawning: 'BOSS 即将刷新',
            spawningSuffix: ' 即将刷新',
            and: ' 和 ',
            eventFallback: '事件'
        },
        orderTypes: {
            PushBot: '推下路', PushMid: '推中路', PushTop: '推上路', AttackGoose: '攻击鹅',
            KillBoss: '击杀BOSS', Bomb: '安放炸弹',
            DefendBot: '防守下路', DefendMid: '防守中路', DefendTop: '防守上路', DefendGoose: '防守鹅', DefendTree: '防守圣树',
            Hold: '原地待命',
            BotJungle: '下路野区', TopJungle: '上路野区'
        },
        jungleTags: {
            JungleTopOwn: '上路野区（己方）',
            JungleBotOwn: '下路野区（己方）',
            JungleTopEnemy: '上路野区（敌方）',
            JungleBotEnemy: '下路野区（敌方）',
            Jungle: '野怪刷新'
        },
        statusTypes: { SquadWiped: '全队阵亡', Regroup: '集合', NeedHelp: '需要支援', Retreating: '撤退中', Autonomous: '自主行动' },
        sidebar: {
            downloadApp: '下载应用',
            download: '下载 {tag}',
            siteGuide: '网站指南',
            overlayGuide: '悬浮窗指南',
            labels: { Player: '玩家', Observer: '队长', Dashboard: '指挥官', Admin: '管理面板', Replay: '回放' }
        },
        observer: {
            pageTitle: '队长',
            pageSubtitle: '向本队下达指令并上报本队状态。',
            loadingProfile: '正在加载资料...',
            signedInAs: '登录身份：',
            team: '小队：',
            unassigned: '未分配',
            enemyHeading: '敌情',
            teamStatusHeading: '小队状态',
            currentStatus: '当前状态',
            autoSwitchHint: '如果不更新，25秒后将自动切换为“自主行动”。',
            incomingOrders: '收到的指令',
            enemyRoles: { TwinBlades: '敌方双刀', Healer: '敌方治疗', Tank: '敌方坦克', Nameless: '敌方无名' }
        },
        menu: {
            brandEyebrow: '战术网络',
            pageTitle: '首页',
            signedInAs: '登录身份 {user}（{role}）',
            activeRoster: '当前编队',
            noTeams: '暂未设置队伍。',
            reserves: '替补',
            noReserves: '暂无替补。',
            account: '账户',
            linkDiscord: '绑定 Discord 账户',
            relinkDiscord: '重新绑定 Discord 账户',
            discordLinked: 'Discord：已绑定为 {name}',
            discordNotLinked: 'Discord：未绑定',
            changePassword: '修改密码',
            currentPassword: '当前密码',
            newPassword: '新密码',
            fillBothFields: '请填写两个字段。',
            passwordChanged: '密码已修改。',
            errorPrefix: '错误：',
            empty: '空缺',
            stats: {
                battle: '战斗', teams: '队伍', inRoster: '编队人数', reserves: '替补',
                active: '进行中', inactive: '未开始',
                noActiveBattle: '暂无进行中的战斗', since: '始于 {time}', inProgress: '进行中'
            },
            roles: { Dps: '输出', Tank: '坦克', Healer: '治疗' },
            bulwark: { Top: '上路壁垒', Center: '中路壁垒', Bottom: '下路壁垒' }
        },
        dashboard: {
            pageTitle: '总指挥 · 实时战报',
            startBattle: '开始战斗',
            endBattle: '结束战斗',
            endBattleConfirm: '记录将被归档到服务器文件，然后对所有人清空。是否继续？',
            battleStarted: '战斗已开始',
            battleEnded: '战斗已结束，记录已归档',
            errorPrefix: '错误：',
            battleInProgressSince: '战斗进行中，开始于 {time}',
            orderMacros: '指令宏',
            macroNamePlaceholder: '宏名称，例如：全员撤退',
            chooseOrder: '选择指令…',
            allTeams: '全部小队',
            saveAsMacro: '保存为宏',
            macroSaveHint: '使用下方当前选中的小队，除非勾选了"全部小队"。',
            macroListEmpty: '尚未保存任何宏——在下方选择目标和指令，然后点击"保存为宏"。',
            issueOrder: '下达指令',
            target: '目标：',
            noneSelected: '未选择',
            selectAll: '全选',
            clear: '清除',
            eventFeed: '事件日志',
            feedEmpty: '暂无事件。',
            failedLoadMacros: '加载宏失败：',
            deleteMacroConfirm: '删除宏"{name}"？',
            deleteMacroTitle: '删除宏',
            giveMacroName: '请先给宏命名。',
            selectTeamOrCheckAll: '请至少选择一个小队，或勾选"全部小队"。',
            selectTeamFirst: '请先选择至少一个小队',
            reservesCount: '替补（{count}）',
            online: '在线',
            offline: '离线',
            noRolePrefix: '⚠ 缺少 ',
            missingHealer: '治疗',
            missingTank: '坦克',
            orderWord: '指令',
            statusWord: '状态',
            sides: { Attack: '进攻', Defense: '防守', Flex: '机动' },
            tagLabels: {
                Jungle: '野区', Boss: 'Boss', Backup: '候补',
                JungleTopOwn: '上路野区（己方）', JungleBotOwn: '下路野区（己方）',
                JungleTopEnemy: '上路野区（敌方）', JungleBotEnemy: '下路野区（敌方）'
            }
        },
        login: {
            heading: '登录战术网络',
            username: '用户名',
            password: '密码',
            logIn: '登录',
            or: '或',
            loginWithDiscord: '使用 Discord 登录',
            tooManyAttempts: '尝试次数过多，请稍后再试。',
            invalidCredentials: '用户名或密码错误。',
            connectionError: '连接错误：'
        },
        register: {
            heading: '即将完成',
            defaultHint: 'Discord 验证成功。请填写您的昵称，管理员将据此把您加入队伍。',
            sessionNotFound: '未找到会话，请返回登录页面重试。',
            verifiedHint: 'Discord 验证成功：{name}。请填写您的昵称——管理员将据此把您加入队伍。',
            nickname: '昵称',
            continue: '继续',
            failedCreateAccount: '创建账户失败。'
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
    setLang(getLang() === 'en' ? 'zh' : 'en');
}

// t('player.needHelp') -> looks up I18N_STRINGS[currentLang].player.needHelp
// Optional vars: t('common.noAccess', { role: 'Admin' }) replaces {role} placeholders.
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
