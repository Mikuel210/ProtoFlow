const { app, BrowserWindow, Notification, ipcMain } = require('electron/main')
const WebSocket = require('ws')
const path = require('node:path')
const { send } = require('node:process')

// #region WINDOW

let window;

const createWindow = () => {
  window = new BrowserWindow({
    width: 1000,
    height: 563,
    minWidth: 800,
    minHeight: 450,
    webPreferences: {
      preload: path.join(__dirname, "preload.js")
	  }
  })

  // window.setMenu(null)
  window.loadFile('public/index.html')
}

// #endregion

// #region COMMUNICATION

let ws;

function connect() {
  ws = new WebSocket('http://127.0.0.1:9006');

  ws.on('error', console.error);
  ws.on('open', () => sendCommand("Connect", { Platform: "Windows" }));
  ws.on('message', handleMessage);
}

function sendCommand(command, args) {
    data = JSON.stringify({
        command: command,
        arguments: args
    });

    ws.send(data);
}

function parseMessage(message) {
    return JSON.parse(message);
}

function handleMessage(message) {
    const parsedMessage = parseMessage(message);

    try {
      switch (parsedMessage.command) {
          case "ShowNotification": showNotification(parsedMessage.arguments.Title, parsedMessage.arguments.Body); break;
          case "CreateAudio": window.webContents.send('CreateAudio', parsedMessage.arguments.AudioID, parsedMessage.arguments.URL); break;
          case "PlayAudio": window.webContents.send('PlayAudio', parsedMessage.arguments.AudioID); break;
          case "PauseAudio": window.webContents.send('PauseAudio', parsedMessage.arguments.AudioID); break;
          case "StopAudio": window.webContents.send('StopAudio', parsedMessage.arguments.AudioID); break;
          case "DestroyAudio": window.webContents.send('DestroyAudio', parsedMessage.arguments.AudioID); break;
          case "PongGetOpenInstances": window.webContents.send('PongGetOpenInstances', parsedMessage.arguments.Instances); break;
          case "PongGetUIElements": window.webContents.send('PongGetUIElements', parsedMessage.arguments.Elements); break;
          case "PongUpdateUIElement": window.webContents.send('PongUpdateUIElement', parsedMessage.arguments.ElementID, parsedMessage.arguments.Property); break;
          case "PongGetOpenableProtocols": window.webContents.send('PongGetOpenableProtocols', parsedMessage.arguments.Protocols); break;
          case "PongOpenProtocol": window.webContents.send('PongOpenProtocol', parsedMessage.arguments.InstanceID); break;

          default: console.error("Unknown command: " + parsedMessage.command); break;
      }
    } catch {}
}

// #endregion

// #region CLIENT

function showNotification (title, body) {
  if (process.platform === 'win32')
    app.setAppUserModelId("ProtoFlow");

	new Notification({ title: title, body: body }).show()
}

// #endregion

// #region HANDLE COMMANDS

function handleCommands() {
  ipcMain.handle('OpenProtocol', () => {
		sendCommand("OpenInstance", {
			Name: "Focus Protocol"
		});	
	});

  ipcMain.handle('PingGetUIElements', (event, instanceID) => {
		sendCommand("PingGetUIElements", {
			InstanceID: instanceID
		});	
	});

  ipcMain.handle('PingUIEvent', (event, elementID, eventName, args) => {
    sendCommand("PingUIEvent", {
			ElementID: elementID,
      EventName: eventName,
      Arguments: args
		});	
  });

  ipcMain.handle('PingGetOpenableProtocols', () => {
    sendCommand("PingGetOpenableProtocols", {});	
  });

  ipcMain.handle('PingOpenProtocol', (event, typeName) => {
    sendCommand("PingOpenProtocol", { 
      TypeName: typeName 
    });	
  });

  ipcMain.handle('PingCloseProtocol', (event, instanceID) => {
    sendCommand("PingCloseProtocol", { 
      InstanceID: instanceID 
    });	
  });
}

// #endregion

// #region ENTRY POINT

app.whenReady().then(() => {
  handleCommands();
	createWindow();

  window.webContents.on('did-finish-load', connect)
})

// #endregion