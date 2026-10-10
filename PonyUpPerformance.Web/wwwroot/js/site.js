// PonyUpPerformance global site JavaScript

(function () {

    const ANALYZER_PATHS =
        new Set([
            "/repairanalyzer",
            "/buyanalyzer",
            "/sellanalyzer",
            "/tradeanalyzer",
            "/upgradeanalyzer"
        ]);

    const ANALYZER_DRAFT_PREFIX =
        "ponyup.analyzerDraft.v1.";

    const ANALYZER_SUBMIT_PREFIX =
        "ponyup.analyzerSubmit.v1.";

    const ANALYZER_DRAFT_MAX_AGE_MS =
        7 * 24 * 60 * 60 * 1000;

    const VIN_HISTORY_KEY =
        "ponyup.vinHistory.v1";

    const MAX_VIN_HISTORY =
        25;

    function normalizeVin(vin) {

        return (vin || "")
            .trim()
            .replace(/\s+/g, "")
            .toUpperCase();
    }

    function readVinHistory() {

        try {

            const stored =
                localStorage.getItem(
                    VIN_HISTORY_KEY);

            if (!stored) {
                return [];
            }

            const parsed =
                JSON.parse(stored);

            if (!Array.isArray(parsed)) {
                return [];
            }

            return parsed
                .filter(item =>
                    item &&
                    typeof item.vin === "string" &&
                    normalizeVin(item.vin).length === 17)
                .sort((a, b) =>
                    (b.lastUsed || 0) -
                    (a.lastUsed || 0));

        }
        catch {

            return [];
        }
    }

    function writeVinHistory(history) {

        try {

            localStorage.setItem(
                VIN_HISTORY_KEY,
                JSON.stringify(history));

        }
        catch {
            // VIN suggestions are optional.
            // Never interfere with analyzer operation.
        }
    }

    function recordVin(vehicle) {

        const vin =
            normalizeVin(vehicle.vin);

        if (!/^[A-HJ-NPR-Z0-9]{17}$/.test(vin)) {
            return;
        }

        const history =
            readVinHistory();

        const record = {
            vin: vin,
            year:
                vehicle.year || "",
            make:
                (vehicle.make || "").trim(),
            model:
                (vehicle.model || "").trim(),
            lastUsed:
                Date.now()
        };

        const updated = [
            record,
            ...history.filter(item =>
                normalizeVin(item.vin) !== vin)
        ]
            .slice(
                0,
                MAX_VIN_HISTORY);

        writeVinHistory(updated);
    }

    function recordDecodedVehicles() {

        const records =
            document.querySelectorAll(
                "[data-vin-history-record]");

        records.forEach(element => {

            recordVin({
                vin:
                    element.dataset.vin,
                year:
                    element.dataset.year,
                make:
                    element.dataset.make,
                model:
                    element.dataset.model
            });
        });
    }

    function buildVinHistoryList(
        input,
        index) {

        if (!input) {
            return;
        }

        let inputId =
            input.id;

        if (!inputId) {
            inputId =
                `ponyup-vin-input-${index}`;

            input.id =
                inputId;
        }

        input.removeAttribute(
            "list");

        input.setAttribute(
            "autocomplete",
            "off");

        const menu =
            document.createElement(
                "div");

        menu.className =
            "ponyup-vin-menu";

        menu.hidden =
            true;

        menu.setAttribute(
            "role",
            "listbox");

        document.body.appendChild(
            menu);

        let accountHistory =
            [];

        function combinedHistory() {

            const merged =
                new Map();

            [
                ...accountHistory,
                ...readVinHistory()
            ]
                .forEach(vehicle => {

                    const vin =
                        normalizeVin(
                            vehicle.vin);

                    if (!/^[A-HJ-NPR-Z0-9]{17}$/.test(vin)) {
                        return;
                    }

                    const current =
                        merged.get(vin)
                        ?? {};

                    merged.set(
                        vin,
                        {
                            vin:
                                vin,

                            year:
                                vehicle.year ||
                                current.year ||
                                "",

                            make:
                                vehicle.make ||
                                current.make ||
                                "",

                            model:
                                vehicle.model ||
                                current.model ||
                                "",

                            lastUsed:
                                vehicle.lastUsed ||
                                current.lastUsed ||
                                0
                        });
                });

            return Array.from(
                merged.values())
                .sort(
                    (a, b) =>
                        (b.lastUsed || 0) -
                        (a.lastUsed || 0))
                .slice(
                    0,
                    MAX_VIN_HISTORY);
        }

        function positionMenu() {

            const rect =
                input.getBoundingClientRect();

            menu.style.left =
                `${rect.left + window.scrollX}px`;

            menu.style.top =
                `${rect.bottom + window.scrollY + 4}px`;

            menu.style.width =
                `${Math.max(rect.width, 280)}px`;
        }

        function relatedValue(
            names) {

            const form =
                input.form;

            if (!form) {
                return "";
            }

            for (const name of names) {

                const field =
                    form.querySelector(
                        `[name="${name}"]`);

                if (field &&
                    field.value) {

                    return field.value;
                }
            }

            return "";
        }

        function currentVehicleRecord() {

            const name =
                input.name || "";

            let stem =
                "Input.";

            if (name.endsWith(
                    "YourVin")) {

                stem =
                    "Input.Your";
            }
            else if (name.endsWith(
                    "TheirVin")) {

                stem =
                    "Input.Their";
            }

            return {
                vin:
                    normalizeVin(
                        input.value),

                year:
                    relatedValue(
                        stem === "Input."
                            ? [
                                "Input.Year",
                                "Input.VehicleYear"
                            ]
                            : [
                                `${stem}Year`
                            ]),

                make:
                    relatedValue(
                        stem === "Input."
                            ? [
                                "Input.Make",
                                "Input.VehicleMake"
                            ]
                            : [
                                `${stem}Make`
                            ]),

                model:
                    relatedValue(
                        stem === "Input."
                            ? [
                                "Input.Model",
                                "Input.VehicleModel"
                            ]
                            : [
                                `${stem}Model`
                            ])
            };
        }

        async function loadAccountHistory() {

            try {

                const response =
                    await fetch(
                        "/VinHistory",
                        {
                            credentials:
                                "same-origin",

                            headers:
                                {
                                    "Accept":
                                        "application/json"
                                }
                        });

                const contentType =
                    response.headers.get(
                        "content-type")
                    || "";

                if (!response.ok ||
                    !contentType.includes(
                        "application/json")) {

                    return;
                }

                const records =
                    await response.json();

                if (!Array.isArray(
                        records)) {

                    return;
                }

                accountHistory =
                    records.map(
                        vehicle => ({
                            vin:
                                vehicle.vin,

                            year:
                                vehicle.year
                                ?? "",

                            make:
                                vehicle.make
                                ?? "",

                            model:
                                vehicle.model
                                ?? "",

                            lastUsed:
                                vehicle.lastUsedOn
                                    ? Date.parse(
                                        vehicle.lastUsedOn)
                                    : 0
                        }));
            }
            catch {
                // VIN history must never block the analyzer.
            }
        }

        async function saveAccountVin(
            vehicle) {

            const vin =
                normalizeVin(
                    vehicle.vin);

            if (!/^[A-HJ-NPR-Z0-9]{17}$/.test(vin)) {
                return;
            }

            try {

                const form =
                    input.form;

                const antiForgery =
                    form?.querySelector(
                        'input[name="__RequestVerificationToken"]');

                if (!antiForgery) {
                    return;
                }

                const body =
                    new FormData();

                body.append(
                    "__RequestVerificationToken",
                    antiForgery.value);

                body.append(
                    "vin",
                    vin);

                if (vehicle.year) {
                    body.append(
                        "year",
                        vehicle.year);
                }

                if (vehicle.make) {
                    body.append(
                        "make",
                        vehicle.make);
                }

                if (vehicle.model) {
                    body.append(
                        "model",
                        vehicle.model);
                }

                await fetch(
                    "/VinHistory?handler=Save",
                    {
                        method:
                            "POST",

                        body:
                            body,

                        credentials:
                            "same-origin",

                        headers:
                            {
                                "Accept":
                                    "application/json"
                            }
                    });
            }
            catch {
                // Account VIN history is convenience-only.
            }
        }

        function renderMenu() {

            const history =
                combinedHistory();

            menu.innerHTML =
                "";

            if (history.length === 0) {

                const empty =
                    document.createElement(
                        "div");

                empty.className =
                    "ponyup-vin-menu-empty";

                empty.textContent =
                    "No saved VINs yet.";

                menu.appendChild(
                    empty);
            }
            else {

                history.forEach(
                    vehicle => {

                        const button =
                            document.createElement(
                                "button");

                        button.type =
                            "button";

                        button.className =
                            "ponyup-vin-menu-item";

                        button.setAttribute(
                            "role",
                            "option");

                        const vin =
                            document.createElement(
                                "strong");

                        vin.textContent =
                            vehicle.vin;

                        const details =
                            [
                                vehicle.year,
                                vehicle.make,
                                vehicle.model
                            ]
                                .filter(Boolean)
                                .join(" ");

                        button.appendChild(
                            vin);

                        if (details) {

                            const description =
                                document.createElement(
                                    "span");

                            description.textContent =
                                details;

                            button.appendChild(
                                description);
                        }

                        button.addEventListener(
                            "mousedown",
                            event =>
                                event.preventDefault());

                        button.addEventListener(
                            "click",
                            function () {

                                input.value =
                                    vehicle.vin;

                                recordVin(
                                    vehicle);

                                menu.hidden =
                                    true;

                                input.dispatchEvent(
                                    new Event(
                                        "input",
                                        {
                                            bubbles:
                                                true
                                        }));

                                input.dispatchEvent(
                                    new Event(
                                        "change",
                                        {
                                            bubbles:
                                                true
                                        }));

                                input.focus();
                            });

                        menu.appendChild(
                            button);
                    });
            }

            positionMenu();

            menu.hidden =
                false;
        }

        async function openMenu() {

            await loadAccountHistory();
            renderMenu();
        }

        input.addEventListener(
            "focus",
            openMenu);

        input.addEventListener(
            "click",
            openMenu);

        input.addEventListener(
            "change",
            async function () {

                const vehicle =
                    currentVehicleRecord();

                if (/^[A-HJ-NPR-Z0-9]{17}$/.test(
                        vehicle.vin)) {

                    recordVin(
                        vehicle);

                    await saveAccountVin(
                        vehicle);

                    await loadAccountHistory();

                    renderGarageVinHistory();
                }
            });

        input.addEventListener(
            "input",
            function () {

                const selectionStart =
                    input.selectionStart;

                const selectionEnd =
                    input.selectionEnd;

                const upper =
                    normalizeVin(
                        input.value);

                if (input.value !== upper) {

                    input.value =
                        upper;

                    try {

                        input.setSelectionRange(
                            selectionStart,
                            selectionEnd);
                    }
                    catch {
                        // Selection restoration is optional.
                    }
                }
            });

        document.addEventListener(
            "mousedown",
            function (event) {

                if (event.target !== input &&
                    !menu.contains(
                        event.target)) {

                    menu.hidden =
                        true;
                }
            });

        window.addEventListener(
            "resize",
            function () {

                if (!menu.hidden) {
                    positionMenu();
                }
            });

        window.addEventListener(
            "scroll",
            function () {

                if (!menu.hidden) {
                    positionMenu();
                }
            },
            true);
    }


    function populateGarageVehicleFromHistory(vin) {

        const normalized =
            normalizeVin(vin);

        if (normalized.length !== 17) {
            return;
        }

        const vehicle =
            readVinHistory()
                .find(item =>
                    normalizeVin(item.vin) === normalized);

        if (!vehicle) {
            return;
        }

        const year =
            document.querySelector(
                "[data-garage-year]");

        const make =
            document.querySelector(
                "[data-garage-make]");

        const model =
            document.querySelector(
                "[data-garage-model]");

        if (year && vehicle.year) {
            year.value =
                vehicle.year;
        }

        if (make && vehicle.make) {
            make.value =
                vehicle.make;
        }

        if (model && vehicle.model) {
            model.value =
                vehicle.model;
        }
    }

    function renderGarageVinHistory() {

        const list =
            document.querySelector(
                "[data-garage-vin-history-list]");

        const wrap =
            document.getElementById(
                "garageRecentVinWrap");

        const vinInput =
            document.querySelector(
                "[data-garage-vin-input]");

        if (!list || !wrap || !vinInput) {
            return;
        }

        const history =
            readVinHistory();

        list.innerHTML =
            "";

        if (history.length === 0) {
            wrap.hidden = true;
            return;
        }

        wrap.hidden = false;

        history
            .slice(0, 10)
            .forEach(vehicle => {

                const button =
                    document.createElement(
                        "button");

                button.type =
                    "button";

                button.className =
                    "garage-recent-vin-option";

                const description =
                    [
                        vehicle.year,
                        vehicle.make,
                        vehicle.model
                    ]
                        .filter(Boolean)
                        .join(" ");

                const vin =
                    document.createElement(
                        "strong");

                vin.textContent =
                    vehicle.vin;

                const label =
                    document.createElement(
                        "span");

                label.textContent =
                    description ||
                    "Previously decoded vehicle";

                button.appendChild(
                    vin);

                button.appendChild(
                    label);

                button.addEventListener(
                    "click",
                    function () {

                        vinInput.value =
                            vehicle.vin;

                        populateGarageVehicleFromHistory(
                            vehicle.vin);

                        vinInput.dispatchEvent(
                            new Event(
                                "input",
                                {
                                    bubbles: true
                                }));

                        const decodeButton =
                            document.querySelector(
                                "[data-garage-decode]");

                        if (decodeButton) {
                            decodeButton.click();
                            return;
                        }

                        vinInput.focus();
                    });

                list.appendChild(
                    button);
            });
    }

    function initializeGarageUi() {

        const modal =
            document.getElementById(
                "garageAddVehicleModal");

        const openButtons =
            document.querySelectorAll(
                "[data-open-garage-add]");

        const closeButtons =
            document.querySelectorAll(
                "[data-close-garage-add]");

        function openGarageAdd() {

            if (!modal) {
                return;
            }

            modal.hidden = false;
            modal.setAttribute(
                "aria-hidden",
                "false");

            document.body.classList.add(
                "garage-modal-open");

            renderGarageVinHistory();

            window.setTimeout(
                function () {

                    document.querySelector(
                        "[data-garage-vin-input]")
                        ?.focus();
                },
                50);
        }

        function closeGarageAdd() {

            if (!modal) {
                return;
            }

            modal.hidden = true;
            modal.setAttribute(
                "aria-hidden",
                "true");

            document.body.classList.remove(
                "garage-modal-open");
        }

        openButtons.forEach(
            button =>
                button.addEventListener(
                    "click",
                    openGarageAdd));

        closeButtons.forEach(
            button =>
                button.addEventListener(
                    "click",
                    closeGarageAdd));

        document.addEventListener(
            "keydown",
            function (event) {

                if (event.key === "Escape" &&
                    modal &&
                    !modal.hidden) {

                    closeGarageAdd();
                }
            });

        const settings =
            document.getElementById(
                "garageSettingsPanel");

        document.querySelectorAll(
            "[data-toggle-garage-settings]")
            .forEach(button =>
                button.addEventListener(
                    "click",
                    function () {

                        if (!settings) {
                            return;
                        }

                        settings.hidden =
                            !settings.hidden;

                        if (!settings.hidden) {

                            settings.scrollIntoView({
                                behavior: "smooth",
                                block: "nearest"
                            });
                        }
                    }));

        document.querySelectorAll(
            "[data-close-garage-settings]")
            .forEach(button =>
                button.addEventListener(
                    "click",
                    function () {

                        if (settings) {
                            settings.hidden = true;
                        }
                    }));

        document.querySelectorAll(
            "[data-focus-mileage]")
            .forEach(button =>
                button.addEventListener(
                    "click",
                    function () {

                        const mileage =
                            document.getElementById(
                                "garageMileage");

                        if (!mileage) {
                            return;
                        }

                        mileage.scrollIntoView({
                            behavior: "smooth",
                            block: "center"
                        });

                        window.setTimeout(
                            function () {

                                mileage.focus();
                                mileage.select();
                            },
                            300);
                    }));

        const garageVin =
            document.querySelector(
                "[data-garage-vin-input]");

        if (garageVin) {

            const autofill =
                function () {

                    populateGarageVehicleFromHistory(
                        garageVin.value);
                };

            garageVin.addEventListener(
                "change",
                autofill);

            garageVin.addEventListener(
                "input",
                function () {

                    if (normalizeVin(
                            garageVin.value)
                            .length === 17) {

                        autofill();
                    }
                });
        }

        renderGarageVinHistory();

        /*
         * A decoded Garage POST returns with values filled.
         * Re-open the Add Vehicle panel so the user can
         * review them and save the vehicle.
         */
        if (modal &&
            garageVin &&
            normalizeVin(garageVin.value).length === 17) {

            openGarageAdd();
        }
    }

    function initializeVinHistory() {

        /*
         * First store any VIN that was just
         * successfully decoded.
         */
        recordDecodedVehicles();

        /*
         * Then attach the shared suggestion list
         * behavior to every PonyUp VIN field.
         */
        const vinInputs =
            document.querySelectorAll(
                "[data-vin-history='true']");

        vinInputs.forEach(
            (input, index) =>
                buildVinHistoryList(
                    input,
                    index));
    }


    function analyzerDraftKey() {
        return ANALYZER_DRAFT_PREFIX +
            window.location.pathname.toLowerCase();
    }

    function analyzerSubmitKey() {
        return ANALYZER_SUBMIT_PREFIX +
            window.location.pathname.toLowerCase();
    }

    function isAnalyzerPage() {
        return ANALYZER_PATHS.has(
            window.location.pathname.toLowerCase());
    }

    function analyzerHasResult() {
        const panel =
            document.querySelector(
                ".ponyup-stoplight-panel");

        return panel &&
            !panel.classList.contains(
                "off");
    }

    function analyzerControls() {
        return Array.from(
            document.querySelectorAll(
                "form input[name], form select[name], form textarea[name]"))
            .filter(control => {
                const type =
                    (control.type || "")
                        .toLowerCase();

                if ([
                    "hidden",
                    "submit",
                    "button",
                    "reset",
                    "file",
                    "image"
                ].includes(type)) {
                    return false;
                }

                const name =
                    control.name || "";

                return name.startsWith("Input.") ||
                    name.startsWith("EstimateInput.");
            });
    }

    function captureAnalyzerDraft() {
        const values =
            analyzerControls()
                .map(control => ({
                    name:
                        control.name,
                    type:
                        (control.type || control.tagName)
                            .toLowerCase(),
                    value:
                        control.value,
                    checked:
                        "checked" in control
                            ? control.checked
                            : undefined
                }));

        return {
            savedAt:
                Date.now(),
            values:
                values
        };
    }

    function saveAnalyzerDraft() {
        if (!isAnalyzerPage()) {
            return;
        }

        try {
            localStorage.setItem(
                analyzerDraftKey(),
                JSON.stringify(
                    captureAnalyzerDraft()));
        }
        catch {
            // Draft restore is optional and must never block an analyzer.
        }
    }

    function readAnalyzerDraft() {
        try {
            const raw =
                localStorage.getItem(
                    analyzerDraftKey());

            if (!raw) {
                return null;
            }

            const draft =
                JSON.parse(raw);

            if (!draft ||
                !Array.isArray(draft.values) ||
                !draft.savedAt ||
                Date.now() - draft.savedAt >
                    ANALYZER_DRAFT_MAX_AGE_MS) {

                localStorage.removeItem(
                    analyzerDraftKey());

                return null;
            }

            return draft;
        }
        catch {
            return null;
        }
    }

    function restoreAnalyzerDraft(draft) {
        if (!draft ||
            !Array.isArray(draft.values)) {
            return;
        }

        const controls =
            analyzerControls();

        draft.values.forEach(saved => {
            controls
                .filter(control =>
                    control.name ===
                    saved.name)
                .forEach(control => {
                    const type =
                        (control.type || "")
                            .toLowerCase();

                    if (type === "radio") {
                        control.checked =
                            control.value ===
                            saved.value &&
                            saved.checked === true;
                    }
                    else if (type === "checkbox") {
                        control.checked =
                            saved.checked === true;
                    }
                    else {
                        control.value =
                            saved.value ?? "";
                    }

                    control.dispatchEvent(
                        new Event(
                            "change",
                            {
                                bubbles: true
                            }));
                });
        });
    }

    function clearAnalyzerDraftAndForm() {
        try {
            localStorage.removeItem(
                analyzerDraftKey());
        }
        catch {
        }

        document.querySelectorAll(
            "form")
            .forEach(form =>
                form.reset());
    }

    function showAnalyzerRestorePrompt(
        draft) {

        const overlay =
            document.createElement(
                "div");

        overlay.setAttribute(
            "role",
            "dialog");

        overlay.setAttribute(
            "aria-modal",
            "true");

        overlay.setAttribute(
            "aria-label",
            "Restore saved PonyUp analyzer values");

        overlay.style.cssText =
            "position:fixed;inset:0;z-index:10000;" +
            "display:flex;align-items:center;justify-content:center;" +
            "padding:20px;background:rgba(0,0,0,.82);";

        const card =
            document.createElement(
                "div");

        card.style.cssText =
            "width:min(520px,100%);padding:28px;text-align:center;" +
            "background:#0b0b0b;border:1px solid rgba(255,255,255,.22);" +
            "border-top:3px solid #ff3131;border-radius:12px;" +
            "box-shadow:0 20px 60px rgba(0,0,0,.8);color:#fff;";

        const logo =
            document.createElement(
                "img");

        logo.src =
            "/images/ponyup-logo.jpg";

        logo.alt =
            "PonyUp Performance";

        logo.style.cssText =
            "width:120px;max-width:40%;height:auto;margin-bottom:16px;border-radius:8px;";

        const title =
            document.createElement(
                "h2");

        title.textContent =
            "RESTORE SAVED VALUES?";

        title.style.cssText =
            "margin:0 0 10px;font-size:1.45rem;";

        const copy =
            document.createElement(
                "p");

        copy.textContent =
            "PonyUp found unfinished values from your last visit to this analyzer.";

        copy.style.cssText =
            "margin:0 0 22px;color:#ccc;line-height:1.5;";

        const actions =
            document.createElement(
                "div");

        actions.style.cssText =
            "display:flex;gap:12px;justify-content:center;flex-wrap:wrap;";

        const restore =
            document.createElement(
                "button");

        restore.type =
            "button";

        restore.textContent =
            "RESTORE VALUES";

        restore.style.cssText =
            "min-width:170px;padding:12px 18px;border:0;border-radius:6px;" +
            "background:#d71920;color:#fff;font-weight:900;cursor:pointer;";

        const fresh =
            document.createElement(
                "button");

        fresh.type =
            "button";

        fresh.textContent =
            "START FRESH";

        fresh.style.cssText =
            "min-width:170px;padding:12px 18px;border:1px solid #777;border-radius:6px;" +
            "background:#1b1b1b;color:#fff;font-weight:900;cursor:pointer;";

        restore.addEventListener(
            "click",
            function () {
                restoreAnalyzerDraft(
                    draft);

                overlay.remove();
            });

        fresh.addEventListener(
            "click",
            function () {
                clearAnalyzerDraftAndForm();
                overlay.remove();
            });

        actions.appendChild(
            restore);

        actions.appendChild(
            fresh);

        card.appendChild(
            logo);

        card.appendChild(
            title);

        card.appendChild(
            copy);

        card.appendChild(
            actions);

        overlay.appendChild(
            card);

        document.body.appendChild(
            overlay);

        restore.focus();
    }

    function initializeAnalyzerDraftRestore() {
        if (!isAnalyzerPage()) {
            return;
        }

        if (analyzerHasResult()) {
            try {
                localStorage.removeItem(
                    analyzerDraftKey());

                sessionStorage.removeItem(
                    analyzerSubmitKey());
            }
            catch {
            }

            return;
        }

        const controls =
            analyzerControls();

        if (controls.length === 0) {
            return;
        }

        let saveTimer;

        const queueSave =
            function () {
                window.clearTimeout(
                    saveTimer);

                saveTimer =
                    window.setTimeout(
                        saveAnalyzerDraft,
                        250);
            };

        controls.forEach(control => {
            control.addEventListener(
                "input",
                queueSave);

            control.addEventListener(
                "change",
                queueSave);
        });

        document.querySelectorAll(
            "form")
            .forEach(form =>
                form.addEventListener(
                    "submit",
                    function () {
                        saveAnalyzerDraft();

                        try {
                            sessionStorage.setItem(
                                analyzerSubmitKey(),
                                "1");
                        }
                        catch {
                        }
                    }));

        let submitted =
            false;

        try {
            submitted =
                sessionStorage.getItem(
                    analyzerSubmitKey()) ===
                "1";

            if (submitted) {
                sessionStorage.removeItem(
                    analyzerSubmitKey());
            }
        }
        catch {
        }

        if (submitted) {
            return;
        }

        const draft =
            readAnalyzerDraft();

        if (draft) {
            showAnalyzerRestorePrompt(
                draft);
        }
    }

    document.addEventListener(
        "DOMContentLoaded",
        function () {

            initializeVinHistory();
            initializeGarageUi();
            initializeAnalyzerDraftRestore();
        });

    /*
     * Keep a small public API available for
     * future Garage/account integration.
     */
    window.PonyUpVinHistory = {

        getAll:
            function () {
                return readVinHistory();
            },

        add:
            function (vehicle) {
                recordVin(vehicle || {});
            },

        clear:
            function () {

                try {

                    localStorage.removeItem(
                        VIN_HISTORY_KEY);

                }
                catch {
                }
            }
    };

})();
