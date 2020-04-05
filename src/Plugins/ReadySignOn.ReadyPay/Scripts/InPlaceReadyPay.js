$(function () {

    // -----------------
    // Type
    // -----------------
    // Element:   an element in the (DOM)
    // jQuery:    a jQuery object 
    // Selector:  a CSS selector used to select DOM documents.
    // [a]:       a list/array of element of a

    // -----------------
    // Data
    // -----------------

    // selector for `readypay` button
    const selReadyPayBtn = "input[type=image]";

    // selector for `readyticket` textbox
    const selReadyTicketTxt = "input[data-readyticket]";
    
    // the parent container that delegates the events
    const $parentContainer = $(document);

    // destination url to post request
    const destUrl = "Plugins/ReadySignOn.ReadyPay/ReadyPay/InPlaceReadyPayPosted";
    // const destUrl = "https://reqres.in/api/users";

    // ----------------
    // Function
    // ----------------

    // freeze :: (jQuery a, jQuery b)  => [a] -> [b] -> String -> ()
    // disables elements in `xs`; 
    // updates image url to `url` for elements in `ys`
    const freeze = (xs, ys, url, disabled = true) => {
        xs.map($x => $x.prop("disabled", disabled));
        ys.map($y => $y.attr("src", url));
    };

    // unFreeze enables controls;
    // updates image url to `url` for elements in `ys`
    const unFreeze = (xs, ys, url) => freeze(xs, ys, url, false);

    // validateTextbox ::  (Element a) => a -> Bool
    // returns `true` if the `elem`'s value is numeric and it's non-empty,
    // otherwise, returns `false`
    const validateTextbox = elem => {
        const val = elem.value;
        return (val.length > 0 && $.isNumeric(val)) ? true : false
    };

    // toggleWhile :: (jQuery a, Element b) =>  [a] -> b -> (b -> Bool) -> ()
    // disables `x`in xs if `p(y)` is true, otherwise enables `x`
    const toggleWhile = (xs, y, p) => {
        xs.map($x => p(y) ? $x.prop("disabled", false) : $x.prop("disabled", true));
    };
    
    // displayConfirmation :: JSON -> ()
    // displays confirmation popup
    function displayReadyPayError() {
        notice = PNotify.error({
            title: `Payment failed.`,
            text: `The payment was denied by the user or an error has occurred while processing the payment, please verify your payment information then try again.`,
            textTrusted: true,
            icon: 'fas fa-question-circle',
            hide: true,
            delay: 8000,
            modules: {
                Confirm: {
                    confirm: true,
                    focus: true,
                    buttons: [
                        {
                            text: 'OK',
                            textTrusted: false,
                            addClass: '',
                            primary: true,
                            // Whether to trigger this button when the user hits enter in a single line
                            // prompt. Also, focus the button if it is a modal prompt.
                            promptTrigger: true,
                            click: (notice, value) => {
                                notice.close();
                            }
                        }
                    ]
                },
                Buttons: {
                    closer: true,
                    sticker: false
                },
                History: {
                    history: true
                }
            }
        });
    };

    const displayConfirmation = res => {
        if (res.order_id) {
            var notice = PNotify.success({
                title: `Thank you for placing your order! `,
                text: `Your order ID is ${res.order_id}. <br> The total amount of USD ${res.charged_total} will be charged to ${res.payment_method}.<br> Your order confirmation will be sent to <b> ${res.email_address} </b>`,
                textTrusted: true,
                icon: 'fas fa-question-circle',
                hide: true,
                delay: 8000,
                modules: {
                    Confirm: {
                        confirm: true,
                        focus: true,
                        buttons: [
                            {
                                text: 'OK',
                                textTrusted: false,
                                addClass: '',
                                primary: true,
                                // Whether to trigger this button when the user hits enter in a single line
                                // prompt. Also, focus the button if it is a modal prompt.
                                promptTrigger: true,
                                click: (notice, value) => {
                                    notice.close();
                                }
                            },
                            {
                                text: `<a href="Order/Details/${res.order_id}" target="_blank">View Order Detail</a>`,
                                textTrusted: true,
                                addClass: '',
                                click: (notice) => {
                                    notice.close();
                                    notice.fire('pnotify.cancel', { notice });
                                }
                            }
                        ],
                    },
                    Buttons: {
                        closer: true,
                        sticker: false
                    },
                    History: {
                        history: true
                    }
                }
            });
        }
        else {
            displayReadyPayError();
        }
    };

    // -----------------
    // Event
    // -----------------

    // when readyticket value changes, update its `data-readyticket` attribute,  
    // toggle `$inputReadyPay`'s state on validation of readyticket 
    $parentContainer.on("input", selReadyTicketTxt, (e) => {
        const textbox = e.target;
        textbox.dataset.readyticket = textbox.value;
        const $inputReadyPay = $(textbox).parent().find(selReadyPayBtn);
        toggleWhile([$inputReadyPay], textbox, validateTextbox);
    });

    //  prevent readyticket textbox from submitting form when "Enter" key is pressed,
    //  trigger `click` event on `$inputReadyPay` 
    $parentContainer.on("keypress", selReadyTicketTxt, (e) => {
        const $inputReadyPay = $(e.target).parent().find(selReadyPayBtn);
        if (e.which === 13) {
            e.preventDefault();
            if (!$inputReadyPay.prop("disabled")) {
                $inputReadyPay.trigger('click');
            }
        }
    });

    // disable readypay buttons when page loads
    $(selReadyPayBtn).prop("disabled", true);

    // make HTTP request, display confirmation on success, log to the console on error
    $parentContainer.on('click', selReadyPayBtn, (e) => {
        const $inputReadyPay = $(e.target);
        const $inputReadyTicket = $inputReadyPay.parent().find(selReadyTicketTxt);
        // get readyticket number
        const ReadyTicket = $inputReadyTicket.data("readyticket");
        // get readyPayload
        const readyPayload = $inputReadyPay.parent().data("readypayload");
        // produce a new payload
        const payLoad = Object.assign(readyPayload, { ReadyTicket });
        e.preventDefault();
        $.ajax({
            type: 'POST',
            url: destUrl,
            headers: {
                'Content-Type': 'application/json'
            },
            contentType: false,
            processData: false,
            data: JSON.stringify(payLoad),
            beforeSend: () => {
                freeze([$inputReadyTicket, $inputReadyPay], [$inputReadyPay], payLoad.LoaderImageUrl);
            },
            success: function (res, textStatus) {
                displayConfirmation(res);
            },
            error: function (jqXHR, textStatus, errorMsg) {
                const error = `${errorMsg}`;
                console.log(error);
                displayReadyPayError();
            },
            complete: () => {
                unFreeze([$inputReadyTicket, $inputReadyPay], [$inputReadyPay], payLoad.SubmitButtonImageUrl);
            }
        });
    });
});
