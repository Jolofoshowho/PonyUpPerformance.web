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
        initializeVinHistory);

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
