const { contextBridge, ipcRenderer } = require('electron');

contextBridge.exposeInMainWorld('bridge', {
    openProtocol: () => ipcRenderer.invoke('OpenProtocol'),
    pingGetUIElements: (instanceId) => ipcRenderer.invoke('PingGetUIElements', instanceId),
    pingUIEvent: (elementID, eventName, args) => ipcRenderer.invoke('PingUIEvent', elementID, eventName, args),
    pingGetOpenableProtocols: () => ipcRenderer.invoke('PingGetOpenableProtocols'),
    pingOpenProtocol: (typeName) => ipcRenderer.invoke('PingOpenProtocol', typeName),
    pingCloseProtocol: (instanceID) => ipcRenderer.invoke('PingCloseProtocol', instanceID),
    onCreateAudio: (callback) => ipcRenderer.on('CreateAudio', callback),
    onPlayAudio: (callback) => ipcRenderer.on('PlayAudio', callback),
    onPauseAudio: (callback) => ipcRenderer.on('PauseAudio', callback),
    onStopAudio: (callback) => ipcRenderer.on('StopAudio', callback),
    onDestroyAudio: (callback) => ipcRenderer.on('DestroyAudio', callback),
    onPongGetOpenInstances: (callback) => ipcRenderer.on('PongGetOpenInstances', callback),
    onPongGetUIElements: (callback) => ipcRenderer.on('PongGetUIElements', callback),
    onPongUpdateUIElement: (callback) => ipcRenderer.on('PongUpdateUIElement', callback),
    onPongGetOpenableProtocols: (callback) => ipcRenderer.on('PongGetOpenableProtocols', callback),
    onPongOpenProtocol: (callback) => ipcRenderer.on('PongOpenProtocol', callback)
});