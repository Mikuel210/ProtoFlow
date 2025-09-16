# SERVER

- ShowNotification
  - Title: string
  - Body: string
- CreateAudio
  - AudioID: string
  - URL: string
- PlayAudio
  - AudioID: string
- PauseAudio
  - AudioID: string
- StopAudio
  - AudioID: string
- DestroyAudio
  - AudioID: string
- PongGetOpenInstances
  - Instances: array:
    - InstanceID: string
    - PluginType: string (System|Protocol)
    - Name: string
    - Description: string
    - Title: string
    - CanClientClose: bool
- PongGetUIElements
  - Elements: array:
    - ElementID: string
    - Type: string (Heading|Paragraph|Input|Button|HorizontalRule)
    - Properties: array
      - string: value
- PongUpdateUIElement
  - ElementID: string
  - Property: object<string, value> (JSON string)
- PongGetOpenableProtocols
  - Protocols: array:
    - TypeName: string
    - Name: string
- PongOpenProtocol
  - InstanceID: string

# CLIENT

- Connect
  - Platform: string (Windows)
- PingGetOpenInstances
- PingGetUIElements
  - InstanceID: string
- PingUIEvent
  - ElementID: string
  - EventName: string (Click|ValueChanged)
  - Arguments: object
    - value: string (ValueChanged)
- PingGetOpenableProtocols
- PingOpenProtocol
  - TypeName: string
- PingCloseProtocol
  - InstanceID: string

# UI ELEMENTS

- Heading
  - Text: string
  - Level: number (1-6)
- Paragraph
  - Text: string
- Input
  - Type: string (Text|Number)
  - Text: string
  - Placeholder: string
- Button
  - Text: string
- HorizontalRule
