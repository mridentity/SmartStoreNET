// -----------------
// Data
// -----------------

// selector for readypay container
const divReadyPay = ".art-ready-pay";

// selector for readypay button
const btnReadyPay = "button";

// selector for readyticket textbox
const txtReadyTicket = "input[data-readyticket]";

// destination url to post the ready ticket
const destUrl = "Plugins/ReadySignOn.ReadyPay/ReadyPay/InPlaceReadyPayPosted";
// const destUrl = "https://reqres.in/api/users";

// -----------------
// Function
// -----------------

// getElementsBy consumes a selector: `s`;
// it produces a list of elements selected by: `s`
const getElementsBy = (s) => [...document.querySelectorAll(s)];

// getSInP consumes a list parent element: `p`, a selector: `s`;
// it produces the elment matching `s` under `p`
const getSInP = (p, s) => p.querySelector(s);

// -----------------
// Main
// -----------------

getElementsBy(divReadyPay).map(div => {
    const elemReadyPay = getSInP(div, btnReadyPay);
    const elemReadyTicket = getSInP(div, txtReadyTicket);
    const productid = div.dataset.productid;
    const cartsubtotal = div.dataset.cartsubtotal;
    const taxtotal = div.dataset.taxtotal;
    const sentinel = div.dataset.sentinel;

    // `bindReadyTicket` consumes an event: `e`;
    // it sets the `readyticket` attribute of `e.target`
    const bindReadyTicket = (e) => {
        const elem = e.target;
        const isValid = elem.value.length !== 0;

        // if Enter key is detected.
        if (e.which === 13) {
            e.preventDefault ? e.preventDefault() : (e.returnValue = false);    // Prevent re-submit
            $('#btnReadyPay').click();
        }

        if (isValid && elemReadyPay.disabled) {
            elemReadyPay.disabled = false;
        } else if (!isValid && !elemReadyPay.disabled) {
            elemReadyPay.disabled = true;
        }
        elem.dataset.readyticket = elem.value;
    };


    const bindReadyPay = (e) => {
        // construct data to be posted
        const readyticket = elemReadyTicket.dataset.readyticket;
        const jsonData = {
            AppData: productid,
            CartSubTotal: cartsubtotal,
            TaxTotal: taxtotal,
            Sentinel: sentinel,
            ReadyTicket: readyticket
        };

        // construct settings for the request
        const options = {
            method: 'POST',
            body: JSON.stringify(jsonData),
            headers: {
                'Content-Type': 'application/json'
            }
        };

        const freeze = () => {
            elemReadyTicket.disabled = true;
            elemReadyPay.setAttribute('class', `waiting`);
            elemReadyPay.disabled = true;
        }

        const unFreeze = () => {
            elemReadyTicket.disabled = false;
            elemReadyPay.setAttribute('class', `normal`);
            elemReadyPay.disabled = false;
        }

        // disable textbox for readyticket;
        // change the background image for the ready pay button
        freeze();

        // make the request; process the response: `res` if it succeeds;
        // otherwise, logs errors to the console.
        fetch(destUrl, options)
            .then(res => {
                unFreeze();
                if (res.ok) {
                    return res.json();
                } else {
                    return Promise.reject(res.statusText);
                }
            })
            .then(res => {
                console.log(res);
                displayConfirmation(res);
            })
            .catch(err => console.log(`Error when making request. ${err}`));
    }

    // disable ReadyPay button on load
    elemReadyPay.disabled = true;

    // initial button background image
    elemReadyPay.setAttribute('class', `normal`);

    // set `data-readyticket` attribute each time users change the ready ticket number 
    elemReadyTicket.addEventListener("input", bindReadyTicket);

    // simulates the readypay button client the user changes the readyTicket text by pressing the enter key 
    elemReadyTicket.addEventListener("keypress", function (event) {
        if (event.which === 13) {   // Enter key pressed.
            event.preventDefault ? event.preventDefault() : (event.returnValue = false);    // Prevent re-submit
            elemReadyPay.click();
        }
    });

    // run `bindReadyPay` when users click readypay button 
    elemReadyPay.addEventListener("click", bindReadyPay);

    const displayConfirmation = (res) => {
        var notice = PNotify.success({
            title: `Thank you for placing your order! `,
            text: `Your order ID is ${res.order_id}. <br> The total amount of USD ${res.charged_total} will be charged to ${res.payment_method}.<br> Your order confimration will be sent to <b> ${res.email_address} </b>`,
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
    };
});
