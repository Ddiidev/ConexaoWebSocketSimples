var socket = new WebSocket("ws://localhost:30505");

(async function main() {
  socket.onopen = function (e) {
    addToList("[open] Connection established");
    socket.send(" conected ");
  };

  socket.onmessage = function (event) {
    addToList(event.data);
  };

  socket.onclose = function (event) {
    if (event.wasClean) {
      addToList(`[close] Connection closed cleanly, code=${event.code} reason=${event.reason}`);
    } else {
      addToList('[close] Connection died');
    }
  };

  socket.onerror = function (error) {
    // console.log(`[error]`);
  };

})();

function sendMsg() {
  const msgSocket = document.getElementById("msgSocket").value;

  socket.send(msgSocket);
}

function addToList(data) {
  const msgs = document.getElementById("msgs");

  msgs.innerHTML += `<li> ${data} </li>`
}