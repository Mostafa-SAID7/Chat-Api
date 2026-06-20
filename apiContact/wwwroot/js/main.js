'use strict';

/* ── Inline SVG icon map (dynamic-only icons) ─────────────── */
const ICONS = {
  sun: `<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><circle cx="12" cy="12" r="4"/><path d="M12 2v2M12 20v2M4.93 4.93l1.41 1.41M17.66 17.66l1.41 1.41M2 12h2M20 12h2M6.34 17.66l-1.41 1.41M19.07 4.93l-1.41 1.41"/></svg>`,
  moon: `<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M12 3a6 6 0 0 0 9 9 9 9 0 1 1-9-9Z"/></svg>`,
  menu: `<svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><line x1="4" x2="20" y1="12" y2="12"/><line x1="4" x2="20" y1="6" y2="6"/><line x1="4" x2="20" y1="18" y2="18"/></svg>`,
  x: `<svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M18 6 6 18"/><path d="m6 6 12 12"/></svg>`,
  check: `<svg xmlns="http://www.w3.org/2000/svg" width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M20 6 9 17l-5-5"/></svg>`,
};

/* ── Page loader ──────────────────────────────────────────── */
function dismissLoader() {
  const loader = document.getElementById('page-loader');
  if (!loader) return;
  requestAnimationFrame(() => {
    loader.classList.add('hidden');
    document.body.classList.add('page-ready');
    loader.addEventListener('transitionend', () => loader.remove(), { once: true });
  });
}

/* ── Theme ───────────────────────────────────────────────── */
const THEME_KEY = 'chatapi-theme';

function applyTheme(theme) {
  document.documentElement.setAttribute('data-theme', theme);
  localStorage.setItem(THEME_KEY, theme);
}

function initTheme() {
  const saved = localStorage.getItem(THEME_KEY)
    || (window.matchMedia('(prefers-color-scheme: light)').matches ? 'light' : 'dark');
  applyTheme(saved);
}

/* ── Hamburger menu ──────────────────────────────────────── */
function initHamburger() {
  const btn  = document.getElementById('hamburger');
  const menu = document.getElementById('mobile-menu');
  if (!btn || !menu) return;

  function closeMenu() {
    menu.classList.remove('open');
    btn.classList.remove('open');
    btn.setAttribute('aria-expanded', 'false');
    document.body.style.overflow = '';
  }

  btn.addEventListener('click', () => {
    const open = menu.classList.toggle('open');
    btn.classList.toggle('open', open);
    btn.setAttribute('aria-expanded', String(open));
    document.body.style.overflow = open ? 'hidden' : '';
  });

  menu.querySelectorAll('a').forEach(a => a.addEventListener('click', closeMenu));

  document.addEventListener('click', e => {
    if (!menu.contains(e.target) && !btn.contains(e.target)) closeMenu();
  });
}

/* ── Active nav link ─────────────────────────────────────── */
function initNav() {
  const path = window.location.pathname;
  document.querySelectorAll('.nav-links a, .mobile-menu a').forEach(a => {
    const href = a.getAttribute('href');
    if (!href) return;
    const match = (href === '/' && (path === '/' || path === '/index.html'))
               || (href !== '/' && path.startsWith(href));
    a.classList.toggle('active', match);
  });
}

/* ── Docs tab navigation ─────────────────────────────────── */
function initDocsTabs() {
  const tabs    = document.querySelectorAll('.dtab');
  const panels  = document.querySelectorAll('.tab-panel');
  if (!tabs.length || !panels.length) return;

  const STORAGE_KEY = 'chatapi-docs-tab';

  function activateTab(tabId, fromClick) {
    tabs.forEach(t => {
      const active = t.dataset.tab === tabId;
      t.classList.toggle('active', active);
      t.setAttribute('aria-selected', String(active));
    });
    panels.forEach(p => {
      p.classList.toggle('active', p.id === 'tab-' + tabId);
    });
    localStorage.setItem(STORAGE_KEY, tabId);

    // Scroll the active tab button into view horizontally in the tab bar
    const activeTab = document.querySelector(`.dtab[data-tab="${tabId}"]`);
    if (activeTab) {
      activeTab.scrollIntoView({ behavior: 'smooth', block: 'nearest', inline: 'center' });
    }

    // When user clicks a tab, scroll the page so content is visible below the two sticky bars
    if (fromClick) {
      const navH    = parseInt(getComputedStyle(document.documentElement).getPropertyValue('--nav-h'))    || 60;
      const tabnavH = parseInt(getComputedStyle(document.documentElement).getPropertyValue('--tabnav-h')) || 48;
      const content = document.querySelector('.docs-content');
      if (content) {
        const top = content.getBoundingClientRect().top + window.scrollY - navH - tabnavH - 12;
        window.scrollTo({ top, behavior: 'smooth' });
      }
    }
  }

  tabs.forEach(tab => {
    tab.addEventListener('click', () => activateTab(tab.dataset.tab, true));
  });

  // Restore last active tab or use URL hash
  const hash    = window.location.hash.replace('#', '');
  const saved   = localStorage.getItem(STORAGE_KEY);
  const validIds = Array.from(tabs).map(t => t.dataset.tab);
  const initial = validIds.includes(hash) ? hash
                : validIds.includes(saved) ? saved
                : validIds[0];
  activateTab(initial, false);
}

/* ── Endpoint accordion ──────────────────────────────────── */
function initEndpointAccordion() {
  document.querySelectorAll('.ep-toggle').forEach(toggle => {
    toggle.addEventListener('click', () => {
      const item = toggle.closest('.ep-item');
      if (!item) return;
      // Close siblings in the same list
      const list = item.closest('.ep-list');
      if (list) {
        list.querySelectorAll('.ep-item.open').forEach(open => {
          if (open !== item) open.classList.remove('open');
        });
      }
      item.classList.toggle('open');
    });
  });
}

/* ── Copy buttons ────────────────────────────────────────── */
function initCopyBtns() {
  document.querySelectorAll('.copy-btn').forEach(btn => {
    btn.addEventListener('click', async () => {
      const block = btn.closest('.code-block');
      const clone = block.cloneNode(true);
      clone.querySelectorAll('button').forEach(b => b.remove());
      const text = clone.innerText.trim();
      try {
        await navigator.clipboard.writeText(text);
        const orig = btn.innerHTML;
        btn.innerHTML = `${ICONS.check} Copied`;
        btn.style.color = 'var(--accent2)';
        setTimeout(() => { btn.innerHTML = orig; btn.style.color = ''; }, 2000);
      } catch {
        const range = document.createRange();
        range.selectNodeContents(block);
        window.getSelection()?.removeAllRanges();
        window.getSelection()?.addRange(range);
      }
    });
  });
}

/* ── Scroll-reveal ───────────────────────────────────────── */
function initReveal() {
  // Observe ALL .reveal elements (containers + children) so parent
  // opacity:0 never hides content that should be visible.
  const els = document.querySelectorAll('.reveal');
  if (!els.length) return;

  const io = new IntersectionObserver(entries => {
    entries.forEach(e => {
      if (e.isIntersecting) {
        e.target.classList.add('visible');
        io.unobserve(e.target);
      }
    });
  }, { threshold: 0.05, rootMargin: '0px 0px -20px 0px' });

  els.forEach(el => io.observe(el));
}

/* ── API status badge ────────────────────────────────────── */
function initStatusBadge() {
  const el = document.getElementById('api-status');
  if (!el) return;
  const check = async () => {
    try {
      const r = await fetch('/health', { signal: AbortSignal.timeout(4000) });
      if (r.ok) { el.textContent = 'API Online'; el.className = 'badge badge-green'; }
      else throw 0;
    } catch {
      el.textContent = 'API Offline'; el.className = 'badge badge-orange';
    }
  };
  check();
  setInterval(check, 20000);
}

/* ── Stat counters ───────────────────────────────────────── */
function initCounters() {
  const els = document.querySelectorAll('[data-count]');
  if (!els.length) return;
  const io = new IntersectionObserver(entries => {
    entries.forEach(e => {
      if (!e.isIntersecting) return;
      io.unobserve(e.target);
      const el = e.target;
      const target = parseInt(el.dataset.count, 10);
      const suffix = el.dataset.suffix || '';
      const dur = 900, start = performance.now();
      const tick = now => {
        const p = Math.min((now - start) / dur, 1);
        el.textContent = Math.round((1 - Math.pow(1 - p, 3)) * target) + suffix;
        if (p < 1) requestAnimationFrame(tick);
      };
      requestAnimationFrame(tick);
    });
  }, { threshold: 0.5 });
  els.forEach(el => io.observe(el));
}

/* ── Typewriter ──────────────────────────────────────────── */
function initTypewriter() {
  const el = document.getElementById('typewriter');
  if (!el) return;
  const texts = ['Real-time Messaging', 'WebSocket Events', 'MongoDB Persistence', 'Redis Pub/Sub', 'JWT Authentication', 'File Uploads'];
  let i = 0, j = 0, del = false;
  const tick = () => {
    const txt = texts[i];
    el.textContent = del ? txt.slice(0, j--) : txt.slice(0, j++);
    if (!del && j > txt.length) { del = true; setTimeout(tick, 1400); return; }
    if (del && j < 0) { del = false; i = (i + 1) % texts.length; j = 0; }
    setTimeout(tick, del ? 38 : 72);
  };
  tick();
}

/* ── Scroll-to-top button ────────────────────────────────── */
function initScrollTop() {
  const btn = document.getElementById('scroll-top');
  if (!btn) return;

  const onScroll = () => {
    btn.classList.toggle('visible', window.scrollY > 320);
  };
  window.addEventListener('scroll', onScroll, { passive: true });
  onScroll();

  btn.addEventListener('click', () => {
    window.scrollTo({ top: 0, behavior: 'smooth' });
  });
}

/* ── Lucide icon init ────────────────────────────────────── */
function initIcons() {
  if (typeof lucide !== 'undefined') lucide.createIcons();
}

/* ── Dynamic base URL ────────────────────────────────────── */
function initBaseUrl() {
  const origin = window.location.origin;
  document.querySelectorAll('[id^="base-url"]').forEach(el => {
    el.textContent = origin;
  });
}

/* ── Error boundary: catch unhandled JS errors ───────────── */
function initErrorBoundary() {
  const toast = (msg) => {
    if (document.getElementById('err-toast')) return;
    const t = document.createElement('div');
    t.id = 'err-toast';
    t.setAttribute('role', 'alert');
    Object.assign(t.style, {
      position: 'fixed', bottom: '4.5rem', left: '50%',
      transform: 'translateX(-50%)',
      background: 'var(--bg2,#1e1e1e)', color: 'var(--text,#e5e5e5)',
      border: '1px solid rgba(249,115,22,.4)', borderRadius: '8px',
      padding: '.55rem 1.1rem', fontSize: '.78rem', zIndex: '9999',
      boxShadow: '0 4px 20px rgba(0,0,0,.4)', maxWidth: '90vw',
      fontFamily: 'sans-serif', lineHeight: '1.5',
      opacity: '0', transition: 'opacity .25s',
      display: 'flex', alignItems: 'center', gap: '8px',
    });
    t.innerHTML = `<span style="color:#f97316">⚠</span> ${msg}`;
    document.body.appendChild(t);
    requestAnimationFrame(() => { t.style.opacity = '1'; });
    setTimeout(() => {
      t.style.opacity = '0';
      t.addEventListener('transitionend', () => t.remove(), { once: true });
    }, 5000);
  };

  window.addEventListener('error', e => {
    if (e.message && !e.message.includes('Script error')) {
      toast('An unexpected error occurred. Check the console for details.');
    }
  });

  window.addEventListener('unhandledrejection', e => {
    const msg = e.reason?.message || String(e.reason || 'Unknown');
    if (!msg.includes('AbortError') && !msg.includes('abort')) {
      toast('An async error occurred. Check the console for details.');
    }
  });
}

/* ── API Playground ──────────────────────────────────────── */
function initPlayground() {
  const PG_REQS = {
    login:      { method:'POST', url:'/api/auth/login',              auth:false, body:{usernameOrEmail:'alice',password:'password123'}, hint:'Demo: alice / password123' },
    register:   { method:'POST', url:'/api/auth/register',           auth:false, body:{username:'newuser',email:'new@example.com',password:'Pass123!',displayName:'New User'}, hint:'Create a new account' },
    me:         { method:'GET',  url:'/api/auth/me',                 auth:true,  body:null, hint:'Returns your authenticated profile' },
    health:     { method:'GET',  url:'/health',                      auth:false, body:null, hint:'Public health check — no auth required' },
    rooms:      { method:'GET',  url:'/api/rooms/mine',              auth:true,  body:null, hint:'Your joined rooms (auth required)' },
    createRoom: { method:'POST', url:'/api/rooms',                   auth:true,  body:{name:'My Room',type:'Group',description:'A test room'}, hint:'Create a new group room' },
    users:      { method:'GET',  url:'/api/users/online',            auth:true,  body:null, hint:'Currently online users (auth required)' },
    sendMsg:    { method:'POST', url:'/api/messages',                auth:true,  body:{roomId:'<replace-with-room-id>',content:'Hello from Playground!'}, hint:'Replace roomId with a real room GUID from My Rooms' },
    audit:      { method:'GET',  url:'/api/audit/recent?limit=5',    auth:true,  body:null, hint:'Recent audit events — admin only' },
  };

  let pgToken = null, pgUser = null, current = 'login';

  const tabEls    = document.querySelectorAll('.pg-tab');
  const methodBdg = document.getElementById('pg-method-badge');
  const urlEl     = document.getElementById('pg-url');
  const bodyWrap  = document.getElementById('pg-body-wrap');
  const bodyEl    = document.getElementById('pg-body');
  const runBtn    = document.getElementById('pg-run');
  const resetBtn  = document.getElementById('pg-reset');
  const hintEl    = document.getElementById('pg-hint');
  const resBody   = document.getElementById('pg-res-body');
  const resMeta   = document.getElementById('pg-res-meta');
  const copyBtn   = document.getElementById('pg-copy-btn');
  const authDot   = document.getElementById('pg-auth-dot');
  const authLabel = document.getElementById('pg-auth-label');
  const logoutBtn = document.getElementById('pg-logout');
  if (!runBtn) return;

  /* ── JSON escape + syntax highlight ── */
  function esc(s) {
    return String(s).replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;');
  }
  function highlight(raw) {
    const str = typeof raw === 'string' ? raw : JSON.stringify(raw, null, 2);
    return esc(str).replace(
      /("(?:\\u[\dA-Fa-f]{4}|\\[^u]|[^"\\])*"(?:\s*:)?|-?\d+(?:\.\d*)?(?:[eE][+-]?\d+)?|true|false|null)/g,
      m => {
        if (/^"/.test(m)) return `<span class="${/:$/.test(m)?'jk':'js'}">${m}</span>`;
        if (/true|false/.test(m)) return `<span class="jb">${m}</span>`;
        if (m === 'null') return `<span class="jx">${m}</span>`;
        return `<span class="jn">${m}</span>`;
      }
    );
  }

  /* ── Auth bar ── */
  function updateAuth() {
    if (pgToken) {
      authDot.className = 'status-dot online';
      authLabel.textContent = pgUser ? `Authenticated as ${pgUser}` : 'Authenticated';
      logoutBtn.style.display = 'flex';
    } else {
      authDot.className = 'status-dot standby';
      authLabel.textContent = 'Not authenticated — run Login first';
      logoutBtn.style.display = 'none';
    }
  }

  /* ── Load request into UI ── */
  function loadReq(key) {
    current = key;
    const r = PG_REQS[key];
    if (!r) return;
    methodBdg.className = `method method-${r.method.toLowerCase()}`;
    methodBdg.textContent = r.method;
    urlEl.textContent = r.url;
    if (r.body !== null) {
      bodyWrap.style.display = 'flex';
      bodyEl.value = JSON.stringify(r.body, null, 2);
    } else {
      bodyWrap.style.display = 'none';
      bodyEl.value = '';
    }
    if (r.auth && !pgToken) {
      hintEl.textContent = '⚠ Login first to authenticate';
      hintEl.classList.add('pg-hint-warn');
    } else {
      hintEl.textContent = r.hint || '';
      hintEl.classList.remove('pg-hint-warn');
    }
    resBody.innerHTML = '<span class="pg-placeholder">Hit "Run" to see the live response here…</span>';
    resMeta.innerHTML = '';
    copyBtn.style.visibility = 'hidden';
  }

  /* ── Tab switching ── */
  tabEls.forEach(t => t.addEventListener('click', () => {
    tabEls.forEach(x => x.classList.remove('active'));
    t.classList.add('active');
    loadReq(t.dataset.req);
  }));

  resetBtn.addEventListener('click', () => {
    const r = PG_REQS[current];
    if (r?.body !== null) bodyEl.value = JSON.stringify(r.body, null, 2);
  });

  logoutBtn.addEventListener('click', () => {
    pgToken = null; pgUser = null;
    updateAuth();
    loadReq(current);
  });

  /* ── Run request ── */
  async function runRequest() {
    const r = PG_REQS[current];
    if (!r) return;

    let parsedBody = null;
    if (r.body !== null) {
      try { parsedBody = JSON.parse(bodyEl.value); }
      catch { resBody.innerHTML = '<span class="pg-err">❌ Invalid JSON in request body — check syntax</span>'; return; }
    }
    if (r.auth && !pgToken) {
      resBody.innerHTML = '<span class="pg-err">⚠ Not authenticated. Switch to the Login tab and run it first.</span>';
      return;
    }

    const headers = { 'Accept': 'application/json', 'Content-Type': 'application/json' };
    if (r.auth && pgToken) headers['Authorization'] = `Bearer ${pgToken}`;

    runBtn.disabled = true;
    runBtn.innerHTML = '<span class="pg-spinner"></span> Running…';
    resBody.innerHTML = '<span class="pg-placeholder">Sending request…</span>';
    resMeta.innerHTML = '';
    copyBtn.style.visibility = 'hidden';

    const t0 = performance.now();
    try {
      const resp = await fetch(r.url, {
        method: r.method,
        headers,
        body: parsedBody !== null ? JSON.stringify(parsedBody) : undefined,
        signal: AbortSignal.timeout(10000),
      });
      const ms = Math.round(performance.now() - t0);
      const ct = resp.headers.get('content-type') || '';
      const raw = await resp.text();
      const isJson = ct.includes('json') || (raw.trim().startsWith('{') || raw.trim().startsWith('['));

      let pretty = raw;
      if (isJson) { try { pretty = JSON.stringify(JSON.parse(raw), null, 2); } catch { /* keep raw */ } }

      const sc = resp.status;
      const cls = sc < 300 ? 'pg-s-ok' : sc < 500 ? 'pg-s-warn' : 'pg-s-err';
      resMeta.innerHTML =
        `<span class="${cls}">${sc} ${resp.statusText}</span>`+
        `<span class="pg-meta-sep">·</span>`+
        `<span class="pg-meta-time">${ms}ms</span>`;

      /* Extract token on successful login */
      if (current === 'login' && resp.ok && isJson) {
        try {
          const parsed = JSON.parse(raw);
          const tok = parsed?.data?.accessToken;
          if (tok) {
            pgToken = tok;
            pgUser  = parsed?.data?.user?.displayName || parsed?.data?.user?.username || 'user';
            updateAuth();
          }
        } catch { /* ignore */ }
      }

      resBody.innerHTML = isJson ? highlight(pretty) : `<span class="pg-raw">${esc(raw)}</span>`;
      copyBtn.style.visibility = 'visible';

    } catch (err) {
      const ms = Math.round(performance.now() - t0);
      resMeta.innerHTML = `<span class="pg-s-err">Error</span><span class="pg-meta-sep">·</span><span class="pg-meta-time">${ms}ms</span>`;
      resBody.innerHTML = `<span class="pg-err">${esc(String(err.message || err))}</span>`;
    } finally {
      runBtn.disabled = false;
      runBtn.innerHTML = '<i data-lucide="play" class="icon-sm"></i> Run';
      if (typeof lucide !== 'undefined') lucide.createIcons();
    }
  }

  runBtn.addEventListener('click', runRequest);
  bodyEl.addEventListener('keydown', e => { if ((e.ctrlKey||e.metaKey) && e.key==='Enter') { e.preventDefault(); runRequest(); } });

  /* ── Copy response ── */
  copyBtn.addEventListener('click', async () => {
    try {
      await navigator.clipboard.writeText(resBody.innerText.trim());
      const orig = copyBtn.textContent;
      copyBtn.textContent = '✓ Copied';
      setTimeout(() => { copyBtn.textContent = orig; }, 1800);
    } catch { /* ignore */ }
  });

  loadReq('login');
  updateAuth();
}

/* ── Safe runner: call fn, log if it throws ─────────────── */
function safe(fn, name) {
  try { fn(); }
  catch (e) { console.error(`[ChatAPI] ${name} failed:`, e); }
}

/* ── Init ────────────────────────────────────────────────── */
document.addEventListener('DOMContentLoaded', () => {
  safe(initErrorBoundary, 'initErrorBoundary');
  safe(initTheme,         'initTheme');
  safe(initBaseUrl,       'initBaseUrl');
  safe(dismissLoader,     'dismissLoader');
  safe(initHamburger,     'initHamburger');
  safe(initNav,           'initNav');
  safe(initDocsTabs,      'initDocsTabs');
  safe(initEndpointAccordion, 'initEndpointAccordion');
  safe(initCopyBtns,      'initCopyBtns');
  safe(initReveal,        'initReveal');
  safe(initStatusBadge,   'initStatusBadge');
  safe(initCounters,      'initCounters');
  safe(initTypewriter,    'initTypewriter');
  safe(initScrollTop,     'initScrollTop');
  safe(initPlayground,    'initPlayground');
  safe(initIcons,         'initIcons');

  document.getElementById('theme-toggle')?.addEventListener('click', () => {
    const cur = document.documentElement.getAttribute('data-theme') || 'dark';
    applyTheme(cur === 'dark' ? 'light' : 'dark');
  });
});
