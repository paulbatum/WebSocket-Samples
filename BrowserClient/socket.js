var connection;

function connect(url) {
    connection = new WebSocket(url);

    connection.onopen = function () {
        console.log("Connection Opened");
    }
    connection.onclose = function () {
        console.log("Connection Closed");
    }
    connection.onerror = function (evt) {
        console.log("The following error occurred: " + evt.data);
    }
    connection.onmessage = function (evt) {
        console.log("The following data was received:" + evt.data);
        appendMessage(evt.data);
    }
}

function appendMessage(msg) {
    var received = document.getElementById('messagesReceived');
    var msgp = document.createElement('p');
    msgp.innerHTML = msg;
    received.appendChild(msgp);
}

function send() {
    // Get text from text box
    var text = document.getElementsByName('messageBox')[0].value;

    connection.send(text);
    console.log("Sent: " + text);
}
