﻿// -----------------
// Data
// -----------------

// selector for readypay container
const divReadyPay = ".art-ready-pay";

// selector for readypay button
const btnReadyPay = "button";

// selector for readyticket textbox
const txtReadyTicket = "input[data-readyticket]";

// destination url to post the ready ticket
const destUrl = "Plugins/ReadySignOn.ReadyPay/ReadyPay/ReadyRequestPosted";

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
    const ordertotal = div.dataset.ordertotal;

    // `bindReadyTicket` consumes an event: `e`;
    // it sets the `readyticket` attribute of `e.target`
    const bindReadyTicket = (e) => {
        const elem = e.target;
        const isValid = elem.value.length !== 0;
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
            ProductId: productid,
            OrderTotal: ordertotal,
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

        // make the request; process the response: `res` if it succeeds;
        // otherwise, logs errors to the console.
        fetch(destUrl, options)
            .then(res => {
                if (res.ok) {
                    return res.json();
                } else {
                    return Promise.reject(res.statusText);
                }
            })
            .then(res => console.log(res))
            .catch(err => console.log(`Error when making request. ${err}`));
    }

    // disable ReadyPay button on load
    elemReadyPay.disabled = true;

    // set `data-readyticket` attribute each time users change the ready ticket number 
    elemReadyTicket.addEventListener("input", bindReadyTicket);

    // run `bindReadyPay` when users click readypay button 
    elemReadyPay.addEventListener("click", bindReadyPay);

});


