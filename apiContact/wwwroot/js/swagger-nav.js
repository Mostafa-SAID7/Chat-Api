// Inject navigation bar into Swagger UI page
(function () {
  function inject() {
    if (document.querySelector('.sw-nav')) return;

    const nav = document.createElement('nav');
    nav.className = 'sw-nav';
    nav.innerHTML = `
      <a class="sw-nav-logo" href="/">
        <div class="sw-nav-dot"></div>
        Chat API
      </a>
      <div class="sw-nav-links">
        <a href="/">Home</a>
        <a href="/docs.html">Docs</a>
        <a href="/swagger" class="active sw-keep">Swagger <span class="sw-nav-badge">UI</span></a>
        <a href="/health">Health</a>
      </div>
    `;

    // Insert before the swagger-ui wrapper
    const target = document.querySelector('#swagger-ui') || document.body.firstChild;
    document.body.insertBefore(nav, document.body.firstChild);
  }

  // Try immediately and also after DOMContentLoaded (Swagger hydrates async)
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', inject);
  } else {
    inject();
  }
})();
