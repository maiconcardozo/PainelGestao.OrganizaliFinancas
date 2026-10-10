(function () {
    var requestedTheme = new URLSearchParams(window.location.search).get("theme");
    var hasBooted = false;
    var observedSwaggerUi = false;

    function readThemeTokens(element) {
        if (!element) {
            return "";
        }

        return [
            element.className,
            element.getAttribute("data-theme"),
            element.getAttribute("data-color-mode"),
            element.getAttribute("color-mode")
        ].join(" ").toLowerCase();
    }

    function syncThemeMarker() {
        if (requestedTheme === "light") {
            document.documentElement.setAttribute("data-global-theme", "light");
            return;
        }

        if (requestedTheme === "dark") {
            document.documentElement.removeAttribute("data-global-theme");
            return;
        }

        if (!hasBooted) {
            // Dark is the default theme: ignore the native Swagger toggle's
            // own auto-detected state (e.g. OS light preference) on first
            // paint so the docs always open in dark mode.
            document.documentElement.removeAttribute("data-global-theme");
            return;
        }

        var swaggerUi = document.querySelector(".swagger-ui");
        var tokens = [
            readThemeTokens(document.documentElement),
            readThemeTokens(document.body),
            readThemeTokens(swaggerUi)
        ].join(" ");

        if (tokens.indexOf("light") >= 0 && tokens.indexOf("dark") < 0) {
            document.documentElement.setAttribute("data-global-theme", "light");
            return;
        }

        if (tokens.indexOf("dark") >= 0) {
            document.documentElement.removeAttribute("data-global-theme");
        }
    }

    function observeThemeHost(element) {
        if (!element) {
            return;
        }

        var observer = new MutationObserver(syncThemeMarker);
        observer.observe(element, {
            attributes: true,
            attributeFilter: ["class", "data-theme", "data-color-mode", "color-mode"]
        });
    }

    function attachSwaggerUiObserver() {
        if (observedSwaggerUi) {
            return;
        }

        var swaggerUi = document.querySelector(".swagger-ui");
        if (!swaggerUi) {
            window.setTimeout(attachSwaggerUiObserver, 150);
            return;
        }

        observedSwaggerUi = true;
        observeThemeHost(swaggerUi);
        syncThemeMarker();
    }

    function watchThemeChanges() {
        syncThemeMarker();
        hasBooted = true;

        observeThemeHost(document.documentElement);
        observeThemeHost(document.body);
        attachSwaggerUiObserver();
    }

    function replaceFavicons() {
        var selector = 'link[rel="icon"], link[rel="shortcut icon"], link[rel="apple-touch-icon"]';
        Array.prototype.forEach.call(document.querySelectorAll(selector), function (link) {
            link.remove();
        });

        [
            { rel: "icon", href: "/swagger-ui/organizali-logo.svg", type: "image/svg+xml" },
            { rel: "shortcut icon", href: "/swagger-ui/organizali-logo.svg", type: "image/svg+xml" },
            { rel: "apple-touch-icon", href: "/swagger-ui/organizali-logo.svg" }
        ].forEach(function (item) {
            var link = document.createElement("link");
            link.rel = item.rel;
            link.href = item.href;
            if (item.type) {
                link.type = item.type;
            }
            document.head.appendChild(link);
        });
    }

    function createIntro() {
        if (document.querySelector(".global-docs-hero")) {
            return;
        }

        var swaggerUi = document.querySelector(".swagger-ui");
        var info = document.querySelector(".information-container");

        if (!swaggerUi || !info) {
            window.setTimeout(createIntro, 150);
            return;
        }

        var hero = document.createElement("section");
        hero.className = "global-docs-hero";
        hero.innerHTML =
            '<div class="global-docs-brand">' +
            '  <img src="/swagger-ui/organizali-logo.svg" alt="Organizali" />' +
            '  <span>Organizali</span>' +
            '</div>' +
            '<div class="global-docs-kicker">Guia de integracao</div>' +
            '<h1>Organizali Finanças — API</h1>' +
            '<p>Acesso à casa, integrantes, moradores, catálogos e tipos de despesa parametrizados. Operações financeiras serão entregues nos próximos marcos.</p>' +
            '<div class="global-docs-actions">' +
            '  <a href="#api-operations" class="global-docs-button primary">Ver operações</a>' +
            '  <a href="/swagger/v1/swagger.json" class="global-docs-button secondary">OpenAPI v1</a>' +
            '  <a href="?theme=light" class="global-docs-button secondary">Tema claro</a><a href="?theme=dark" class="global-docs-button secondary">Tema escuro</a>' +
            '</div>';

        var cards = document.createElement("section");
        cards.className = "global-docs-cards";
        cards.innerHTML =
            '<article><strong>API em execução</strong><span>Use /health/live para verificar o processo.</span></article>' +
            '<article><strong>Persistência</strong><span>/health/ready verifica conexão e tabela base.</span></article>' +
            '<article><strong>Correlação</strong><span>Consulte X-Correlation-ID ao investigar uma resposta.</span></article>' +
            '<article><strong>Acesso protegido</strong><span>As operações respeitam a casa e as permissões da sessão.</span></article>';
        info.parentNode.insertBefore(hero, info);
        info.parentNode.insertBefore(cards, info);
        info.id = "api-operations";
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", function () {
            watchThemeChanges();
            replaceFavicons();
            createIntro();
        });
    } else {
        watchThemeChanges();
        replaceFavicons();
        createIntro();
    }
})();
