// PonyUpPerformance global site JavaScript

(function () {

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

        if (vin.length !== 17) {
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

        const listId =
            `${inputId}-history`;

        let dataList =
            document.getElementById(
                listId);

        if (!dataList) {

            dataList =
                document.createElement(
                    "datalist");

            dataList.id =
                listId;

            document.body.appendChild(
                dataList);
        }

        input.setAttribute(
            "list",
            listId);

        input.setAttribute(
            "autocomplete",
            "off");

        function refreshSuggestions() {

            const history =
                readVinHistory();

            dataList.innerHTML =
                "";

            history.forEach(vehicle => {

                const option =
                    document.createElement(
                        "option");

                option.value =
                    vehicle.vin;

                const description =
                    [
                        vehicle.year,
                        vehicle.make,
                        vehicle.model
                    ]
                        .filter(Boolean)
                        .join(" ");

                if (description) {

                    option.label =
                        description;
                }

                dataList.appendChild(
                    option);
            });
        }

        input.addEventListener(
            "focus",
            refreshSuggestions);

        input.addEventListener(
            "input",
            function () {

                const start =
                    input.selectionStart;

                const end =
                    input.selectionEnd;

                const upper =
                    normalizeVin(
                        input.value);

                if (input.value !== upper) {

                    input.value =
                        upper;

                    try {

                        input.setSelectionRange(
                            start,
                            end);

                    }
                    catch {
                        // Selection restoration is optional.
                    }
                }

                refreshSuggestions();
            });

        refreshSuggestions();
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

    document.addEventListener(
        "DOMContentLoaded",
        function () {

            initializeVinHistory();
            initializeGarageUi();
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
