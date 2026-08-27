const TacnetHud = (function () {


    const HOVER_PROXIMITY_PADDING_PX = 14;

    function initHoverInteractive(selectors) {
        if (!window.tacnetOverlay || !selectors || !selectors.length) return;
        const selector = selectors.join(', ');

        let interactive = false;

        function isNearAnyTarget(x, y) {
            const p = HOVER_PROXIMITY_PADDING_PX;
            const els = document.querySelectorAll(selector);
            for (let i = 0; i < els.length; i++) {
                const r = els[i].getBoundingClientRect();
                if (x >= r.left - p && x <= r.right + p && y >= r.top - p && y <= r.bottom + p) {
                    return true;
                }
            }
            return false;
        }


        document.addEventListener('mousemove', e => {
            if (!interactive && isNearAnyTarget(e.clientX, e.clientY)) {
                interactive = true;
                window.tacnetOverlay.setInteractive(true);
            }
        });


        document.addEventListener('mousedown', e => {
            if (!interactive) return;
            const onTarget = !!(e.target && e.target.closest && e.target.closest(selector));
            if (!onTarget) {
                interactive = false;
                window.tacnetOverlay.setInteractive(false);
            }
        }, true);


        document.addEventListener('mouseleave', () => {
            if (interactive) {
                interactive = false;
                window.tacnetOverlay.setInteractive(false);
            }
        });
    }


    const MIN_WIDGET_WIDTH = 160;
    const MIN_WIDGET_HEIGHT = 70;

    function ensureResizeHandleStyles() {
        if (document.getElementById('tacnet-resize-handle-styles')) return;
        const style = document.createElement('style');
        style.id = 'tacnet-resize-handle-styles';
        style.textContent =
            '.tacnet-resize-handle{position:absolute;right:-7px;bottom:-7px;width:16px;height:16px;' +
            'border-radius:50%;background:#7c6cf0;border:2px solid rgba(0,0,0,0.65);cursor:nwse-resize;' +
            'display:none;z-index:10;touch-action:none;box-shadow:0 0 0 1px rgba(255,255,255,0.25);}' +
            'body.tacnet-edit-mode .tacnet-widget .tacnet-resize-handle{display:block;}' +
            'body.tacnet-edit-mode .tacnet-widget.tacnet-user-hidden .tacnet-resize-handle{display:none;}';
        document.head.appendChild(style);
    }

    function initLayoutEditor(widgets, storageKey) {
        if (!window.tacnetOverlay) return;
        ensureResizeHandleStyles();

        function loadLayout() {
            try { return JSON.parse(localStorage.getItem(storageKey)) || {}; }
            catch { return {}; }
        }
        function saveLayout(layout) {
            try { localStorage.setItem(storageKey, JSON.stringify(layout)); } catch {}
        }
        function patchWidget(key, patch) {
            const layout = loadLayout();
            layout[key] = Object.assign({}, layout[key], patch);
            saveLayout(layout);
        }

        function pin(el, left, top) {
            el.style.left = left + 'px';
            el.style.top = top + 'px';
            el.style.right = 'auto';
            el.style.bottom = 'auto';
        }


        function applySize(el, width, height) {
            el.style.width = width + 'px';
            el.style.maxWidth = width + 'px';
            el.style.height = height + 'px';
            el.style.maxHeight = height + 'px';
            el.style.overflowY = 'auto';
        }

        function clearSize(el) {
            el.style.width = '';
            el.style.maxWidth = '';
            el.style.height = '';
            el.style.maxHeight = '';
            el.style.overflowY = '';
        }

        function applySavedLayout() {
            const layout = loadLayout();
            widgets.forEach(w => {
                const el = w.get();
                if (!el) return;
                const saved = layout[w.key];
                if (!saved) return;
                if (saved.left != null && saved.top != null) pin(el, saved.left, saved.top);
                if (saved.width != null && saved.height != null) applySize(el, saved.width, saved.height);
                if (saved.hidden) el.classList.add('tacnet-user-hidden');
            });
        }


        widgets.forEach(w => {
            const el = w.get();
            if (!el) return;
            el.classList.add('tacnet-widget');

            let dragging = false, pointerId = null, startX, startY, origLeft, origTop;

            el.addEventListener('pointerdown', e => {
                if (!document.body.classList.contains('tacnet-edit-mode')) return;
                dragging = true;
                pointerId = e.pointerId;
                el.setPointerCapture(pointerId);
                const rect = el.getBoundingClientRect();
                startX = e.clientX; startY = e.clientY;
                origLeft = rect.left; origTop = rect.top;
                pin(el, origLeft, origTop);
                e.preventDefault();
            });
            el.addEventListener('pointermove', e => {
                if (!dragging || e.pointerId !== pointerId) return;
                const left = Math.max(0, Math.min(window.innerWidth - el.offsetWidth, origLeft + (e.clientX - startX)));
                const top = Math.max(0, Math.min(window.innerHeight - el.offsetHeight, origTop + (e.clientY - startY)));
                el.style.left = left + 'px';
                el.style.top = top + 'px';
            });
            el.addEventListener('pointerup', e => {
                if (!dragging || e.pointerId !== pointerId) return;
                dragging = false;
                el.releasePointerCapture(pointerId);
                patchWidget(w.key, { left: parseFloat(el.style.left), top: parseFloat(el.style.top) });
            });

            const handle = document.createElement('div');
            handle.className = 'tacnet-resize-handle';
            handle.title = 'Drag to resize, double-click to reset';
            el.appendChild(handle);

            let resizing = false, resizePointerId = null, resizeStartX, resizeStartY,
                origWidth, origHeight, resizeOrigLeft, resizeOrigTop, pendingWidth, pendingHeight;

            handle.addEventListener('pointerdown', e => {
                if (!document.body.classList.contains('tacnet-edit-mode')) return;
                e.stopPropagation();
                e.preventDefault();
                resizing = true;
                resizePointerId = e.pointerId;
                handle.setPointerCapture(resizePointerId);
                resizeStartX = e.clientX; resizeStartY = e.clientY;
                const rect = el.getBoundingClientRect();
                origWidth = rect.width; origHeight = rect.height;
                resizeOrigLeft = rect.left; resizeOrigTop = rect.top;
                pendingWidth = origWidth; pendingHeight = origHeight;
            });
            handle.addEventListener('pointermove', e => {
                if (!resizing || e.pointerId !== resizePointerId) return;
                e.stopPropagation();

                const maxWidth = window.innerWidth - resizeOrigLeft - 4;
                const maxHeight = window.innerHeight - resizeOrigTop - 4;
                pendingWidth = Math.max(MIN_WIDGET_WIDTH, Math.min(maxWidth, origWidth + (e.clientX - resizeStartX)));
                pendingHeight = Math.max(MIN_WIDGET_HEIGHT, Math.min(maxHeight, origHeight + (e.clientY - resizeStartY)));
                applySize(el, pendingWidth, pendingHeight);
            });
            handle.addEventListener('pointerup', e => {
                if (!resizing || e.pointerId !== resizePointerId) return;
                e.stopPropagation();
                resizing = false;
                handle.releasePointerCapture(resizePointerId);
                patchWidget(w.key, { width: pendingWidth, height: pendingHeight });
            });
            handle.addEventListener('dblclick', e => {
                if (!document.body.classList.contains('tacnet-edit-mode')) return;
                e.stopPropagation();
                clearSize(el);
                patchWidget(w.key, { width: null, height: null });
            });
        });

        function buildPalette() {
            if (document.getElementById('tacnet-edit-palette')) return;
            const layout = loadLayout();
            const panel = document.createElement('div');
            panel.id = 'tacnet-edit-palette';
            panel.innerHTML =
                '<div class="tep-title">Overlay layout — \u2193 to exit</div>' +
                '<div class="tep-hint">Drag a widget to move it. Drag its corner dot to resize — a bigger panel shows more of its fields/buttons (double-click the dot to reset size).</div>' +
                widgets.map(w => {
                    const hidden = (layout[w.key] || {}).hidden;
                    return `<label class="tep-row"><input type="checkbox" data-key="${w.key}" ${hidden ? '' : 'checked'}> ${w.label}</label>`;
                }).join('') +
                '<button type="button" id="tep-reset">Reset layout</button>';
            document.body.appendChild(panel);

            panel.querySelectorAll('input[type=checkbox]').forEach(cb => {
                cb.addEventListener('change', () => {
                    const w = widgets.find(w => w.key === cb.dataset.key);
                    const el = w && w.get();
                    if (!el) return;
                    const hidden = !cb.checked;
                    el.classList.toggle('tacnet-user-hidden', hidden);
                    patchWidget(cb.dataset.key, { hidden });
                });
            });
            panel.querySelector('#tep-reset').addEventListener('click', () => {
                localStorage.removeItem(storageKey);
                location.reload();
            });
        }

        function removePalette() {
            const panel = document.getElementById('tacnet-edit-palette');
            if (panel) panel.remove();
        }

        function setEditMode(on) {
            document.body.classList.toggle('tacnet-edit-mode', on);
            window.tacnetOverlay.setInteractive(on);
            if (on) buildPalette(); else removePalette();
        }

        applySavedLayout();

        if (window.tacnetOverlay.onToggleEditMode) {
            window.tacnetOverlay.onToggleEditMode(() => {
                setEditMode(!document.body.classList.contains('tacnet-edit-mode'));
            });
        }
    }


    function init({ hoverSelectors, widgets, storageKey }) {
        if (!window.tacnetOverlay) return false;
        document.body.classList.add('tacnet-hud');
        initHoverInteractive(hoverSelectors);
        if (widgets && widgets.length) initLayoutEditor(widgets, storageKey);
        return true;
    }

    return { init, initHoverInteractive, initLayoutEditor };
})();
