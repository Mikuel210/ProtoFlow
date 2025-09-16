// #region Instance list

let instanceData = [];
let openInstanceID;

const closeButton = document.getElementById('close-button');
const confirmCloseButton = document.getElementById('confirm-close-button');
const instanceContainer = document.getElementById('instance-container');

const instanceUI = document.getElementById('instance-ui');
const instanceUIContainer = document.getElementById('instance-ui-container');
const instanceUITitle = document.getElementById('instance-ui-title');
const instanceUIName = document.getElementById('instance-ui-name');

bridge.onPongGetOpenInstances((event, instances) => {
    instanceContainer.innerHTML = '';
    instanceData = instances;

    instances.forEach(instance => {
        const instanceElement = document.createElement('li');
        instanceElement.className = 'nav-item w-100 d-block mb-1';

        const button = document.createElement('button');
        button.className = 'btn w-100 d-block text-start text-truncate';
        button.id = instance.InstanceID

        button.textContent = instance.Title;
        instanceElement.appendChild(button);
        instanceContainer.appendChild(instanceElement);

        button.addEventListener('click', () => {
            openInstanceID = instance.InstanceID;
            bridge.pingGetUIElements(instance.InstanceID);
            updateOpenInstanceOnList();
        });
    })

    updateOpenInstanceOnList()
    updateInstanceUI();
});

function updateOpenInstanceOnList() {
    children = Array.from(instanceContainer.childNodes).map(e => e.childNodes[0]);

    children.forEach((button) => {
        if (button.id == openInstanceID)
        {
            button.classList.remove("btn-secondary")
            button.classList.add("btn-light")
        } else {
            button.classList.remove("btn-light")
            button.classList.add("btn-secondary")
        }
    })
}

function updateInstanceUI()
{
    let instance = instanceData.find(e => e.InstanceID == openInstanceID);

    if (instance != undefined)
    {
        instanceUITitle.textContent = instance.Title;
        instanceUIName.textContent = instance.Name + ' — ' + instance.Description;
        instanceUI.classList.replace('d-none', 'd-block');

        if (instance.PluginType == 'System' || !instance.CanClientClose)
            closeButton.classList.add('d-none');
        else
            closeButton.classList.remove('d-none');
    } else {
        instanceUI.classList.replace('d-block', 'd-none');
    }
}

updateInstanceUI();

// #endregion

// #region Instance UI

const instanceAudios = document.getElementById('instance-audios')

bridge.onCreateAudio((event, audioID, url) => {
    let audio = document.createElement('audio')
    audio.id = audioID
    audio.src = url

    instanceAudios.appendChild(audio)
})

bridge.onPlayAudio((event, audioID) => {
    let audio = document.getElementById(audioID)
    audio.play()
})

bridge.onPauseAudio((event, audioID) => {
    let audio = document.getElementById(audioID)
    audio.pause()
})

bridge.onStopAudio((event, audioID) => {
    let audio = document.getElementById(audioID)
    audio.pause();
    audio.currentTime = 0;
})

bridge.onDestroyAudio((event, audioID) => {
    let audio = document.getElementById(audioID)
    instanceAudios.removeChild(audio)
})

bridge.onPongGetUIElements((event, elements) => {
    instanceUIContainer.innerHTML = '';

    elements.forEach(element => {
        let properties = JSON.parse(element.Properties);
        let elementName;
        let className = '';
        
        switch (element.Type) {
            case "Heading": elementName = 'h' + properties.Level; break;
            case "Paragraph": elementName = 'p'; break;
            case "HorizontalRule": elementName = 'hr'; break

            case "Button": 
                elementName = 'button'; 
                className = 'btn btn-primary mt-1 mb-1 d-block w-100'
                break;

            case "Input": 
                elementName = 'input';
                className = 'form-control mb-2'
                break;

            case "Checkbox":
                let div = document.createElement('div');
                let input = document.createElement('input');
                let label = document.createElement('label');

                div.classList = 'form-check';
                div.id = element.ElementID;

                input.classList = 'form-check-input';
                input.type = 'checkbox';
                input.id = 'input-' + element.ElementID;

                label.classList = 'form-check-label'
                label.setAttribute("for", input.id);

                instanceUIContainer.append(div);
                div.appendChild(input);
                div.appendChild(label);

                updateElementProperties(div, element.Type, properties)

                input.addEventListener('change', (event) => {
                    let state = event.currentTarget.checked
                    bridge.pingUIEvent(element.ElementID, 'StateChanged', { state: state });

                    if (state)
                        bridge.pingUIEvent(element.ElementID, 'Checked', { });
                    else
                        bridge.pingUIEvent(element.ElementID, 'Unchecked', { });
                })

                return;
        }
            
        let elementInstance = document.createElement(elementName);
        elementInstance.id = element.ElementID;
        elementInstance.className = className;

        instanceUIContainer.append(elementInstance);
        updateElementProperties(elementInstance, element.Type, properties)

        // Handle events
        switch (element.Type) {
            case "Button":
                elementInstance.addEventListener('click', () => {
                    bridge.pingUIEvent(element.ElementID, 'Click', {});
                });

                break;

            case "Input":
                elementInstance.addEventListener('input', () => {
                    bridge.pingUIEvent(element.ElementID, 'ValueChanged', { value: elementInstance.value });
                });

                break;
        }
    })

    updateInstanceUI();
})

bridge.onPongUpdateUIElement((event, elementID, propertyJson) => {
    let elementInstance = document.getElementById(elementID);
    let property = JSON.parse(propertyJson);

    let tagName = elementInstance.tagName.toLowerCase();
    let type;

    if (tagName.startsWith('h')) type = "Heading";
    if (tagName == 'p') type = "Paragraph";
    if (tagName == 'hr') type = "HorizontalRule";
    if (tagName == 'button') type = "Button";
    if (tagName == 'input') type = "Input";
    if (tagName == 'div') type = "Checkbox";

    updateElementProperties(elementInstance, type, property);
})

function updateElementProperties(elementInstance, type, properties)
{
    switch (type) {
        case "Heading":
        case "Paragraph":
        case "Button":
            if ("Text" in properties) elementInstance.textContent = properties.Text;
            break;

        case "Input":
            if ("Type" in properties) elementInstance.type = properties.Type.toLowerCase();
            if ("Text" in properties) elementInstance.value = properties.Text;
            if ("Placeholder" in properties) elementInstance.placeholder = properties.Placeholder;
            break;

        case "Checkbox":
            if ("Text" in properties) elementInstance.querySelector('label').innerHTML = properties.Text;

            if ("Checked" in properties) {
                if (properties["Checked"])
                    elementInstance.querySelector('input').setAttribute("checked", "");
                else
                    elementInstance.querySelector('input').removeAttribute("checked");
            }

            break;
    }
}

// #endregion

// #region Open dropdown

const openButton = document.getElementById('open-button');
const openMenu = document.getElementById('open-menu');

openButton.addEventListener('click', toggleOpenDropdown)

function toggleOpenDropdown() {
    openMenu.classList.toggle('d-none');
    openMenu.classList.toggle('d-block');

    if (openMenu.classList.contains('d-block'))
        bridge.pingGetOpenableProtocols();
}

bridge.onPongGetOpenableProtocols((event, protocols) => {
    openMenu.innerHTML = '';

    protocols.forEach(protocol => {
        let li = document.createElement('li');
        let button = document.createElement('button');

        button.textContent = protocol.Name;
        button.className = 'dropdown-item';
        
        li.appendChild(button);
        openMenu.appendChild(li);

        button.addEventListener('click', () => {
            bridge.pingOpenProtocol(protocol.TypeName)
            openMenu.classList.add('d-none');
            openMenu.classList.remove('d-block');
        })
    });
});

bridge.onPongOpenProtocol((event, instanceID) => {
    openInstanceID = instanceID;
    bridge.pingGetUIElements(instanceID);
    updateOpenInstanceOnList();
})

// #endregion

// #region Close instances

confirmCloseButton.addEventListener('click', () => {
    bridge.pingCloseProtocol(openInstanceID);
});

// #endregion