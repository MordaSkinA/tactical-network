
(function () {
  function init() {
    if (document.getElementById('sk-egg-btn')) return;

    const style = document.createElement('style');
    style.textContent = `
      #sk-egg-btn {
        position: fixed;
        right: 22px;
        bottom: 22px;
        width: 50px;
        height: 50px;
        border-radius: 50%;
        background: var(--surface-2, #1a1a1d);
        border: 1px solid var(--border, #262629);
        display: flex;
        align-items: center;
        justify-content: center;
        font-size: 24px;
        line-height: 1;
        cursor: pointer;
        z-index: 2147483000;
        box-shadow: 0 6px 18px rgba(0,0,0,0.45);
        animation: sk-egg-float 2.6s ease-in-out infinite;
        transition: transform 0.2s ease, filter 0.2s ease;
        user-select: none;
      }
      #sk-egg-btn:hover {
        transform: scale(1.12) rotate(-8deg);
        filter: drop-shadow(0 0 8px rgba(155, 143, 245, 0.55));
      }
      #sk-egg-btn:active { transform: scale(0.92); }
      @keyframes sk-egg-float {
        0%, 100% { transform: translateY(0) rotate(0deg); }
        50%      { transform: translateY(-6px) rotate(-4deg); }
      }
      @keyframes sk-egg-shake {
        0%, 100% { transform: translate(0,0); }
        20%      { transform: translate(-2px,1px); }
        40%      { transform: translate(2px,-1px); }
        60%      { transform: translate(-2px,-1px); }
        80%      { transform: translate(2px,1px); }
      }
      .sk-egg-shaking { animation: sk-egg-shake 0.35s linear 2; }
      #sk-egg-toast {
        position: fixed;
        left: 50%;
        bottom: 26px;
        transform: translate(-50%, 12px);
        background: var(--surface-3, #202024);
        color: var(--text, #f0f0f2);
        border: 1px solid var(--border, #262629);
        padding: 10px 16px;
        border-radius: 999px;
        font: 600 13px/1 'Inter', system-ui, sans-serif;
        z-index: 2147483000;
        opacity: 0;
        pointer-events: none;
        transition: opacity 0.4s ease, transform 0.4s ease;
        white-space: nowrap;
      }
      #sk-egg-toast.show { opacity: 1; transform: translate(-50%, 0); }
    `;
    document.head.appendChild(style);

    const btn = document.createElement('div');
    btn.id = 'sk-egg-btn';
    btn.title = 'не нажимай сюда';
    btn.textContent = '\u{1F480}'; // 💀
    document.body.appendChild(btn);

    let triggered = false;

    btn.addEventListener('click', () => {
      if (triggered) return;
      triggered = true;
      btn.classList.add('sk-egg-shaking');
      document.documentElement.classList.add('sk-egg-shaking');
      setTimeout(() => {
        document.documentElement.classList.remove('sk-egg-shaking');
        collapseEverything(btn);
      }, 260);
    });
  }

  function collapseEverything(btn) {
    const skipTags = new Set(['SCRIPT', 'STYLE', 'LINK', 'META', 'TITLE', 'HEAD', 'HTML', 'BODY']);
    const nodes = Array.from(document.querySelectorAll('body *')).filter(el => {
      if (skipTags.has(el.tagName)) return false;
      if (el === btn || btn.contains(el)) return false;
      if (el.id === 'sk-egg-toast') return false;
      const cs = getComputedStyle(el);
      if (cs.display === 'none' || cs.visibility === 'hidden') return false;
      return true;
    });

    // freeze every node at its current on-screen position first,
    // so removing one from flow doesn't shove the others around
    const frozen = nodes.map(el => {
      const rect = el.getBoundingClientRect();
      return { el, rect };
    }).filter(f => f.rect.width > 0 && f.rect.height > 0);

    frozen.forEach(({ el, rect }) => {
      el.style.position = 'fixed';
      el.style.left = rect.left + 'px';
      el.style.top = rect.top + 'px';
      el.style.width = rect.width + 'px';
      el.style.height = rect.height + 'px';
      el.style.margin = '0';
      el.style.pointerEvents = 'none';
      el.style.zIndex = String(Math.floor(Math.random() * 50) + 1);
      el.style.transition = 'none';
      // A lingering transform/filter/perspective on ANY of these
      // elements would turn it into a containing block for its own
      // position:fixed children, throwing their left/top off relative
      // to the viewport (looks like a sideways jump before the fall).
      // Clear both here so every node's fixed coordinates stay
      // anchored to the real viewport.
      el.style.transform = 'none';
      el.style.filter = 'none';
      el.style.animation = 'none';
    });

    // next frame: send everything tumbling to the floor
    requestAnimationFrame(() => {
      const vh = window.innerHeight;
      frozen.forEach(({ el, rect }, i) => {
        const delay = Math.random() * 0.4;
        const duration = 0.7 + Math.random() * 0.7;
        const rotate = (Math.random() * 260 - 130).toFixed(1);
        const driftX = (Math.random() * 140 - 70).toFixed(0);
        const fallY = (vh - rect.top + 80 + Math.random() * 120).toFixed(0);
        el.style.transition =
          `transform ${duration}s cubic-bezier(.55,.06,.68,.19) ${delay}s, ` +
          `opacity 0.5s ease ${delay + duration * 0.7}s`;
        el.style.transform = `translate(${driftX}px, ${fallY}px) rotate(${rotate}deg)`;
      });

      btn.style.transition = 'transform 0.4s ease 0.2s';
      btn.style.transform = 'translateY(-140%) rotate(20deg)';

      setTimeout(showToast, 1400);
    });
  }

  function showToast() {
    const toast = document.createElement('div');
    toast.id = 'sk-egg-toast';
    toast.textContent = '\u{1F480} всё упало. F5, чтобы вернуть на место';
    document.body.appendChild(toast);
    requestAnimationFrame(() => toast.classList.add('show'));
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }
})();
