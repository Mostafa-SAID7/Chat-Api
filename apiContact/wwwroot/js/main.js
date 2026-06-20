'use strict';

/* ── Theme ───────────────────────────────────────────────── */
const THEME_KEY = 'chatapi-theme';

function applyTheme(theme) {
  document.documentElement.setAttribute('data-theme', theme);
  const btn = document.getElementById('theme-toggle');
  if (btn) btn.textContent = theme === 'dark' ? '☀️' : '🌙';
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

  btn.addEventListener('click', () => {
    const open = menu.classList.toggle('open');
    btn.setAttribute('aria-expanded', open);
    btn.textContent = open ? '✕' : '☰';
    document.body.style.overflow = open ? 'hidden' : '';
  });

  // Close on link click
  menu.querySelectorAll('a').forEach(a => {
    a.addEventListener('click', () => {
      menu.classList.remove('open');
      btn.textContent = '☰';
      document.body.style.overflow = '';
    });
  });

  // Close on outside click
  document.addEventListener('click', (e) => {
    if (!menu.contains(e.target) && !btn.contains(e.target)) {
      menu.classList.remove('open');
      btn.textContent = '☰';
      document.body.style.overflow = '';
    }
  });
}

/* ── Docs sidebar collapse ───────────────────────────────── */
function initSidebar() {
  const layout  = document.querySelector('.docs-layout');
  const sidebar = document.querySelector('.docs-sidebar');
  const toggle  = document.getElementById('sidebar-toggle');
  if (!layout || !sidebar || !toggle) return;

  const COLLAPSED_KEY = 'chatapi-sidebar';

  function setSidebar(collapsed) {
    layout.classList.toggle('sidebar-collapsed', collapsed);
    sidebar.classList.toggle('mobile-open', false);
    toggle.title = collapsed ? 'Show sidebar' : 'Hide sidebar';
    toggle.textContent = collapsed ? '▶' : '◀';
    localStorage.setItem(COLLAPSED_KEY, collapsed ? '1' : '0');
  }

  // On mobile, sidebar is off-canvas; toggle opens/closes it differently
  function isMobile() { return window.innerWidth <= 768; }

  toggle.addEventListener('click', () => {
    if (isMobile()) {
      const open = sidebar.classList.toggle('mobile-open');
      toggle.textContent = open ? '✕' : '☰';
    } else {
      const collapsed = !layout.classList.contains('sidebar-collapsed');
      setSidebar(collapsed);
    }
  });

  // Restore state (desktop only)
  if (!isMobile()) {
    const saved = localStorage.getItem(COLLAPSED_KEY) === '1';
    setSidebar(saved);
  } else {
    toggle.textContent = '☰';
    toggle.title = 'Open sidebar';
  }

  // Recalc on resize
  window.addEventListener('resize', () => {
    if (!isMobile()) {
      sidebar.classList.remove('mobile-open');
      toggle.textContent = layout.classList.contains('sidebar-collapsed') ? '▶' : '◀';
    } else {
      toggle.textContent = '☰';
    }
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

/* ── Copy buttons ────────────────────────────────────────── */
function initCopyBtns() {
  document.querySelectorAll('.copy-btn').forEach(btn => {
    btn.addEventListener('click', () => {
      const block = btn.closest('.code-block');
      const clone = block.cloneNode(true);
      clone.querySelectorAll('button').forEach(b => b.remove());
      const text = clone.innerText.trim();
      navigator.clipboard.writeText(text).then(() => {
        const orig = btn.textContent;
        btn.textContent = '✓ Copied';
        setTimeout(() => { btn.textContent = orig; }, 2000);
      });
    });
  });
}

/* ── Scroll-reveal ───────────────────────────────────────── */
function initReveal() {
  const els = document.querySelectorAll('.card, .endpoint-card, .stat, .tech-chip, .ws-demo');
  if (!els.length) return;
  const io = new IntersectionObserver(entries => {
    entries.forEach(e => {
      if (e.isIntersecting) { e.target.classList.add('reveal', 'visible'); io.unobserve(e.target); }
    });
  }, { threshold: 0.08 });
  els.forEach(el => { el.classList.add('reveal'); io.observe(el); });
}

/* ── API status badge ────────────────────────────────────── */
function initStatusBadge() {
  const el = document.getElementById('api-status');
  if (!el) return;
  const check = async () => {
    try {
      const r = await fetch('/health');
      if (r.ok) {
        el.textContent = 'API Online';
        el.className = 'badge badge-green';
      } else throw 0;
    } catch {
      el.textContent = 'API Offline';
      el.className = 'badge badge-orange';
    }
  };
  check();
  setInterval(check, 20000);
}

/* ── Stat counters ───────────────────────────────────────── */
function initCounters() {
  document.querySelectorAll('[data-count]').forEach(el => {
    const target = parseInt(el.dataset.count, 10);
    const suffix = el.dataset.suffix || '';
    let cur = 0;
    const step = Math.max(1, Math.ceil(target / 40));
    const iv = setInterval(() => {
      cur = Math.min(cur + step, target);
      el.textContent = cur + suffix;
      if (cur >= target) clearInterval(iv);
    }, 28);
  });
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
    setTimeout(tick, del ? 38 : 75);
  };
  tick();
}

/* ── Docs sidebar scroll-spy ─────────────────────────────── */
function initScrollSpy() {
  const sections = document.querySelectorAll('h2[id]');
  const links    = document.querySelectorAll('.docs-sidebar a[href^="#"]');
  if (!sections.length || !links.length) return;

  const onScroll = () => {
    let cur = '';
    sections.forEach(s => {
      if (window.scrollY + 110 >= s.offsetTop) cur = s.id;
    });
    links.forEach(a => a.classList.toggle('active', a.getAttribute('href') === '#' + cur));
  };
  window.addEventListener('scroll', onScroll, { passive: true });
  onScroll();
}

/* ── Init ────────────────────────────────────────────────── */
document.addEventListener('DOMContentLoaded', () => {
  initTheme();
  initHamburger();
  initSidebar();
  initNav();
  initCopyBtns();
  initReveal();
  initStatusBadge();
  initCounters();
  initTypewriter();
  initScrollSpy();

  // Theme toggle wiring
  document.getElementById('theme-toggle')?.addEventListener('click', () => {
    const cur = document.documentElement.getAttribute('data-theme') || 'dark';
    applyTheme(cur === 'dark' ? 'light' : 'dark');
  });
});
